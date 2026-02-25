using System.Net;
using Allure.NUnit.Attributes;
using FluentAssertions;
using Scriptube.Automation.Api.Clients;
using Scriptube.Automation.Api.Tests;

namespace Scriptube.Automation.Tests.Api.Regression;

/// <summary>
/// Regression tests for remaining API endpoints not covered by earlier test classes:
/// GET /api/v1/usage, GET /api/v1/credits/history (ignored, not live),
/// and GET /api/v1/transcripts/{batch_id} negative paths (non-existent and foreign IDs).
/// </summary>
[TestFixture]
[Category("Regression")]
[Category("API")]
[AllureSuite("Regression")]
[AllureFeature("Transcripts")]
[AllureTag("Regression", "API", "Endpoints")]
public sealed class RemainingEndpointTests : BaseApiTest
{
    private UsageClient _usage = null!;
    private CreditsClient _credits = null!;
    private TranscriptsClient _transcripts = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _usage = new UsageClient(Settings);
        _credits = new CreditsClient(Settings);
        _transcripts = new TranscriptsClient(Settings);
    }

    [TearDown]
    public override async Task TearDown()
    {
        _usage.Dispose();
        _credits.Dispose();
        _transcripts.Dispose();
        await base.TearDown();
    }

    // GET /api/v1/usage

    [Test]
    [AllureStep("GET /api/v1/usage — HTTP 200, plan name present, numeric quotas non-negative")]
    public async Task GetUsage_ReturnsHttp200_WithValidQuotaFields()
    {
        var response = await _usage.GetUsageAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "GET /api/v1/usage must return HTTP 200 for an authenticated user");
        response.Data.Should().NotBeNull();

        var data = response.Data!;

        data.Plan.Should().NotBeNullOrWhiteSpace(
            because: "usage response must identify the current plan");
        data.DailyUsed.Should().BeGreaterThanOrEqualTo(0,
            because: "daily_used is a non-negative counter");
        data.DailyLimit.Should().BeGreaterThanOrEqualTo(0,
            because: "daily_limit is a non-negative quota");
        data.DailyRemaining.Should().BeGreaterThanOrEqualTo(0,
            because: "daily_remaining is a non-negative value");
        data.TotalProcessed.Should().BeGreaterThanOrEqualTo(0,
            because: "total_processed is a non-negative cumulative counter");
    }

    // GET /api/v1/credits/history

    [Test]
    [Ignore("GET /api/v1/credits/history is not yet live on the public API.")]
    [AllureStep("GET /api/v1/credits/history — HTTP 200, transactions list present")]
    public async Task GetCreditHistory_ReturnsHttp200_WithTransactionsList()
    {
        var response = await _credits.GetHistoryAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "GET /api/v1/credits/history must return HTTP 200 for an authenticated user");
        response.Data.Should().NotBeNull();

        var data = response.Data!;

        data.Transactions.Should().NotBeNull(
            because: "transactions list must be present even if empty");
        data.Total.Should().BeGreaterThanOrEqualTo(0,
            because: "total is a non-negative count of transaction records");

        if (data.Transactions.Count > 0)
        {
            var first = data.Transactions[0];
            first.TransactionId.Should().NotBeNullOrWhiteSpace(
                because: "each transaction must have a non-empty ID");
            first.Type.Should().NotBeNullOrWhiteSpace(
                because: "each transaction must have a type (e.g. 'debit', 'credit')");
        }
    }

    // GET /api/v1/transcripts/{batch_id} negative paths

    [Test]
    [AllureStep("GET /api/v1/transcripts/{batch_id} with non-existent ID — HTTP 404")]
    public async Task GetBatch_NonExistentId_Returns404()
    {
        // Use the nil UUID so the ID passes format validation but cannot exist in the database.
        const string nonExistentBatchId = "00000000-0000-0000-0000-000000000000";

        var response = await _transcripts.GetBatchAsync(nonExistentBatchId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: "requesting a batch that does not exist must return HTTP 404");
    }

    [Test]
    [AllureStep("GET /api/v1/transcripts/{batch_id} with a well-formed but foreign batch ID — HTTP 404")]
    public async Task GetBatch_ForeignBatchId_Returns404()
    {
        // A well-formed UUID that is extremely unlikely to belong to the test account.
        // The API must treat any batch not owned by the authenticated user as not found
        // rather than returning 403, to avoid leaking batch existence to other users.
        const string foreignBatchId = "00000000-0000-0000-0000-000000000001";

        var response = await _transcripts.GetBatchAsync(foreignBatchId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            because: "a batch owned by another user must not be visible to the current user — " +
                     "the API must return 404 to avoid disclosing that the resource exists");
    }
}
