# Ændringslog

Alle væsentlige ændringer i Rust Optimizer er dokumenteret her.

## Unreleased
- Tilføjet bløde hover- og tryk-overgange til knapper, og rettet et farveglimt, der kortvarigt opstod ved skift af det aktive sidebar-element, mest tydeligt i mørk tilstand.

## 0.8.1
- Tilføjet en rigtig "Launch Rust"-knap, der starter spillet via Steam og automatisk deaktiverer sig selv, mens Rust allerede kører.
- Tilføjet de manglende danske og russiske oversættelser til dashboard, sidebar og indstillinger, som tidligere faldt tilbage til engelsk.

## 0.8.0

- Tilføjet dashboardets sidebar-navigation: systemoverblik, hurtige optimeringsprofiler og forudindstillede profiler (i øjeblikket testdata, indtil rigtig systemdetektering er på plads).
- Redesignet Indstillinger-siden med ikonbaserede vælgere til Lys/Mørk/System-tema og sprog.
- Opgraderet Om-siden med en manuel "Tjek for opdateringer"-knap samt links til GitHub, Discord og Ko-fi.
- Skiftet ikonsæt til Phosphor Icons (med Simple Icons til brandlogoer), hvilket gør appens downloadstørrelse markant mindre.

![testdata](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/0.8.0%20mock%20ui.png)

## 0.7.0

- Tilføjet en Windows-installer (`Setup.exe`) som alternativ til de bærbare zip-filer, der installeres pr. bruger med genveje i startmenuen og en valgfri skrivebordsgenvej.
- Tilføjet automatisk opdateringstjek: ved opstart tjekkes GitHub for en nyere udgivelse, og hvis der findes en, vises versionen og den viste ændringslog direkte i vinduet, før du beslutter dig for at opdatere.
- Tilføjet opdatering med et enkelt klik, der downloader og anvender den nye version (genkører installationsprogrammet for installerede kopier, udskifter filer for bærbare kopier) og genstarter automatisk.

## 0.6.1

- Rettet at billeder og GIF'er i ændringsloggen blev for store, når vinduet blev bredere, ved at begrænse deres visningsstørrelse i stedet for at lade dem strække sig til den tilgængelige bredde.

## 0.6.0

- Tilføjet en indbygget ændringslog-visning, så opdateringer kan forklare *hvorfor* de skete, i stedet for bare at annoncere et nyt versionsnummer.
- Tilføjet understøttelse af billeder og animerede GIF'er i ændringsloggen, afkodet billede-for-billede uden ekstra afhængigheder.

![Ændringslog-visning](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/screenshot-0.6.0.png)

![Demo af sprogskift](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/demo-0.6.0.gif)

## 0.5.0

- Rettet at framework-afhængige builds medsendte selvstændige runtime-filer.
- Ændret produktversionen, der bruges til filversionen vist i titellinjen.

## 0.4.0

- Tilføjet en brugerdefineret, kantløs `TitleBar`-kontrol på Windows, som erstatter den native titellinje.
- Tilføjet understøttelse af lokalisering (engelsk, dansk, russisk) med automatisk registrering af systemsprog.
- Tilføjet skift mellem lyst/mørkt/system-tema.
- Tilføjet hele IconPacks.Avalonia-ikonsættet via et submodul (indtil en officiel Avalonia 12-udgivelse findes).
