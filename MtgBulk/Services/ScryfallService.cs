using System.Net.Http.Json;
using MtgBulk.Models;

namespace MtgBulk.Services;

/// <summary>Hämtar kortdata från Scryfall med enkel throttle.</summary>
public class ScryfallService(HttpClient http)
{
    private void EnsureHeaders()
    {
        if (!http.DefaultRequestHeaders.Contains("User-Agent"))
            http.DefaultRequestHeaders.UserAgent.ParseAdd("MtgBulk/1.0 (school-project)");
        if (http.DefaultRequestHeaders.Accept.All(h => h.MediaType != "application/json"))
            http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }
    /// <summary>Söker ett kort på ungefärligt namn (OCR-text). Returnerar null om inget hittas.</summary>
    public async Task<ScryfallCardDto?> FindByFuzzyNameAsync(string name, CancellationToken ct = default)
    {
        var cleaned = (name ?? string.Empty).Trim();
        if (cleaned.Length < 2)
            throw new ArgumentException("Kortnamn för kort för Scryfall-sökning.", nameof(name));

        EnsureHeaders();
        var url = $"cards/named?fuzzy={Uri.EscapeDataString(cleaned)}";
        await Task.Delay(75, ct);

        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Scryfall {((int)response.StatusCode)}: {body}");
        }

        return await response.Content.ReadFromJsonAsync<ScryfallCardDto>(ct);
    }

    /// <summary>Hämtar ett kort direkt på Scryfall-ID (används vid CSV-anrikning).</summary>
    public async Task<ScryfallCardDto?> GetByIdAsync(string scryfallId, CancellationToken ct = default)
    {
        var cleaned = (scryfallId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            throw new ArgumentException("ScryfallId saknas.", nameof(scryfallId));

        EnsureHeaders();
        var url = $"cards/{Uri.EscapeDataString(cleaned)}";
        await Task.Delay(75, ct);

        using var response = await http.GetAsync(url, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScryfallCardDto>(ct);
    }
}
