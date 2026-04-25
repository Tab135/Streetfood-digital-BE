using BO.DTO.AI;
using BO.DTO.Branch;
using BO.DTO.Dietary;
using BO.Exceptions;
using Microsoft.Extensions.Configuration;
using Service.Interfaces;
using Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service
{
    public class AiAssistantService : IAiAssistantService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IUserDietaryPreferenceService _userDietaryPreferenceService;
        private readonly IBranchService _branchService;
        private readonly IAiConversationMemoryService _conversationMemoryService;

        public AiAssistantService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IUserDietaryPreferenceService userDietaryPreferenceService,
            IBranchService branchService,
            IAiConversationMemoryService conversationMemoryService)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userDietaryPreferenceService = userDietaryPreferenceService ?? throw new ArgumentNullException(nameof(userDietaryPreferenceService));
            _branchService = branchService ?? throw new ArgumentNullException(nameof(branchService));
            _conversationMemoryService = conversationMemoryService ?? throw new ArgumentNullException(nameof(conversationMemoryService));
        }

        public async Task<AiChatResponseDto> ChatAsync(int userId, AiChatRequestDto request)
        {
            if (request == null)
            {
                throw new DomainExceptions("Yêu cầu không được để trống");
            }

            var memoryHistory = _conversationMemoryService.GetHistory(userId);

            // Backward-compatible: if client sends full history, seed memory once.
            if (memoryHistory.Count == 0 && request.History != null && request.History.Count > 0)
            {
                foreach (var message in request.History
                    .Where(h => !string.IsNullOrWhiteSpace(h.Content))
                    .TakeLast(6))
                {
                    _conversationMemoryService.AddMessage(userId, message.Role, message.Content);
                }

                memoryHistory = _conversationMemoryService.GetHistory(userId);
            }

            var requestWithHistory = new AiChatRequestDto
            {
                Message = request.Message,
                Lat = request.Lat,
                Long = request.Long,
                DistanceKm = request.DistanceKm,
                History = memoryHistory
            };

            var userDietaryPreferences = await _userDietaryPreferenceService.GetPreferencesByUserId(userId);
            var aiDecision = await GetGroqDecisionAsync(requestWithHistory, userDietaryPreferences);
            var query = NormalizeQuery(aiDecision.SearchQuery, requestWithHistory, userDietaryPreferences);

            var isRecommendation = string.Equals(aiDecision.Intent, "recommend_food", StringComparison.OrdinalIgnoreCase);
            var response = new AiChatResponseDto
            {
                Intent = isRecommendation ? "recommend_food" : "chat",
                Reply = string.IsNullOrWhiteSpace(aiDecision.Reply)
                    ? (isRecommendation
                        ? "Dưới đây là một số gợi ý quán gần bạn dựa trên sở thích của bạn."
                        : "Tôi có thể giúp bạn gợi ý món ăn và tìm kiếm các quán ăn.")
                    : aiDecision.Reply.Trim(),
                Query = query,
                MatchedBranchCount = 0,
                RecommendedBranches = new List<AiRecommendedBranchDto>()
            };

            if (isRecommendation)
            {
                var recommendationResult = await FindRecommendedBranchesAsync(query);
                response.Query.DistanceKm = recommendationResult.AppliedDistanceKm;
                var keywordMatchedBranches = FilterBranchesByKeyword(recommendationResult.Result.Items, query.Keyword);
                response.MatchedBranchCount = keywordMatchedBranches.Count;
                response.RecommendedBranches = keywordMatchedBranches
                    .Select(branch => MapToRecommendedBranchDto(branch, query.Keyword))
                    .ToList();
            }

            // Save current turn in memory so frontend only needs to send the latest message.
            _conversationMemoryService.AddMessage(userId, "user", request.Message);
            _conversationMemoryService.AddMessage(userId, "assistant", response.Reply);

            return response;
        }

        private async Task<(ActiveBranchListResponseDto Result, double? AppliedDistanceKm)> FindRecommendedBranchesAsync(AiRecommendationQueryDto query)
        {
            var filter = BuildBranchFilter(query);
            var result = await _branchService.GetActiveBranchesFilteredAsync(filter);
            return (result, filter.Distance);
        }

        private static ActiveBranchFilterDto BuildBranchFilter(AiRecommendationQueryDto query)
        {
            return new ActiveBranchFilterDto
            {
                Lat = query.Lat,
                Long = query.Long,
                Distance = query.DistanceKm,
                DietaryIds = query.DietaryIds.Count > 0 ? query.DietaryIds : null,
                TasteIds = query.TasteIds.Count > 0 ? query.TasteIds : null,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice,
                CategoryIds = query.CategoryIds.Count > 0 ? query.CategoryIds : null
            };
        }

        private static List<ActiveBranchResponseDto> FilterBranchesByKeyword(List<ActiveBranchResponseDto> branches, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return branches;
            }

            return branches
                .Where(branch => branch.Dishes.Any(dish => IsDishMatchingKeyword(dish.Name, keyword)))
                .ToList();
        }

        private static AiRecommendedBranchDto MapToRecommendedBranchDto(ActiveBranchResponseDto branch, string? keyword)
        {
            var keywordMatchedDishes = string.IsNullOrWhiteSpace(keyword)
                ? branch.Dishes
                : branch.Dishes.Where(d => IsDishMatchingKeyword(d.Name, keyword));

            var recommendedDishes = keywordMatchedDishes
                .Where(d => !d.IsSoldOut)
                .Select(d => d.Name)
                .Take(3)
                .ToList();

            if (recommendedDishes.Count == 0)
            {
                recommendedDishes = keywordMatchedDishes
                    .Select(d => d.Name)
                    .Take(3)
                    .ToList();
            }

            if (recommendedDishes.Count == 0)
            {
                recommendedDishes = branch.Dishes
                    .Select(d => d.Name)
                    .Take(3)
                    .ToList();
            }

            return new AiRecommendedBranchDto
            {
                BranchId = branch.BranchId,
                VendorId = branch.VendorId,
                VendorName = branch.VendorName,
                Name = branch.Name,
                AddressDetail = branch.AddressDetail,
                City = branch.City,
                Ward = branch.Ward,
                AvgRating = branch.AvgRating,
                DistanceKm = branch.DistanceKm,
                FinalScore = branch.FinalScore,
                DietaryPreferenceNames = branch.DietaryPreferenceNames,
                RecommendedDishes = recommendedDishes
            };
        }

        private static bool IsDishMatchingKeyword(string? dishName, string? keyword)
        {
            if (string.IsNullOrWhiteSpace(dishName) || string.IsNullOrWhiteSpace(keyword))
            {
                return false;
            }

            var normalizedDishName = TextNormalizer.NormalizeForSearch(dishName);
            var normalizedKeyword = TextNormalizer.NormalizeForSearch(keyword);

            if (string.IsNullOrWhiteSpace(normalizedDishName) || string.IsNullOrWhiteSpace(normalizedKeyword))
            {
                return false;
            }

            return normalizedDishName.Contains(normalizedKeyword);
        }

        private async Task<GroqDecisionPayload> GetGroqDecisionAsync(AiChatRequestDto request, List<DietaryPreferenceDto> userDietaryPreferences)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new DomainExceptions("Groq API key chưa được cấu hình. Vui lòng thiết lập Groq:ApiKey hoặc GROQ_API_KEY.");
            }

            var endpoint = _configuration["Groq:Endpoint"] ?? "https://api.groq.com/openai/v1/chat/completions";
            var model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

            var requestPayload = new
            {
                model,
                temperature = 0.2,
                response_format = new { type = "json_object" },
                messages = BuildGroqMessages(request, userDietaryPreferences)
            };

            var client = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var response = await client.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new DomainExceptions($"Yêu cầu Groq thất bại với trạng thái {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            var content = ExtractAssistantContent(responseBody);
            var json = ExtractJsonPayload(content);

            try
            {
                var decision = JsonSerializer.Deserialize<GroqDecisionPayload>(json, JsonOptions);
                if (decision == null)
                {
                    throw new JsonException("AI output is null");
                }

                return decision;
            }
            catch
            {
                return new GroqDecisionPayload
                {
                    Intent = DetectRecommendationIntent(request.Message) ? "recommend_food" : "chat",
                    Reply = content,
                    SearchQuery = new GroqSearchQueryPayload()
                };
            }
        }

        private static List<object> BuildGroqMessages(AiChatRequestDto request, List<DietaryPreferenceDto> userDietaryPreferences)
        {
            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = "Bạn là một trợ lý ẩm thực. Luôn trả về JSON hợp lệ với cấu trúc sau: " +
                              "{\"intent\":\"chat|recommend_food\",\"reply\":\"string\",\"searchQuery\":{\"keyword\":\"string|null\",\"lat\":number|null,\"long\":number|null,\"distanceKm\":number|null,\"dietaryIds\":number[],\"tasteIds\":number[],\"minPrice\":number|null,\"maxPrice\":number|null,\"categoryIds\":number[]}}. " +
                              "Chọn intent=recommend_food khi người dùng yêu cầu gợi ý ăn uống, nên ăn gì, hoặc gợi ý quán gần đó. " +
                              "Nếu không rõ, giữ các trường số là null và các mảng là rỗng. Không thêm markdown."
                }
            };

            if (request.History != null && request.History.Count > 0)
            {
                foreach (var historyMessage in request.History
                    .Where(h => !string.IsNullOrWhiteSpace(h.Content))
                    .TakeLast(6))
                {
                    var role = NormalizeHistoryRole(historyMessage.Role);
                    messages.Add(new
                    {
                        role,
                        content = historyMessage.Content.Trim()
                    });
                }
            }

            var userContext = new
            {
                message = request.Message,
                gps = new { lat = request.Lat, @long = request.Long, distanceKm = request.DistanceKm },
                dietaryPreferences = userDietaryPreferences.Select(x => new
                {
                    id = x.DietaryPreferenceId,
                    name = x.Name
                })
            };

            messages.Add(new
            {
                role = "user",
                content = JsonSerializer.Serialize(userContext)
            });

            return messages;
        }

        private static string NormalizeHistoryRole(string role)
        {
            if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                return "assistant";
            }

            if (string.Equals(role, "system", StringComparison.OrdinalIgnoreCase))
            {
                return "system";
            }

            return "user";
        }

        private static string ExtractAssistantContent(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                throw new DomainExceptions("Phản hồi từ Groq không chứa các lựa chọn");
            }

            var content = choices[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new DomainExceptions("Groq trả về nội dung trống");
            }

            return content;
        }

        private static string ExtractJsonPayload(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return content[start..(end + 1)];
            }

            return "{}";
        }

        private static bool DetectRecommendationIntent(string message)
        {
            var normalized = TextNormalizer.NormalizeForSearch(message).ToLowerInvariant();
            var recommendationKeywords = new[]
            {
                "recommend",
                "suggest",
                "what should i eat",
                "nearby food",
                "near me",
                "an gi",
                "goi y",
                "de xuat",
                "quan gan day"
            };

            return recommendationKeywords.Any(normalized.Contains);
        }

        private static AiRecommendationQueryDto NormalizeQuery(
            GroqSearchQueryPayload? rawQuery,
            AiChatRequestDto request,
            List<DietaryPreferenceDto> userDietaryPreferences)
        {
            var lat = request.Lat ?? rawQuery?.Lat;
            var lng = request.Long ?? rawQuery?.Long;

            if (!lat.HasValue || !lng.HasValue)
            {
                lat = null;
                lng = null;
            }

            var distanceKm = request.DistanceKm ?? rawQuery?.DistanceKm;
            if (!lat.HasValue || !lng.HasValue)
            {
                distanceKm = null;
            }
            else if (distanceKm.HasValue)
            {
                distanceKm = Math.Clamp(distanceKm.Value, 0.1, 500);
            }

            var dietaryIds = new HashSet<int>(
                userDietaryPreferences
                    .Select(x => x.DietaryPreferenceId)
                    .Where(x => x > 0));

            if (rawQuery?.DietaryIds != null)
            {
                foreach (var dietaryId in rawQuery.DietaryIds.Where(x => x > 0))
                {
                    dietaryIds.Add(dietaryId);
                }
            }

            var minPrice = rawQuery?.MinPrice;
            var maxPrice = rawQuery?.MaxPrice;

            if (minPrice.HasValue && minPrice.Value < 0)
            {
                minPrice = 0;
            }

            if (maxPrice.HasValue && maxPrice.Value < 0)
            {
                maxPrice = 0;
            }

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
            {
                (minPrice, maxPrice) = (maxPrice, minPrice);
            }

            return new AiRecommendationQueryDto
            {
                Keyword = string.IsNullOrWhiteSpace(rawQuery?.Keyword) ? null : rawQuery!.Keyword!.Trim(),
                Lat = lat,
                Long = lng,
                DistanceKm = distanceKm,
                DietaryIds = dietaryIds.OrderBy(x => x).ToList(),
                TasteIds = rawQuery?.TasteIds?.Where(x => x > 0).Distinct().OrderBy(x => x).ToList() ?? new List<int>(),
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                CategoryIds = rawQuery?.CategoryIds?.Where(x => x > 0).Distinct().OrderBy(x => x).ToList() ?? new List<int>()
            };
        }

        private sealed class GroqDecisionPayload
        {
            public string? Intent { get; set; }
            public string? Reply { get; set; }
            public GroqSearchQueryPayload? SearchQuery { get; set; }
        }

        private sealed class GroqSearchQueryPayload
        {
            public string? Keyword { get; set; }
            public double? Lat { get; set; }
            public double? Long { get; set; }
            public double? DistanceKm { get; set; }
            public List<int>? DietaryIds { get; set; }
            public List<int>? TasteIds { get; set; }
            public decimal? MinPrice { get; set; }
            public decimal? MaxPrice { get; set; }
            public List<int>? CategoryIds { get; set; }
        }
    }
}
