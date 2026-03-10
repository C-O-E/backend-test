public class AssetEntityFilterParameters
{
    public string? EntityType { get; set; }  // "legal" | "natural"
    public string? RiskLevel { get; set; }   // "low" | "medium" | "high" | "critical"
    public string? Tag { get; set; }         // Filter by a specific tag
}
