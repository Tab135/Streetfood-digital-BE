using BO.DTO.Search;
using BO.Entities;
using Microsoft.Extensions.Logging;
using Repository.Interfaces;
using Service.Interfaces;
using Service.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service
{
    public class SearchService : ISearchService
    {
        private const int MaxDishesPerBranch = 10;
        private const double DishWeight = 0.01;
        private const double WDist = 0.6;
        private const double WRate = 0.4;

        private readonly IBranchRepository _branchRepository;
        private readonly IVendorDashboardService _dashboardService;
        private readonly ILogger<SearchService>? _logger;

        public SearchService(
            IBranchRepository branchRepository,
            IVendorDashboardService dashboardService,
            ILogger<SearchService>? logger = null)
        {
            _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
            _logger = logger;
        }

        public async Task<List<SearchResultDto>> SearchAsync(SearchFilterDto filter)
        {
            bool hasKeyword = !string.IsNullOrWhiteSpace(filter.Keyword);
            string normalizedKeyword = hasKeyword ? TextNormalizer.NormalizeForSearch(filter.Keyword!) : string.Empty;
            var keywordForms = hasKeyword
                ? TextNormalizer.ExpandWithSynonyms(normalizedKeyword)
                : new HashSet<string>();

            double? userLat = filter.Lat;
            double? userLong = filter.Long;

            var filteredItems = await _branchRepository.GetActiveBranchesFilteredAsync(
                userLat,
                userLong,
                filter.Distance,
                filter.DietaryIds,
                filter.TasteIds,
                filter.MinPrice,
                filter.MaxPrice,
                filter.CategoryIds,
                filter.IsSubscribed,
                filter.Wards
            );

            if (filteredItems == null || filteredItems.Count == 0)
                return new List<SearchResultDto>();

            // Score every filter-matched branch. A post-scoring gate below drops branches whose
            // DisplayName, Dish, and Similar scores are all zero when a keyword is set — this keeps
            // Jollibee (DisplayName=0, later lifted to Similar) in scope while excluding unrelated shops.
            var scored = filteredItems
                .Select(item => new ScoredBranch(item.branch, item.distanceKm))
                .ToList();
              

            if (hasKeyword)
            {
                // Display name scoring uses only the original normalized keyword, not synonym-expanded
                // forms. Synonym expansion is intentionally limited to dish matching so that brand-name
                // synonyms (e.g. "jollibee" ↔ "kfc") do not inflate a competitor's display name score.
                // Vendor similarity via synonym overlap is handled separately by ScoreSimilar (KFC Rule).
                foreach (var sb in scored)
                {
                    sb.DisplayNameScore = SearchScorer.ScoreDisplayName(
                        normalizedKeyword, sb.Branch.Vendor?.Name, sb.Branch.Name);
                }
            }

            // ---------- KFC Rule: Similar bucket for non-anchor vendors ----------
            // Every vendor matched by the filter is a candidate for best-seller metadata;
            // anchor vendors (displayNameScore == 100 on any branch) seed the signature union.
            var anchorVendorIds = scored
                .Where(sb => sb.DisplayNameScore >= SearchScorer.DisplayNameExact && sb.Branch.VendorId.HasValue)
                .Select(sb => sb.Branch.VendorId!.Value)
                .Distinct()
                .ToList();

            var allVendorIds = scored
                .Where(sb => sb.Branch.VendorId.HasValue)
                .Select(sb => sb.Branch.VendorId!.Value)
                .Distinct()
                .ToList();

            var signatureResolver = new AnchorSignatureResolver(_dashboardService);

            // Build IsSignature dish names from already-loaded branch data — no extra DB round-trip.
            // When a vendor has no IsSignature dishes the resolver falls back to best-seller order.
            var preloadedSignatures = scored
                .Where(sb => sb.Branch.VendorId.HasValue)
                .GroupBy(sb => sb.Branch.VendorId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(sb => sb.Branch.BranchDishes ?? new List<BranchDish>())
                           .Where(bd => bd.Dish != null && bd.Dish.IsActive && bd.Dish.IsSignature)
                           .Select(bd => TextNormalizer.NormalizeForSearch(bd.Dish.Name ?? string.Empty))
                           .Where(n => !string.IsNullOrEmpty(n))
                           .Distinct()
                           .ToList()
                );

            // Resolve every vendor in scope so we can set per-dish IsBestSeller flags
            // without extra roundtrips beyond the anchor-only spec minimum.
            var signaturesByVendor = await signatureResolver.ResolveAsync(allVendorIds, preloadedSignatures);

            if (hasKeyword && anchorVendorIds.Count > 0)
            {
                var anchorSignatureUnion = anchorVendorIds
                    .SelectMany(id => signaturesByVendor.TryGetValue(id, out var sigs) ? sigs : new List<string>())
                    .Distinct()
                    .ToList();

                // Group candidate branches by vendor so the Similar score applies to every branch of a non-anchor brand.
                var vendorGroups = scored
                    .Where(sb => sb.Branch.VendorId.HasValue)
                    .GroupBy(sb => sb.Branch.VendorId!.Value);

                foreach (var g in vendorGroups)
                {
                    // Anchor vendors already scored 100 — skip.
                    if (anchorVendorIds.Contains(g.Key)) continue;

                    // Only recompute when every branch of this vendor currently has DisplayName = 0
                    // (i.e. no exact/fuzzy match anywhere for this vendor).
                    if (g.Any(sb => sb.DisplayNameScore > 0)) continue;

                    var vendorDishNames = g
                        .SelectMany(sb => sb.Branch.BranchDishes ?? new List<BranchDish>())
                        .Where(bd => bd.Dish != null && bd.Dish.IsActive)
                        .Select(bd => bd.Dish.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .ToList();

                    var similar = SearchScorer.ScoreSimilar(vendorDishNames, anchorSignatureUnion);
                    if (similar > 0)
                    {
                        foreach (var sb in g) sb.DisplayNameScore = similar;
                    }
                }
            }

            // ---------- Dish scoring + top-10 selection per branch ----------
            foreach (var sb in scored)
            {
                var branch = sb.Branch;
                var signatureSet = branch.VendorId.HasValue && signaturesByVendor.TryGetValue(branch.VendorId.Value, out var sigs)
                    ? new HashSet<string>(sigs)
                    : new HashSet<string>();

                var dishEntries = (branch.BranchDishes ?? new List<BranchDish>())
                    .Where(bd => bd.Dish != null && bd.Dish.IsActive)
                    .Select(bd =>
                    {
                        var normalizedDishName = TextNormalizer.NormalizeForSearch(bd.Dish.Name ?? string.Empty);
                        var isBestSeller = signatureSet.Contains(normalizedDishName);

                        double bestDishScore = 0.0;
                        if (hasKeyword)
                        {
                            foreach (var form in keywordForms)
                            {
                                var s = SearchScorer.ScoreDish(form, bd.Dish.Name,
                                    isBestSeller, branch.AvgRating, branch.TotalReviewCount);
                                if (s > bestDishScore) bestDishScore = s;
                            }
                        }
                        else
                        {
                            // No keyword: surface prominent dishes. Base "prominence" = 50 for best-sellers, 10 otherwise.
                            bestDishScore = isBestSeller ? 50.0 : 10.0;
                        }

                        return new { BranchDish = bd, Score = bestDishScore, IsBestSeller = isBestSeller, bd.Dish.IsSignature };
                    })
                    .Where(e => !hasKeyword || e.Score > 0)
                    .OrderByDescending(e => e.Score)
                    .ThenByDescending(e => e.IsBestSeller)
                    .ThenBy(e => e.BranchDish.Dish.DishId)
                    .Take(MaxDishesPerBranch)
                    .ToList();

                sb.Dishes = dishEntries
                    .Select(e => new DishSearchDto
                    {
                        DishId = e.BranchDish.Dish.DishId,
                        Name = e.BranchDish.Dish.Name,
                        Price = e.BranchDish.Dish.Price,
                        Description = e.BranchDish.Dish.Description,
                        ImageUrl = e.BranchDish.Dish.ImageUrl,
                        IsSoldOut = e.BranchDish.IsSoldOut,
                        CategoryName = e.BranchDish.Dish.Category?.Name ?? string.Empty,
                        Score = e.Score,
                        IsBestSeller = e.IsBestSeller,
                        IsSignature = e.IsSignature,
                    })
                    .ToList();

                sb.DishScore = dishEntries.Count > 0 ? dishEntries.Max(e => e.Score) : 0.0;
            }

            // ---------- Drop branches that match only by dish when no dish actually scored > 0 ----------
            // A keyword-search candidate must either have DisplayName > 0 or at least one dishScore > 0.
            if (hasKeyword)
            {
                scored = scored
                    .Where(sb => sb.DisplayNameScore > 0 || sb.DishScore > 0)
                    .ToList();
            }

            // ---------- Compute finalScore per branch ----------
            foreach (var sb in scored)
            {
                var branch = sb.Branch;
                double tierWeight = branch.Tier != null ? branch.Tier.Weight : 1.0;
                double subMultiplier = branch.IsSubscribed ? 1.2 : 0.7;

                double distanceScore = (sb.DistanceKm == 0 && (!userLat.HasValue || !userLong.HasValue))
                    ? 0.0
                    : (1.0 / (sb.DistanceKm + 1.0)) * WDist;

                double ratingScore = (branch.AvgRating / 5.0) * WRate;

                sb.FinalScore = (distanceScore + ratingScore) * tierWeight * subMultiplier
                                + sb.DishScore * DishWeight;
            }

            // ---------- Collapse per-vendor: pick primary, siblings in OtherBranches ----------
            var vendorBuckets = scored
                .Where(sb => sb.Branch.VendorId.HasValue)
                .GroupBy(sb => sb.Branch.VendorId!.Value)
                .Select(g =>
                {
                    ScoredBranch primary;
                    if (userLat.HasValue && userLong.HasValue)
                    {
                        primary = g.OrderBy(sb => sb.DistanceKm).First();
                    }
                    else
                    {
                        primary = g.OrderByDescending(sb => sb.FinalScore).First();
                    }

                    var siblings = g
                        .Where(sb => sb.Branch.BranchId != primary.Branch.BranchId)
                        .OrderBy(sb => sb.DistanceKm)
                        .ToList();

                    return new { Primary = primary, Siblings = siblings };
                })
                .ToList();

            // ---------- Two-pass ordering: DisplayName bucket first, then finalScore within bucket ----------
            int BucketOf(double score)
            {
                if (score >= SearchScorer.DisplayNameExact) return 3;
                if (score >= SearchScorer.DisplayNameFuzzy) return 2;
                if (score >= SearchScorer.DisplayNameSimilarFloor) return 1;
                return 0;
            }

            var orderedVendors = vendorBuckets
                .OrderByDescending(v => BucketOf(v.Primary.DisplayNameScore))
                .ThenByDescending(v => v.Primary.FinalScore)
                .ToList();

            // ---------- Project to DTOs ----------
            var results = orderedVendors
                .Select(v =>
                {
                    var primaryDto = ToBranchDto(v.Primary);
                    primaryDto.OtherBranches = v.Siblings.Select(ToBranchDto).ToList();

                    var vendorName = v.Primary.Branch.Vendor?.Name ?? string.Empty;
                    return new SearchResultDto
                    {
                        VendorId = v.Primary.Branch.VendorId ?? 0,
                        VendorName = vendorName,
                        ManagerId = v.Primary.Branch.ManagerId ?? 0,
                        IsActive = v.Primary.Branch.Vendor?.IsActive ?? false,
                        Branches = new List<BranchSearchDto> { primaryDto }
                    };
                })
                .ToList();

            if (_logger != null && hasKeyword)
            {
                foreach (var r in results)
                {
                    var primary = r.Branches.FirstOrDefault();
                    if (primary == null) continue;
                    _logger.LogInformation(
                        "Search keyword='{Keyword}' vendorId={VendorId} displayNameScore={DisplayNameScore} dishScore={DishScore} finalScore={FinalScore} isAnchor={IsAnchor} siblings={SiblingCount}",
                        filter.Keyword,
                        r.VendorId,
                        primary.DisplayNameScore,
                        primary.DishScore,
                        primary.FinalScore,
                        primary.DisplayNameScore >= SearchScorer.DisplayNameExact,
                        primary.OtherBranches.Count);
                }
            }

            return results;
        }

        private static BranchSearchDto ToBranchDto(ScoredBranch sb) => new BranchSearchDto
        {
            BranchId = sb.Branch.BranchId,
            Name = sb.Branch.Name,
            AddressDetail = sb.Branch.AddressDetail,
            City = sb.Branch.City,
            Ward = sb.Branch.Ward,
            Lat = sb.Branch.Lat,
            Long = sb.Branch.Long,
            AvgRating = sb.Branch.AvgRating,
            TotalReviewCount = sb.Branch.TotalReviewCount,
            FinalScore = sb.FinalScore,
            DistanceKm = sb.DistanceKm,
            IsSubscribed = sb.Branch.IsSubscribed,
            IsVerified = sb.Branch.IsVerified,
            IsActive = sb.Branch.IsActive,
            DisplayNameScore = sb.DisplayNameScore,
            DishScore = sb.DishScore,
            Dishes = sb.Dishes,
        };

        private sealed class ScoredBranch
        {
            public Branch Branch { get; }
            public double DistanceKm { get; }
            public double DisplayNameScore { get; set; }
            public double DishScore { get; set; }
            public double FinalScore { get; set; }
            public List<DishSearchDto> Dishes { get; set; } = new();

            public ScoredBranch(Branch branch, double distanceKm)
            {
                Branch = branch;
                DistanceKm = distanceKm;
            }
        }
    }
}
