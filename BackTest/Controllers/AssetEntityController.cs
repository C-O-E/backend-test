using BackTest.DTOs;
using BackTest.Models;
using BackTest.Services;
using Microsoft.AspNetCore.Mvc;
using BackTest.Errors;

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
    public async Task<ActionResult<IEnumerable<AssetEntity>>> GetAssetEntities(
        [FromQuery] PaginationParameters pagination,
        [FromQuery] AssetEntityFilterParameters filter)
    {
        var assetEntities = await _service.GetAllEntitiesAsync(pagination, filter);
        return Ok(assetEntities);
    }

    // GET: api/AssetEntity/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<AssetEntity>> GetAssetEntity(Guid id)
    {
        var assetEntity = await _service.GetEntityByIdAsync(id);
        return assetEntity == null ? NotFound() : Ok(assetEntity);
    }

    // POST: api/AssetEntity
    // Requires "type" discriminator in request body: "legal" or "natural".
    [HttpPost]
    public async Task<ActionResult<AssetEntity>> CreateAssetEntity([FromBody] CreateAssetEntityRequest request)
    {
        if (request == null)
        {
            return BadRequest();
        }
        // TenantId taken from HttpContext set by TenantResolutionMiddleware — not from request body
        Guid? tenantId = HttpContext.Items["TenantId"] is Guid id ? id : null;
        if (tenantId == null)
        {
            return BadRequest(new Error { Code = 400, Title = "Bad Request", Detail = "TenantId is required in header" });
        }
        
        var assetEntity = request.ToDomainModel();
        assetEntity.Tenant_Id = tenantId;

        await _service.CreateEntityAsync(assetEntity);
        return CreatedAtAction(nameof(GetAssetEntity), new { id = assetEntity.Entity_Id }, assetEntity);
    }

    // PUT: api/AssetEntity/{id}
    // Sends only the fields to update; null fields are left unchanged.
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAssetEntity(Guid id, [FromBody] UpdateAssetEntityRequest request)
    {
        var assetEntity = await _service.GetEntityByIdAsync(id);
        if (assetEntity == null)
        {
            return NotFound();
        }
        await _service.UpdateEntityAsync(id, request);
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

    // GET: api/AssetEntity/{id}/relationships
    [HttpGet("{id}/relationships")]
    public async Task<ActionResult<IEnumerable<Relationship>>> GetRelationships(Guid id)
    {
        var entity = await _service.GetEntityByIdAsync(id);
        if (entity == null) return NotFound();

        var relationships = await _service.GetIndirectRelationshipsAsync(id, 1);
        return Ok(relationships);
    }

    // GET: api/AssetEntity/{id}/relationships/indirect?depth=3
    [HttpGet("{id}/relationships/indirect")]
    public async Task<ActionResult<IEnumerable<Relationship>>> GetIndirectRelationships(Guid id, [FromQuery] int depth = 2)
    {
        var relationships = await _service.GetIndirectRelationshipsAsync(id, depth);
        return Ok(relationships);
    }

    // POST: api/AssetEntity/relationships
    [HttpPost("relationships")]
    public async Task<ActionResult<Relationship>> CreateRelationship([FromBody] Relationship relationship)
    {
        if (relationship == null) return BadRequest();
        await _service.CreateOrUpdateRelationshipAsync(relationship);
        return Ok(relationship);
    }

    // PUT: api/AssetEntity/relationships
    [HttpPut("relationships")]
    public async Task<IActionResult> UpdateRelationship([FromBody] Relationship relationship)
    {
        if (relationship == null) return BadRequest();
        await _service.CreateOrUpdateRelationshipAsync(relationship);
        return NoContent();
    }

    // DELETE: api/AssetEntity/relationships/{relationshipId}
    [HttpDelete("relationships/{relationshipId}")]
    public async Task<IActionResult> DeleteRelationship(Guid relationshipId)
    {
        var retrievedRelationship = await _service.GetRelationshipByIdAsync(relationshipId);
        if (retrievedRelationship == null)
        {
            return NotFound();
        }
        await _service.DeleteRelationshipAsync(relationshipId);
        return NoContent();
    }   
}
