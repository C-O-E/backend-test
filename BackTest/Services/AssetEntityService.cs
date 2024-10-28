using BackTest.Models;
using BackTest.Repositories;

namespace BackTest.Services;

public interface IAssetEntityService
{
    Task<IEnumerable<AssetEntity>> GetAllEntitiesAsync();
    Task<AssetEntity?> GetEntityByIdAsync(Guid id);
    Task CreateEntityAsync(AssetEntity entity);
    Task UpdateEntityAsync(Guid id, AssetEntity updatedEntity);
    Task DeleteEntityAsync(Guid id);
    Task<IEnumerable<Relationship>> GetIndirectRelationshipsAsync(Guid entityId, int depth);
    Task CreateOrUpdateRelationshipAsync(Relationship relationship);
}

public class AssetEntityService : IAssetEntityService
{
    private readonly IAssetEntityRepository _repository;

    public AssetEntityService(IAssetEntityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AssetEntity>> GetAllEntitiesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<AssetEntity?> GetEntityByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task CreateEntityAsync(AssetEntity entity)
    {
        await _repository.AddAsync(entity);
    }

    public async Task UpdateEntityAsync(Guid id, AssetEntity updatedEntity)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity != null)
        {
            existingEntity.EntityReference = updatedEntity.EntityReference;
            existingEntity.PreferredLanguage = updatedEntity.PreferredLanguage;
            existingEntity.EntityType = updatedEntity.EntityType;
            existingEntity.RiskLevel = updatedEntity.RiskLevel;
            existingEntity.Tags = updatedEntity.Tags;
            existingEntity.UpdatedAt = DateTime.UtcNow;
            existingEntity.IsUnderReview = updatedEntity.IsUnderReview;
            await _repository.UpdateAsync(existingEntity);
        }
    }

    public async Task DeleteEntityAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<Relationship>> GetIndirectRelationshipsAsync(Guid entityId, int depth)
    {
        return await _repository.GetIndirectRelationshipsAsync(entityId, depth);
    }

    public async Task CreateOrUpdateRelationshipAsync(Relationship relationship)
    {
        var existingRelationship = await _repository.GetRelationshipByIdAsync(relationship.Relationship_Id);
        if (existingRelationship == null)
        {
            // Ensure that there are no circular dependencies before creating
            var hasCircularDependency = await CheckCircularRelationshipAsync(relationship.SourceEntity_Id, relationship.TargetEntity_Id);
            if (hasCircularDependency)
            {
                throw new InvalidOperationException("Circular relationship detected.");
            }

            await _repository.CreateRelationship(relationship);
        }
        else
        {
            existingRelationship.RelationshipType = relationship.RelationshipType;
            existingRelationship.Role = relationship.Role;
            existingRelationship.IsBidirectional = relationship.IsBidirectional;
            existingRelationship.UpdatedAt = DateTime.UtcNow;
            existingRelationship.AdditionalMetadata = relationship.AdditionalMetadata;

            await _repository.UpdateRelationship(existingRelationship);
        }
    }

    private async Task<bool> CheckCircularRelationshipAsync(Guid sourceEntityId, Guid targetEntityId)
    {
        var relationships = await _repository.GetRelationshipsByEntityIdAsync(sourceEntityId);
        return relationships.Any(r => r.TargetEntity_Id == targetEntityId || r.SourceEntity_Id == targetEntityId);
    }

    public async Task<Relationship?> GetRelationshipByIdAsync(Guid relationshipId)
    {
        return await _repository.GetRelationshipByIdAsync(relationshipId);
    }
}
