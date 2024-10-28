using BackTest.Models;
using Microsoft.EntityFrameworkCore;

namespace BackTest.Repositories;

public class AssetManagementDbContext : DbContext
{
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
}
