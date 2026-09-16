using System.Net.Http.Json;
using MtgBulk.Models;

namespace MtgBulk.Services;

/// <summary>Hämtar kortdata från Scryfall med enkel throttle.</summary>
public class ScryfallService(HttpClient http)
{
    /// <summary>Söker ett kort på ungefärligt namn (OCR-text). Returnerar null om inget hittas.</summary>
    public async Task<ScryfallCardDto?> FindByFuzzyNameAsync(string name, CancellationToken ct = default)
    {
        var url = $"https://api.scryfall.com/cards/named?fuzzy={Uri.EscapeDataString(name)}";
        await Task.Delay(75, ct);
        return await http.GetFromJsonAsync<ScryfallCardDto>(url, ct);
    }

    /// <summary>Hämtar ett kort direkt på Scryfall-ID (används vid CSV-anrikning).</summary>
    public async Task<ScryfallCardDto?> GetByIdAsync(string scryfallId, CancellationToken ct = default)
    {
        var url = $"https://api.scryfall.com/cards/{scryfallId}";
        await Task.Delay(75, ct);
        return await http.GetFromJsonAsync<ScryfallCardDto>(url, ct);
    }
}
