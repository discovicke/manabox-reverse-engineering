using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
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
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        };
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);
        var rows = csv.GetRecords<ManaBoxCsvRow>().ToList();

        int imported = 0, updated = 0, skipped = 0;
        var errors = new List<string>();

        var ids = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ScryfallId))
            .Select(r => r.ScryfallId.Trim())
            .Distinct()
            .ToList();

        var existingById = await db.Cards
            .Where(c => ids.Contains(c.ScryfallId))
            .ToDictionaryAsync(c => c.ScryfallId, ct);

        var pendingById = new Dictionary<string, MtgCard>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ScryfallId))
            {
                skipped++;
                continue;
            }

            var id = row.ScryfallId.Trim();

            try
            {
                if (existingById.TryGetValue(id, out var existing))
                {
                    existing.Quantity += row.Quantity;
                    existing.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else if (pendingById.TryGetValue(id, out var pending))
                {
                    pending.Quantity += row.Quantity;
                    pending.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    var card = row.ToMtgCard();
                    if (enrichFromScryfall)
                    {
                        try
                        {
                            var dto = await scryfall.GetByIdAsync(card.ScryfallId, ct);
                            if (dto is not null)
                            {
                                card.PriceEur = ScryfallPricesDto.ParsePrice(dto.Prices.Eur);
                                card.PriceUsd = ScryfallPricesDto.ParsePrice(dto.Prices.Usd);
                                card.ImageSmall = dto.ResolvedSmallImage;
                                card.ImageNormal = dto.ResolvedNormalImage;
                                card.ScryfallUri = dto.ScryfallUri;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{row.Name}: Scryfall-hopp ({ex.Message}) - sparad utan pris/bild");
                        }
                    }
                    pendingById[id] = card;
                    db.Cards.Add(card);
                    imported++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{row.Name}: {ex.Message}");
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            errors.Add($"Sparfel: {ex.InnerException?.Message ?? ex.Message}");
        }

        return new ImportSummary(imported, updated, skipped, errors);
    }
}
