using BO.DTO.Dashboard;
using BO.DTO.Search;
using BO.Entities;
using Moq;
using Repository.Interfaces;
using Service;
using Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
// Aliases: sibling test folders named Dish/ and Vendor/ expose namespaces that shadow the entities.
using DishEntity = BO.Entities.Dish;
using VendorEntity = BO.Entities.Vendor;

namespace StreetFood.Tests.Search
{
    public class SearchServiceTests
    {
        private readonly Mock<IBranchRepository> _branchRepoMock = new();
        private readonly Mock<IVendorDashboardService> _dashboardMock = new();
        private readonly SearchService _service;

        public SearchServiceTests()
        {
            _service = new SearchService(_branchRepoMock.Object, _dashboardMock.Object);
        }

        // ----- Helpers ----------------------------------------------------------

        private static Branch MakeBranch(
            int branchId,
            int vendorId,
            string vendorName,
            string branchName,
            double avgRating = 4.0,
            int reviews = 10,
            bool isSubscribed = false,
            double tierWeight = 1.0,
            IEnumerable<string>? dishes = null)
        {
            var branchDishes = (dishes ?? Enumerable.Empty<string>())
                .Select((name, idx) => new BranchDish
                {
                    BranchId = branchId,
                    DishId = branchId * 100 + idx,
                    IsSoldOut = false,
                    Dish = new DishEntity
                    {
                        DishId = branchId * 100 + idx,
                        Name = name,
                        Price = 50000m,
                        IsActive = true,
                        VendorId = vendorId,
                        Category = new Category { Name = "Main" }
                    }
                })
                .ToList();

            return new Branch
            {
                BranchId = branchId,
                VendorId = vendorId,
                ManagerId = vendorId,
                Name = branchName,
                AddressDetail = $"{branchName} address",
                City = "HCM",
                Ward = "Ward 1",
                Lat = 10.77,
                Long = 106.70,
                IsVerified = true,
                IsActive = true,
                IsSubscribed = isSubscribed,
                AvgRating = avgRating,
                TotalReviewCount = reviews,
                Tier = new Tier { TierId = 2, Name = "Silver", Weight = tierWeight },
                Vendor = new VendorEntity { VendorId = vendorId, Name = vendorName, IsActive = true },
                BranchDishes = branchDishes,
            };
        }

        private void SetupFilteredBranches(params (Branch branch, double distanceKm)[] items)
        {
            _branchRepoMock
                .Setup(r => r.GetActiveBranchesFilteredAsync(
                    It.IsAny<double?>(), It.IsAny<double?>(), It.IsAny<double?>(),
                    It.IsAny<List<int>?>(), It.IsAny<List<int>?>(),
                    It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                    It.IsAny<List<int>?>(), It.IsAny<bool?>(), It.IsAny<List<string>?>()))
                .ReturnsAsync(items.ToList());
        }

        private void SetupDashboard(int vendorId, params (string dish, int qty)[] topDishes)
        {
            _dashboardMock
                .Setup(d => d.GetDishDashboardByVendorAsync(vendorId))
                .ReturnsAsync(new DishDashboardDto
                {
                    TopDishes = topDishes
                        .Select((t, i) => new TopDishDto
                        {
                            DishId = vendorId * 1000 + i,
                            DishName = t.dish,
                            TotalQuantityOrdered = t.qty
                        })
                        .ToList()
                });
        }

        private void SetupEmptyDashboardForAll()
        {
            _dashboardMock
                .Setup(d => d.GetDishDashboardByVendorAsync(It.IsAny<int>()))
                .ReturnsAsync(new DishDashboardDto());
        }

        // ----- KFC Rule end-to-end ---------------------------------------------

        [Fact]
        public async Task SearchKfc_ReturnsKfcBeforeSimilarBrand_AndExcludesUnrelated()
        {
            var kfc = MakeBranch(1, 10, "KFC", "Nguyễn Trãi", dishes: new[] { "Gà rán", "Burger gà" });
            var jollibee = MakeBranch(2, 20, "Jollibee", "Quận 1", dishes: new[] { "Gà rán giòn", "Spaghetti" });
            var pho24 = MakeBranch(3, 30, "Phở 24", "Bùi Viện", dishes: new[] { "Phở bò", "Phở gà" });

            SetupFilteredBranches((kfc, 0.5), (jollibee, 1.2), (pho24, 0.3));
            SetupDashboard(10, ("Gà rán", 500), ("Burger gà", 200)); // KFC best-sellers
            SetupDashboard(20, ("Gà rán giòn", 300));
            SetupDashboard(30, ("Phở bò", 400));

            var results = await _service.SearchAsync(new SearchFilterDto { Keyword = "KFC", Lat = 10.77, Long = 106.70 });

            Assert.Equal(2, results.Count);
            Assert.Equal("KFC", results[0].VendorName);
            Assert.Equal("Jollibee", results[1].VendorName);
            Assert.Equal(100.0, results[0].Branches[0].DisplayNameScore);
            Assert.InRange(results[1].Branches[0].DisplayNameScore, 50.0, 70.0);
        }

        // ----- Synonym expansion -----------------------------------------------

        [Fact]
        public async Task SearchComTam_MatchesVendorSellingComSuon_ViaSynonym()
        {
            var vendor = MakeBranch(1, 10, "Cơm nhà", "Quận 3", dishes: new[] { "Cơm sườn nướng" });
            SetupFilteredBranches((vendor, 0.8));
            SetupEmptyDashboardForAll();

            var results = await _service.SearchAsync(new SearchFilterDto { Keyword = "cơm tấm", Lat = 10.77, Long = 106.70 });

            Assert.Single(results);
            Assert.Equal("Cơm nhà", results[0].VendorName);
            // "com tam" ↔ "com suon" synonym: dish name matches via expansion.
            Assert.True(results[0].Branches[0].DishScore > 0,
                "Synonym-expanded dish should score > 0");
        }

        // ----- Multi-branch collapse -------------------------------------------

        [Fact]
        public async Task MultiBranchVendor_CollapsesToClosest_SiblingsSortedByDistance()
        {
            var close = MakeBranch(1, 10, "KFC", "Q1", dishes: new[] { "Gà rán" });
            var mid = MakeBranch(2, 10, "KFC", "Q3", dishes: new[] { "Gà rán" });
            var far = MakeBranch(3, 10, "KFC", "Q7", dishes: new[] { "Gà rán" });
            SetupFilteredBranches((close, 0.5), (mid, 2.0), (far, 5.0));
            SetupDashboard(10, ("Gà rán", 500));

            var results = await _service.SearchAsync(new SearchFilterDto { Keyword = "KFC", Lat = 10.77, Long = 106.70 });

            Assert.Single(results);
            Assert.Single(results[0].Branches);
            var primary = results[0].Branches[0];
            Assert.Equal(1, primary.BranchId);
            Assert.Equal(2, primary.OtherBranches.Count);
            Assert.Equal(2, primary.OtherBranches[0].BranchId); // closer sibling first
            Assert.Equal(3, primary.OtherBranches[1].BranchId);
        }

        // ----- Dish cap --------------------------------------------------------

        [Fact]
        public async Task BranchWith25MatchingDishes_ReturnsTop10BySearchScore()
        {
            var dishNames = Enumerable.Range(1, 25).Select(i => $"Gà rán số {i}").ToArray();
            var branch = MakeBranch(1, 10, "KFC", "Q1", dishes: dishNames);
            SetupFilteredBranches((branch, 0.5));
            SetupDashboard(10, ("Gà rán số 1", 500));

            var results = await _service.SearchAsync(new SearchFilterDto { Keyword = "gà rán", Lat = 10.77, Long = 106.70 });

            Assert.Single(results);
            var primary = results[0].Branches[0];
            Assert.Equal(10, primary.Dishes.Count);
            for (int i = 1; i < primary.Dishes.Count; i++)
            {
                Assert.True(primary.Dishes[i - 1].Score >= primary.Dishes[i].Score,
                    $"Dishes must be sorted by score desc: {primary.Dishes[i - 1].Score} >= {primary.Dishes[i].Score}");
            }
        }

        // ----- No-GPS fallback -------------------------------------------------

        [Fact]
        public async Task NoGps_PrimaryBranchChosenByFinalScoreDesc()
        {
            // Two branches of the same vendor, no GPS → pick the higher finalScore one as primary.
            var highRating = MakeBranch(1, 10, "KFC", "Q1", avgRating: 4.9, reviews: 500, dishes: new[] { "Gà rán" });
            var lowRating = MakeBranch(2, 10, "KFC", "Q3", avgRating: 3.5, reviews: 10, dishes: new[] { "Gà rán" });
            SetupFilteredBranches((highRating, 0.0), (lowRating, 0.0));
            SetupDashboard(10, ("Gà rán", 500));

            var results = await _service.SearchAsync(new SearchFilterDto { Keyword = "KFC" });

            Assert.Single(results);
            var primary = results[0].Branches[0];
            Assert.Equal(1, primary.BranchId);
            Assert.Single(primary.OtherBranches);
            Assert.Equal(2, primary.OtherBranches[0].BranchId);
        }

        // ----- DisplayName beats dish match (PRD Scenario 1) -------------------

        [Fact]
        public async Task ShopMatchingByName_OutranksShopMatchingOnlyByDish()
        {
            var shopA = MakeBranch(1, 10, "Pizza Hut", "Q1", dishes: new[] { "Pepperoni" });
            var shopB = MakeBranch(2, 20, "Random Diner", "Q3", dishes: new[] { "pizza margherita" });
            SetupFilteredBranches((shopA, 1.0), (shopB, 0.2));
            SetupEmptyDashboardForAll();

            var results = await _service.SearchAsync(new SearchFilterDto { Keyword = "pizza", Lat = 10.77, Long = 106.70 });

            Assert.Equal(2, results.Count);
            Assert.Equal("Pizza Hut", results[0].VendorName);
            Assert.Equal("Random Diner", results[1].VendorName);
        }
    }
}
