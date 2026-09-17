using Microsoft.EntityFrameworkCore;
using MtgBulk.Data;

namespace MtgBulk.Services;

public record RarityStat(string Rarity, int Unique, int Quantity, double ValueEur);
public record ColorStat(string Group, int Unique, int Quantity, double ValueEur);
public record CollectionStats(
    int TotalUnique,
    int TotalQuantity,
    double TotalValueEur,
    List<RarityStat> ByRarity,
    List<ColorStat> ByColor);

/// <summary>Räknar sammanlagd statistik för samlingen i EUR.</summary>
public class CollectionStatsService(AppDbContext db)
{
    /// <summary>Hämtar totaler, per raritet och per färggrupp. Flerfärgade hamnar i Multicolor.</summary>
    public async Task<CollectionStats> GetStatsAsync(CancellationToken ct = default)
    {
        var cards = await db.Cards.AsNoTracking().ToListAsync(ct);

        double ValueOf(Models.MtgCard c) => (c.PriceEur ?? 0) * c.Quantity;

        var byRarity = cards
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Rarity) ? "okänd" : c.Rarity.ToLowerInvariant())
            .Select(g => new RarityStat(g.Key, g.Count(), g.Sum(c => c.Quantity), g.Sum(ValueOf)))
            .OrderByDescending(r => r.ValueEur)
            .ToList();

        var byColor = cards
            .GroupBy(ColorGroup)
            .Select(g => new ColorStat(g.Key, g.Count(), g.Sum(c => c.Quantity), g.Sum(ValueOf)))
            .OrderByDescending(r => r.ValueEur)
            .ToList();

        return new CollectionStats(
            cards.Count,
            cards.Sum(c => c.Quantity),
            cards.Sum(ValueOf),
            byRarity,
            byColor);
    }

    private static string ColorGroup(Models.MtgCard c)
    {
        if (string.IsNullOrEmpty(c.Colors))
            return "Färglöst";
        if (c.Colors.Length == 1)
            return c.Colors;
        return "Multicolor";
    }
}
