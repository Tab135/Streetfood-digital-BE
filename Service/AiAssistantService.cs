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
            var aiDecision = await GetGeminiDecisionAsync(requestWithHistory, userDietaryPreferences);
            var query = AiAssistantSupport.NormalizeQuery(aiDecision.SearchQuery, requestWithHistory, userDietaryPreferences);

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
                var keywordMatchedBranches = AiAssistantSupport.FilterBranchesBySearchTerms(recommendationResult.Result.Items, query.SearchTerms, requestWithHistory.Message);
                response.MatchedBranchCount = keywordMatchedBranches.Count;
                response.RecommendedBranches = keywordMatchedBranches
                    .Select(branch => AiAssistantSupport.MapToRecommendedBranchDto(branch, query.SearchTerms, requestWithHistory.Message))
                    .ToList();

                response.Reply = AiAssistantSupport.BuildRecommendationReplyFromResults(response.Query, response.RecommendedBranches, response.MatchedBranchCount, requestWithHistory.Message);
            }

            // Save current turn in memory so frontend only needs to send the latest message.
            _conversationMemoryService.AddMessage(userId, "user", request.Message);
            _conversationMemoryService.AddMessage(userId, "assistant", AiAssistantSupport.BuildAssistantMemoryContent(response));

            return response;
        }

        private async Task<(ActiveBranchListResponseDto Result, double? AppliedDistanceKm)> FindRecommendedBranchesAsync(AiRecommendationQueryDto query)
        {
            var filter = AiAssistantSupport.BuildBranchFilter(query);
            var result = await _branchService.GetActiveBranchesFilteredAsync(filter);
            return (result, filter.Distance);
        }

        private async Task<AiAssistantSupport.GeminiDecisionPayload> GetGeminiDecisionAsync(AiChatRequestDto request, List<DietaryPreferenceDto> userDietaryPreferences)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new DomainExceptions("Gemini API key chưa được cấu hình. Vui lòng thiết lập Gemini:ApiKey hoặc GOOGLE_API_KEY.");
            }

            var endpointTemplate = _configuration["Gemini:Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
            var endpoint = endpointTemplate.Contains("{model}", StringComparison.OrdinalIgnoreCase)
                ? endpointTemplate.Replace("{model}", model, StringComparison.OrdinalIgnoreCase)
                : endpointTemplate;

            if (!endpoint.Contains("key=", StringComparison.OrdinalIgnoreCase))
            {
                endpoint += endpoint.Contains("?", StringComparison.Ordinal) ? "&" : "?";
                endpoint += $"key={Uri.EscapeDataString(apiKey)}";
            }

            var requestPayload = AiAssistantSupport.BuildGeminiRequestPayload(request, userDietaryPreferences);

            var client = _httpClientFactory.CreateClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(httpRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new DomainExceptions($"Yêu cầu Gemini thất bại với trạng thái {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            var content = AiAssistantSupport.ExtractAssistantContent(responseBody);
            var json = AiAssistantSupport.ExtractJsonPayload(content);

            try
            {
                var decision = JsonSerializer.Deserialize<AiAssistantSupport.GeminiDecisionPayload>(json, JsonOptions);
                if (decision == null)
                {
                    throw new JsonException("AI output is null");
                }

                return decision;
            }
            catch
            {
                return new AiAssistantSupport.GeminiDecisionPayload
                {
                    Intent = "chat",
                    Reply = content,
                    SearchQuery = new AiAssistantSupport.GeminiSearchQueryPayload()
                };
            }
        }
    }
}
