using BackTest.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace BackTest.Repositories;

public class AssetManagementDbContext : DbContext
{
    private Guid? _currentTenantId;

    public AssetManagementDbContext(DbContextOptions<AssetManagementDbContext> options) : base(options) { }

    public DbSet<AssetEntity> AssetEntities { get; set; }
    public DbSet<LegalEntity> LegalEntities { get; set; }
    public DbSet<NaturalEntity> NaturalEntities { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<RealEstate> RealEstates { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<IPAsset> IPAssets { get; set; }
    public DbSet<AssetOwnership> AssetOwnerships { get; set; }
    public DbSet<Relationship> Relationships { get; set; }
    public DbSet<EntityPosition> EntityPositions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    public void SetTenant(Guid? tenantId)
    {
        _currentTenantId = tenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPH discriminator configuration
        modelBuilder.Entity<AssetEntity>()
            .HasDiscriminator<string>("EntityDiscriminator")
            .HasValue<AssetEntity>("base")
            .HasValue<LegalEntity>("legal")
            .HasValue<NaturalEntity>("natural");

        modelBuilder.Entity<Asset>()
            .HasDiscriminator<string>("AssetDiscriminator")
            .HasValue<RealEstate>("realEstate")
            .HasValue<Stock>("stock")
            .HasValue<IPAsset>("ipAsset");

        // Value converter for Tags stored as JSON
        var stringListConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => v == null ? null : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
        );

        modelBuilder.Entity<AssetEntity>()
            .Property(e => e.Tags)
            .HasConversion(stringListConverter);

        // Relationship configuration
        modelBuilder.Entity<Relationship>()
            .HasOne(r => r.SourceEntity)
            .WithMany()
            .HasForeignKey(r => r.SourceEntity_Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Relationship>()
            .HasOne(r => r.TargetEntity)
            .WithMany(e => e.Relationships)
            .HasForeignKey(r => r.TargetEntity_Id)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filters
        modelBuilder.Entity<AssetEntity>().HasQueryFilter(e => !e.IsDeleted);
        // modelBuilder.Entity<Relationship>().HasQueryFilter(r => !r.IsDeleted);

        // Tenant isolation filter
        modelBuilder.Entity<AssetEntity>().HasQueryFilter(e => !e.IsDeleted && (e.Tenant_Id == _currentTenantId || _currentTenantId == null));

        // AssetOwnership relationships
        modelBuilder.Entity<AssetOwnership>()
            .HasOne(ao => ao.Entity)
            .WithMany()
            .HasForeignKey(ao => ao.Entity_Id)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssetOwnership>()
            .HasOne(ao => ao.Asset)
            .WithMany()
            .HasForeignKey(ao => ao.Asset_Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Entity Position
        modelBuilder.Entity<EntityPosition>()
            .HasOne(ep => ep.Entity)
            .WithMany(e => e.Positions)
            .HasForeignKey(ep => ep.Entity_Id)
            .OnDelete(DeleteBehavior.Cascade);

        // Tenant self-referencing hierarchy
        modelBuilder.Entity<Tenant>()
            .HasOne(t => t.ParentTenant)
            .WithMany(t => t.ChildTenants)
            .HasForeignKey(t => t.ParentTenant_Id)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<CommonBase>())
        {
            switch (entry.State)
            {
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.IsActive = false;
                    entry.Entity.DeletedAt = DateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
            }
        }

        // Audit logging
        foreach (var entry in ChangeTracker.Entries<CommonBase>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entry.Properties.FirstOrDefault(p => p.Metadata.Name.EndsWith("_Id"))?.CurrentValue?.ToString() ?? "unknown",
                    Action = entry.State == EntityState.Added ? "created" : "updated",
                    Changes = JsonSerializer.Serialize(entry.Entity),
                    Timestamp = DateTime.UtcNow,
                    Tenant_Id = entry.Entity.Tenant_Id
                };
                AuditLogs.Add(auditLog);
            }
            catch
            {
                // TODO: Add proper error handling
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private void PreventModificationOfLockedEntities()
    {
        var lockedEntities = ChangeTracker.Entries<CommonBase>()
            .Where(e => e.State == EntityState.Modified && e.Entity.IsLocked);

        foreach (var entry in lockedEntities)
        {
            throw new InvalidOperationException($"Cannot modify locked entity: {entry.Entity.GetType().Name}");
        }
    }
}
