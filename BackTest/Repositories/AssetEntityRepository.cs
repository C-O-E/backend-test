using BackTest.Models;
using Microsoft.EntityFrameworkCore;

namespace BackTest.Repositories;

public interface IAssetEntityRepository
{
    Task<IEnumerable<AssetEntity>> GetAllAsync(PaginationParameters pagination, AssetEntityFilterParameters filter);
    Task<AssetEntity?> GetByIdAsync(Guid id);
    Task AddAsync(AssetEntity entity);
    Task UpdateAsync(AssetEntity entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Relationship>> GetIndirectRelationshipsAsync(Guid entityId, int depth);
    Task<IEnumerable<Relationship>> GetRelationshipsByEntityIdAsync(Guid entityId);
    Task<Relationship?> GetRelationshipByIdAsync(Guid relationshipId);
    Task CreateRelationship(Relationship relationship);
    Task UpdateRelationship(Relationship relationship);
    Task DeleteRelationship(Guid relationshipId);
    Task CreateOwnership(AssetOwnership ownership);
    Task<IEnumerable<AssetOwnership>> GetOwnershipsByEntityIdAsync(Guid entityId);
    Task<IEnumerable<AssetOwnership>> GetOwnershipsByAssetIdAsync(Guid assetId);
}

public class AssetEntityRepository : IAssetEntityRepository
{
    private readonly AssetManagementDbContext _context;

    public AssetEntityRepository(AssetManagementDbContext context)
    {
        _context = context;
    }

    // Get all entities with optional composable filtering and pagination
    public async Task<IEnumerable<AssetEntity>> GetAllAsync(PaginationParameters pagination, AssetEntityFilterParameters filter)
    {
        var query = _context.AssetEntities.AsQueryable();

        if (!string.IsNullOrEmpty(filter.EntityType))
        {
            query = query.Where(e => e.EntityType == filter.EntityType);
        }

        if (!string.IsNullOrEmpty(filter.RiskLevel))
        {
            query = query.Where(e => e.RiskLevel != null && e.RiskLevel == filter.RiskLevel);
        }

        if (!string.IsNullOrEmpty(filter.Tag))
        {
            query = query.Where(e => e.Tags != null && e.Tags.Contains(filter.Tag));
        }

        if (pagination.PageSize.HasValue)
        {
            query = query
                .Skip((pagination.Page - 1) * pagination.PageSize.Value)
                .Take(pagination.PageSize.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<AssetEntity?> GetByIdAsync(Guid id)
    {
        // Use FirstOrDefaultAsync to inherit Global Query Filter of soft-deletion
        return await _context.AssetEntities.FirstOrDefaultAsync(e => e.Entity_Id == id);
    }

    public async Task AddAsync(AssetEntity entity)
    {
        await _context.AssetEntities.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AssetEntity entity)
    {
        _context.AssetEntities.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.AssetEntities.FirstOrDefaultAsync(e => e.Entity_Id == id);
        if (entity != null)
        {
            _context.AssetEntities.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Relationship>> GetIndirectRelationshipsAsync(Guid entityId, int depth)
    {
        var indirectRelationships = new List<Relationship>();
        var currentDepthEntities = new List<Guid> { entityId };

        for (int i = 0; i < depth; i++)
        {
            var relationships = await _context.Relationships
                .AsNoTracking()
                .Where(r => currentDepthEntities.Contains(r.SourceEntity_Id) || currentDepthEntities.Contains(r.TargetEntity_Id))
                .ToListAsync();

            indirectRelationships.AddRange(relationships);

            currentDepthEntities = relationships.Select(r => r.TargetEntity_Id).Distinct().ToList();
        }

        return indirectRelationships;
    }

    public async Task<IEnumerable<Relationship>> GetRelationshipsByEntityIdAsync(Guid entityId)
    {
        return await _context.Relationships
            .AsNoTracking()
            .Where(r => r.SourceEntity_Id == entityId || r.TargetEntity_Id == entityId)
            .ToListAsync();
    }

    public async Task<Relationship?> GetRelationshipByIdAsync(Guid relationshipId)
    {
        // Use FirstOrDefaultAsync to inherit Global Query Filter of soft-deletion
        return await _context.Relationships.FirstOrDefaultAsync(r => r.Relationship_Id == relationshipId);
    }

    public async Task CreateRelationship(Relationship relationship)
    {
        await _context.Relationships.AddAsync(relationship);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRelationship(Relationship relationship)
    {
        _context.Relationships.Update(relationship);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRelationship(Guid relationshipId)
    {
        var relationship = await GetRelationshipByIdAsync(relationshipId);
        if (relationship != null)
        {
            _context.Relationships.Remove(relationship);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateOwnership(AssetOwnership ownership)
    {
        await _context.AssetOwnerships.AddAsync(ownership);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AssetOwnership>> GetOwnershipsByEntityIdAsync(Guid entityId)
    {
        return await _context.AssetOwnerships
            .Where(o => o.Entity_Id == entityId)
            .Include(o => o.Asset)
            .ToListAsync();
    }

    public async Task<IEnumerable<AssetOwnership>> GetOwnershipsByAssetIdAsync(Guid assetId)
    {
        return await _context.AssetOwnerships
            .Where(o => o.Asset_Id == assetId)
            .Include(o => o.Entity)
            .ToListAsync();
    }
}
