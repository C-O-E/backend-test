using System.Net;
using System.Net.Http.Json;
using BackTest.DTOs;
using BackTest.Models;
using BackTest.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackTest.Tests.Controllers;

/// <summary>
/// Component tests for PUT /api/AssetEntity/{id}.
/// Each test seeds an entity directly into the EF DB, calls the HTTP endpoint,
/// then re-reads the DB to assert field-level changes.
/// </summary>
[Collection(SharedCollection.Name)]
public class UpdateAssetEntityTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _factory;
    private readonly List<Guid> _seededIds = [];

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-333333333333");

    public UpdateAssetEntityTests(WebAppFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
    }

    // IAsyncLifetime.InitializeAsync — nothing to prepare before each test
    public Task InitializeAsync() => Task.CompletedTask;

    // IAsyncLifetime.DisposeAsync — unlock then remove every entity seeded by this test
    public async Task DisposeAsync()
    {
        if (_seededIds.Count == 0) return;
        using var ctx = GetDbContext();
        var toDelete = await ctx.AssetEntities
            .IgnoreQueryFilters()
            .Where(e => _seededIds.Contains(e.Entity_Id))
            .ToListAsync();
        foreach (var e in toDelete) e.IsLocked = false; // unlock before delete
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
    // Test 1 — updating a LegalEntity: legal name changes, natural-specific
    //          fields (FirstName, LastName) are ignored
    //
    // Seed:   LegalEntity { LegalName = "Old Corp", ... }
    // PUT:    { LegalName = "New Corp", FirstName = "NOISE", LastName = "NOISE" }
    // Expect: LegalName == "New Corp"  (changed)
    //         DB row has NO FirstName / LastName columns (TPH — natural fields
    //         belong to NaturalEntity rows only)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Put_LegalEntity_LegalNameUpdated_NaturalFieldsIgnored()
    {
        // Arrange — generate a unique Id and seed a LegalEntity
        var entityId = Guid.NewGuid();
        var original = new LegalEntity
        {
            Entity_Id  = entityId,
            EntityType = "legal",
            LegalName  = "Old Corp",
            TradeName  = "Old Trade",
            Tenant_Id  = TenantId,
        };

        using (var ctx = GetDbContext())
        {
            ctx.LegalEntities.Add(original);
            await ctx.SaveChangesAsync();
        }
        _seededIds.Add(entityId); // DisposeAsync will clean this up

        // Act — PUT with a new LegalName plus two natural-specific noise fields
        var request = new UpdateAssetEntityRequest
        {
            LegalName = "New Corp",    // should be applied
            FirstName = "NOISE",       // natural-specific — must be ignored for legal entity
            LastName  = "NOISE",       // natural-specific — must be ignored for legal entity
        };

        var response = await _client.PutAsJsonAsync($"/api/AssetEntity/{entityId}", request);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert DB — re-read the entity as LegalEntity
        // IgnoreQueryFilters: the test DbContext scope has no tenant set so the
        // global tenant filter would otherwise filter out the row.
        using var assertCtx = GetDbContext();
        var updated = await assertCtx.LegalEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(e => e.Entity_Id == entityId);

        Assert.Equal("New Corp", updated.LegalName);    // changed
        Assert.Equal("Old Trade", updated.TradeName);   // untouched
    }

    // -------------------------------------------------------------------------
    // Test 2 — updating a NaturalEntity: first name changes, legal-specific
    //          field (LegalName) is ignored
    //
    // Seed:   NaturalEntity { FirstName = "Alice", LastName = "Smith", ... }
    // PUT:    { FirstName = "Bob", LegalName = "NOISE" }
    // Expect: FirstName == "Bob"        (changed)
    //         LastName  == "Smith"      (unchanged)
    //         LegalName field does not exist on NaturalEntity (ApplyTo() skips it)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Put_NaturalEntity_FirstNameUpdated_LegalFieldIgnored()
    {
        // Generate a unique Id and seed a NaturalEntity
        var entityId = Guid.NewGuid();
        var original = new NaturalEntity
        {
            Entity_Id  = entityId,
            EntityType = "natural",
            FirstName  = "Alice",
            LastName   = "Smith",
            Tenant_Id  = TenantId,
        };

        using (var ctx = GetDbContext())
        {
            ctx.NaturalEntities.Add(original);
            await ctx.SaveChangesAsync();
        }
        _seededIds.Add(entityId); // DisposeAsync will clean this up

        // PUT with a new FirstName plus one legal-specific noise field
        var request = new UpdateAssetEntityRequest
        {
            FirstName = "Bob",          // should be applied
            LegalName = "NOISE Corp",   // legal-specific — must be ignored for natural entity
        };

        var response = await _client.PutAsJsonAsync($"/api/AssetEntity/{entityId}", request);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert DB — re-read the entity as NaturalEntity
        // IgnoreQueryFilters: the test DbContext scope has no tenant set so the
        // global tenant filter would otherwise filter out the row.
        using var assertCtx = GetDbContext();
        var updated = await assertCtx.NaturalEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(e => e.Entity_Id == entityId);

        Assert.Equal("Bob",   updated.FirstName);   // changed
        Assert.Equal("Smith", updated.LastName);    // untouched
    }

    // -------------------------------------------------------------------------
    // KO Test — PUT on a locked NaturalEntity must be rejected
    //
    // Seed:   NaturalEntity { FirstName = "Carlos", IsLocked = true, ... }
    // PUT:    { FirstName = "CHANGED" }
    // Expect: 500 Internal Server Error (could improve to 403 or 409 later)
    //         DB row still has FirstName == "Carlos" (unchanged)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Put_LockedNaturalEntity_ReturnsInternalServerError_AndFieldUnchanged()
    {
        // Arrange — seed a locked NaturalEntity
        var entityId = Guid.NewGuid();
        var original = new NaturalEntity
        {
            Entity_Id  = entityId,
            EntityType = "natural",
            FirstName  = "Carlos",
            LastName   = "Locked",
            IsLocked   = true,
            Tenant_Id  = TenantId,
        };

        using (var ctx = GetDbContext())
        {
            ctx.NaturalEntities.Add(original);
            await ctx.SaveChangesAsync();
        }
        _seededIds.Add(entityId); // DisposeAsync will unlock + clean this up

        // Act — attempt to update a locked entity
        var request = new UpdateAssetEntityRequest
        {
            FirstName = "CHANGED",
        };

        var response = await _client.PutAsJsonAsync($"/api/AssetEntity/{entityId}", request);

        // Assert HTTP — must be rejected
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // Assert DB — field must remain unchanged
        // IgnoreQueryFilters: the test DbContext scope has no tenant set so the
        // global tenant filter would otherwise filter out the row.
        using var assertCtx = GetDbContext();
        var untouched = await assertCtx.NaturalEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(e => e.Entity_Id == entityId);

        Assert.Equal("Carlos", untouched.FirstName);    // not modified
        Assert.True(untouched.IsLocked);                // still locked
    }
}
