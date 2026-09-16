using Azure;
using Azure.AI.Vision.ImageAnalysis;
using MtgBulk.Data;
using MtgBulk.Models;
using Microsoft.EntityFrameworkCore;

namespace MtgBulk.Services;

/// <summary>Resultat av en bildskanning: lyckades det, vilket namn hittades och vilket kort sparades.</summary>
public record RecognizeResult(bool Success, string? CardName, string RawText, MtgCard? Card, string? Error);

/// <summary>Skannar kortfoton med Azure Vision OCR, matchar mot Scryfall och sparar i databasen.</summary>
public class CardRecognitionService(
    IConfiguration config,
    ScryfallService scryfall,
    AppDbContext db)
{
    /// <summary>Skapar Vision-klienten från .env. Returnerar null om nycklar saknas.</summary>
    private ImageAnalysisClient? CreateClient()
    {
        var endpoint = config["VISION_ENDPOINT"] ?? Environment.GetEnvironmentVariable("VISION_ENDPOINT");
        var key = config["VISION_KEY"] ?? Environment.GetEnvironmentVariable("VISION_KEY");
        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
            return null;
        return new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
    }

    /// <summary>Kör OCR på bilden, slår upp namnet i Scryfall och sparar/ökar antal i databasen.</summary>
    public async Task<RecognizeResult> RecognizeAndSaveAsync(Stream imageStream, CancellationToken ct = default)
    {
        var client = CreateClient();
        if (client is null)
            return new RecognizeResult(false, null, string.Empty, null, "VISION_ENDPOINT/VISION_KEY saknas i .env");

        BinaryData data = await BinaryData.FromStreamAsync(imageStream, ct);
        var result = await client.AnalyzeAsync(data, VisualFeatures.Read, cancellationToken: ct);

        var rawText = result.Value.Read is null
            ? string.Empty
            : string.Join("\n", result.Value.Read.Blocks.SelectMany(b => b.Lines.Select(l => l.Text)));

        var candidate = rawText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Length >= 2 && l.Any(char.IsLetter))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(candidate))
            return new RecognizeResult(false, null, rawText, null, "Ingen text hittad - fota rakt i dagsljus");

        ScryfallCardDto? dto;
        try
        {
            dto = await scryfall.FindByFuzzyNameAsync(candidate.Trim(), ct);
        }
        catch (ArgumentException ex)
        {
            return new RecognizeResult(false, candidate, rawText, null, $"Ogiltigt kortnamn '{candidate}': {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            return new RecognizeResult(false, candidate, rawText, null, $"Scryfall hittade inte '{candidate}': {ex.Message}");
        }

        if (dto is null)
            return new RecognizeResult(false, candidate, rawText, null, $"Scryfall hittade inte '{candidate}'");

        var existing = await db.Cards.SingleOrDefaultAsync(c => c.ScryfallId == dto.Id, ct);
        if (existing is not null)
        {
            existing.Quantity += 1;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.PriceEur = ScryfallPricesDto.ParsePrice(dto.Prices.Eur) ?? existing.PriceEur;
            existing.PriceUsd = ScryfallPricesDto.ParsePrice(dto.Prices.Usd) ?? existing.PriceUsd;
            if (string.IsNullOrEmpty(existing.Colors))
                existing.Colors = ScryfallCardDto.NormalizeColors(dto.Colors, dto.CardFaces);
            await db.SaveChangesAsync(ct);
            return new RecognizeResult(true, dto.Name, rawText, existing, null);
        }

        var card = dto.ToMtgCard(CardSource.Ocr, rawText);
        db.Cards.Add(card);
        await db.SaveChangesAsync(ct);
        return new RecognizeResult(true, dto.Name, rawText, card, null);
    }
}
