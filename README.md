# InkOverlay

Et lite skjermtegneverktøy for Windows, bygget med C#, WPF og .NET 10.

## Installer

Last ned `InkOverlay-win-Setup.exe` fra [Releases](https://github.com/Busemann-ai/InkOverlay/releases)
og kjør den. Den installerer bare for deg (ingen adminrettigheter) til `%LocalAppData%\InkOverlay`,
legger InkOverlay i Start-menyen og starter appen. Avinstaller fra **Innstillinger → Apper**.

Versjoner som ikke er kodesignert ennå (se [Kodesignering](#code-signing-policy)) kan gi en
advarsel fra Windows SmartScreen første gang: velg **Mer info → Kjør likevel**.

- **Start med Windows**: første gang appen starter etter installasjon, spør den om den skal
  starte automatisk. Slå det av/på senere i menyen i statusfeltet.
- **Oppdateringer**: ved oppstart sjekker appen GitHub Releases og laster ned en ny versjon i
  bakgrunnen; den tas i bruk neste gang appen starter. «Se etter oppdateringer» i menyen
  oppdaterer og starter på nytt med en gang. Første gang en ny versjon starter, får du en melding
  om at den er oppdatert. **Om InkOverlay** i menyen viser hvilken versjon du har og når den sist
  ble installert eller oppdatert.

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

Releaser bygges, signeres og publiseres av GitHub Actions: **Actions → Release → Run workflow**,
og skriv inn versjonen. Signeringen må godkjennes i SignPath før releasen publiseres.
Versjonsnummeret må være høyere enn forrige publiserte versjon for at installerte kopier skal oppdatere seg.

For å teste en usignert Setup.exe lokalt (engangsoppsett: `dotnet tool install -g vpk`):

```powershell
.\build-release.ps1 -Version 1.0.1            # bygger Releases\InkOverlay-win-Setup.exe
```

## Ikon

Ikonet ligger i `Assets\InkOverlay.ico` (16–256 px i samme fil), med kilden i `Assets\InkOverlay.svg`.
Det brukes for exe-filen, Setup.exe, Start-menyen og statusfeltet. Bytt ut filen og bygg på nytt for å endre det.

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
| `build-release.ps1` | Bygger Setup.exe lokalt |
| `.github/workflows/release.yml` | Bygger, signerer og publiserer en release |
| `.signpath/` | Artefaktkonfigurasjoner for SignPath |

## Begrensninger i MVP

- Hvis skjermoppsettet endres mens appen kjører, må den startes på nytt. Det gjelder også om du slår
  automatisk skjuling av oppgavelinjen av eller på.
- Tegningen lagres ikke mellom økter.
- Ingen figurer, tekst, whiteboard eller skjermbilder ennå. Det er neste trinn.

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by
[SignPath Foundation](https://signpath.org/).

Bare filer som er bygget av [release-workflowen](.github/workflows/release.yml) fra kildekoden i
dette repoet, blir signert, og hver release godkjennes manuelt før den signeres.

| Rolle | Medlemmer |
|---|---|
| Committers og reviewers | [Busemann-ai](https://github.com/Busemann-ai) |
| Approvers | [Busemann-ai](https://github.com/Busemann-ai) |

## Personvern

InkOverlay samler ikke inn data og sender ingenting om deg eller det du tegner. Den eneste
nettverkstrafikken er oppdateringssjekken: ved oppstart, og når du velger «Se etter oppdateringer»,
henter appen listen over releaser fra GitHub (`github.com/Busemann-ai/InkOverlay`) og laster ned en
eventuell ny versjon derfra. GitHub ser da IP-adressen din, som ved ethvert besøk; se
[GitHubs personvernerklæring](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement).
Innstillingene lagres bare lokalt i `%AppData%\InkOverlay\settings.json`.

## Lisens

[MIT](LICENSE)
