# InkOverlay

Et lite skjermtegneverktøy for Windows (inspirert av Epic Pen), bygget med C#, WPF og .NET 10.

## Installer

Last ned `InkOverlay-win-Setup.exe` fra [Releases](https://github.com/Busemann-ai/InkOverlay/releases)
og kjør den. Den installerer bare for deg (ingen adminrettigheter) til `%LocalAppData%\InkOverlay`,
legger InkOverlay i Start-menyen og starter appen. Avinstaller fra **Innstillinger → Apper**.

Programmet er ikke kodesignert, så Windows SmartScreen kan advare første gang: velg
**Mer info → Kjør likevel**.

- **Start med Windows** er slått på etter installasjon. Slå det av/på i menyen i statusfeltet.
- **Oppdateringer**: ved oppstart sjekker appen GitHub Releases og laster ned en ny versjon i
  bakgrunnen; den tas i bruk neste gang appen starter. «Se etter oppdateringer» i menyen
  oppdaterer og starter på nytt med en gang.

## Kjør fra kildekoden

```powershell
dotnet run
```

Appen starter i **pekermodus**: overlegget er usynlig og slipper alle klikk gjennom.
Verktøylinjen ligger til venstre på hovedskjermen, og ikonet ligger i systemstatusfeltet.

## Hurtigtaster

| Globalt | |
|---|---|
| Ctrl+Shift+D | Veksle mellom tegning og peker |
| Ctrl+Shift+Z | Angre |
| Ctrl+Shift+X | Tøm skjermen |
| Ctrl+Shift+H | Vis/skjul blekk |
| Ctrl+Shift+Y | Vis/skjul verktøylinjen |

| I tegnemodus | |
|---|---|
| P / H / E | Penn / markeringstusj / viskelær |
| F | Fjern tegning etter: Av → 3 → 5 → 10 → 15 s |
| 1–6 | Farge |
| + / − | Større/mindre strek |
| Ctrl+Z | Angre |
| Delete | Tøm |
| Esc | Tilbake til peker |

## Verktøylinjen

Øverst på verktøylinjen er det to små knapper: pila minimerer linjen til bare toppen (huskes til
neste oppstart), og streken skjuler den helt. En skjult linje vises igjen med Ctrl+Shift+Y eller
fra menyen i statusfeltet. Hold musen over en knapp for å se hva den gjør.

## Fjern tegning automatisk

Knappen ⏱ på verktøylinjen (eller F, eller menyen i statusfeltet) velger om nye streker skal
forsvinne etter 3, 5, 10 eller 15 sekunder. Streken toner ut det siste halve sekundet og
forsvinner også fra angre-historikken. Innstillingen gjelder streker som tegnes etter at den er
valgt, og huskes til neste oppstart.

## Lag en ny versjon

Engangsoppsett: `dotnet tool install -g vpk` og `gh auth login`.

```powershell
.\build-release.ps1 -Version 1.0.1            # bygger Releases\InkOverlay-win-Setup.exe
.\build-release.ps1 -Version 1.0.1 -Publish   # ... og publiserer den på GitHub Releases
```

Versjonsnummeret må være høyere enn forrige publiserte versjon for at installerte kopier skal oppdatere seg.

## Ikon

Legg ikonet i `Assets\InkOverlay.ico` (helst med 16, 24, 32, 48 og 256 px i samme fil) og bygg på nytt.
Det brukes da for exe-filen, Setup.exe, Start-menyen og statusfeltet. Uten filen brukes Windows sitt standardikon.

## Oppbygning

| Fil | Ansvar |
|---|---|
| `Program.cs` | `Main`: kjører Velopack (installasjon, avinstallering, oppdatering) før WPF starter |
| `App.xaml.cs` | Oppstart, én instans (mutex), kommandoer og lokale hurtigtaster |
| `AppState.cs` | Verktøy, farge, størrelse og synlighet, med `Changed`-hendelse |
| `OverlayWindow` | Gjennomsiktig `InkCanvas` per skjerm. Klikk gjennom med `WS_EX_TRANSPARENT` |
| `ToolbarWindow` | Flytbar verktøylinje |
| `FadingInk.cs` | Fjerner streker etter valgt antall sekunder, med uttoning |
| `UndoManager.cs` | Angre på tvers av skjermer, gruppert per pennebevegelse |
| `HotkeyManager.cs` | Globale hurtigtaster via `RegisterHotKey` |
| `TrayIcon.cs` | Ikon og meny i systemstatusfeltet (WinForms `NotifyIcon`) |
| `Settings.cs` | Farge og størrelse i `%AppData%\InkOverlay\settings.json` |
| `Autostart.cs` | «Start med Windows» via `HKCU\...\CurrentVersion\Run` |
| `Updater.cs` | Oppdateringer fra GitHub Releases (Velopack) |
| `build-release.ps1` | Bygger Setup.exe og publiserer en release |

## Begrensninger i MVP

- Hvis skjermoppsettet endres mens appen kjører, må den startes på nytt.
- Tegningen lagres ikke mellom økter.
- Ingen figurer, tekst, whiteboard eller skjermbilder ennå. Det er neste trinn.
