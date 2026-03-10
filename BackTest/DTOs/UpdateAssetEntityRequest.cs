using BackTest.Models;

namespace BackTest.DTOs;

/// <summary>
/// Flat PUT request DTO for updating an AssetEntity.
/// All fields are optional (null = keep existing value).
/// Legal-specific fields (LegalName, TradeName, etc.) are ignored for NaturalEntity, and vice versa.
/// </summary>
public class UpdateAssetEntityRequest
{
    // --- Common fields ---
    public string? EntityReference { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? RiskLevel { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsUnderReview { get; set; }

    // --- LegalEntity-specific fields ---
    public string? LegalName { get; set; }
    public string? TradeName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? Jurisdiction_Iso3 { get; set; }

    // --- NaturalEntity-specific fields ---
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Nationality_Iso3 { get; set; }

    /// <summary>Applies non-null fields only, change common and subtype-specific fields</summary>
    public void ApplyTo(AssetEntity existing)
    {
        // Common fields — only update if provided
        if (EntityReference   != null) existing.EntityReference   = EntityReference;
        if (PreferredLanguage != null) existing.PreferredLanguage = PreferredLanguage;
        if (RiskLevel         != null) existing.RiskLevel         = RiskLevel;
        if (Tags          != null) existing.Tags          = Tags;
        if (IsUnderReview != null) existing.IsUnderReview = IsUnderReview.Value;

        existing.UpdatedAt = DateTime.UtcNow;

        // Subtype-specific fields
        switch (existing)
        {
            case LegalEntity legal:
                if (LegalName          != null) legal.LegalName          = LegalName;
                if (TradeName          != null) legal.TradeName          = TradeName;
                if (RegistrationNumber != null) legal.RegistrationNumber = RegistrationNumber;
                if (Jurisdiction_Iso3  != null) legal.Jurisdiction_Iso3  = Jurisdiction_Iso3;
                break;

            case NaturalEntity natural:
                if (FirstName        != null) natural.FirstName        = FirstName;
                if (LastName         != null) natural.LastName         = LastName;
                if (DateOfBirth      != null) natural.DateOfBirth      = DateOfBirth;
                if (Nationality_Iso3 != null) natural.Nationality_Iso3 = Nationality_Iso3;
                break;
        }
    }
}
