# PassTypePro

PassTypePro ist ein Windows-Tray-Tool als technischer Entwurf fuer den Anwendungsfall, Zugangsdaten oder andere Texte per simulierten Tastatureingaben in Zielsysteme einzutippen, in denen `Copy/Paste` unzuverlaessig oder gesperrt ist, zum Beispiel in virtuellen Maschinen, RDP-Sessions, Konsolenfenstern oder Remote-Installationsdialogen.

Der aktuelle Stand ist ein bewusst schlankes MVP-Grundgeruest mit Fokus auf:

- kleine Tray-App statt grosses Hauptfenster
- globale Hotkeys fuer schnelle Aktionen mit echter Hotkey-Erfassung
- eigener globaler Hotkey pro Secret
- simulierte Tastatureingabe ueber `SendInput`
- Typing-Sequenzen fuer Login-Ablaufprofile
- integrierte TOTP-Unterstuetzung fuer 2FA-Codes
- lokale, benutzergebundene Secret-Speicherung mit Windows DPAPI
- verschluesselter Import/Export fuer Secret-Profile
- Clipboard-Historie nur im Arbeitsspeicher
- dunkles Theme, Menueleiste und eigenes Schloss-Icon
- optionales Always-on-top fuer das Hauptfenster
- optionaler App-Lock mit PIN; standardmaessig bei Windows-Sperre statt ueber Inaktivitaet
- optionale Windows-Autostart-Registrierung
- nachvollziehbare Struktur mit Services, Models und UI

## Architektur

Projektziel: robuste Windows-Desktop-Anwendung mit klar getrennten Verantwortlichkeiten.

- `Program.cs`
  Startpunkt der WinForms-App.
- `TrayApplicationContext.cs`
  Lebenszyklus der Tray-App, Kontextmenue, Timer, Hotkeys, Fensterverwaltung.
- `Services/AppConfigService.cs`
  Laden und Speichern der nicht-sensitiven Konfiguration.
- `Services/SecureSecretStore.cs`
  Verschluesselte Ablage sensibler Eintraege per DPAPI (`CurrentUser`).
- `Services/KeyboardInjectionService.cs`
  Gekapselte Tipp-Simulation ueber `SendInput`.
- `Services/TypingSequenceService.cs`
  Interpreter fuer Login-Sequenzen wie Username, Tab, Passwort, Enter.
- `Services/TotpService.cs`
  Generiert aktuelle TOTP-Codes aus Base32-Seeds.
- `Services/SecretImportExportService.cs`
  Passwortbasierter AES-GCM-Import/Export fuer Secret-Profile.
- `Services/GlobalHotkeyService.cs`
  Registrierung globaler Tastenkombinationen.
- `Services/AppLockService.cs`
  Session-Lock, PIN-Validierung und Auto-Lock-Logik.
- `Services/AutoStartService.cs`
  Verwaltung des HKCU-Run-Registry-Eintrags.
- `Services/ClipboardHistoryService.cs`
  In-Memory-Verlauf fuer Clipboard-Texte.
- `UI/MainForm.cs`
  Verwaltungsfenster fuer Settings, Secrets, Lock und Clipboard-Verlauf.
- `UI/SecretEditForm.cs`
  Dialog fuer Erfassung und Bearbeitung eines Secrets inklusive Typing-Sequenz.

## Sicherheitsmodell

Weil das Tool sensible Daten verarbeitet, gelten im Entwurf diese Grundsaetze:

- Secrets werden nicht im Klartext gespeichert.
- Die Persistenz ist an den aktuellen Windows-Benutzer gebunden.
- Clipboard-Historie wird absichtlich nicht dauerhaft gespeichert.
- Es gibt eine klare Trennung zwischen Konfiguration und sensiblen Daten.
- Die Tipp-Simulation ist als eigener Service kapsuliert und dadurch spaeter testbarer und austauschbar.
- Der App-Lock ist eine zusaetzliche Session-Schutzschicht, ersetzt aber nicht die Windows-Anmeldung.

Wichtig: Auch mit DPAPI ist das kein Ersatz fuer einen vollwertigen Enterprise Password Manager. Fuer produktiven Einsatz sollten wir als naechste Schritte Logging, Fehlertelemetrie ohne Secret-Leaks, Auto-Lock, Timeout-Verhalten und optional Windows Hello oder Master-Unlock ergaenzen.

## Aktuelle Funktionen im Entwurf

- Tray-Icon mit Kontextmenue
- globaler Hotkey fuer "Primary Secret tippen"
- globaler Hotkey fuer "Manager oeffnen"
- Auswahl mehrerer gespeicherter Secrets
- Festlegen eines Primary Secrets
- explizites Setzen eines Standard-Secrets im Manager
- optionaler Hotkey pro Secret
- Startverzoegerung pro Secret
- Zeichenverzoegerung pro Secret fuer langsame Zielsysteme
- Typing-Sequenzen pro Profil
- TOTP-Vorschau und `{TOTP}`-Token pro Profil
- verschluesselter Export in `.ptpsec`-Dateien
- verschluesselter Import vorhandener `.ptpsec`-Dateien
- manuelles Sperren und Entsperren
- Sperren bei Windows-Geraetesperre
- optionaler Auto-Lock nach konfigurierbarer Inaktivitaet
- Autostart-Option
- Always-on-top fuer das Hauptfenster
- Menueleiste fuer Datei, Aktionen und Ansicht
- Clipboard-Verlauf mit begrenzter Historie
- Rueckkopieren eines Verlaufseintrags in die Zwischenablage

Standard-Hotkeys:

- `Ctrl+Shift+G` tippt das Primary Secret
- `Ctrl+Shift+P` oeffnet den Manager

## Typing-Sequenzen

Jedes Secret kann eine eigene Sequenz definieren. Damit lassen sich Login-Flows fuer VMs, Konsolen oder Remote-Dialoge abbilden.

Zusatz pro Secret:

- eigenes Standard-Flag
- eigener globaler Hotkey
- eigene Startverzoegerung vor dem Tippen
- eigene Zeichenverzoegerung zwischen Eingaben

Unterschied:

- `Startverzoegerung`: wartet einmal vor Beginn des Tippens
- `Zeichenverzoegerung`: wartet zwischen jedem Zeichen bzw. Schritt

## Lock-Verhalten

Standardmaessig sperrt sich die App erst dann, wenn Windows selbst gesperrt wird, sofern App-Lock aktiviert ist.

- `Bei Windows-Sperre sperren`: standardmaessig aktiv
- `Auto-Lock nach Minuten`: standardmaessig `0`, also deaktiviert
- manuelles Sperren bleibt weiterhin moeglich

Unterstuetzte Tokens:

- `{USERNAME}`
- `{SECRET}`
- `{TOTP}`
- `{TAB}`
- `{ENTER}`
- `{SPACE}`
- `{DELAY:500}`
- `{TEXT:abc}`

Beispiele:

- Nur Passwort eintippen: `{SECRET}`
- Benutzername und Passwort: `{USERNAME}{TAB}{SECRET}{ENTER}`
- Benutzername, Passwort und 2FA: `{USERNAME}{TAB}{SECRET}{ENTER}{DELAY:800}{TOTP}{ENTER}`
- Mit kurzer Wartezeit: `{USERNAME}{TAB}{SECRET}{DELAY:300}{ENTER}`

## Import und Export

Secrets koennen jetzt ueber den Manager verschluesselt exportiert und wieder importiert werden.

- Exportformat: `.ptpsec`
- Verschluesselung: passwortbasiert mit PBKDF2 + AES-GCM
- Importierte Eintraege erhalten neue interne IDs
- Wenn importierte Secrets ein Primary-Profil enthalten, wird die bisherige Primary-Markierung aufgehoben

## Build

Voraussetzung: installiertes .NET 8 SDK mit Windows Desktop Support.

```powershell
dotnet restore
dotnet build
dotnet run
```

Projektdatei:

- `C:\Users\steve\Github\PassTypePro\PassTypePro.csproj`

## Build-Status

Der Build wurde lokal erfolgreich verifiziert mit:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' build C:\Users\steve\Github\PassTypePro\PassTypePro.csproj -c Debug
```

Ergebnis:

- `0 Warnungen`
- `0 Fehler`

## Empfohlene naechste Schritte

1. Clipboard-Verlauf optional verschluesselt persistieren
2. Multi-Feld-Profile und Feldkategorien ergaenzen
3. Installer, Signierung und Update-Mechanismus hinzufuegen
4. Optional Windows Hello oder Master-Unlock integrieren
5. Suchfunktion und Kategorien fuer grosse Secret-Bestaende
