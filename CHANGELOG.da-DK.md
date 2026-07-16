# Ændringslog

Alle væsentlige ændringer i Rust Optimizer er dokumenteret her.

## Unreleased
- Tilføjet Raidboi-1129 (iOS og Android) til Værktøjer-sidens ressourceliste - en mobil raid-omkostnings- og loot-beregner til Rust.

## 0.8.6
- Fjernet den separate Configs-side og lagt den ind i en ny Sikkerhedskopi & Gendan-side, da de begge reelt handlede om det samme: at administrere Rusts cfg-filer.
- Tilføjet en Sikkerhedskopi & Gendan-side: skift mellem Indstillinger (`client.cfg`) og Tastebindinger (`keys.cfg`), se hver enkelts sikkerhedskopihistorik, opret en manuelt navngivet sikkerhedskopi (eller lad navnet stå tomt for et tidsstempel), og gendan eller slet enhver post - begge dele beder om bekræftelse først, da en sletning ikke kan fortrydes.
- Sikkerhedskopier tages nu automatisk, før en forudindstillet profil anvendes, og før en gendannelse overskriver den aktive fil, hvilket erstatter den gamle enkelte `client.cfg.bak`, der kun nogensinde gemte én kopi.
- Låst Dashboardets forudindstillede profiler, medmindre Rust er installeret, i stedet for at de kunne klikkes uden effekt.
- Systemsidens Lagerplads-kort opdaterer nu hvert par sekunder i stedet for kun at indlæse én gang, når siden åbnes.
- Rettet at knappers hover-baggrunde i hele appen viste skarpe firkantede hjørner i stedet for at følge knappens egen afrundede form.

## 8.0.5
- Tilføjet en Systemside (tilgængelig fra sidebaren): CPU-/GPU-/RAM-forbrug og specifikationer, bundkort/BIOS, lagerplads og OS-oplysninger, samt OS-niveau-justeringer - strømplan, markørpræcision, Spilfunktion, baggrundsoptagelse og fuldskærmsoptimeringer for Rust.
- Tilføjet advarselsikoner ved enhver indstilling på Systemsiden, der ikke står på den anbefalede værdi, med et tooltip, der forklarer hvorfor - herunder for lidt RAM (under 16 GB), RAM der kører under sin vurderede hastighed (XMP/EXPO ikke aktiveret i BIOS), og lidt ledig plads på Rusts drev (under 10%).
- Koblet Dashboardets Optimeringsoversigt-System-kort til Systemsidens rigtige indstillinger i stedet for en hardkodet "12 / 16 settings"-pladsholder, og givet det en tre-trins rød/gul/grøn status i stedet for en simpel god/ikke-optimeret-opdeling.
- Nedtonet Optimeringsoversigtens Ydeevne-, Netværk- og Grafik-kort, som stadig er testdata, så de matcher "kommer snart"-stilen, der allerede bruges til andre ufærdige funktioner, så kun System-kortet fremstår interaktivt.
- Gjort Optimeringsoversigtens System-kort klikbart, så det springer direkte til Systemsiden - samme destination som Systemoplysninger-kortets "Flere detaljer"-række.
- Rettet at advarselsikonernes tooltip nogle gange krævede, at musen blev flyttet frem og tilbage, før det viste sig.
- Rettet at Dashboardets System-score ikke opdaterede sig, efter en indstilling på Systemsiden blev ændret, før appen blev genstartet.
- Tilføjet understøttelse af ~~gennemstregning~~ i ændringslog-visningen.

## 0.8.4
- Tilføjet rigtig registrering af Rust-installationen: appen finder nu Rusts faktiske installationsmappe via Steam, så "Start Rust" og "Verificér spilfiler" korrekt deaktiverer sig selv (med en rød statusindikator i sidebaren), hvis Rust ikke er installeret, i stedet for at antage, at det altid er.
- Tilføjet fungerende forudindstillede profiler: Svag PC, Konkurrence, Streamer og Filmisk ændrer nu faktisk Rusts grafik-/ydeevneindstillinger i `client.cfg` (med sikkerhedskopiering af originalen først) i stedet for at være ikke-funktionelle pladsholdere.
- Fjernet "Hurtig optimering"-sektionen, som duplikerede forudindstillede profiler uden at tilføje noget nyt.

## 0.8.3
- Rettet at Dashboardet valgte din integrerede GPU i stedet for den dedikerede (og viste dens forbrug) på systemer med begge dele, f.eks. en AMD APU sammen med et Radeon-kort.
- Flyttet registrering af CPU-/GPU-navn og RAM fra WMI og Win32-hukommelses-API'et til den samme LibreHardwareMonitor-backend, der allerede bruges til aktuelt forbrug.

## 0.8.2
- Tilføjet rigtige systemoplysninger til Dashboardets Systemoplysninger-kort: din faktiske CPU-/GPU-model og aktuelle forbrug, samt RAM brugt/i alt, i stedet for de gamle pladsholderværdier.
- Tilføjet bløde hover- og tryk-overgange til knapper, og rettet et farveglimt, der kortvarigt opstod ved skift af det aktive sidebar-element, mest tydeligt i mørk tilstand.
- Rettet at changelog-tekst løb ud over vinduets kant i stedet for at ombryde.
- Tilføjet understøttelse af citater og kodeblokke i changelog-visningen, og gav indlejret `kode` en rigtig baggrund og kant i stedet for blot at skifte skrifttype.

> Sådan ser et citat ud nu.

```
kodeblokke vises nu
i en indrammet, monospaced boks
```

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