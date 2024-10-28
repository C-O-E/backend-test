using System.ComponentModel.DataAnnotations;

namespace BackTest.Models;

// Base entity that represents common features of entities that can interact with assets.
public class AssetEntity
{
    [Key]
    public Guid Entity_Id { get; set; } = Guid.NewGuid();

    public string? EntityReference { get; set; }
    public string? PreferredLanguage { get; set; }

    [Required]
    public string EntityType { get; set; } = "Unknown"; // "legal" or "natural"
    public string? RiskLevel { get; set; }

    public List<string>? Tags { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsUnderReview { get; set; } = false;

    public virtual ICollection<Relationship>? Relationships { get; set; } = [];
    public virtual ICollection<EntityPosition>? Positions { get; set; } = [];
}

// Represents a legal entity that can own assets.
public class LegalEntity : AssetEntity
{
    [Required]
    public string LegalName { get; set; } = "Required";
    public string? TradeName { get; set; }
    public virtual ICollection<AssetOwnership>? OwnedAssets { get; set; } = [];
}

// Represents a natural person that can own assets.
public class NaturalEntity : AssetEntity
{
    [Required]
    public required string FirstName { get; set; }
    [Required]
    public required string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public virtual ICollection<AssetOwnership>? OwnedAssets { get; set; } = [];
}

// Abstract asset class
public abstract class Asset
{
    [Key]
    public Guid Asset_Id { get; set; } = Guid.NewGuid();
    public string? AssetName { get; set; }
    public required string AssetType { get; set; }
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
public class AssetOwnership
{
    [Key]
    public Guid Ownership_Id { get; set; } = Guid.NewGuid();
    public Guid Entity_Id { get; set; }
    public virtual required AssetEntity Entity { get; set; }
    public Guid Asset_Id { get; set; }
    public virtual required Asset Asset { get; set; }
    public float OwnershipPercentage { get; set; }
}

// Relationships such as management, ownership, etc.
public class Relationship
{
    [Key]
    public Guid Relationship_Id { get; set; } = Guid.NewGuid();
    public required string RelationshipType { get; set; }
    public string? Role { get; set; }

    public Guid SourceEntity_Id { get; set; }
    public virtual required AssetEntity SourceEntity { get; set; }

    public Guid TargetEntity_Id { get; set; }
    public virtual required AssetEntity TargetEntity { get; set; }
    public bool IsBidirectional { get; set; } = true;
    public string? AdditionalMetadata { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
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
