# MtgBulk - MTG-samling med Azure AI Vision

> Skolprojekt i utbildningssyfte. Blazor Server-app i C# där användaren skannar Magic-kort med mobilkamera eller importerar en ManaBox-CSV.

![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4)
![SQLite](https://img.shields.io/badge/SQLite-EF_Core-003B57)
![Azure AI Vision](https://img.shields.io/badge/Azure_AI-Vision-0078D4)
![Status](https://img.shields.io/badge/Status-Skolprojekt-yellow)

## Vad är detta

Skolprojekt utvecklat i utbildningssyfte för att öva AI-integration i .NET. Användaren skannar Magic-kort med mobilkamera på sidan `/scan` eller importerar en ManaBox-CSV på sidan `/import`. Azure AI Vision läser kortnamnet med OCR, Scryfall fyller på med information som pris och bild och samlingen sparas i SQLite med modellen `MtgCard`.

## Snabb överblick av programmet

- **Minst en Azure AI-tjänst:** Azure AI Vision med Image Analysis (`Read` och OCR).
- **AI-koppling via C#:** `Services/CardRecognitionService.cs` använder paketet `Azure.AI.Vision.ImageAnalysis` direkt från Blazor Server.
- **Användaren matar in data:** kortfoto på `/scan` eller CSV-fil på `/import`.
- **AI bearbetar datan:** Vision-OCR extraherar kortnamn ur bilden.
- **Resultat presenteras:** träff med bild, pris och antal plus OCR-råtext, samt statistik på `/` och tabell på `/collection`.

> Flöde för ett foto:
>
> `Bild` till `Azure OCR-text` till `Scryfall-match` till `SQLite (MtgCard)` till `Blazor-UI`.

## Kom igång

```powershell
# 1. Klona och gå till repot
git clone https://github.com/discovicke/manabox-reverse-engineering.git
cd manabox-reverse-engineering

# 2. Skapa din lokala nyckelfil
copy MtgBulk\.env.example MtgBulk\.env
```

### Lägg till API-nycklar

Nycklar ligger inte i repot. Filen `.env` är ignorerad i `.gitignore`, endast `.env.example` committas.

1. Skapa en **Azure AI Vision**-resurs eller en multipurpose Azure AI Services-resurs i Azure-portalen.
2. Kopiera `Endpoint` och `Key 1` under Resource Management och Keys and Endpoint.
3. Fyll i `MtgBulk/.env`:

```ini
VISION_ENDPOINT=https://<din-resurs>.cognitiveservices.azure.com/
VISION_KEY=<din-vision-nyckel>
```

`Program.cs` laddar filen med `DotNetEnv` via `Env.Load` och `CardRecognitionService` läser `VISION_ENDPOINT` och `VISION_KEY` via `IConfiguration` och miljövariabler. Saknas de får du felet `VISION_ENDPOINT/VISION_KEY saknas i .env` på `/scan`. Resten av appen med import och samling fungerar ändå.

> Om en `.env` med riktig nyckel råkat committas någon gång: rensa historiken och rotera nyckeln under Keys and Endpoint och Regenerate.

```powershell
# 3. Databas och kör
dotnet ef database update --project MtgBulk
dotnet run --project MtgBulk
# Öppna https://localhost:5001 (eller http://localhost:5000)
```

Exempel-CSV för import finns i `MtgBulk/ManaBox_Collection.csv`.

## AI-tjänster: vad används och varför

**Används: Azure AI Vision med Image Analysis (`Azure.AI.Vision.ImageAnalysis`, `VisualFeatures.Read`)**

- Valdes för att problemet är visuellt: kort fotas snett, i blandljus och med foil-glans. Visions `Read`-modell är tränad för scen-OCR och slår en generell LLM eller egen regex på råa pixlar.
- Enkel C#-SDK med `ImageAnalysisClient.AnalyzeAsync(BinaryData, VisualFeatures.Read)`.
- Billig och snabb för ett anrop per kort och ingen träning behövs.

**Stödtjänst: Scryfall API**. Gratis community-API för kortnamn, priser i EUR och USD, bilder och färger. Anropas med throttle på 75 ms och `User-Agent: MtgBulk/1.0`.
