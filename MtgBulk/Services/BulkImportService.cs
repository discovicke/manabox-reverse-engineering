using System.Globalization;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using MtgBulk.Data;
using MtgBulk.Models;

namespace MtgBulk.Services;

/// <summary>Sammanfattning av en CSV-import: nya, uppdaterade, överhoppade och fel.</summary>
public record ImportSummary(int Imported, int Updated, int Skipped, List<string> Errors);

/// <summary>Läser ManaBox-CSV och sparar kort i databasen, valfritt anrikad med Scryfall.</summary>
public class BulkImportService(AppDbContext db, ScryfallService scryfall)
{
    /// <summary>Importerar CSV-strömmen. Ökar antal om ScryfallId redan finns.</summary>
    public async Task<ImportSummary> ImportManaBoxCsvAsync(Stream csvStream, bool enrichFromScryfall, CancellationToken ct = default)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<ManaBoxCsvRow>().ToList();

        int imported = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ScryfallId))
            {
                skipped++;
                continue;
            }

            try
            {
                var existing = await db.Cards.SingleOrDefaultAsync(c => c.ScryfallId == row.ScryfallId.Trim(), ct);
                if (existing is not null)
                {
                    existing.Quantity += row.Quantity;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    var card = row.ToMtgCard();
                    if (enrichFromScryfall)
                    {
                        var dto = await scryfall.GetByIdAsync(card.ScryfallId, ct);
                        if (dto is not null)
                        {
                            card.PriceEur = ScryfallPricesDto.ParseDecimal(dto.Prices.Eur);
                            card.PriceUsd = ScryfallPricesDto.ParseDecimal(dto.Prices.Usd);
                            card.ImageSmall = dto.ResolvedSmallImage;
                            card.ImageNormal = dto.ResolvedNormalImage;
                            card.ScryfallUri = dto.ScryfallUri;
                        }
                    }
                    db.Cards.Add(card);
                    imported++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Name}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(ct);
        return new ImportSummary(imported, updated, skipped, errors);
    }
}
