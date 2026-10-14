# InkOverlay

Et lite skjermtegneverktøy for Windows (inspirert av Epic Pen), bygget med C#, WPF og .NET 10.

## Kjør

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

## Oppbygning

| Fil | Ansvar |
|---|---|
| `App.xaml.cs` | Oppstart, én instans (mutex), kommandoer og lokale hurtigtaster |
| `AppState.cs` | Verktøy, farge, størrelse og synlighet, med `Changed`-hendelse |
| `OverlayWindow` | Gjennomsiktig `InkCanvas` per skjerm. Klikk gjennom med `WS_EX_TRANSPARENT` |
| `ToolbarWindow` | Flytbar verktøylinje |
| `FadingInk.cs` | Fjerner streker etter valgt antall sekunder, med uttoning |
| `UndoManager.cs` | Angre på tvers av skjermer, gruppert per pennebevegelse |
| `HotkeyManager.cs` | Globale hurtigtaster via `RegisterHotKey` |
| `TrayIcon.cs` | Ikon og meny i systemstatusfeltet (WinForms `NotifyIcon`) |
| `Settings.cs` | Farge og størrelse i `%AppData%\InkOverlay\settings.json` |

## Begrensninger i MVP

- Hvis skjermoppsettet endres mens appen kjører, må den startes på nytt.
- Tegningen lagres ikke mellom økter.
- Ingen figurer, tekst, whiteboard eller skjermbilder ennå. Det er neste trinn.
