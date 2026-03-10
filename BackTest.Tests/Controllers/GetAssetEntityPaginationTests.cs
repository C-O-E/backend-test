using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BackTest.Models;
using BackTest.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackTest.Tests.Controllers;

/// <summary>
/// Component tests for GET /api/AssetEntity with PaginationParameters.
/// Implements IAsyncLifetime so InitializeAsync wipes all TenantId rows before
/// each test, guaranteeing a clean slate for pagination count assertions.
/// DisposeAsync removes only the IDs seeded by the current test.
/// </summary>
[Collection(SharedCollection.Name)]
public class GetAssetEntityPaginationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _factory;
    private readonly List<Guid> _seededIds = [];

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-333333333333");

    public GetAssetEntityPaginationTests(WebAppFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
    }

    // -------------------------------------------------------------------------
    // IAsyncLifetime.InitializeAsync — runs BEFORE each test method.
    // Removes all AssetEntities belonging to TenantId so every test starts
    // with an empty slate; pagination counts are then fully predictable.
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
    // Removes only the IDs registered by the current test via SeedAsync helpers.
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
    // Seed helpers — register each Guid so DisposeAsync can clean them up
    // -------------------------------------------------------------------------
    private async Task<Guid> SeedLegalEntityAsync(string legalName)
    {
        var id = Guid.NewGuid();
        using var ctx = GetDbContext();
        ctx.LegalEntities.Add(new LegalEntity
        {
            Entity_Id  = id,
            EntityType = "legal",
            LegalName  = legalName,
            Tenant_Id  = TenantId,
        });
        await ctx.SaveChangesAsync();
        _seededIds.Add(id);
        return id;
    }

    private async Task<Guid> SeedNaturalEntityAsync(string firstName, string lastName)
    {
        var id = Guid.NewGuid();
        using var ctx = GetDbContext();
        ctx.NaturalEntities.Add(new NaturalEntity
        {
            Entity_Id  = id,
            EntityType = "natural",
            FirstName  = firstName,
            LastName   = lastName,
            Tenant_Id  = TenantId,
        });
        await ctx.SaveChangesAsync();
        _seededIds.Add(id);
        return id;
    }

    // -------------------------------------------------------------------------
    // OK Test 1 — page=1&pageSize=1 then page=2&pageSize=1
    //
    // Seed:   1 LegalEntity + 1 NaturalEntity
    // InitializeAsync guarantees DB has exactly these 2 rows for TenantId.
    // GET ?page=1&pageSize=1  → 200 OK, exactly 1 entity returned
    // GET ?page=2&pageSize=1  → 200 OK, exactly 1 entity returned
    // Assert: union of both pages == { legalId, naturalId } exactly
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Get_WithPageSize1_TwoPages_UnionEqualsSeededIds()
    {
        // Arrange — InitializeAsync already wiped the DB; seed exactly 2 rows
        var legalId   = await SeedLegalEntityAsync("Pagination Corp");
        var naturalId = await SeedNaturalEntityAsync("Page", "Turner");
        var seededIds = new HashSet<Guid> { legalId, naturalId };

        // Act — page 1
        var response1 = await _client.GetAsync("/api/AssetEntity?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var page1 = await response1.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(page1);
        Assert.Single(page1);

        // Act — page 2
        var response2 = await _client.GetAsync("/api/AssetEntity?page=2&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var page2 = await response2.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(page2);
        Assert.Single(page2);

        // Assert — union of both pages == exactly the two seeded IDs (order-independent)
        var returnedIds = new HashSet<Guid>
        {
            page1[0].GetProperty("entity_Id").GetGuid(),
            page2[0].GetProperty("entity_Id").GetGuid(),
        };
        Assert.Equal(seededIds, returnedIds);
    }

    // -------------------------------------------------------------------------
    // OK Test 2 — page=1 with no pageSize (returns all entities)
    //
    // Seed:   1 LegalEntity + 1 NaturalEntity
    // InitializeAsync guarantees DB has exactly these 2 rows for TenantId.
    // GET ?page=1 (no pageSize) → 200 OK, both seeded IDs present in response
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Get_WithNoPageSize_ReturnsAllSeededEntities()
    {
        // Arrange — InitializeAsync already wiped the DB; seed exactly 2 rows
        var legalId   = await SeedLegalEntityAsync("AllPages Corp");
        var naturalId = await SeedNaturalEntityAsync("All", "Pages");

        // Act
        var response = await _client.GetAsync("/api/AssetEntity?page=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(results);

        // Assert — both seeded IDs are present (order-independent)
        var returnedIds = results.Select(e => e.GetProperty("entity_Id").GetGuid()).ToHashSet();
        Assert.Contains(legalId,   returnedIds);
        Assert.Contains(naturalId, returnedIds);
    }
}
