using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BackTest.Models;

// Base class providing audit, soft-delete, and tenant fields
public abstract class CommonBase
{
    public bool IsDeleted { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Multi-tenant support
    public Guid? Tenant_Id { get; set; }
}

// Base entity that represents common features of entities that can interact with assets.
[JsonDerivedType(typeof(LegalEntity), "legal")]
[JsonDerivedType(typeof(NaturalEntity), "natural")]
public class AssetEntity : CommonBase
{
    [Key]
    public Guid Entity_Id { get; set; } = Guid.NewGuid();

    public string? EntityReference { get; set; }
    public string? PreferredLanguage { get; set; }

    [Required]
    public string EntityType { get; set; } = "Unknown"; // "legal" or "natural"
    public string? RiskLevel { get; set; } // low, medium, high, critical

    public List<string>? Tags { get; set; }

    public bool IsUnderReview { get; set; } = false;

    public virtual ICollection<Relationship> Relationships { get; set; } = [];
    public virtual ICollection<EntityPosition> Positions { get; set; } = [];
}

// Represents a legal entity that can own assets.
public class LegalEntity : AssetEntity
{
    [Required]
    public string LegalName { get; set; } = "Required";
    public string? TradeName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Jurisdiction_Iso3 { get; set; }
    public virtual ICollection<AssetOwnership> OwnedAssets { get; set; } = [];
}

// Represents a natural person that can own assets.
public class NaturalEntity : AssetEntity
{
    [Required]
    public required string FirstName { get; set; }
    [Required]
    public required string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality_Iso3 { get; set; }
    public virtual ICollection<AssetOwnership> OwnedAssets { get; set; } = [];
}

// Abstract asset class
[JsonDerivedType(typeof(RealEstate), "realEstate")]
[JsonDerivedType(typeof(Stock), "stock")]
[JsonDerivedType(typeof(IPAsset), "ipAsset")]
public abstract class Asset : CommonBase
{
    [Key]
    public Guid Asset_Id { get; set; } = Guid.NewGuid();
    public string? AssetName { get; set; }
    public required string AssetType { get; set; }
    public decimal? EstimatedValue { get; set; }
    [MaxLength(3)]
    public string? Currency { get; set; }
}

// Concrete asset classes
public class RealEstate : Asset
{
    public required string Location { get; set; }
    public decimal MarketValue { get; set; }
    public double BuildingSize { get; set; }
}

public class Stock : Asset
{
    public required string StockSymbol { get; set; }
    public int NumberOfShares { get; set; }
    public decimal CurrentPrice { get; set; }
}

public class IPAsset : Asset
{
    public required string PatentNumber { get; set; }
    public DateTime ExpiryDate { get; set; }
}

// Represents ownership relationships between entities and assets.
public class AssetOwnership : CommonBase
{
    [Key]
    public Guid Ownership_Id { get; set; } = Guid.NewGuid();
    public Guid Entity_Id { get; set; }
    public virtual required AssetEntity Entity { get; set; }
    public Guid Asset_Id { get; set; }
    public virtual required Asset Asset { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public DateTime? AcquisitionDate { get; set; }
}

// Relationships such as management, ownership, etc.
public class Relationship : CommonBase
{
    [Key]
    public Guid Relationship_Id { get; set; } = Guid.NewGuid();
    public required string RelationshipType { get; set; } // ownership, management, directorship, partnership, subsidiary
    public string? Role { get; set; }

    public Guid SourceEntity_Id { get; set; }
    public virtual required AssetEntity SourceEntity { get; set; }

    public Guid TargetEntity_Id { get; set; }
    public virtual required AssetEntity TargetEntity { get; set; }
    public bool IsBidirectional { get; set; } = true;
    public string? AdditionalMetadata { get; set; }

    public bool IsUnderReview { get; set; } = false;
}

// Represents entity positions for visual representation.
public class EntityPosition
{
    [Key]
    public Guid EntityPosition_Id { get; set; } = Guid.NewGuid();
    public float X { get; set; }
    public float Y { get; set; }
    public Guid Entity_Id { get; set; }
    public virtual required AssetEntity Entity { get; set; }
}

// Tenant model for multi-tenancy
public class Tenant
{
    [Key]
    public Guid Tenant_Id { get; set; } = Guid.NewGuid();
    [Required]
    public required string TenantName { get; set; }
    public string? TenantCode { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ParentTenant_Id { get; set; }

    [ForeignKey("ParentTenant_Id")]
    public virtual Tenant? ParentTenant { get; set; }
    public virtual ICollection<Tenant> ChildTenants { get; set; } = [];
}

// Audit log for tracking changes
public class AuditLog
{
    [Key]
    public Guid AuditLog_Id { get; set; } = Guid.NewGuid();
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public required string Action { get; set; } // created, updated, deleted
    public string? Changes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? PerformedBy { get; set; }
    public Guid? Tenant_Id { get; set; }
}
