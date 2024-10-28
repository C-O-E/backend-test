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
}
