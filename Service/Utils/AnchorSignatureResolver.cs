using Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Utils
{
    /// <summary>
    /// Per-request memoizer that resolves signature dish names for a set of vendors.
    /// Priority: dishes flagged <c>IsSignature == true</c> (passed in via pre-loaded branch data);
    /// fall back to top-N best-seller order from <see cref="IVendorDashboardService.GetDishDashboardByVendorAsync"/>
    /// when no signature dishes are available for a vendor.
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
        /// Returns a map { vendorId -> normalized signature dish names }.
        /// When <paramref name="preloadedSignatures"/> contains a non-empty list for a vendor those
        /// names are used directly (no dashboard call). Otherwise the dashboard best-seller order is used.
        /// </summary>
        /// <param name="vendorIds">All vendor IDs to resolve.</param>
        /// <param name="preloadedSignatures">
        /// Optional map of vendorId → already-normalized names of dishes with <c>IsSignature == true</c>
        /// extracted from the branches that were loaded for this search request.
        /// </param>
        public async Task<Dictionary<int, List<string>>> ResolveAsync(
            IEnumerable<int> vendorIds,
            IReadOnlyDictionary<int, List<string>>? preloadedSignatures = null)
        {
            if (vendorIds == null)
                return new Dictionary<int, List<string>>();

            var ids = vendorIds.Distinct().ToList();
            foreach (var vendorId in ids)
            {
                if (_cache.ContainsKey(vendorId)) continue;

                // Prefer explicit IsSignature dishes when available.
                if (preloadedSignatures != null &&
                    preloadedSignatures.TryGetValue(vendorId, out var sigNames) &&
                    sigNames.Count > 0)
                {
                    _cache[vendorId] = sigNames;
                    continue;
                }

                // Fall back to best-seller order from the dashboard.
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
        /// Returns the normalized signature dish names for a previously-resolved vendor
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
