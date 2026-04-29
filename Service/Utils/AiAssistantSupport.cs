using BO.DTO.AI;
using BO.DTO.Branch;
using BO.DTO.Dietary;
using BO.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Service.Utils
{
    internal static class AiAssistantSupport
    {
        public static object BuildGeminiRequestPayload(AiChatRequestDto request, List<AiChatHistoryMessageDto> history, List<DietaryPreferenceDto> userDietaryPreferences)
        {
            return new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = "Bạn là một trợ lý ẩm thực. Luôn trả về JSON hợp lệ với cấu trúc sau: " +
                                "{\"intent\":\"chat|recommend_food\",\"reply\":\"string\",\"searchQuery\":{\"keyword\":\"string|null\",\"searchTermsAccented\":[\"string\"],\"searchTermsNormalized\":[\"string\"],\"lat\":number|null,\"long\":number|null,\"distanceKm\":number|null,\"dietaryIds\":number[],\"tasteIds\":number[],\"minPrice\":number|null,\"maxPrice\":number|null,\"categoryIds\":number[]}}. " +
                                "Chọn intent=recommend_food khi người dùng yêu cầu gợi ý ăn uống, nên ăn gì, hoặc gợi ý quán gần đó. " +
                                "Khi người dùng hỏi về sở thích ăn uống của chính họ, hãy dựa vào hồ sơ dietary đã lưu để trả lời rõ ràng, không nói là không biết nếu có dữ liệu. " +
                                "Nếu intent=recommend_food, hãy tự suy luận 1 đến 5 searchTerms tiếng Việt thực tế và cụ thể để tìm món/quán, có thể gồm từ đồng nghĩa, biến thể món, tên món phổ biến liên quan; không dùng từ chung chung như 'món ăn' nếu có thể xác định cụ thể hơn. " +
                                "Nếu không rõ, giữ các trường số là null và các mảng là rỗng. Không thêm markdown. " +
                                "BẮT BUỘC: Trong trường `searchQuery` trả về cả hai mảng: `searchTermsAccented` (từ/fras tiếng Việt có dấu, dễ đọc cho người) và `searchTermsNormalized` (phiên bản đã bỏ dấu, dùng để đối chiếu tìm kiếm). `keyword` cũng nên giữ dấu khi có. " +
                                "BẮT BUỘC: reply phải dùng tiếng Việt tự nhiên, ngắn gọn, không dùng tiếng Anh."
                        }
                    }
                },
                contents = BuildGeminiContents(request, history, userDietaryPreferences),
                generationConfig = new
                {
                    temperature = 0.2,
                    responseMimeType = "application/json"
                }
            };
        }

        public static List<object> BuildGeminiContents(AiChatRequestDto request, List<AiChatHistoryMessageDto> history, List<DietaryPreferenceDto> userDietaryPreferences)
        {
            var contents = new List<object>();

            if (history != null && history.Count > 0)
            {
                foreach (var historyMessage in history
                    .Where(h => !string.IsNullOrWhiteSpace(h.Content))
                    .TakeLast(6))
                {
                    var role = NormalizeHistoryRoleForGemini(historyMessage.Role);
                    contents.Add(new
                    {
                        role,
                        parts = new[]
                        {
                            new
                            {
                                text = historyMessage.Content.Trim()
                            }
                        }
                    });
                }
            }

            var userContext = new
            {
                message = request.Message,
                gps = new { lat = request.Lat, @long = request.Long, distanceKm = request.DistanceKm },
                knownUserDietaryPreferences = userDietaryPreferences.Select(x => new
                {
                    id = x.DietaryPreferenceId,
                    name = x.Name
                }),
                knownUserDietaryPreferenceSummary = userDietaryPreferences.Count > 0
                    ? string.Join(", ", userDietaryPreferences.Select(x => x.Name))
                    : "chưa có sở thích ăn uống nào được lưu",
                recommendationSearchHint = "Nếu cần gợi ý món/quán, hãy tự tạo searchTerms tiếng Việt cụ thể và phù hợp từ ngữ cảnh của message thay vì chỉ giữ nguyên một cụm chung chung.",
                dietaryPreferences = userDietaryPreferences.Select(x => new
                {
                    id = x.DietaryPreferenceId,
                    name = x.Name
                })
            };

            contents.Add(new
            {
                role = "user",
                parts = new[]
                {
                    new
                    {
                        text = JsonSerializer.Serialize(userContext)
                    }
                }
            });

            return contents;
        }

        private static string NormalizeHistoryRoleForGemini(string role)
        {
            if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                return "model";
            }

            return "user";
        }

        public static ActiveBranchFilterDto BuildBranchFilter(AiRecommendationQueryDto query)
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

        public static List<ActiveBranchResponseDto> FilterBranchesBySearchTerms(List<ActiveBranchResponseDto> branches, IReadOnlyCollection<string> searchTerms, string? userMessage)
        {
            return branches
                .Select(branch => new
                {
                    Branch = branch,
                    Score = GetBranchRelevanceScore(branch, searchTerms, userMessage)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Branch.FinalScore)
                .Select(x => x.Branch)
                .ToList();
        }

        public static AiRecommendedBranchDto MapToRecommendedBranchDto(ActiveBranchResponseDto branch, IReadOnlyCollection<string> searchTerms, string? userMessage)
        {
            var keywordMatchedDishes = branch.Dishes
                .Select(dish => new
                {
                    Dish = dish,
                    Score = GetDishRelevanceScore(dish.Name, searchTerms, userMessage)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Dish.IsSoldOut)
                .Select(x => x.Dish)
                .ToList();

            var selectedDishes = keywordMatchedDishes
                .Where(d => !d.IsSoldOut)
                .Take(3)
                .ToList();

            if (selectedDishes.Count == 0)
            {
                selectedDishes = keywordMatchedDishes
                    .Take(3)
                    .ToList();
            }

            if (selectedDishes.Count == 0)
            {
                selectedDishes = branch.Dishes
                    .Take(3)
                    .ToList();
            }

            var recommendedDishes = selectedDishes
                .Select(d => new AiRecommendedDishDto
                {
                    DishId = d.DishId,
                    Name = d.Name
                })
                .ToList();

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

        public static AiRecommendationQueryDto NormalizeQuery(GeminiSearchQueryPayload? rawQuery, AiChatRequestDto request, List<DietaryPreferenceDto> userDietaryPreferences)
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

            // Prefer normalized terms explicitly returned by the model (if any).
            var searchTerms = NormalizeSearchTerms(rawQuery?.SearchTermsNormalized ?? rawQuery?.SearchTerms);
            if (searchTerms.Count == 0 && !string.IsNullOrWhiteSpace(rawQuery?.Keyword))
            {
                searchTerms = NormalizeSearchTerms(new[] { rawQuery.Keyword! });
            }

            return new AiRecommendationQueryDto
            {
                Keyword = string.IsNullOrWhiteSpace(rawQuery?.Keyword) ? null : rawQuery!.Keyword!.Trim(),
                SearchTerms = searchTerms,
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

        public static string BuildRecommendationReplyFromResults(AiRecommendationQueryDto query, List<AiRecommendedBranchDto> recommendedBranches, int matchedBranchCount, string? userMessage)
        {
            if (recommendedBranches == null || recommendedBranches.Count == 0 || matchedBranchCount == 0)
            {
                var noResultHint = string.IsNullOrWhiteSpace(query.Keyword)
                    ? "Hiện chưa tìm thấy quán phù hợp với tiêu chí của bạn."
                    : $"Hiện chưa tìm thấy quán có món liên quan đến \"{query.Keyword}\".";

                return noResultHint + " Bạn có thể mở rộng khoảng cách hoặc đổi từ khóa để mình gợi ý tốt hơn.";
            }

            var topBranches = recommendedBranches.Take(3).ToList();
            var branchSummary = string.Join("; ", topBranches.Select((branch, index) =>
            {
                var dishes = branch.RecommendedDishes != null && branch.RecommendedDishes.Count > 0
                    ? string.Join(", ", branch.RecommendedDishes.Take(1).Select(d => d.Name))
                    : "chưa có món nổi bật";

                var distanceText = branch.DistanceKm.HasValue
                    ? $"{Math.Round(branch.DistanceKm.Value, 1)} km"
                    : "không rõ khoảng cách";

                return $"{index + 1}) {branch.Name} ({distanceText}) - gợi ý: {dishes}";
            }));

            var keywordText = string.IsNullOrWhiteSpace(query.Keyword)
                ? "theo sở thích của bạn"
                : $"cho món \"{query.Keyword}\"";

            var preferenceNote = BuildPreferenceNote(userMessage);
            return $"Mình đã tìm thấy {matchedBranchCount} quán phù hợp {keywordText}{preferenceNote}. Gợi ý nổi bật: {branchSummary}.";
        }

        public static string BuildAssistantMemoryContent(AiChatResponseDto response)
        {
            if (!string.Equals(response.Intent, "recommend_food", StringComparison.OrdinalIgnoreCase))
            {
                return response.Reply;
            }

            var topBranchNames = response.RecommendedBranches
                .Take(5)
                .Select(x => x.Name)
                .ToList();

            var summary = new
            {
                intent = response.Intent,
                reply = response.Reply,
                matchedBranchCount = response.MatchedBranchCount,
                query = new
                {
                    keyword = response.Query.Keyword,
                    lat = response.Query.Lat,
                    @long = response.Query.Long,
                    distanceKm = response.Query.DistanceKm,
                    dietaryIds = response.Query.DietaryIds,
                    tasteIds = response.Query.TasteIds,
                    minPrice = response.Query.MinPrice,
                    maxPrice = response.Query.MaxPrice,
                    categoryIds = response.Query.CategoryIds
                },
                topBranchNames
            };

            return "Tóm tắt kết quả gợi ý gần nhất: " + JsonSerializer.Serialize(summary);
        }

        public static string ExtractAssistantContent(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                throw new DomainExceptions("Phản hồi từ Gemini không chứa candidates");
            }

            var firstCandidate = candidates[0];
            if (!firstCandidate.TryGetProperty("content", out var candidateContent) ||
                !candidateContent.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                throw new DomainExceptions("Gemini trả về content trống");
            }

            var content = string.Join("\n", parts
                .EnumerateArray()
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString())
                .Where(text => !string.IsNullOrWhiteSpace(text)));

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new DomainExceptions("Gemini trả về nội dung trống");
            }

            return content;
        }

        public static string ExtractJsonPayload(string content)
        {
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');

            if (start >= 0 && end > start)
            {
                return content[start..(end + 1)];
            }

            return "{}";
        }

        private static bool IsDishMatchingAnyKeyword(string? dishName, IReadOnlyCollection<string> searchTerms)
        {
            if (string.IsNullOrWhiteSpace(dishName) || searchTerms.Count == 0)
            {
                return false;
            }

            var normalizedDishName = TextNormalizer.NormalizeForSearch(dishName);

            if (string.IsNullOrWhiteSpace(normalizedDishName))
            {
                return false;
            }

            return searchTerms.Any(searchTerm => normalizedDishName.Contains(searchTerm));
        }

        private static int GetBranchRelevanceScore(ActiveBranchResponseDto branch, IReadOnlyCollection<string> searchTerms, string? userMessage)
        {
            var bestDishScore = branch.Dishes
                .Select(dish => GetDishRelevanceScore(dish.Name, searchTerms, userMessage))
                .DefaultIfEmpty(0)
                .Max();

            if (bestDishScore <= 0)
            {
                return 0;
            }

            var branchNameScore = GetTextBonus(branch.Name, userMessage);
            return bestDishScore + branchNameScore;
        }

        private static int GetDishRelevanceScore(string? dishName, IReadOnlyCollection<string> searchTerms, string? userMessage)
        {
            if (string.IsNullOrWhiteSpace(dishName) || searchTerms.Count == 0)
            {
                return 0;
            }

            var normalizedDishName = NormalizeSearchTerm(dishName);
            if (string.IsNullOrWhiteSpace(normalizedDishName))
            {
                return 0;
            }

            var matchedTerms = searchTerms
                .Where(searchTerm => normalizedDishName.Contains(searchTerm))
                .ToList();

            if (matchedTerms.Count == 0)
            {
                return 0;
            }

            var score = matchedTerms.Max(term => term.Length);
            score += GetFormPreferenceBonus(normalizedDishName, userMessage);

            return score;
        }

        private static int GetFormPreferenceBonus(string normalizedDishName, string? userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return 0;
            }

            var normalizedMessage = NormalizeSearchTerm(userMessage);
            if (string.IsNullOrWhiteSpace(normalizedMessage))
            {
                return 0;
            }

            var wantsWetDish = normalizedMessage.Contains("mon nuoc") || normalizedMessage.Contains("nuoc") || normalizedMessage.Contains("canh") || normalizedMessage.Contains("sup");
            var wantsDryDish = normalizedMessage.Contains("mon kho") || normalizedMessage.Contains("kho");

            var bonus = 0;
            if (wantsWetDish)
            {
                if (normalizedDishName.Contains("kho"))
                {
                    bonus -= 120;
                }

                if (normalizedDishName.Contains("nuoc") || normalizedDishName.Contains("canh") || normalizedDishName.Contains("sup"))
                {
                    bonus += 40;
                }
            }

            if (wantsDryDish)
            {
                if (normalizedDishName.Contains("nuoc") || normalizedDishName.Contains("canh") || normalizedDishName.Contains("sup"))
                {
                    bonus -= 120;
                }

                if (normalizedDishName.Contains("kho"))
                {
                    bonus += 40;
                }
            }

            return bonus;
        }

        private static int GetTextBonus(string? text, string? userMessage)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(userMessage))
            {
                return 0;
            }

            var normalizedText = NormalizeSearchTerm(text);
            var normalizedMessage = NormalizeSearchTerm(userMessage);
            if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(normalizedMessage))
            {
                return 0;
            }

            return normalizedText.Contains(normalizedMessage) ? 10 : 0;
        }

        private static List<string> NormalizeSearchTerms(IEnumerable<string>? searchTerms)
        {
            if (searchTerms == null)
            {
                return new List<string>();
            }

            var normalizedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var term in searchTerms)
            {
                var normalizedTerm = NormalizeSearchTerm(term);
                if (!string.IsNullOrWhiteSpace(normalizedTerm))
                {
                    normalizedTerms.Add(normalizedTerm);
                }
            }

            return normalizedTerms.ToList();
        }

        private static string NormalizeSearchTerm(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return TextNormalizer.NormalizeForSearch(value).Trim().ToLowerInvariant();
        }

        private static string BuildPreferenceNote(string? userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return string.Empty;
            }

            var normalizedMessage = NormalizeSearchTerm(userMessage);
            if (normalizedMessage.Contains("nuoc") || normalizedMessage.Contains("canh") || normalizedMessage.Contains("sup"))
            {
                return " và đã ưu tiên món nước";
            }

            if (normalizedMessage.Contains("kho"))
            {
                return " và đã ưu tiên món khô";
            }

            return string.Empty;
        }

        internal sealed class GeminiDecisionPayload
        {
            public string? Intent { get; set; }
            public string? Reply { get; set; }
            public GeminiSearchQueryPayload? SearchQuery { get; set; }
        }

        internal sealed class GeminiSearchQueryPayload
        {
            public string? Keyword { get; set; }
            public List<string>? SearchTerms { get; set; }
            public List<string>? SearchTermsAccented { get; set; }
            public List<string>? SearchTermsNormalized { get; set; }
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