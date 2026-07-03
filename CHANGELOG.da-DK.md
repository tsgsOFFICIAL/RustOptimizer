# Ændringslog

Alle væsentlige ændringer i Rust Optimizer er dokumenteret her.

## Unreleased

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
