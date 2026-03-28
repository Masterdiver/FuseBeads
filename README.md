# Technische Dokumentation – FuseBeads

## 1. Projektübersicht

**FuseBeads** ist eine plattformübergreifende Mobile- und Desktop-Applikation auf Basis von **.NET 10** und **.NET MAUI**. Die App konvertiert beliebige Bilder automatisch in Bügelperlen-Muster (Fuse Bead Patterns). Benutzer können ein Foto oder Bild importieren, es in ein pixeliertes Bügelperlen-Raster umwandeln, das Muster bearbeiten, speichern und drucken.

### Unterstützte Plattformen

| Plattform      | Mindestversion         |
|----------------|------------------------|
| Android        | API Level 21 (Android 5.0) |
| iOS            | 15.0                   |
| macOS Catalyst | 15.0                   |
| Windows        | Windows 10 (19041)     |

---

## 2. Architektur

Das Projekt folgt den Prinzipien der **Clean Architecture** (Onion Architecture) mit strikter Schichtentrennung und Dependency Injection. Der Abhängigkeitsfluss verläuft stets nach innen: von der Infrastruktur über die Applikation zur Domäne.

```
┌─────────────────────────────────────────┐
│          FuseBeads (Präsentation)       │  .NET MAUI · XAML · MVVM
├─────────────────────────────────────────┤
│       FuseBeads.Application             │  Services · DTOs
├─────────────────────────────────────────┤
│       FuseBeads.Infrastructure          │  SkiaSharp · JSON · Paletten
├─────────────────────────────────────────┤
│         FuseBeads.Domain                │  Entitäten · Interfaces
└─────────────────────────────────────────┘
```

### Projekte

| Projekt                     | Zweck                                                                 |
|-----------------------------|-----------------------------------------------------------------------|
| `FuseBeads.Domain`          | Kerndomäne: Entitäten, Interfaces (keine externen Abhängigkeiten)     |
| `FuseBeads.Application`     | Anwendungslogik: PatternService, DTOs                                 |
| `FuseBeads.Infrastructure`  | Technische Implementierungen: Bildverarbeitung, Paletten, Persistenz  |
| `FuseBeads`                 | Präsentationsschicht: MAUI-UI, ViewModels, Seiten                     |

---

## 3. Domänenschicht (`FuseBeads.Domain`)

### Entitäten

#### `BeadPattern`
Repräsentiert ein vollständiges Bügelperlen-Muster als zweidimensionales Gitter.

- `Grid[row, col]` – Gitter aus `BeadCell?`-Einträgen
- `CheckedCells` – Menge bereits abgehakter (platzierter) Perlen
- `ToggleChecked(row, col)` – Umschalten des Abgehakt-Status einer Zelle
- `GetColorSummary()` – Farbbedarf sortiert nach Häufigkeit
- `ToShoppingList()` – Formatierte Einkaufsliste als Text
- `TotalBeads` – Gesamtanzahl der Perlen im Muster

#### `BeadCell`
Einzelne Zelle im Raster (`record`): Position (Row, Column) und Farbe.

#### `BeadColor`
Bügelperlenfarbe (`record`): Name, RGB-Werte, Hex-Code.  
Methode `DistanceTo(r, g, b)` berechnet den euklidischen Farbabstand im RGB-Raum.

#### `PaletteType` (Enum)
```
Standard | Hama | Artkal | Perler
```

### Interfaces

| Interface                  | Beschreibung                                          |
|----------------------------|-------------------------------------------------------|
| `IBeadColorPalette`        | Farbpalette mit Farbliste und Nächste-Farbe-Suche     |
| `IBeadColorPaletteFactory` | Factory zur Auswahl und Bereitstellung von Paletten   |
| `IImageLoader`             | Laden, Skalieren und Anpassen von Bildern             |
| `IPatternRenderer`         | Rendern des Musters als Byte-Array (Bild)             |
| `IPrintRenderer`           | Rendern einer Druckseite als PNG oder PDF             |
| `IPatternStorage`          | Speichern und Laden von Mustern                       |
| `IProgressStorage`         | Speichern und Laden des Legefortschritts              |

---

## 4. Anwendungsschicht (`FuseBeads.Application`)

### `PatternService` (implements `IPatternService`)

Orchestriert den gesamten Konvertierungsprozess von einem Eingabebild zu einem fertigen Bügelperlen-Muster:

1. Bild laden via `IImageLoader`
2. Bildvorverarbeitung (Helligkeit, Kontrast, Sättigung)
3. Automatische Gitterhöhe aus dem Seitenverhältnis berechnen
4. Bild auf Rasterdimension skalieren
5. Optional: Floyd-Steinberg-Dithering anwenden
6. Jeden Pixel der nächstgelegenen Perlenfarbe der Palette zuordnen
7. Optional: Farblimitierung (Top-N-Farben behalten, Rest auf nächste erlaubte Farbe remappen)
8. Muster als Bild rendern
9. Farbbedarf-Statistik berechnen

### DTOs

#### `PatternSettings`
Konfigurationsparameter für die Mustergenerierung:

| Eigenschaft      | Typ     | Standard  | Beschreibung                              |
|------------------|---------|-----------|-------------------------------------------|
| `Width`          | `int`   | 29        | Breite des Rasters in Perlen              |
| `Height`         | `int`   | 0         | Höhe (0 = automatisch aus Seitenverhältnis) |
| `BeadSizePx`     | `int`   | 20        | Pixelgröße je Perle in der Vorschau       |
| `PaletteType`    | Enum    | Standard  | Gewählte Farbpalette                      |
| `MaxColors`      | `int`   | 0         | Max. Anzahl Farben (0 = unbegrenzt)       |
| `Brightness`     | `float` | 0.0       | Helligkeit (−1.0 bis +1.0)               |
| `Contrast`       | `float` | 0.0       | Kontrast (−1.0 bis +1.0)                 |
| `Saturation`     | `float` | 0.0       | Sättigung (−1.0 bis +1.0)                |
| `ShowBoardGrid`  | `bool`  | false     | Brett-Raster-Overlay einblenden           |
| `BoardWidth`     | `int`   | 29        | Brettbreite für das Overlay               |
| `BoardHeight`    | `int`   | 29        | Bretthöhe für das Overlay                 |
| `EnableDithering`| `bool`  | false     | Floyd-Steinberg-Dithering aktivieren      |

#### `PatternResult`
Ergebnis der Mustergenerierung: fertiges `BeadPattern`, gerendertes Bild als `byte[]`, Liste von `ColorInfo`-Objekten (Name, Hex, Anzahl, Prozentsatz).

---

## 5. Infrastrukturschicht (`FuseBeads.Infrastructure`)

### Bildverarbeitung (SkiaSharp)

#### `SkiaImageLoader`
Implementierung von `IImageLoader` via **SkiaSharp**:
- Dekodieren von Bildern aus einem `Stream`
- Skalierung auf Zieldimensionen (Bilinear-Interpolation)
- Anpassung von Helligkeit, Kontrast und Sättigung im RGB-Raum

#### `SkiaPatternRenderer`
Implementierung von `IPatternRenderer`:
- Rendert jede Perle als ausgefüllten Kreis in der Perlenfarbe
- Zeichnet eine halbtransparente Mittelbohrung
- Zieht ein Gitter zwischen den Perlen
- Hebt abgehakte Perlen mit einem dunklen Overlay und einem weißen Häkchen hervor
- Optionales Brett-Gitter-Overlay als farbige Liniensegmente

#### `SkiaPrintRenderer`
Implementierung von `IPrintRenderer` für A4 bei 300 DPI (2480 px Breite):
- `RenderPrintPage()` – Ausgabe als PNG
- `RenderPrintPageAsPdf()` – Ausgabe als PDF via `SKDocument`
- Druckseite enthält: Titel, Statistiken (Raster, Perlenanzahl, Farben), Musterbild, Farblegende

### Paletten

Alle Paletten implementieren `IBeadColorPalette` mit einer Farbliste und der Methode `FindClosestColor(r, g, b)` (euklidischer RGB-Abstand):

| Klasse                      | Palette   | Farben (ca.) |
|-----------------------------|-----------|--------------|
| `StandardBeadColorPalette`  | Standard  | ~40          |
| `HamaBeadColorPalette`      | Hama      | ~40          |
| `ArtkalBeadColorPalette`    | Artkal    | ~100+        |
| `PerlerBeadColorPalette`    | Perler    | ~70+         |

`BeadColorPaletteFactory` implementiert `IBeadColorPaletteFactory` und stellt alle Paletten per `GetPalette(PaletteType)` bereit.

### Persistenz

#### `JsonPatternStorage`
- Speichert und lädt `BeadPattern`-Objekte als JSON (System.Text.Json mit Source Generation)
- Unterstützt Datei-Streams für die Dateiauswahl-Integration
- Automatisches Speichern im App-Datenverzeichnis (`current_pattern.json`)

#### `JsonProgressStorage`
- Speichert und lädt den Legefortschritt (gesetzte Zellpositionen) als JSON
- Persistenzpfad: `LocalApplicationData/bead_progress.json`

---

## 6. Präsentationsschicht (`FuseBeads`)

### Muster (MVVM)

Das MVVM-Muster wird mit **CommunityToolkit.Mvvm** (Source Generators) umgesetzt. ViewModels erben von `ObservableObject`, Commands werden via `[RelayCommand]` generiert.

### Seiten und ViewModels

#### `MainPage` / `MainViewModel`
Hauptseite der App mit folgenden Funktionen (Commands):

| Command                     | Beschreibung                                                       |
|-----------------------------|--------------------------------------------------------------------|
| `PickImageCommand`          | Bild aus Gerätegalerie auswählen und Muster generieren             |
| `RegeneratePatternCommand`  | Muster mit neuen Einstellungen aus dem letzten Bild neu generieren |
| `PrintCommand`              | Druckbild als PNG erzeugen und teilen                              |
| `ExportPdfCommand`          | Muster als PDF exportieren und teilen                              |
| `ShowInstructionsCommand`   | Zur Reihenanleitung-Seite navigieren                               |
| `SavePatternCommand`        | Muster als JSON-Datei speichern                                    |
| `LoadPatternCommand`        | JSON-Muster aus Datei laden                                        |
| `ShareShoppingListCommand`  | Einkaufsliste als Text teilen                                      |
| `UndoCommand`               | Letzte manuelle Zellenbearbeitung rückgängig machen                |
| `RedoCommand`               | Rückgängig gemachte Bearbeitung wiederherstellen                   |
| `ResetProgressCommand`      | Legefortschritt zurücksetzen                                       |
| `ToggleControlsPanelCommand`| Einstellungsbereich ein-/ausklappen                                |

**Weitere Funktionalitäten im ViewModel:**
- `EditCell(row, col, newColor)` – Manuelle Einzelzellenbearbeitung mit Undo-Stack
- `ToggleBead(row, col)` – Perle als platziert markieren / Markierung aufheben
- `InitializeAsync()` – Lädt automatisch das zuletzt gespeicherte Muster beim Start
- Automatisches Speichern nach jeder Mustergenerierung

#### `InstructionPage` / `InstructionViewModel`
Zeigt das Muster reihenweise als farbige Perlenreihen-Anleitung an. Jede Zeile enthält die Perlenfarben der entsprechenden Musterreihe. Die Seite empfängt das `BeadPattern`-Objekt via Shell-Navigation (Query-Parameter).

### Lokalisierung

Die App unterstützt mehrsprachige Zeichenketten über `AppResources.resx` mit dem MAUI `LocalizeExtension`-Helper.

### Navigation

Die Navigationsstruktur basiert auf der MAUI Shell:
- `MainPage` – Hauptseite (Root)
- `InstructionPage` – Reihenanleitung (navigierbar via Shell Route `InstructionPage`)

---

## 7. Implementierte Features

| # | Feature                        | Beschreibung                                                                                     |
|---|--------------------------------|--------------------------------------------------------------------------------------------------|
| 1 | **Bildimport**                 | Auswahl eines Fotos oder Bildes vom Gerät                                                        |
| 2 | **Automatische Mustergenerierung** | Konvertierung des Bildes in ein Bügelperlen-Raster mit Farbzuordnung per nächstem RGB-Abstand   |
| 3 | **Palette-Auswahl**            | Unterstützung von vier Markenpaletten: Standard, Hama, Artkal, Perler                           |
| 4 | **Bildvorverarbeitung**        | Einstellbare Helligkeit, Kontrast und Sättigung vor der Mustergenerierung                        |
| 5 | **Farblimitierung**            | Begrenzung der Anzahl verwendeter Farben; seltene Farben werden auf die nächste erlaubte remappt |
| 6 | **Floyd-Steinberg-Dithering**  | Verbesserte Farbdarstellung durch Fehlerstreuung für detailreichere Muster                       |
| 7 | **Brett-Raster-Overlay**       | Einblendbares Hilfsraster, das die Pegboard-Brettgrenzen visualisiert                           |
| 8 | **Manuelle Zellenbearbeitung** | Einzelne Perlen im Muster per Tap manuell umfärben                                              |
| 9 | **Undo / Redo**                | Unbegrenzte Rückgängig- und Wiederherstellen-Funktion für manuelle Bearbeitungen                 |
| 10| **Drucken (PNG)**              | Erzeugung einer druckoptimierten PNG-Datei (A4, 300 DPI) mit Muster und Farblegende            |
| 11| **PDF-Export**                 | Export als PDF-Datei mit Titel, Statistiken, Muster und Farblegende                             |
| 12| **Schritt-für-Schritt-Anleitung** | Reihenweise Farbübersicht aller Perlen als Legeanleitung                                     |
| 13| **Muster speichern / laden**   | Export und Import von Mustern im JSON-Format                                                     |
| 14| **Automatisches Speichern**    | Das aktuelle Muster wird automatisch beim Verlassen gesichert und beim Start wiederhergestellt   |
| 15| **Einkaufsliste teilen**       | Generierung und Teilen einer Farbbedarfsliste mit Stückzahlen und Prozentwerten                 |
| 16| **Legefortschritt-Tracking**   | Einzelne Perlen als gelegt markieren; Fortschritt wird persistent gespeichert                   |
| 17| **Fortschritt zurücksetzen**   | Alle Fortschrittsmarkierungen in einem Schritt löschen                                          |

---

## 8. Technologie-Stack

| Technologie / Bibliothek           | Version    | Verwendungszweck                               |
|------------------------------------|------------|------------------------------------------------|
| .NET 10                            | 10.0       | Zielframework                                  |
| .NET MAUI                          | 10.x       | Plattformübergreifende UI                      |
| SkiaSharp.Views.Maui.Controls      | 3.119.0    | Bildverarbeitung, Pattern- und Druckrendering  |
| CommunityToolkit.Mvvm              | 8.4.0      | MVVM-Implementierung (Source Generators)       |
| CommunityToolkit.Maui              | 13.0.0     | MAUI-Erweiterungen (FileSaver etc.)            |
| System.Text.Json                   | (built-in) | JSON-Serialisierung mit Source Generation      |
| Microsoft.Extensions.DependencyInjection | (built-in) | Dependency Injection Container           |

---

## 9. Dependency Injection

Die Registrierung aller Dienste erfolgt in `FuseBeads.Infrastructure.DependencyInjection` über die Extension-Methode `AddInfrastructure()`:

```
IBeadColorPalette         → StandardBeadColorPalette    (Singleton)
IBeadColorPaletteFactory  → BeadColorPaletteFactory     (Singleton)
IImageLoader              → SkiaImageLoader              (Singleton)
IPatternRenderer          → SkiaPatternRenderer          (Singleton)
IPrintRenderer            → SkiaPrintRenderer            (Singleton)
IPatternStorage           → JsonPatternStorage           (Singleton)
IProgressStorage          → JsonProgressStorage          (Singleton)
IPatternService           → PatternService               (Transient)
IFileSaver                → FileSaver.Default            (Singleton)
MainViewModel             →                              (Transient)
InstructionViewModel      →                              (Transient)
```

---

## 10. Datenpersistenz

| Datei                                          | Format | Inhalt                            |
|------------------------------------------------|--------|-----------------------------------|
| `<AppDataDirectory>/current_pattern.json`      | JSON   | Aktuelles Muster (Auto-Save)      |
| `<LocalApplicationData>/bead_progress.json`    | JSON   | Legefortschritt (gesetzte Perlen) |
| Benutzerdefinierter Pfad (via FileSaver)        | JSON   | Manuell gespeichertes Muster      |
| `<CacheDirectory>/Bugelperlen-Druck.png`       | PNG    | Temporäre Druckdatei              |
| `<CacheDirectory>/Bugelperlen-Muster.pdf`      | PDF    | Temporäre PDF-Exportdatei         |
