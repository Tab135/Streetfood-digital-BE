using BO.Common;
using BO.DTO.Quest;
using FluentAssertions;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests.E2E;

/// <summary>
/// E2E tests for the customer-facing Quest flow.
///
/// Flow under test:
///   1. GET  /api/Quest/public?campaignId={id}       — browse quests for a campaign
///   2. GET  /api/Quest/campaign/{id}/my-progress    — check enrollment (empty before enrolling)
///   3. POST /api/Quest/{questId}/enroll             — enroll in a quest
///   4. GET  /api/Quest/campaign/{id}/my-progress    — verify IN_PROGRESS with 0 completed tasks
///   5. POST /api/Quest/checkin/{branchId}            — record a visit (advances VISIT task)
///   6. GET  /api/Quest/campaign/{id}/my-progress    — verify VISIT task has progressed
/// </summary>
public class QuestFlowE2ETests : IClassFixture<StreetFoodWebFactory>
{
    private readonly StreetFoodWebFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public QuestFlowE2ETests(StreetFoodWebFactory factory)
    {
        _factory = factory;
        _factory.EnsureSeeded();
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private async Task<T?> ReadData<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        // Responses are wrapped: { status, message, data, errorCode }
        using var doc = JsonDocument.Parse(json);
        var dataElement = doc.RootElement.GetProperty("data");
        return JsonSerializer.Deserialize<T>(dataElement.GetRawText(), JsonOpts);
    }

    // ── 1. Public quest listing ───────────────────────────────────────────

    [Fact]
    public async Task GetPublicQuests_NoCampaignFilter_ReturnsSeededQuest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Quest/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ReadData<PaginatedResponse<QuestResponseDto>>(response);
        data.Should().NotBeNull();
        data!.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        data.Items.Should().Contain(q => q.QuestId == StreetFoodWebFactory.QuestId);
    }

    [Fact]
    public async Task GetPublicQuests_WithMatchingCampaignId_ReturnsOnlyLinkedQuest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/Quest/public?campaignId={StreetFoodWebFactory.CampaignId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ReadData<PaginatedResponse<QuestResponseDto>>(response);
        data!.Items.Should().HaveCount(1);
        var quest = data.Items[0];
        quest.QuestId.Should().Be(StreetFoodWebFactory.QuestId);
        quest.Title.Should().Be("Khám Phá Ẩm Thực Đường Phố HCM");
        quest.Tasks.Should().HaveCount(2);
        quest.Tasks.Should().Contain(t => t.Type == "VISIT");
        quest.Tasks.Should().Contain(t => t.Type == "REVIEW");
    }

    [Fact]
    public async Task GetPublicQuests_WithNonMatchingCampaignId_ReturnsEmptyList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/Quest/public?campaignId=9999");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ReadData<PaginatedResponse<QuestResponseDto>>(response);
        data!.Items.Should().BeEmpty();
    }

    // ── 2. Authentication guard ───────────────────────────────────────────

    [Fact]
    public async Task GetCampaignQuestProgress_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnrollInQuest_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 3. Pre-enrollment state ───────────────────────────────────────────

    [Fact]
    public async Task GetCampaignQuestProgress_WhenNotEnrolled_ReturnsEmptyList()
    {
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);

        var response = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ReadData<List<UserQuestProgressDto>>(response);
        data.Should().BeEmpty();
    }

    // ── 4. Enrollment ─────────────────────────────────────────────────────

    [Fact]
    public async Task EnrollInQuest_Success_ReturnsProgressWithAllTasksAtZero()
    {
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);

        var response = await client.PostAsync(
            $"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var progress = await ReadData<UserQuestProgressDto>(response);
        progress.Should().NotBeNull();
        progress!.QuestId.Should().Be(StreetFoodWebFactory.QuestId);
        progress.Status.Should().Be("IN_PROGRESS");
        progress.CompletedTasks.Should().Be(0);
        progress.TotalTasks.Should().Be(2);
        progress.Tasks.Should().HaveCount(2);
        progress.Tasks.Should().AllSatisfy(t => t.CurrentValue.Should().Be(0));
        progress.Tasks.Should().AllSatisfy(t => t.IsCompleted.Should().BeFalse());
    }

    [Fact]
    public async Task EnrollInQuest_WhenAlreadyEnrolled_ReturnsBadRequest()
    {
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);

        // First enroll succeeds
        var first = await client.PostAsync(
            $"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second enroll must fail
        var second = await client.PostAsync(
            $"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 5. Post-enrollment progress ───────────────────────────────────────

    [Fact]
    public async Task GetCampaignQuestProgress_AfterEnroll_ReturnsInProgressEntry()
    {
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);

        await client.PostAsync($"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);

        var response = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ReadData<List<UserQuestProgressDto>>(response);
        data.Should().HaveCount(1);
        data![0].Status.Should().Be("IN_PROGRESS");
        data[0].CompletedTasks.Should().Be(0);
        data[0].TotalTasks.Should().Be(2);
    }

    // ── 6. Check-in advances VISIT task ──────────────────────────────────

    [Fact]
    public async Task CheckIn_AfterEnroll_AdvancesVisitTaskCurrentValue()
    {
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);
        await client.PostAsync($"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);

        // Perform one check-in (branchId is not validated by the endpoint)
        var checkIn = await client.PostAsync("/api/Quest/checkin/1", null);
        checkIn.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify VISIT task has advanced
        var progressResponse = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");
        var data = await ReadData<List<UserQuestProgressDto>>(progressResponse);
        var visitTask = data![0].Tasks.Single(t => t.Type == "VISIT");
        visitTask.CurrentValue.Should().Be(1);
        visitTask.IsCompleted.Should().BeFalse(); // target=2, only 1 done
    }

    [Fact]
    public async Task CheckIn_TwiceOnTargetOne_CompletesVisitTask()
    {
        // Re-seed a quest with VISIT target=1 is not available without extra setup,
        // so we check with 2 check-ins to reach target=2 instead.
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);
        await client.PostAsync($"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);

        await client.PostAsync("/api/Quest/checkin/1", null);
        await client.PostAsync("/api/Quest/checkin/1", null);

        var progressResponse = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");
        var data = await ReadData<List<UserQuestProgressDto>>(progressResponse);
        var visitTask = data![0].Tasks.Single(t => t.Type == "VISIT");
        visitTask.CurrentValue.Should().Be(2);
        visitTask.IsCompleted.Should().BeTrue();
        data[0].CompletedTasks.Should().Be(1); // VISIT done, REVIEW still pending
    }

    // ── 7. Full customer flow ─────────────────────────────────────────────

    [Fact]
    public async Task FullCustomerFlow_BrowseEnrollProgressCheckin()
    {
        var userId = _factory.SeedUser();
        var client = _factory.CreateAuthClient(userId);

        // Step 1 — browse quests for the campaign (public, no auth required)
        var questsResponse = await _factory.CreateClient().GetAsync(
            $"/api/Quest/public?campaignId={StreetFoodWebFactory.CampaignId}");
        questsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var quests = await ReadData<PaginatedResponse<QuestResponseDto>>(questsResponse);
        quests!.Items.Should().HaveCount(1);

        // Step 2 — check progress before enrolling → empty
        var preEnrollProgress = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");
        var preData = await ReadData<List<UserQuestProgressDto>>(preEnrollProgress);
        preData.Should().BeEmpty();

        // Step 3 — enroll
        var enrollResponse = await client.PostAsync(
            $"/api/Quest/{StreetFoodWebFactory.QuestId}/enroll", null);
        enrollResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var enrolled = await ReadData<UserQuestProgressDto>(enrollResponse);
        enrolled!.Status.Should().Be("IN_PROGRESS");

        // Step 4 — progress is now IN_PROGRESS with 0 completed tasks
        var postEnrollProgress = await client.GetAsync(
            $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress");
        var postEnrollData = await ReadData<List<UserQuestProgressDto>>(postEnrollProgress);
        postEnrollData.Should().HaveCount(1);
        postEnrollData![0].CompletedTasks.Should().Be(0);

        // Step 5 — first check-in (VISIT task: 1/2)
        (await client.PostAsync("/api/Quest/checkin/1", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var afterFirstCheckin = await ReadData<List<UserQuestProgressDto>>(
            await client.GetAsync(
                $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress"));
        afterFirstCheckin![0].Tasks.Single(t => t.Type == "VISIT").CurrentValue
            .Should().Be(1);
        afterFirstCheckin[0].CompletedTasks.Should().Be(0);

        // Step 6 — second check-in (VISIT task: 2/2 → completed, +50 points)
        (await client.PostAsync("/api/Quest/checkin/1", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var afterSecondCheckin = await ReadData<List<UserQuestProgressDto>>(
            await client.GetAsync(
                $"/api/Quest/campaign/{StreetFoodWebFactory.CampaignId}/my-progress"));
        var visitTask = afterSecondCheckin![0].Tasks.Single(t => t.Type == "VISIT");
        visitTask.IsCompleted.Should().BeTrue();
        afterSecondCheckin[0].CompletedTasks.Should().Be(1);
        afterSecondCheckin[0].Status.Should().Be("IN_PROGRESS"); // REVIEW still pending
    }
}
