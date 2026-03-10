using BackTest.DTOs;
using BackTest.Models;
using BackTest.Repositories;

namespace BackTest.Services;

public interface IAssetEntityService
{
    Task<IEnumerable<AssetEntity>> GetAllEntitiesAsync(PaginationParameters pagination, AssetEntityFilterParameters filter);
    Task<AssetEntity?> GetEntityByIdAsync(Guid id);
    Task CreateEntityAsync(AssetEntity entity);
    Task UpdateEntityAsync(Guid id, UpdateAssetEntityRequest request);
    Task DeleteEntityAsync(Guid id);
    Task<Relationship?> GetRelationshipByIdAsync(Guid relationshipId);
    Task<IEnumerable<Relationship>> GetIndirectRelationshipsAsync(Guid entityId, int depth);
    Task CreateOrUpdateRelationshipAsync(Relationship relationship);
    Task DeleteRelationshipAsync(Guid relationshipId);
}

public class AssetEntityService : IAssetEntityService
{
    private readonly IAssetEntityRepository _repository;

    public AssetEntityService(IAssetEntityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AssetEntity>> GetAllEntitiesAsync(PaginationParameters pagination, AssetEntityFilterParameters filter)
    {
        return await _repository.GetAllAsync(pagination, filter);
    }

    public async Task<AssetEntity?> GetEntityByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task CreateEntityAsync(AssetEntity entity)
    {
        InitCommonFieldsOfNewEntity(entity);
        await _repository.AddAsync(entity);
    }

    private static void InitCommonFieldsOfNewEntity(AssetEntity entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = null;
        entity.DeletedAt = null;
        entity.IsDeleted = false;
        entity.IsActive = true;
        entity.IsLocked = false;
    }

    public async Task UpdateEntityAsync(Guid id, UpdateAssetEntityRequest request)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity != null)
        {
            request.ApplyTo(existingEntity);
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

    public async Task DeleteRelationshipAsync(Guid relationshipId)
    {
        await _repository.DeleteRelationship(relationshipId);
    }

    public async Task CreateOwnershipAsync(AssetOwnership ownership)
    {
        await _repository.CreateOwnership(ownership);
    }

    public async Task<IEnumerable<AssetOwnership>> GetOwnershipsByEntityIdAsync(Guid entityId)
    {
        return await _repository.GetOwnershipsByEntityIdAsync(entityId);
    }
}
