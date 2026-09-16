using System.Text.Json.Serialization;

namespace MtgBulk.Models;

/// <summary>DTO för ett kort från Scryfall-API:et. Omvandlas till MtgCard med ToMtgCard.</summary>
public class ScryfallCardDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("set")]
    public string Set { get; set; } = string.Empty;

    [JsonPropertyName("set_name")]
    public string SetName { get; set; } = string.Empty;

    [JsonPropertyName("collector_number")]
    public string CollectorNumber { get; set; } = string.Empty;

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = string.Empty;

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = "en";

    [JsonPropertyName("foil")]
    public bool Foil { get; set; }

    [JsonPropertyName("prices")]
    public ScryfallPricesDto Prices { get; set; } = new();

    [JsonPropertyName("image_uris")]
    public ScryfallImageUrisDto? ImageUris { get; set; }

    [JsonPropertyName("colors")]
    public List<string>? Colors { get; set; }

    [JsonPropertyName("color_identity")]
    public List<string>? ColorIdentity { get; set; }

    [JsonPropertyName("card_faces")]
    public List<ScryfallCardFaceDto>? CardFaces { get; set; }

    [JsonPropertyName("scryfall_uri")]
    public string? ScryfallUri { get; set; }

    /// <summary>Liten bild-URL. Fungerar även för dubbelsidiga kort via card_faces.</summary>
    public string? ResolvedSmallImage =>
        ImageUris?.Small ?? CardFaces?.FirstOrDefault(f => f.ImageUris != null)?.ImageUris?.Small;

    /// <summary>Normalstor bild-URL. Fungerar även för dubbelsidiga kort via card_faces.</summary>
    public string? ResolvedNormalImage =>
        ImageUris?.Normal ?? CardFaces?.FirstOrDefault(f => f.ImageUris != null)?.ImageUris?.Normal;

    /// <summary>Normaliserar farger till sorterade bokstaver. Tom lista = farglost.</summary>
    public static string NormalizeColors(List<string>? colors, List<ScryfallCardFaceDto>? faces)
    {
        var src = colors is { Count: > 0 } ? colors : faces?.FirstOrDefault(f => f.Colors is { Count: > 0 })?.Colors;
        if (src is null || src.Count == 0)
            return string.Empty;
        var order = "WUBRG";
        return string.Concat(src.Where(c => c.Length == 1).Select(c => c.ToUpperInvariant()).Distinct().OrderBy(c => order.IndexOf(c)));
    }

    /// <summary>Skapar ett MtgCard från Scryfall-datan.</summary>
    public MtgCard ToMtgCard(CardSource source = CardSource.Ocr, string? rawInput = null, int quantity = 1)
    {
        return new MtgCard
        {
            ScryfallId = Id,
            Name = Name,
            SetCode = Set,
            SetName = SetName,
            CollectorNumber = CollectorNumber,
            Rarity = Rarity,
            Colors = NormalizeColors(Colors, CardFaces),
            Language = Lang,
            Foil = Foil,
            PriceEur = ScryfallPricesDto.ParsePrice(Prices.Eur),
            PriceUsd = ScryfallPricesDto.ParsePrice(Prices.Usd),
            ImageSmall = ResolvedSmallImage,
            ImageNormal = ResolvedNormalImage,
            ScryfallUri = ScryfallUri,
            Source = source,
            RawInput = rawInput ?? Name,
            Quantity = quantity
        };
    }
}

/// <summary>Priser från Scryfall. Eur/Usd kommer som strängar och kan vara null.</summary>
public class ScryfallPricesDto
{
    [JsonPropertyName("eur")]
    public string? Eur { get; set; }

    [JsonPropertyName("usd")]
    public string? Usd { get; set; }

    /// <summary>Tolkar en Scryfall-prissträng till double. Returnerar null om den saknas.</summary>
    public static double? ParsePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }
}

/// <summary>Bild-URL:er från Scryfall i olika storlekar.</summary>
public class ScryfallImageUrisDto
{
    [JsonPropertyName("small")]
    public string? Small { get; set; }

    [JsonPropertyName("normal")]
    public string? Normal { get; set; }
}

/// <summary>En sida av ett dubbelsidigt kort. Används för att hitta bild när image_uris saknas.</summary>
public class ScryfallCardFaceDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("colors")]
    public List<string>? Colors { get; set; }

    [JsonPropertyName("image_uris")]
    public ScryfallImageUrisDto? ImageUris { get; set; }
}
