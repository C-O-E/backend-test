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
}
