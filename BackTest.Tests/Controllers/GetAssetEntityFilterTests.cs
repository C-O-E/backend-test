using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackTest.Models;
using BackTest.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackTest.Tests.Controllers;

/// <summary>
/// Component tests for GET /api/AssetEntity with AssetEntityFilterParameters.
/// Each test seeds 2 entities — one matching the filter, one not — calls GET
/// with the corresponding query param, and asserts only the matching entity
/// is returned.
///
/// Implements IAsyncLifetime: InitializeAsync wipes all TenantId rows before
/// each test for a clean slate; DisposeAsync removes what the test seeded.
/// </summary>
[Collection(SharedCollection.Name)]
public class GetAssetEntityFilterTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _factory;
    private readonly List<Guid> _seededIds = [];

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-333333333333");

    public GetAssetEntityFilterTests(WebAppFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
    }

    // -------------------------------------------------------------------------
    // IAsyncLifetime.InitializeAsync — runs BEFORE each test method.
    // Wipes all AssetEntities for TenantId so filter assertions are not
    // contaminated by rows left over from other tests.
    // -------------------------------------------------------------------------
    public async Task InitializeAsync()
    {
        using var ctx = GetDbContext();
        var existing = await ctx.AssetEntities
            .IgnoreQueryFilters()
            .Where(e => e.Tenant_Id == TenantId)
            .ToListAsync();
        ctx.AssetEntities.RemoveRange(existing);
        await ctx.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // IAsyncLifetime.DisposeAsync — runs AFTER each test method.
    // Removes only the IDs registered by the current test.
    // -------------------------------------------------------------------------
    public async Task DisposeAsync()
    {
        if (_seededIds.Count == 0) return;
        using var ctx = GetDbContext();
        var toDelete = await ctx.AssetEntities
            .IgnoreQueryFilters()
            .Where(e => _seededIds.Contains(e.Entity_Id))
            .ToListAsync();
        ctx.AssetEntities.RemoveRange(toDelete);
        await ctx.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // Helper — resolve a fresh DbContext scope from the test server's DI container
    // -------------------------------------------------------------------------
    private AssetManagementDbContext GetDbContext()
        => _factory.Services.CreateScope().ServiceProvider
               .GetRequiredService<AssetManagementDbContext>();

    // -------------------------------------------------------------------------
    // Seed helpers — each registers the new Guid so DisposeAsync can clean it up
    // -------------------------------------------------------------------------
    private async Task<Guid> SeedLegalEntityAsync(string legalName, string? riskLevel = null, List<string>? tags = null)
    {
        var id = Guid.NewGuid();
        using var ctx = GetDbContext();
        ctx.LegalEntities.Add(new LegalEntity
        {
            Entity_Id  = id,
            EntityType = "legal",
            LegalName  = legalName,
            RiskLevel  = riskLevel,
            Tags       = tags,
            Tenant_Id  = TenantId,
        });
        await ctx.SaveChangesAsync();
        _seededIds.Add(id);
        return id;
    }

    private async Task<Guid> SeedNaturalEntityAsync(string firstName, string? riskLevel = null, List<string>? tags = null)
    {
        var id = Guid.NewGuid();
        using var ctx = GetDbContext();
        ctx.NaturalEntities.Add(new NaturalEntity
        {
            Entity_Id  = id,
            EntityType = "natural",
            FirstName  = firstName,
            LastName   = "Test",
            RiskLevel  = riskLevel,
            Tags       = tags,
            Tenant_Id  = TenantId,
        });
        await ctx.SaveChangesAsync();
        _seededIds.Add(id);
        return id;
    }

    // -------------------------------------------------------------------------
    // Filter Test 1 — ?entityType=legal
    //
    // Seed:   1 LegalEntity + 1 NaturalEntity
    // GET ?entityType=legal
    // Expect: only the LegalEntity is returned; NaturalEntity is excluded
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Get_FilterByEntityType_ReturnsOnlyMatchingType()
    {
        // Arrange
        var legalId   = await SeedLegalEntityAsync("Filter Corp");
        var naturalId = await SeedNaturalEntityAsync("Alice");

        // Act
        var response = await _client.GetAsync("/api/AssetEntity?entityType=legal");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(results);

        // Assert — only the legal entity is returned
        var returnedIds = results.Select(e => e.GetProperty("entity_Id").GetGuid()).ToHashSet();
        Assert.Contains(legalId,       returnedIds);
        Assert.DoesNotContain(naturalId, returnedIds);
    }

    // -------------------------------------------------------------------------
    // Filter Test 2 — ?riskLevel=high
    //
    // Seed:   1 LegalEntity with riskLevel="high"
    //         1 LegalEntity with riskLevel="low"
    // GET ?riskLevel=high
    // Expect: only the high-risk entity is returned; low-risk entity is excluded
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Get_FilterByRiskLevel_ReturnsOnlyMatchingRiskLevel()
    {
        // Arrange
        var highRiskId = await SeedLegalEntityAsync("High Risk Corp", riskLevel: "high");
        var lowRiskId  = await SeedLegalEntityAsync("Low Risk Corp",  riskLevel: "low");

        // Act
        var response = await _client.GetAsync("/api/AssetEntity?riskLevel=high");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(results);

        // Assert — only the high-risk entity is returned
        var returnedIds = results.Select(e => e.GetProperty("entity_Id").GetGuid()).ToHashSet();
        Assert.Contains(highRiskId,    returnedIds);
        Assert.DoesNotContain(lowRiskId, returnedIds);
    }

    // -------------------------------------------------------------------------
    // Filter Test 3 — ?tag=vip
    //
    // Seed:   1 LegalEntity with Tags=["vip","active"]
    //         1 NaturalEntity with Tags=["standard"]
    // GET ?tag=vip
    // Expect: only the entity tagged "vip" is returned; the other is excluded
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Get_FilterByTag_ReturnsOnlyEntityWithMatchingTag()
    {
        // Arrange
        var vipId      = await SeedLegalEntityAsync("VIP Corp",      tags: ["vip", "active"]);
        var standardId = await SeedNaturalEntityAsync("Bob",          tags: ["standard"]);

        // Act
        var response = await _client.GetAsync("/api/AssetEntity?tag=vip");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(results);

        // Assert — only the vip-tagged entity is returned
        var returnedIds = results.Select(e => e.GetProperty("entity_Id").GetGuid()).ToHashSet();
        Assert.Contains(vipId,           returnedIds);
        Assert.DoesNotContain(standardId, returnedIds);
    }
}
