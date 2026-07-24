# Ændringslog

Alle væsentlige ændringer i Rust Optimizer er dokumenteret her.

## 0.10.4

### Grafikprofiler

![Grafikprofiler](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/graphics-profiles.gif)

Grafik-siden har nu en profilvælger, tilgået fra Dashboardets **Administrer profiler**-link. Vælg Low End PC, Competitive eller Cinematic, og alle skydere springer til at matche; flyt en skyder bagefter, og den markeres som ugemt, så du altid ved, om det du kigger på, rent faktisk er det, der er anvendt.

- **Gem dine egne profiler** - juster skyderne fra et hvilket som helst udgangspunkt, og gem resultatet under sit eget navn. Vælger du en gemt profil senere, anvendes præcis de samme indstillinger igen.
- **Omdøb og slet** dine egne profiler. De tre indbyggede forudindstillinger kan ikke ændres.
- Hvis dine skydere tilfældigvis lander på nøjagtig de samme indstillinger som en af de indbyggede forudindstillinger, genkender vælgeren det som den forudindstilling i stedet for at lade dig gemme en duplikat.
- **Skyggekvalitet og vandkvalitet har nu tre reelt forskellige niveauer.** Mellem var tidligere identisk med Lav under motorhjelmen for begge, så det ikke gjorde nogen forskel at skifte mellem dem. Mellem er nu sin egen, adskilte indstilling.

## 0.10.3

Ryd Cache er blevet testet på en rigtig AMD-maskine og en aktiv Steam-installation, og flere af de steder, den kiggede, viste sig ikke at være steder. Den rydder nu en hel del mere end før.

- **Steams indbyggede browser bliver nu ryddet** - butikken, fællesskabet og overlayet cacher alle igennem den, og den var vokset til omkring 500 MB uden nogensinde at blive rørt. Det er nu typisk det største enkeltstående, Ryd Cache frigør. Din indlogning og dine butiksindstillinger bliver ikke rørt.
- **Steams depot-cache blev aldrig ryddet** - der blev ledt efter den én mappe fra, hvor den rent faktisk ligger. Omkring 55 MB på en typisk installation.
- Ryd Cache rydder nu også Steams downloadmappe og dens nedbrudsrapporter. Spilgrafik og Steams egne logfiler bliver bevidst ikke rørt, da grafikken koster en ny download at genskabe, og logfilerne er dem, Steams support beder om.
- **Flere AMD-shadercaches bliver nu ryddet** - Vulkan, OpenGL og DirectX 9, plus endnu en DirectX-cache, der slet ikke blev fundet. Der blev tidligere ledt efter OpenGL-cachen under NVIDIAs navn for den, så på en AMD-maskine blev den aldrig fundet, og der blev ledt efter en mappe, som ganske enkelt ikke findes på nogen driver.
- **Rusts Unity-logfiler bliver nu rent faktisk ryddet.** Der blev ledt efter dem under `Facepunch`, men den mappe Rust skriver til, hedder `Facepunch Studios LTD`, så `Player.log` var aldrig blevet fjernet - og da intet roterer den, vokser den, så længe installationen findes.
- Ryd Cache rydder nu Rusts logfiler før Windows' midlertidige filer i stedet for efter. En af logplaceringerne ligger inde i den midlertidige mappe, så den tidligere rækkefølge betød, at temp-trinnet allerede havde slettet den, og logtrinnet meldte, at det intet fandt.

## 0.10.2

- Rettet at **Behold logs i** ikke havde nogen effekt over 30 dage. Gamle logfiler blev ryddet, mens appen startede - altså før din indstilling var læst - så alt ældre end 30 dage blev slettet uanset hvad du havde valgt. Oprydningen venter nu, til din indstilling er kendt.

## 0.10.1

- Rettet at skærmbilleder og GIF'er i denne ændringslog var låst til en lille fast bredde, så de lå forladt midt i et bredt vindue. De bruger nu den plads, der er til rådighed, uden at blive skaleret op ud over deres egen opløsning.

## 0.10.0

### Indstillinger, bygget om

![Indstillinger](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/settings-page.gif)

Grupperet i **Udseende**, **Program**, **Opdateringer**, **Enheder** og **Data** - og hver mulighed forklarer nu, hvad den rent faktisk gør, i stedet for kun at være en overskrift.

- **Tema og Sprog er dropdowns** - mere overskueligt, efterhånden som der kommer flere sprog til, og hvert sprog vises med sit flag.
- **Start med Windows** - starter Rust Optimizer, når du logger ind. Pr. bruger, så den beder aldrig om administratorrettigheder.
- **Opdateringsindstillinger** - søg efter en nyere version ved opstart, og installer den eventuelt automatisk. Automatisk installation er slået fra som standard, da en opdatering genstarter appen.
- **Enheder for netværkshastighed** - `MB/s`, der svarer til filstørrelser, eller `Mbps`, der svarer til den måde forbindelser markedsføres på. Netværkssiden følger dit valg.
- **Logindstillinger** - gem logfiler i 7, 30 eller 90 dage, åbn logmappen direkte, og slå **detaljeret logning** til.

> Slå detaljeret logning til, *før* du genskaber et problem. Så forklarer loggen, hvad appen rent faktisk lavede - hvilke stier den kiggede på, hvad den fandt, og hvor lang tid hvert trin tog.

> **Bemærk:** dit tema og sprog nulstilles til standardværdierne én gang, når du opdaterer. Alle indstillinger ligger nu i én samlet fil, og de gamle overføres ikke. Vælg dem igen, så bliver de husket fremover.

### Om

- Tilføjet en **Programoplysninger**-sektion - version, builddato, styresystem og licens - så en fejlrapport kan indeholde de oplysninger, der betyder noget, uden at du skal lede efter dem.
- Flyttet Ko-fi-linket ned i sidefoden ved siden af GitHub og Discord, så det kan nås fra alle sider i stedet for kun herfra.

### Andet

- Links, appen åbner, fortæller nu modtagersiden, at de kommer fra Rust Optimizer.

### Rettelser

- Almindelige knapper havde skarpere hjørner end alle andre knapper i appen.
- De danske og russiske navne for *Backup & Gendan* blev skåret af i sidemenuen.
- Grafik, Gameplay, Indstillinger og Om lå i en anden bredde end de øvrige sider, hvilket fik deres overskrifter til at stå forskudt.

## 0.9.0
- Tilføjet **Ryd cache** til Dashboardets hurtige handlinger: rydder midlertidige Windows-filer, GPU'ens shader-caches (NVIDIA, AMD, Intel og DirectX), Steams download-, depot- og butikscaches, Rusts Unity-logs og programmernes nedbrudsdumps - og viser derefter, hvor meget der rent faktisk blev frigjort under knappen.
- Ryd cache spørger, før den kører, med tre ting du kan slå fra først: tømning af papirkurven, rydning af miniaturecachen og medtagelse af systemfiler (som beder om administratorgodkendelse). Alt andet, den rydder, er sikkert at fjerne og bygges op igen af sig selv.
- Ryd cache viser sit forløb undervejs og navngiver hver gruppe, mens den arbejder, og den kan stoppes undervejs - en afbrudt kørsel viser stadig, hvor meget den nåede at frigøre.
- Ryd cache rører ikke shader-caches, mens Rust kører - og siger det, i stedet for i stilhed at frigøre mindre end forventet. Filer, der reelt er i brug, springes over og tælles med i stedet for at blive behandlet som fejl.
- Rust kan tage lidt længere tid om at starte første gang efter en rydning af shader-caches, mens de bygges igen. Det er forventet, og det står i prompten på forhånd.
- Tilføjet en **Netværk**-side: live info om din aktive adapter (linkhastighed, lokal IPv4/gateway/MAC/DNS), en løbende opdateret ping- og jitter-måling til 1.1.1.1, live download-/upload-hastighed, og et valgfrit offentligt IP-opslag, du selv tjekker i stedet for at det sker automatisk - samt et link til Speedtest.net for en fuld download-/upload-/latenstest.
- Tilføjet tre Netværk-justeringer - deaktivering af Windows' netværksdrosling, strømbesparelse for netværkskort og reserveret QoS-båndbredde - som hver især kort viser en administratorprompt, når de anvendes, da disse specifikke indstillinger kræver forhøjede rettigheder for at ændre.
- Tilføjet et advarselsikon på Netværk-siden, når din aktive forbindelse er Wi-Fi i stedet for kabelforbundet Ethernet, da et kabel giver markant lavere og mere konsistent latenstid.
- Forbundet Dashboardets Optimeringsoversigt Netværk-felt til Netværk-sidens rigtige indstillinger i stedet for en hardkodet "8 / 10 indstillinger"-pladsholder, ligesom System-feltet allerede fungerer.

![Netværk-siden](https://raw.githubusercontent.com/tsgsOFFICIAL/RustOptimizer/master/Assets/Changelog/network-page.png)

## 0.8.8
- Tilføjet en **Grafik**-side: forenklede kvalitetsskydere (Skyggekvalitet, Teksturkvalitet, Effektkvalitet, Synsafstand, Verdensdetaljer, Vandkvalitet, Inventar-visning), der hver skriver direkte til `client.cfg` - de samme convar-værdier, som Forudindstillede profiler allerede bruger, nu justerbare én indstilling ad gangen i stedet for kun som hele pakker.
- Hver Grafik-skyder viser et live forhåndsvisningsbillede, der opdateres, når du trækker mellem Lav/Mellem/Høj, inklusive animerede GIF'er til før/efter-sammenligninger - falder tilbage til en simpel pladsholder for enhver indstilling, der endnu ikke har et forhåndsvisningsbillede.

## 0.8.7
- Tilføjet en **Gameplay**-side: en kurateret liste af valgfrie `client.cfg`-justeringer, der forbedrer synlighed eller klarhed uden ydelsesomkostninger, opdelt i "Anbefalet til alle" (ingen reel ulempe for nogen) og "Præferencer" (et spørgsmål om smag) - hver justering kan tændes/slukkes individuelt, og hver sektion har en hovedkontakt, der slår alt i den til/fra på én gang.
- At slå en Gameplay-justering til/fra skriver nu direkte til `client.cfg` uden at tage et fuldt sikkerhedskopi-øjebliksbillede hver gang, i modsætning til Forudindstillede profiler - at vende et par indstillinger frem og tilbage fylder ikke længere Sikkerhedskopi & Gendan-historikken med overflødige poster.
- Tilføjet Raidboi-1129 (iOS og Android) til Værktøjer-sidens ressourceliste - en mobil raid-omkostnings- og loot-beregner til Rust.
- Fjernet Streamer-profilen fra Forudindstillede profiler (nu Svag PC, Konkurrence og Filmisk) for at reducere antallet af valgmuligheder.
- Tilføjet et Skærm-kort til Systemsiden: nuværende vs. maksimal opdateringsfrekvens og opløsning, markeret med et advarselsikon og værktøjstip, når en af delene kører under det, skærmen faktisk understøtter - f.eks. en 144Hz-skærm der sidder fast på 60Hz, eller en 4K-skærm der kører i 1080p.
- Udfyldt de manglende danske og russiske oversættelser for hele Sikkerhedskopi & Gendan-siden og dens bekræftelsesdialoger, som tidligere stille faldt tilbage til engelsk.
- Rettet at Indstillinger-sidens Lys/Mørk/System-temaetiketter og Sikkerhedskopi & Gendan-sidens Gendan/Slet-værktøjstips var hardkodet på engelsk uanset det valgte sprog.

## 0.8.6
- Fjernet den separate Configs-side og lagt den ind i en ny Sikkerhedskopi & Gendan-side, da de begge reelt handlede om det samme: at administrere Rusts cfg-filer.
- Tilføjet en Sikkerhedskopi & Gendan-side: skift mellem Indstillinger (`client.cfg`) og Tastebindinger (`keys.cfg`), se hver enkelts sikkerhedskopihistorik, opret en manuelt navngivet sikkerhedskopi (eller lad navnet stå tomt for et tidsstempel), og gendan eller slet enhver post - begge dele beder om bekræftelse først, da en sletning ikke kan fortrydes.
- Sikkerhedskopier tages nu automatisk, før en forudindstillet profil anvendes, og før en gendannelse overskriver den aktive fil, hvilket erstatter den gamle enkelte `client.cfg.bak`, der kun nogensinde gemte én kopi.
- Låst Dashboardets forudindstillede profiler, medmindre Rust er installeret, i stedet for at de kunne klikkes uden effekt.
- Systemsidens Lagerplads-kort opdaterer nu hvert par sekunder i stedet for kun at indlæse én gang, når siden åbnes.
- Rettet at knappers hover-baggrunde i hele appen viste skarpe firkantede hjørner i stedet for at følge knappens egen afrundede form.

## 0.8.5
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