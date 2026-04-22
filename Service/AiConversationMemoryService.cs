using BO.DTO.AI;
using Microsoft.Extensions.Caching.Memory;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service
{
    public class AiConversationMemoryService : IAiConversationMemoryService
    {
        private const int MaxHistoryMessages = 12;
        private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromHours(6);
        private static readonly object SyncRoot = new();

        private readonly IMemoryCache _cache;

        public AiConversationMemoryService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public List<AiChatHistoryMessageDto> GetHistory(int userId)
        {
            var key = BuildCacheKey(userId);
            if (_cache.TryGetValue(key, out List<AiChatHistoryMessageDto>? history) && history != null)
            {
                return history.Select(CloneMessage).ToList();
            }

            return new List<AiChatHistoryMessageDto>();
        }

        public void AddMessage(int userId, string role, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var normalizedRole = NormalizeRole(role);
            var normalizedContent = content.Trim();
            var key = BuildCacheKey(userId);

            lock (SyncRoot)
            {
                var history = GetInternalHistory(key);
                history.Add(new AiChatHistoryMessageDto
                {
                    Role = normalizedRole,
                    Content = normalizedContent
                });

                if (history.Count > MaxHistoryMessages)
                {
                    history = history.TakeLast(MaxHistoryMessages).ToList();
                }

                SetHistory(key, history);
            }
        }

        public void ClearHistory(int userId)
        {
            _cache.Remove(BuildCacheKey(userId));
        }

        private List<AiChatHistoryMessageDto> GetInternalHistory(string key)
        {
            if (_cache.TryGetValue(key, out List<AiChatHistoryMessageDto>? history) && history != null)
            {
                return history.Select(CloneMessage).ToList();
            }

            return new List<AiChatHistoryMessageDto>();
        }

        private void SetHistory(string key, List<AiChatHistoryMessageDto> history)
        {
            _cache.Set(
                key,
                history.Select(CloneMessage).ToList(),
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = SlidingExpiration,
                    AbsoluteExpirationRelativeToNow = AbsoluteExpiration
                });
        }

        private static string BuildCacheKey(int userId) => $"ai-chat-history:{userId}";

        private static string NormalizeRole(string role)
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

        private static AiChatHistoryMessageDto CloneMessage(AiChatHistoryMessageDto source)
        {
            return new AiChatHistoryMessageDto
            {
                Role = NormalizeRole(source.Role),
                Content = source.Content ?? string.Empty
            };
        }
    }
}
