using BackTest.Middleware;
using BackTest.Models;
using BackTest.Repositories;
using BackTest.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddDbContext<AssetManagementDbContext>(options =>
    options.UseInMemoryDatabase("AssetManagementTestDb"));

builder.Services.AddScoped<IAssetEntityRepository, AssetEntityRepository>();
builder.Services.AddScoped<IAssetEntityService, AssetEntityService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AssetManagementDbContext>();

    // Create tenants
    var tenant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    var tenant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

    context.Tenants.AddRange(
        new Tenant { Tenant_Id = tenant1Id, TenantName = "Acme Bank", TenantCode = "ACME" },
        new Tenant { Tenant_Id = tenant2Id, TenantName = "Beta Finance", TenantCode = "BETA" }
    );

    // Create entities for tenant 1
    var legalEntity1 = new LegalEntity
    {
        Entity_Id = Guid.Parse("aaaa0001-0000-0000-0000-000000000001"),
        EntityReference = "LE-001",
        LegalName = "Global Holdings Ltd",
        TradeName = "GHL",
        EntityType = "legal",
        RiskLevel = "medium",
        RegistrationNumber = "REG-2024-001",
        Jurisdiction_Iso3 = "LUX",
        Tags = new List<string> { "holding", "eu-regulated" },
        Tenant_Id = tenant1Id,
        PreferredLanguage = "en"
    };

    var legalEntity2 = new LegalEntity
    {
        Entity_Id = Guid.Parse("aaaa0001-0000-0000-0000-000000000002"),
        EntityReference = "LE-002",
        LegalName = "Tech Ventures Inc",
        TradeName = "TechV",
        EntityType = "legal",
        RiskLevel = "low",
        RegistrationNumber = "REG-2024-002",
        Jurisdiction_Iso3 = "USA",
        Tags = new List<string> { "technology", "startup" },
        Tenant_Id = tenant1Id,
        PreferredLanguage = "en"
    };

    var naturalEntity1 = new NaturalEntity
    {
        Entity_Id = Guid.Parse("bbbb0001-0000-0000-0000-000000000001"),
        EntityReference = "NE-001",
        FirstName = "John",
        LastName = "Smith",
        EntityType = "natural",
        RiskLevel = "low",
        DateOfBirth = new DateTime(1985, 6, 15),
        Nationality_Iso3 = "GBR",
        Tags = new List<string> { "director", "pep" },
        Tenant_Id = tenant1Id,
        PreferredLanguage = "en"
    };

    var naturalEntity2 = new NaturalEntity
    {
        Entity_Id = Guid.Parse("bbbb0001-0000-0000-0000-000000000002"),
        EntityReference = "NE-002",
        FirstName = "Maria",
        LastName = "Garcia",
        EntityType = "natural",
        RiskLevel = "high",
        DateOfBirth = new DateTime(1990, 3, 22),
        Nationality_Iso3 = "ESP",
        Tags = new List<string> { "ubo", "under-review" },
        Tenant_Id = tenant1Id,
        PreferredLanguage = "es",
        IsUnderReview = true
    };

    var legalEntity3 = new LegalEntity
    {
        Entity_Id = Guid.Parse("aaaa0002-0000-0000-0000-000000000001"),
        EntityReference = "LE-003",
        LegalName = "Separate Corp",
        EntityType = "legal",
        RiskLevel = "critical",
        Tenant_Id = tenant2Id,
        PreferredLanguage = "fr",
        IsLocked = true
    };

    context.AssetEntities.AddRange(legalEntity1, legalEntity2, naturalEntity1, naturalEntity2, legalEntity3);

    // Create relationships
    var rel1 = new Relationship
    {
        Relationship_Id = Guid.Parse("cccc0001-0000-0000-0000-000000000001"),
        RelationshipType = "directorship",
        Role = "Director",
        SourceEntity_Id = naturalEntity1.Entity_Id,
        SourceEntity = naturalEntity1,
        TargetEntity_Id = legalEntity1.Entity_Id,
        TargetEntity = legalEntity1,
        IsBidirectional = false,
        Tenant_Id = tenant1Id
    };

    var rel2 = new Relationship
    {
        Relationship_Id = Guid.Parse("cccc0001-0000-0000-0000-000000000002"),
        RelationshipType = "ownership",
        Role = "UBO",
        SourceEntity_Id = naturalEntity2.Entity_Id,
        SourceEntity = naturalEntity2,
        TargetEntity_Id = legalEntity1.Entity_Id,
        TargetEntity = legalEntity1,
        IsBidirectional = false,
        Tenant_Id = tenant1Id
    };

    var rel3 = new Relationship
    {
        Relationship_Id = Guid.Parse("cccc0001-0000-0000-0000-000000000003"),
        RelationshipType = "subsidiary",
        Role = "Parent",
        SourceEntity_Id = legalEntity1.Entity_Id,
        SourceEntity = legalEntity1,
        TargetEntity_Id = legalEntity2.Entity_Id,
        TargetEntity = legalEntity2,
        IsBidirectional = false,
        Tenant_Id = tenant1Id
    };

    var deletedRel = new Relationship
    {
        Relationship_Id = Guid.Parse("cccc0001-0000-0000-0000-000000000004"),
        RelationshipType = "partnership",
        Role = "Former Partner",
        SourceEntity_Id = legalEntity1.Entity_Id,
        SourceEntity = legalEntity1,
        TargetEntity_Id = legalEntity2.Entity_Id,
        TargetEntity = legalEntity2,
        IsBidirectional = true,
        Tenant_Id = tenant1Id,
        IsDeleted = true,
        DeletedAt = DateTime.UtcNow.AddDays(-30)
    };

    context.Relationships.AddRange(rel1, rel2, rel3, deletedRel);

    // Create assets
    var realEstate1 = new RealEstate
    {
        Asset_Id = Guid.Parse("dddd0001-0000-0000-0000-000000000001"),
        AssetName = "Luxembourg Office",
        AssetType = "realEstate",
        Location = "Luxembourg City, Luxembourg",
        MarketValue = 2500000m,
        BuildingSize = 450.5,
        EstimatedValue = 2500000m,
        Currency = "EUR",
        Tenant_Id = tenant1Id
    };

    var stock1 = new Stock
    {
        Asset_Id = Guid.Parse("dddd0001-0000-0000-0000-000000000002"),
        AssetName = "Tech Corp Shares",
        AssetType = "stock",
        StockSymbol = "TECH",
        NumberOfShares = 10000,
        CurrentPrice = 150.50m,
        EstimatedValue = 1505000m,
        Currency = "USD",
        Tenant_Id = tenant1Id
    };

    context.Assets.AddRange(realEstate1, stock1);

    // Create ownerships
    var ownership1 = new AssetOwnership
    {
        Ownership_Id = Guid.Parse("eeee0001-0000-0000-0000-000000000001"),
        Entity_Id = legalEntity1.Entity_Id,
        Entity = legalEntity1,
        Asset_Id = realEstate1.Asset_Id,
        Asset = realEstate1,
        OwnershipPercentage = 75.0f,
        AcquisitionDate = new DateTime(2023, 1, 15),
        Tenant_Id = tenant1Id
    };

    var ownership2 = new AssetOwnership
    {
        Ownership_Id = Guid.Parse("eeee0001-0000-0000-0000-000000000002"),
        Entity_Id = legalEntity2.Entity_Id,
        Entity = legalEntity2,
        Asset_Id = realEstate1.Asset_Id,
        Asset = realEstate1,
        OwnershipPercentage = 40.0f,
        AcquisitionDate = new DateTime(2023, 6, 1),
        Tenant_Id = tenant1Id
    };

    context.AssetOwnerships.AddRange(ownership1, ownership2);

    // Create entity positions for graph view
    context.EntityPositions.AddRange(
        new EntityPosition { Entity_Id = legalEntity1.Entity_Id, Entity = legalEntity1, X = 400, Y = 200 },
        new EntityPosition { Entity_Id = legalEntity2.Entity_Id, Entity = legalEntity2, X = 600, Y = 400 },
        new EntityPosition { Entity_Id = naturalEntity1.Entity_Id, Entity = naturalEntity1, X = 200, Y = 100 },
        new EntityPosition { Entity_Id = naturalEntity2.Entity_Id, Entity = naturalEntity2, X = 200, Y = 300 }
    );

    context.SaveChanges();
}

app.Run();
