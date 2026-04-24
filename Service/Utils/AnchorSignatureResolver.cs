using Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Utils
{
    /// <summary>
    /// Per-request memoizer that fetches the top-N best-seller dish names for a set of anchor vendors
    /// via <see cref="IVendorDashboardService.GetDishDashboardByVendorAsync"/>. Used by the KFC Rule
    /// Similar-bucket scorer so each anchor vendor is only queried once per search request.
    /// </summary>
    public sealed class AnchorSignatureResolver
    {
        public const int DefaultTopN = 5;

        private readonly IVendorDashboardService _dashboardService;
        private readonly Dictionary<int, List<string>> _cache = new();
        private readonly int _topN;

        public AnchorSignatureResolver(IVendorDashboardService dashboardService, int topN = DefaultTopN)
        {
            _dashboardService = dashboardService;
            _topN = topN > 0 ? topN : DefaultTopN;
        }

        /// <summary>
        /// Returns a map { anchorVendorId -> top-N normalized signature dish names sorted by completed-order quantity desc }.
        /// Anchor ids already resolved in this instance are served from cache.
        /// </summary>
        public async Task<Dictionary<int, List<string>>> ResolveAsync(IEnumerable<int> anchorVendorIds)
        {
            if (anchorVendorIds == null)
                return new Dictionary<int, List<string>>();

            var ids = anchorVendorIds.Distinct().ToList();
            foreach (var vendorId in ids)
            {
                if (_cache.ContainsKey(vendorId)) continue;

                var dashboard = await _dashboardService.GetDishDashboardByVendorAsync(vendorId);
                var topNames = (dashboard?.TopDishes ?? new())
                    .Where(d => d.TotalQuantityOrdered > 0 && !string.IsNullOrWhiteSpace(d.DishName))
                    .OrderByDescending(d => d.TotalQuantityOrdered)
                    .Take(_topN)
                    .Select(d => TextNormalizer.NormalizeForSearch(d.DishName))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                _cache[vendorId] = topNames;
            }

            return ids.ToDictionary(id => id, id => _cache[id]);
        }

        /// <summary>
        /// Returns the normalized set of best-seller dish ids for a previously-resolved anchor
        /// (for marking <c>IsBestSeller</c> on matching dishes). Empty when vendor hasn't been resolved.
        /// </summary>
        public IReadOnlySet<string> BestSellerNames(int vendorId)
        {
            if (_cache.TryGetValue(vendorId, out var names))
                return new HashSet<string>(names);
            return new HashSet<string>();
        }
    }
}
