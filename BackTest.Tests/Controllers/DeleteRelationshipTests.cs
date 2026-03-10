using System.Net;
using BackTest.Models;
using BackTest.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackTest.Tests.Controllers;

/// <summary>
/// Component tests for DELETE /api/AssetEntity/relationships/{relationshipId}.
///
/// Implements IAsyncLifetime: InitializeAsync is a no-op; DisposeAsync removes
/// all entity and relationship rows seeded by the current test.
/// </summary>
[Collection(SharedCollection.Name)]
public class DeleteRelationshipTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _factory;
    private readonly List<Guid> _seededEntityIds      = [];
    private readonly List<Guid> _seededRelationshipIds = [];

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-333333333333");

    public DeleteRelationshipTests(WebAppFixture factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
    }

    // IAsyncLifetime.InitializeAsync — nothing to prepare before each test
    public Task InitializeAsync() => Task.CompletedTask;

    // -------------------------------------------------------------------------
    // IAsyncLifetime.DisposeAsync — removes all relationships then entities
    // seeded by the current test (relationships must be removed first due to FK).
    // -------------------------------------------------------------------------
    public async Task DisposeAsync()
    {
        using var ctx = GetDbContext();

        if (_seededRelationshipIds.Count > 0)
        {
            var relationships = await ctx.Relationships
                .IgnoreQueryFilters()
                .Where(r => _seededRelationshipIds.Contains(r.Relationship_Id))
                .ToListAsync();
            ctx.Relationships.RemoveRange(relationships);
        }

        if (_seededEntityIds.Count > 0)
        {
            var entities = await ctx.AssetEntities
                .IgnoreQueryFilters()
                .Where(e => _seededEntityIds.Contains(e.Entity_Id))
                .ToListAsync();
            ctx.AssetEntities.RemoveRange(entities);
        }

        await ctx.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // Helper — resolve a fresh DbContext scope from the test server's DI container
    // -------------------------------------------------------------------------
    private AssetManagementDbContext GetDbContext()
        => _factory.Services.CreateScope().ServiceProvider
               .GetRequiredService<AssetManagementDbContext>();

    // -------------------------------------------------------------------------
    // Seed helper — creates two LegalEntities and one Relationship between them.
    // Registers all three IDs for cleanup in DisposeAsync.
    // -------------------------------------------------------------------------
    private async Task<Guid> SeedRelationshipAsync()
    {
        using var ctx = GetDbContext();

        var source = new LegalEntity
        {
            Entity_Id  = Guid.NewGuid(),
            EntityType = "legal",
            LegalName  = "Source Corp",
            Tenant_Id  = TenantId,
        };
        var target = new LegalEntity
        {
            Entity_Id  = Guid.NewGuid(),
            EntityType = "legal",
            LegalName  = "Target Corp",
            Tenant_Id  = TenantId,
        };
        ctx.LegalEntities.AddRange(source, target);
        await ctx.SaveChangesAsync();
        _seededEntityIds.Add(source.Entity_Id);
        _seededEntityIds.Add(target.Entity_Id);

        var relationship = new Relationship
        {
            Relationship_Id  = Guid.NewGuid(),
            RelationshipType = "management",
            SourceEntity_Id  = source.Entity_Id,
            SourceEntity     = source,
            TargetEntity_Id  = target.Entity_Id,
            TargetEntity     = target,
            IsBidirectional  = false,
        };
        ctx.Relationships.Add(relationship);
        await ctx.SaveChangesAsync();
        _seededRelationshipIds.Add(relationship.Relationship_Id);

        return relationship.Relationship_Id;
    }

    // -------------------------------------------------------------------------
    // OK Test — DELETE an existing relationship
    //
    // Seed:   1 Relationship between 2 LegalEntities
    // DELETE /api/AssetEntity/relationships/{relationshipId}
    // Expect: 204 No Content
    //         Relationship no longer exists in DB (soft-deleted: IsDeleted == true)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Delete_ExistingRelationship_ReturnsNoContent_AndSoftDeleted()
    {
        // Arrange
        var relationshipId = await SeedRelationshipAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/AssetEntity/relationships/{relationshipId}");

        // Assert HTTP
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Assert DB — relationship is soft-deleted (IsDeleted == true)
        using var ctx = GetDbContext();
        var deleted = await ctx.Relationships
            .IgnoreQueryFilters()
            .SingleAsync(r => r.Relationship_Id == relationshipId);

        Assert.True(deleted.IsDeleted);
    }

    // -------------------------------------------------------------------------
    // KO Test — DELETE a relationship that does not exist
    //
    // DELETE /api/AssetEntity/relationships/{randomId}
    // Expect: 404 Not Found
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Delete_NonExistentRelationship_ReturnsNotFound()
    {
        // Arrange — a Guid that was never seeded
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/AssetEntity/relationships/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
