using System.ComponentModel.DataAnnotations;

namespace MtgBulk.Models;

/// <summary>Visar varifrån kortet kommer: CSV-import, OCR-skanning eller manuell inmatning.</summary>
public enum CardSource
{
    Csv,
    Ocr,
    Manual
}

/// <summary>Platt databasmodell för ett Magic-kort. Gemensamt mål för Scryfall, ManaBox-CSV och OCR.</summary>
public class MtgCard
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string ScryfallId { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(16)]
    public string SetCode { get; set; } = string.Empty;

    [MaxLength(200)]
    public string SetName { get; set; } = string.Empty;

    [MaxLength(16)]
    public string CollectorNumber { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Rarity { get; set; } = string.Empty;

    [MaxLength(8)]
    public string Language { get; set; } = "en";

    public bool Foil { get; set; }

    [MaxLength(32)]
    public string Condition { get; set; } = "near_mint";

    public int Quantity { get; set; } = 1;

    public decimal? PriceEur { get; set; }
    public decimal? PriceUsd { get; set; }

    [MaxLength(500)]
    public string? ImageSmall { get; set; }

    [MaxLength(500)]
    public string? ImageNormal { get; set; }

    [MaxLength(500)]
    public string? ScryfallUri { get; set; }

    public CardSource Source { get; set; } = CardSource.Manual;

    public string? RawInput { get; set; }

    public int? ManaBoxId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
