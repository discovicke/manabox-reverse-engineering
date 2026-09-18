# MtgBulk - MTG-samling med Azure AI Vision

Blazor Server-app (C#) där användaren skannar Magic-kort med mobilkamera eller importerar en ManaBox-CSV. Azure AI Vision läser kortnamnet (OCR), Scryfall fyller på med information som pris och bild. Samlingen sparas i SQLite.

## Snabb överblick av programmet:

- **Minst en Azure AI-tjänst:** Azure AI Vision – Image Analysis (`Read`/OCR).
- **AI-koppling via C#:** `Services/CardRecognitionService.cs` använder `Azure.AI.Vision.ImageAnalysis`-SDK:t direkt från Blazor Server.
- **Användaren matar in data:** kortfoto på `/scan` eller CSV-fil på `/import`.
- **AI bearbetar datan:** Vision-OCR extraherar kortnamn ur bilden.
- **Resultat presenteras:** träff med bild, pris och antal + OCR-råtext, samt statistik på `/` och tabell på `/collection`.

> Flöde för ett foto: 
> 
> **Bild > Azure OCR-text > Scryfall-match > SQLite (`MtgCard`) > Blazor-UI**.

## Kom igång

```powershell
# 1. Klona och gå till repot
git clone <repo-url>
cd net25-analysis-ai-discovicke

# 2. Skapa din lokala nyckelfil
copy MtgBulk\.env.example MtgBulk\.env
# Linux/macOS: cp MtgBulk/.env.example MtgBulk/.env
```

### Lägg till API-nycklar

Nycklar ligger **inte** i repot. `.env` är ignorerad i `.gitignore`, endast `.env.example` committas.

1. Skapa en **Azure AI Vision**-resurs (eller multipurpose Azure AI Services-resurs) i Azure-portalen.
2. Kopiera `Endpoint` + `Key 1` under Resource Management > Keys and Endpoint.
3. Fyll i `MtgBulk/.env`:

```ini
VISION_ENDPOINT=https://<din-resurs>.cognitiveservices.azure.com/
VISION_KEY=<din-vision-nyckel>
```

`Program.cs` laddar filen med `DotNetEnv` (`Env.Load`) och `CardRecognitionService` läser `VISION_ENDPOINT` / `VISION_KEY` via `IConfiguration` / miljövariabler. Saknas de får du felet `VISION_ENDPOINT/VISION_KEY saknas i .env` på `/scan` – resten av appen (import, samling) fungerar ändå.

> Har `.env` med riktig nyckel råkat committas någon gång? Rensa historiken och rotera nyckeln under Keys and Endpoint > Regenerate.

```powershell
# 3. Databas + kör
dotnet ef database update --project MtgBulk
dotnet run --project MtgBulk
# Öppna https://localhost:5001 (eller http://localhost:5000)
```

Exempel-CSV för import finns i `MtgBulk/ManaBox_Collection.csv`.

## AI-tjänster: vad används och varför

**Används: Azure AI Vision – Image Analysis(`Azure.AI.Vision.ImageAnalysis`, `VisualFeatures.Read`)**

- Valdes för att problemet är visuellt: kort fotas snett, i blandljus och med foil-glans. Visions `Read`-modell är tränad för scen-OCR och slår en generell LLM eller egen regex på råa pixlar.
- Enkel C#-SDK med `ImageAnalysisClient.AnalyzeAsync(BinaryData, VisualFeatures.Read)`
- Billig och snabb för ett anrop per kort; ingen träning behövs.

**Stödtjänst: Scryfall API**. Gratis community-API för kortnamn, priser (EUR/USD), bilder och färger. Anropas med throttle (75 ms) och `User-Agent: MtgBulk/1.0`.
