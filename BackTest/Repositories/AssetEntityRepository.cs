using BackTest.Models;
using Microsoft.EntityFrameworkCore;

namespace BackTest.Repositories;

public interface IAssetEntityRepository
{
    Task<IEnumerable<AssetEntity>> GetAllAsync();
    Task<AssetEntity?> GetByIdAsync(Guid id);
    Task AddAsync(AssetEntity entity);
    Task UpdateAsync(AssetEntity entity);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<Relationship>> GetIndirectRelationshipsAsync(Guid entityId, int depth);
    Task<IEnumerable<Relationship>> GetRelationshipsByEntityIdAsync(Guid entityId);
    Task<Relationship?> GetRelationshipByIdAsync(Guid relationshipId);
    Task CreateRelationship(Relationship relationship);
    Task UpdateRelationship(Relationship relationship);
}

public class AssetEntityRepository : IAssetEntityRepository
{
    private readonly AssetManagementDbContext _context;

    public AssetEntityRepository(AssetManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AssetEntity>> GetAllAsync()
    {
        return await _context.AssetEntities.ToListAsync();
    }

    public async Task<AssetEntity?> GetByIdAsync(Guid id)
    {
        return await _context.AssetEntities.FindAsync(id);
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
        var entity = await _context.AssetEntities.FindAsync(id);
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
            .Where(r => r.SourceEntity_Id == entityId || r.TargetEntity_Id == entityId)
            .ToListAsync();
    }

    public async Task<Relationship?> GetRelationshipByIdAsync(Guid relationshipId)
    {
        return await _context.Relationships.FindAsync(relationshipId);
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

}
