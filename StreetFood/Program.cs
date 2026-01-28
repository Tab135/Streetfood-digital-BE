
using DAL;
using Repository;
using Service;
using Service.JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Repository.Interfaces;
using Service.Interfaces;

namespace StreetFood
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
                // options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddDbContext<StreetFoodDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register DAL
            builder.Services.AddScoped<UserDAO>();
            builder.Services.AddScoped<OtpVerifyDAO>();
            builder.Services.AddScoped<BadgeDAO>();
            builder.Services.AddScoped<UserBadgeDAO>();
            // Dietary DAOs
            builder.Services.AddScoped<DietaryPreferenceDAO>();
            builder.Services.AddScoped<UserDietaryPreferenceDAO>();

            // Register Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IOtpVerifyRepository, OtpVerifyRepository>();
            builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
            builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
            builder.Services.AddScoped<IDietaryPreferenceRepository, DietaryPreferenceRepository>();
            builder.Services.AddScoped<IUserDietaryPreferenceRepository, UserDietaryPreferenceRepository>();

            // Register Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJwtService, JWTService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IDietaryPreferenceService, DietaryPreferenceService>();
            builder.Services.AddScoped<IUserDietaryPreferenceService, UserDietaryPreferenceService>();
            builder.Services.AddHostedService<OtpCleanupService>();

            // JWT Authentication Configuration
            var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourDefaultJwtKeyHere"; // Add to appsettings
            var key = Encoding.ASCII.GetBytes(jwtKey);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "StreetFood",
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"] ?? "StreetFoodUsers",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            // Authorization
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("User", policy =>
                    policy.RequireRole("User", "Admin"));
            });

            // Add OpenAPI (for Scalar)
            builder.Services.AddOpenApi("v1", options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "StreetFood API";
                    options.Theme = ScalarTheme.Moon;
                    options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
                });
            }

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(uploadDir),
                RequestPath = "/uploads"
            });

            app.UseHttpsRedirection();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/Auth/google-login"))
                {
                    var originalCoop = context.Response.Headers.ContainsKey("Cross-Origin-Opener-Policy");
                    var originalCoep = context.Response.Headers.ContainsKey("Cross-Origin-Embedder-Policy");

                    context.Response.Headers.Remove("Cross-Origin-Opener-Policy");
                    context.Response.Headers.Remove("Cross-Origin-Embedder-Policy");

                    var removedCoop = !context.Response.Headers.ContainsKey("Cross-Origin-Opener-Policy");
                    var removedCoep = !context.Response.Headers.ContainsKey("Cross-Origin-Embedder-Policy");

                    Console.WriteLine($"Google login endpoint - COOP: {originalCoop}->{removedCoop}, COEP: {originalCoep}->{removedCoep}");
                }
                await next();
            });

            app.UseCors("AllowFrontend");
            app.UseMiddleware<StreetFood.Middleware.ResponseMiddleware>();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseStaticFiles();
            app.MapControllers();

            app.Run();
        }
    }

    internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        private readonly Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider _authenticationSchemeProvider;

        public BearerSecuritySchemeTransformer(Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider)
        {
            _authenticationSchemeProvider = authenticationSchemeProvider;
        }

        public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var authenticationSchemes = await _authenticationSchemeProvider.GetAllSchemesAsync();
            if (authenticationSchemes.Any(authScheme => authScheme.Name == JwtBearerDefaults.AuthenticationScheme))
            {
                var securityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = securityScheme;

                // Apply security globally to all operations
                foreach (var path in document.Paths)
                {
                    foreach (var operation in path.Value.Operations)
                    {
                        operation.Value.Security ??= new List<OpenApiSecurityRequirement>();
                        operation.Value.Security.Add(new OpenApiSecurityRequirement
                        {
                            [securityScheme] = Array.Empty<string>()
                        });
                    }
                }
            }
        }
    }
}
