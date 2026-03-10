using BackTest.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BackTest.DTOs;

/// <summary>
/// Polymorphic POST request DTO for creating an AssetEntity.
/// The "type" discriminator is required: "legal" or "natural".
///
/// Example (LegalEntity):
/// {
///   "type": "legal",
///   "legalName": "Acme Corp",
///   "entityType": "legal",
///   "entityReference": "LE-001"
/// }
/// </summary>
[JsonPolymorphic(
    TypeDiscriminatorPropertyName = "type",
    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization)]
[JsonDerivedType(typeof(CreateLegalEntityRequest), "legal")]
[JsonDerivedType(typeof(CreateNaturalEntityRequest), "natural")]
public abstract class CreateAssetEntityRequest
{
    public string? EntityReference { get; set; }
    public string? PreferredLanguage { get; set; }

    [Required]
    public string EntityType { get; set; } = "Unknown";
    public string? RiskLevel { get; set; }
    public List<string>? Tags { get; set; }

    /// <summary>Maps the DTO to the corresponding EF domain entity.</summary>
    public abstract AssetEntity ToDomainModel();
}

public class CreateLegalEntityRequest : CreateAssetEntityRequest
{
    [Required]
    public string LegalName { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Jurisdiction_Iso3 { get; set; }

    public override LegalEntity ToDomainModel() => new()
    {
        EntityReference    = EntityReference,
        PreferredLanguage  = PreferredLanguage,
        EntityType         = EntityType,
        RiskLevel          = RiskLevel,
        Tags               = Tags,
        LegalName          = LegalName,
        TradeName          = TradeName,
        RegistrationNumber = RegistrationNumber,
        Jurisdiction_Iso3  = Jurisdiction_Iso3,
    };
}

public class CreateNaturalEntityRequest : CreateAssetEntityRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality_Iso3 { get; set; }

    public override NaturalEntity ToDomainModel() => new()
    {
        EntityReference   = EntityReference,
        PreferredLanguage = PreferredLanguage,
        EntityType        = EntityType,
        RiskLevel         = RiskLevel,
        Tags              = Tags,
        FirstName         = FirstName,
        LastName          = LastName,
        DateOfBirth       = DateOfBirth,
        Nationality_Iso3  = Nationality_Iso3,
    };
}
