using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BackTest.Models;
using BackTest.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BackTest.Tests.Controllers;

/// <summary>
/// Component tests for POST /api/AssetEntity.
/// Each test calls the HTTP endpoint, captures the returned Guid,
/// then reads the EF DB to assert that the correct entity type and
/// fields were persisted.
/// </summary>
[Collection(SharedCollection.Name)]
public class CreateAssetEntityTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly WebAppFixture _factory;
    private readonly List<Guid> _seededIds = [];

    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-333333333333");

    public CreateAssetEntityTests(WebAppFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", TenantId.ToString());
    }

    // IAsyncLifetime.InitializeAsync — nothing to prepare before each test
    public Task InitializeAsync() => Task.CompletedTask;

    // IAsyncLifetime.DisposeAsync — remove every entity created during this test
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
    // OK Test 1 — POST with type = "legal"
    //
    // Request: { "type": "legal", "entityType": "legal",
    //            "legalName": "Acme Corp", "tradeName": "Acme",
    //            "registrationNumber": "REG-2024-001" }
    // Expect:  201 Created  with a Location header containing the new entity id
    //          DB contains a LegalEntity row with matching fields and correct TenantId
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Post_LegalEntity_Created_And_PersistedInDb()
    {
        // Arrange
        var requestBody = new
        {
            type           = "legal",
            entityType     = "legal",
            legalName      = "Acme Corp",
            tradeName      = "Acme",
            registrationNumber = "REG-2024-001",
        };

        var json    = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/AssetEntity", content);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Capture the new entity's Guid from the response body
        var created  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entityId = created.GetProperty("entity_Id").GetGuid();
        _seededIds.Add(entityId); // DisposeAsync will clean this up

        // Assert DB — entity exists as a LegalEntity with correct fields
        // IgnoreQueryFilters: the test DbContext scope has no tenant set so the
        // global tenant filter would otherwise filter out the row.
        using var ctx = GetDbContext();
        var persisted = await ctx.LegalEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(e => e.Entity_Id == entityId);

        Assert.Equal("legal",          persisted.EntityType);
        Assert.Equal("Acme Corp",       persisted.LegalName);
        Assert.Equal("Acme",            persisted.TradeName);
        Assert.Equal("REG-2024-001",    persisted.RegistrationNumber);
        Assert.Equal(TenantId,          persisted.Tenant_Id);
    }

    // -------------------------------------------------------------------------
    // OK Test 2 — POST with type = "natural"
    //
    // Request: { "type": "natural", "entityType": "natural",
    //            "firstName": "Alice", "lastName": "Smith",
    //            "nationality_Iso3": "FRA" }
    // Expect:  201 Created with a Location header containing the new entity id
    //          DB contains a NaturalEntity row with matching fields and correct TenantId
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Post_NaturalEntity_Created_And_PersistedInDb()
    {
        // Arrange
        var requestBody = new
        {
            type             = "natural",
            entityType       = "natural",
            firstName        = "Alice",
            lastName         = "Smith",
            nationality_Iso3 = "FRA",
        };

        var json    = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/AssetEntity", content);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Capture the new entity's Guid from the response body
        var created  = await response.Content.ReadFromJsonAsync<JsonElement>();
        var entityId = created.GetProperty("entity_Id").GetGuid();
        _seededIds.Add(entityId); // DisposeAsync will clean this up

        // Assert DB — entity exists as a NaturalEntity with correct fields
        // IgnoreQueryFilters: the test DbContext scope has no tenant set so the
        // global tenant filter would otherwise filter out the row.
        using var ctx = GetDbContext();
        var persisted = await ctx.NaturalEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleAsync(e => e.Entity_Id == entityId);

        Assert.Equal("natural", persisted.EntityType);
        Assert.Equal("Alice",   persisted.FirstName);
        Assert.Equal("Smith",   persisted.LastName);
        Assert.Equal("FRA",     persisted.Nationality_Iso3);
        Assert.Equal(TenantId,  persisted.Tenant_Id);
    }

    // -------------------------------------------------------------------------
    // KO Test 1 — POST body has no "type" discriminator
    //
    // Expect: 400 Bad Request
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Post_NoTypeDiscriminator_ReturnsBadRequest()
    {
        // Arrange — valid-looking body but without the "type" property
        var json    = """{ "entityType": "legal", "legalName": "Ghost Corp" }""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/AssetEntity", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // KO Test 2 — POST without the X-Tenant-Id header
    //
    // TenantResolutionMiddleware leaves HttpContext.Items["TenantId"] unset
    // Expect: 400 Bad Request
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Post_NoTenantIdHeader_ReturnsBadRequest()
    {
        // Arrange — create a fresh client WITHOUT the X-Tenant-Id header
        var clientWithoutTenant = _factory.CreateClient();

        var requestBody = new
        {
            type       = "legal",
            entityType = "legal",
            legalName  = "No Tenant Corp",
        };

        var json    = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await clientWithoutTenant.PostAsync("/api/AssetEntity", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("TenantId is required", body, StringComparison.OrdinalIgnoreCase);
    }
}
