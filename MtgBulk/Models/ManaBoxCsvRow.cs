using CsvHelper.Configuration.Attributes;

namespace MtgBulk.Models;

/// <summary>En rad från ManaBox-CSV:en. Mappas mot kolumnnamn och omvandlas till MtgCard.</summary>
public class ManaBoxCsvRow
{
    [Name("Name")]
    public string Name { get; set; } = string.Empty;

    [Name("Set code")]
    public string SetCode { get; set; } = string.Empty;

    [Name("Set name")]
    public string SetName { get; set; } = string.Empty;

    [Name("Collector number")]
    public string CollectorNumber { get; set; } = string.Empty;

    [Name("Foil")]
    public string Foil { get; set; } = "normal";

    [Name("Rarity")]
    public string Rarity { get; set; } = string.Empty;

    [Name("Quantity")]
    public int Quantity { get; set; } = 1;

    [Name("ManaBox ID")]
    public int? ManaBoxId { get; set; }

    [Name("Scryfall ID")]
    public string ScryfallId { get; set; } = string.Empty;

    [Name("Condition")]
    public string Condition { get; set; } = "near_mint";

    [Name("Language")]
    public string Language { get; set; } = "en";

    /// <summary>True om Foil-kolumnen är något annat än "normal".</summary>
    public bool IsFoil => !string.Equals(Foil, "normal", StringComparison.OrdinalIgnoreCase);

    /// <summary>Skapar ett MtgCard från CSV-raden.</summary>
    public MtgCard ToMtgCard()
    {
        return new MtgCard
        {
            ScryfallId = ScryfallId.Trim(),
            Name = Name,
            SetCode = SetCode.ToLowerInvariant(),
            SetName = SetName,
            CollectorNumber = CollectorNumber,
            Rarity = Rarity.ToLowerInvariant(),
            Language = Language.ToLowerInvariant(),
            Foil = IsFoil,
            Condition = Condition.ToLowerInvariant(),
            Quantity = Quantity,
            Source = CardSource.Csv,
            RawInput = $"{Name} [{SetCode}#{CollectorNumber}]",
            ManaBoxId = ManaBoxId
        };
    }
}
