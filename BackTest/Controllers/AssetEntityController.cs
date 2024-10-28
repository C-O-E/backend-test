using BackTest.Models;
using BackTest.Services;
using Microsoft.AspNetCore.Mvc;

// Controllers for Asset Management API
[Route("api/[controller]")]
[ApiController]
public class AssetEntityController : ControllerBase
{
    private readonly IAssetEntityService _service;

    public AssetEntityController(IAssetEntityService service)
    {
        _service = service;
    }

    // GET: api/AssetEntity
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetEntity>>> GetAssetEntities()
    {
        var assetEntities = await _service.GetAllEntitiesAsync();
        return Ok(assetEntities);
    }

    // GET: api/AssetEntity/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<AssetEntity>> GetAssetEntity(Guid id)
    {
        var assetEntity = await _service.GetEntityByIdAsync(id);
        return assetEntity == null ? (ActionResult<AssetEntity>)NotFound() : (ActionResult<AssetEntity>)Ok(assetEntity);
    }

    // POST: api/AssetEntity
    [HttpPost]
    public async Task<ActionResult<AssetEntity>> CreateAssetEntity([FromBody] AssetEntity assetEntity)
    {
        if (assetEntity == null)
        {
            return BadRequest();
        }
        await _service.CreateEntityAsync(assetEntity);
        return CreatedAtAction(nameof(GetAssetEntity), new { id = assetEntity.Entity_Id }, assetEntity);
    }

    // PUT: api/AssetEntity/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAssetEntity(Guid id, [FromBody] AssetEntity updatedEntity)
    {
        var assetEntity = await _service.GetEntityByIdAsync(id);
        if (assetEntity == null)
        {
            return NotFound();
        }
        await _service.UpdateEntityAsync(id, updatedEntity);
        return NoContent();
    }

    // DELETE: api/AssetEntity/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssetEntity(Guid id)
    {
        var assetEntity = await _service.GetEntityByIdAsync(id);
        if (assetEntity == null)
        {
            return NotFound();
        }
        await _service.DeleteEntityAsync(id);
        return NoContent();
    }
}
