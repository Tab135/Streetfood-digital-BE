using DAL;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Repository;
using Repository.Interfaces;
using Scalar.AspNetCore;
using Service;
using Service.Interfaces;
using Service.JWT;
using Service.PaymentsService;
using StreetFood.Hubs;
using StreetFood.Services;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            builder.Services.AddSignalR();

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
            // DAL
            builder.Services.AddScoped<PaymentDAO>();

            // Repository
            builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

            // Service
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddHostedService<SubscriptionExpiryService>();
            builder.Services.AddHostedService<TierResetService>();
            // Register DAL
            builder.Services.AddScoped<UserDAO>();
            builder.Services.AddScoped<OtpVerifyDAO>();
            builder.Services.AddScoped<BadgeDAO>();
            builder.Services.AddScoped<UserBadgeDAO>();
            // Dietary DAOs
            builder.Services.AddScoped<DietaryPreferenceDAO>();
            builder.Services.AddScoped<UserDietaryPreferenceDAO>();
            builder.Services.AddScoped<VendorDietaryPreferenceDAO>();
            builder.Services.AddScoped<VendorDAO>();
            builder.Services.AddScoped<BranchDAO>();
            builder.Services.AddScoped<FeedbackTagDAO>();
            builder.Services.AddScoped<FeedbackDAO>();
            // Flow 2: Review & Rating DAOs
            builder.Services.AddScoped<OrderDAO>();
            builder.Services.AddScoped<CartDAO>();
            builder.Services.AddScoped<FeedbackVoteDAO>();
            builder.Services.AddScoped<VendorReplyDAO>();
            builder.Services.AddScoped<NotificationDAO>();
            // Menu Management DAOs
            builder.Services.AddScoped<CategoryDAO>();
            builder.Services.AddScoped<TasteDAO>();
            builder.Services.AddScoped<DishDAO>();
            builder.Services.AddScoped<TierDAO>();

            // Register Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITierRepository, TierRepository>();
            builder.Services.AddScoped<IOtpVerifyRepository, OtpVerifyRepository>();
            builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
            builder.Services.AddScoped<IUserBadgeRepository, UserBadgeRepository>();
            builder.Services.AddScoped<IDietaryPreferenceRepository, DietaryPreferenceRepository>();
            builder.Services.AddScoped<IUserDietaryPreferenceRepository, UserDietaryPreferenceRepository>();
            builder.Services.AddScoped<IVendorDietaryPreferenceRepository, VendorDietaryPreferenceRepository>();
            builder.Services.AddScoped<IVendorRepository, VendorRepository>();
            builder.Services.AddScoped<IBranchRepository, BranchRepository>();
            

            builder.Services.AddScoped<IFeedbackTagRepository, FeedbackTagRepository>();
            builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            // Flow 2: Review & Rating Repositories
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ICartRepository, CartRepository>();
            builder.Services.AddScoped<IFeedbackVoteRepository, FeedbackVoteRepository>();
            builder.Services.AddScoped<IVendorReplyRepository, VendorReplyRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            // Menu Management Repositories
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<ITasteRepository, TasteRepository>();
            builder.Services.AddScoped<IDishRepository, DishRepository>();

            // Register Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IJwtService, JWTService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IFacebookService, FacebookService>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IDietaryPreferenceService, DietaryPreferenceService>();
            builder.Services.AddScoped<IUserDietaryPreferenceService, UserDietaryPreferenceService>();
            builder.Services.AddScoped<IVendorDietaryPreferenceService, VendorDietaryPreferenceService>();
            builder.Services.AddScoped<IVendorService, VendorService>();
            builder.Services.AddScoped<ITierService, TierService>();
            builder.Services.AddScoped<IBranchService, BranchService>();
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<IFeedbackTagService, FeedbackTagService>();
            // Flow 2: Review & Rating Services
            builder.Services.AddScoped<IBranchMetricsService, BranchMetricsService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();
            builder.Services.AddScoped<IFeedbackVoteService, FeedbackVoteService>();
            builder.Services.AddScoped<IVendorReplyService, VendorReplyService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<ICartService, CartService>();
            // Menu Management Services
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<ITasteService, TasteService>();
            builder.Services.AddScoped<IDishService, DishService>();
            // Flow 4: Campaigns
        builder.Services.AddScoped<DAL.CampaignDAO>();
        builder.Services.AddScoped<DAL.BranchCampaignDAO>();
        builder.Services.AddScoped<Repository.Interfaces.ICampaignRepository, Repository.CampaignRepository>();
        builder.Services.AddScoped<Repository.Interfaces.IBranchCampaignRepository, Repository.BranchCampaignRepository>();
        builder.Services.AddScoped<Service.Interfaces.ICampaignService, Service.CampaignService>();

        // Search Service
            builder.Services.AddScoped<ISearchService, SearchService>();
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
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
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
            app.MapHub<NotificationHub>("/hubs/notifications");

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

            // Fallback response schema for actions returning anonymous IActionResult payloads.
            // Without this, Scalar can display "No body" because OpenAPI cannot infer schemas.
            foreach (var path in document.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    operation.Value.Responses ??= new OpenApiResponses();

                    if (!operation.Value.Responses.ContainsKey("200"))
                    {
                        operation.Value.Responses["200"] = new OpenApiResponse { Description = "Success" };
                    }

                    foreach (var response in operation.Value.Responses.Values)
                    {
                        response.Content ??= new Dictionary<string, OpenApiMediaType>();

                        if (!response.Content.ContainsKey("application/json"))
                        {
                            response.Content["application/json"] = new OpenApiMediaType
                            {
                                Schema = CreateApiResponseEnvelopeSchema()
                            };
                            continue;
                        }

                        var jsonContent = response.Content["application/json"];
                        var existingSchema = jsonContent.Schema;

                        if (existingSchema == null)
                        {
                            jsonContent.Schema = CreateApiResponseEnvelopeSchema();
                            jsonContent.Example = CreateEnvelopeExample(jsonContent.Schema, document);
                            continue;
                        }

                        if (IsApiResponseEnvelopeSchema(existingSchema))
                        {
                            jsonContent.Example = CreateEnvelopeExample(existingSchema, document);
                            continue;
                        }

                        jsonContent.Schema = CreateApiResponseEnvelopeSchema(existingSchema);
                        jsonContent.Example = CreateEnvelopeExample(jsonContent.Schema, document);
                    }
                }
            }
        }

        private static OpenApiSchema CreateApiResponseEnvelopeSchema(OpenApiSchema? dataSchema = null)
        {
            return new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["status"] = new OpenApiSchema { Type = "integer", Format = "int32" },
                    ["message"] = new OpenApiSchema { Type = "string" },
                    ["data"] = dataSchema ?? new OpenApiSchema { Type = "object", Nullable = true, AdditionalPropertiesAllowed = true },
                    ["errorCode"] = new OpenApiSchema { Type = "string", Nullable = true }
                },
                Required = new HashSet<string> { "status", "message" }
            };
        }

        private static bool IsApiResponseEnvelopeSchema(OpenApiSchema schema)
        {
            return schema.Properties != null
                && schema.Properties.ContainsKey("status")
                && schema.Properties.ContainsKey("message")
                && schema.Properties.ContainsKey("data")
                && schema.Properties.ContainsKey("errorCode");
        }

        private static IOpenApiAny CreateEnvelopeExample(OpenApiSchema envelopeSchema, OpenApiDocument document)
        {
            var dataSchema = ResolveSchema(envelopeSchema.Properties != null && envelopeSchema.Properties.TryGetValue("data", out var ds)
                ? ds
                : new OpenApiSchema { Type = "object" }, document);

            return new OpenApiObject
            {
                ["status"] = new OpenApiInteger(200),
                ["message"] = new OpenApiString("Success"),
                ["data"] = CreateExampleFromSchema(dataSchema, document, 0, new HashSet<string>()),
                ["errorCode"] = new OpenApiNull()
            };
        }

        private static IOpenApiAny CreateExampleFromSchema(OpenApiSchema schema, OpenApiDocument document, int depth, HashSet<string> visitedRefs)
        {
            if (depth > 4)
            {
                return new OpenApiObject();
            }

            schema = ResolveSchema(schema, document, visitedRefs);

            if (schema.Enum != null && schema.Enum.Count > 0)
            {
                return schema.Enum[0];
            }

            if (schema.OneOf != null && schema.OneOf.Count > 0)
            {
                return CreateExampleFromSchema(schema.OneOf[0], document, depth + 1, visitedRefs);
            }

            if (schema.AnyOf != null && schema.AnyOf.Count > 0)
            {
                return CreateExampleFromSchema(schema.AnyOf[0], document, depth + 1, visitedRefs);
            }

            if (schema.AllOf != null && schema.AllOf.Count > 0)
            {
                var obj = new OpenApiObject();
                foreach (var part in schema.AllOf)
                {
                    var partExample = CreateExampleFromSchema(part, document, depth + 1, visitedRefs);
                    if (partExample is OpenApiObject partObj)
                    {
                        foreach (var kvp in partObj)
                        {
                            obj[kvp.Key] = kvp.Value;
                        }
                    }
                }
                return obj;
            }

            return schema.Type switch
            {
                "string" => new OpenApiString("string"),
                "integer" => new OpenApiInteger(1),
                "number" => new OpenApiDouble(1),
                "boolean" => new OpenApiBoolean(true),
                "array" => new OpenApiArray
                {
                    CreateExampleFromSchema(schema.Items ?? new OpenApiSchema { Type = "object" }, document, depth + 1, visitedRefs)
                },
                _ => CreateObjectExample(schema, document, depth, visitedRefs)
            };
        }

        private static OpenApiObject CreateObjectExample(OpenApiSchema schema, OpenApiDocument document, int depth, HashSet<string> visitedRefs)
        {
            var obj = new OpenApiObject();
            if (schema.Properties == null)
            {
                return obj;
            }

            foreach (var prop in schema.Properties)
            {
                var propSchema = ResolveSchema(prop.Value, document, visitedRefs);
                obj[prop.Key] = CreateExampleFromSchema(propSchema, document, depth + 1, visitedRefs);
            }

            return obj;
        }

        private static OpenApiSchema ResolveSchema(OpenApiSchema schema, OpenApiDocument document, HashSet<string>? visitedRefs = null)
        {
            if (schema.Reference?.Id == null)
            {
                return schema;
            }

            visitedRefs ??= new HashSet<string>();
            if (visitedRefs.Contains(schema.Reference.Id))
            {
                return new OpenApiSchema { Type = "object" };
            }

            if (document.Components?.Schemas == null || !document.Components.Schemas.TryGetValue(schema.Reference.Id, out var resolved))
            {
                return schema;
            }

            visitedRefs.Add(schema.Reference.Id);
            return resolved;
        }
    }
}
