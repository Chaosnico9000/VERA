# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
dieses Projekt hält sich an [Semantic Versioning](https://semver.org/lang/de/).

---

## [Unreleased]

---

## [1.7.0] - 2026-04-07

### Added
- **„Eingeloggt bleiben"-Checkbox** auf der Login-Seite — standardmäßig aktiv; wenn deaktiviert, wird der Refresh-Token nach dem Login nicht persistiert, sodass beim nächsten App-Start die Login-Seite wieder erscheint.

### Fixed
- **Theme-Wechsel funktioniert jetzt korrekt** — alle XAML-Seiten nutzen `{DynamicResource}` statt `{StaticResource}` für theme-veränderliche Farben (AppBackground, CardColor, TextPrimary usw.), sodass Farbänderungen live übernommen werden.
- **`ThemeService.ApplySaved()` hatte keine Wirkung** — der Aufruf wurde in `App.xaml.cs` verschoben, wo `Application.Current` bereits gesetzt ist (zuvor in `MauiProgram.CreateMauiApp()` aufgerufen, wo `Application.Current` noch `null` ist).
- **`??` Emojis in Einstellungen** — Theme-Buttons (Dunkel/Hell/System) und Export-Button zeigten fehlerhafte Zeichen; ersetzt durch korrekte Unicode-Entities (🌙 ☀ ⚙ 📤).

### Changed
- **Logo auf Hauptseite vergrößert** — `HeightRequest` von 52 auf 64, `WidthRequest="160"`, `Aspect="AspectFill"` mit Clip-Rechteck; der weiße Rand des Logo-Assets wird abgeschnitten.

---

## [1.6.0]

### Added
- **Dark/Light/System Mode Toggle** in Einstellungen — Theme wird persistent gespeichert und beim Start wiederhergestellt
- **Teilen-Funktion** in Einstellungen — Tagesübersicht als formatierten Text über WhatsApp, Signal o.a. teilen
- **vera_logo.png** als App Icon, Splash Screen und Flyout-Header
- Logo ersetzt Text-Header auf der Hauptseite

### Changed
- Flyout Header zeigt jetzt das VERA-Logo anstelle von Text
- App Icon und Splash Screen nutzen das echte VERA-Logo (vera_logo.png)
- Build-Workflow: NuGet + MAUI-Workload gecacht, ABI auf arm64-v8a beschränkt (schnellere CI)

---

## [1.5.5] - 2026-04-05

### Fixed
- **App-Icon:** Das „.NET“-Logo wurde durch den eigenen VERA-Schriftzug ersetzt. Das Icon nutzt jetzt ein SVG-Background (`#080C1E`) + SVG-Foreground mit dem VERA-Wortmarken-Pfad, einem Cyan-Akzentpunkt und einem Cyan→Blau→Violett-Unterstrich.
- **App-Icon füllt den Bereich nicht aus:** `MauiIcon` wurde von der alten `appicon.png` (Standard-.NET-Template) auf das neue `appicon.svg` umgestellt — der Foreground-Pfad füllt die Adaptive-Icon-Safe-Zone jetzt vollständig aus.
- **Splash Screen:** `BaseSize` von `128,128` auf `512,512` erhöht für scharfe Darstellung auf allen Displaydichten. Splash zeigt denselben VERA-Schriftzug inkl. Untertitel „VERTRAUENSARBEITSZEIT“.

---

## [1.5.4]

### Fixed
- **Wochenende wird korrekt erkannt:** Samstag und Sonntag haben keine Sollzeit mehr. Der Tagesfortschrittsbalken zeigt ein neutrales Bild, der Puffer-Badge zeigt `+Xh` (freiwillige Arbeit) statt Fehlstunden, und der Motivationstext lautet „Gutes Wochenende!“ / „Fleißig am Wochenende“ statt Soll-Bezug.
- **Wochenübersicht am Wochenende:** Zeigt 5 Soll-Tage (Woche abgeschlossen), sodass der Wochenpuffer die korrekte Bilanz der abgelaufenen Mo–Fr-Tage ausweist.

---

## [1.5.3]

### Fixed
- **Wochenübersicht zeigt falsche Fehlstunden:** Die Berechnung nutzte immer ein fixes Ziel von 5 × Sollzeit, auch wenn die Woche erst am Mittwoch begann (z.B. 1. April 2026). Jetzt werden nur die tatsächlich vergangenen Werktage der aktuellen Kalenderwoche (Mo–Fr bis heute) als Pflicht gewertet — wer am Mittwoch anfängt, hat 3 Soll-Tage, nicht 5.
- **Tage-Anzeige Wochenübersicht:** Zeigt jetzt `X / Y Tage` mit dem tatsächlichen Nenner (vergangene Werktage), statt immer `/ 5`.

### Changed
- **Download-Geschwindigkeit (In-App Update):** Buffer von 80 KB auf 256 KB erhöht, `FileStream` mit `useAsync: true` und explizitem `FlushAsync` — APK-Downloads laufen spürbar schneller durch.
- **HTTP-Timeout Update-Check:** Von 10 s auf 30 s erhöht, um Timeouts bei langsamen Verbindungen zu vermeiden.
- **`TimeTrackingService` Thread-Safety:** `SemaphoreSlim` schützt den Lade-/Schreib-Pfad gegen Race-Conditions bei gleichzeitigem Zugriff.
- **`ConfigureAwait(false)`:** Alle `await`-Aufrufe in `DashboardViewModel`, `TimeTrackingService` und `UpdateService` nutzen jetzt `ConfigureAwait(false)` — reduziert Context-Switch-Overhead auf dem UI-Thread.
- **Entry-Cache TTL:** Von 45 s auf 30 s reduziert für frischere Daten beim Pull-to-Refresh.

---

## [1.5.2]

### Changed
- **Timer-Karte:** Border leuchtet cyan wenn der Timer läuft — sofort erkennbar auf einen Blick. Status-Zeile zeigt Startzeit direkt neben "Läuft".
- **Pull-to-Refresh:** Die gesamte Seite kann jetzt durch Runterziehen aktualisiert werden. Timestamp "Aktualisiert HH:mm:ss" im Header.
- **Motivationstext:** Kontextsensitiver Text unter der Timer-Uhr — abhängig vom Wochentag, Fortschritt und verbleibender Zeit.
- **Lade-Spinner:** Beim ersten Öffnen der Seite erscheint ein Ladeindikator statt leerer Karten.
- **Tagesfortschritt:** Puffer-Badge hat jetzt farbigen Border passend zum Vorzeichen. Sollzeit wird als "von Xh Sollzeit" angezeigt.
- **Wochenübersicht:** Puffer-Badge ebenfalls farbig. Tages-Fortschritt "X / 5 Tage" rechtbündig.
- **Sondertypen-Karte:** Zeigt ein lila Badge mit dem eingetragenen Sondertyp wenn heute bereits einer vorhanden ist. Buttons werden halbtransparent deaktiviert.
- **Letzte Einträge:** Zeitbereich (z.B. "08:00 – 16:30") wird neben der Startzeit angezeigt. Leer-Zustand mit Icon verbessert.
- **Entry-Cache:** Einträge werden 45 Sekunden gecacht — kein doppelter API-Aufruf beim Pausen-Check und Statistik-Refresh.
- **Pausen-Vorschlag:** Banner ist jetzt kompakter mit dunklerer Hintergrundfarbe.

---

## [1.5.1]

### Changed
- **Changelog-Seite komplett überarbeitet:** Statt rohem Markdown-Text werden jetzt strukturierte Release-Karten angezeigt — mit farbigen NEU/FIX/ÄNDERUNG-Badges, Titel + Beschreibung getrennt, aktuelle Version grün hervorgehoben und einer Puffer-Anzeige mit Anzahl der Änderungen pro Release.

---

## [1.5.0]

### Added
- **Live-Uhrzeit im Header:** Die aktuelle Uhrzeit wird im Datums-Badge rechts oben sekündlich aktualisiert — unabhängig vom Gerätedisplay immer im Blick.
- **Pausen-Vorschlag:** Die App erkennt automatisch, wenn du heute ≥ 5 Stunden 30 Minuten ohne erkennbare Pause gearbeitet hast, und blendet einen grünen Hinweis-Banner ein. Der Banner lässt sich per ✕ schließen und meldet sich danach alle 2 Stunden erneut.
- **Wochenübersicht:** Eine neue Karte auf der Hauptseite zeigt die Summe der letzten 7 Tage in Stunden, einen Fortschrittsbalken gegenüber dem 5-Tage-Wochenziel und den Puffer (grün = Überstunden, lila = noch offen).

---

## [1.4.3]

### Added
- **In-App Update-Installer (Android):** Die App lädt Updates jetzt direkt herunter und startet den Android-System-Installer automatisch — kein manueller Browser-Download mehr nötig. Fortschritt wird im Update-Button angezeigt. Funktioniert sowohl beim App-Start-Dialog als auch über Einstellungen → Über die App.
- **Changelog in der App:** Einstellungen → Über die App → „📋 Changelog anzeigen" zeigt den vollständigen Changelog direkt in der App.
- **Auto-Login nach Registrierung:** Nach erfolgreicher Registrierung wird der Nutzer automatisch eingeloggt und landet direkt in der App. Schlägt der Auto-Login wider Erwarten fehl, wird die LoginPage mit vorausgefülltem Benutzernamen geöffnet.

### Fixed
- **App-Crash beim Registrieren:** `ApiClient.SetBaseUrl` warf `InvalidOperationException` wenn `HttpClient.BaseAddress` nach dem ersten Request neu gesetzt wurde (Singleton-Instanz). Fix: `SetBaseUrl` erkennt URL-Änderungen, erstellt einen neuen `HttpClient` (Timeout, `X-Client-Version`-Header und Bearer-Token werden übertragen) und disposed den alten — bei unveränderter URL ist die Methode ein No-op.
- **Falsche Navigation nach Token-Ablauf:** `ClearTokens` navigierte durch einen Operator-Precedenz-Bug (`||` statt `&&`) fast immer zur LoginPage — auch wenn der Nutzer bereits dort war. Fix: Pattern Matching für korrekte Prüfung ob das aktuelle Fenster bereits eine LoginPage zeigt.
- **Doppelter `CheckServerAsync`-Call beim Login:** `OnLoginClicked` rief `CheckServerAsync` nochmals auf, obwohl der Verbindungsstatus-Badge oben bereits aktuell war. Der redundante Aufruf wurde entfernt — `LoginAsync` gibt bei Verbindungsfehlern ohnehin einen Fehler zurück.
- **Rate-Limit-Counter lief nach 429 weiter (Server):** Der Zähler wurde auch dann erhöht wenn bereits 429 zurückgegeben wurde. Fix: Zähler wird nur für tatsächlich weitergeleitete Requests erhöht.
- **RefreshToken-Tabelle wuchs unbegrenzt (Server):** Bei jedem Token-Refresh wurden abgelaufene und revozierte Tokens nicht bereinigt. Fix: `RefreshAsync` löscht jetzt alle veralteten Tokens des Nutzers vor dem `SaveChanges`.

---

## [1.4.2]

### Fixed
- GitHub Actions: `build-ios`-Job und alle iOS-bezogenen Schritte aus `release.yml` entfernt — verursachte fehlgeschlagene Releases weil das `vera-ios`-Artefakt nicht existierte und der `release`-Job dadurch keine `vera-server.zip` und `vera-android.apk` hochlud
- Workflow vereinfacht auf 3 Jobs: `read-version` → `build-server` + `build-android` (parallel) → `release`

---

## [1.4.1] - 2026-04-04

### Fixed
- **App-Crash beim Login:** `OnSleep` leitete zur LoginPage weiter auch wenn der Nutzer sich gerade einloggte (z.B. während Biometrie-Dialog oder Screen-Off). Fix: OnSleep prüft jetzt ob die aktuelle Page bereits eine Auth-Page (Login/Register) ist — kein Redirect in diesem Fall
- **„Benutzer nicht gefunden" bei Netzwerkfehlern:** `LoginAsync` gab `NoAccountFound` auch bei Verbindungsfehlern zurück. Fix: catch-Block gibt `InvalidPassword` + Verbindungsfehler-Meldung zurück; `NotFound (404)` ist jetzt der einzige Weg zu `NoAccountFound`
- **Server-URL nach App-Neustart:** `ApiClient` lud die gespeicherte Server-URL nicht im Konstruktor — Token-Refresh schlug nach App-Neustart fehl weil `BaseAddress` null war
- **Sitzung abgelaufen ohne Weiterleitung:** Wenn der Refresh-Token abläuft (`ClearTokens`), wird der Nutzer jetzt automatisch zur LoginPage weitergeleitet statt leere AppShell zu sehen
- **Username nicht getrimmt:** `LoginAsync` sendet `username.Trim()` an den Server (führende/nachfolgende Leerzeichen werden entfernt)

---

## [1.4.0] - 2026-04-04

### Added
- GitHub Actions: `build-ios`-Job (macOS-Runner) — baut `vera-ios.ipa` wenn Apple-Secrets gesetzt sind (`APPLE_CERTIFICATE_P12_BASE64`, `APPLE_PROVISIONING_PROFILE_BASE64`, `APPLE_TEAM_ID`)
- Workflow: iOS-Job wird still übersprungen (kein Fehler) wenn Apple-Secrets fehlen
- Workflow: Release-Job erkennt automatisch vorhandene Assets (IPA wird hinzugefügt wenn vorhanden)
- Release-Notizen: iOS-Installationsanleitung (AltStore, Sideloadly) direkt im GitHub Release

### Changed
- `release` Job: läuft auch wenn `build-ios` übersprungen oder fehlgeschlagen ist (`always()` + Bedingung)
- README: vollständig überarbeitet — iOS/macOS Installationsanleitungen (AltStore, Sideloadly, TestFlight), GitHub-Secrets-Tabellen, plattformübergreifende Build-Befehle
- `AGENTS.md`: iOS-Build-Secrets dokumentiert, Workflow-Struktur auf 4 Jobs aktualisiert

---

## [1.3.0] - 2026-04-04

### Added
- iOS / macOS Catalyst Support: `NSAppTransportSecurity` in `Info.plist` (iOS + macCatalyst) — erlaubt HTTP-Verbindungen zu user-konfigurierten Servern (`NSAllowsArbitraryLoads=true`, `NSAllowsLocalNetworking=true`)
- Server: `UseForwardedHeaders`-Middleware für korrektes X-Forwarded-For / X-Forwarded-Proto hinter Pelican-Reverse-Proxy

### Changed
- Android `network_security_config.xml`: `cleartextTrafficPermitted` auf `true` geändert — HTTP-Verbindungen werden toleriert, da die Server-URL vom Nutzer konfiguriert wird (HTTP und HTTPS)
- Server: `UseHsts()` und `UseHttpsRedirection()` entfernt — HTTPS wird am Pelican-Reverse-Proxy terminiert, nicht im Container
- `AGENTS.md`: Pflicht-Lesepflicht pro Session, iOS/macOS Plattform-Sektion, HTTPS-Policy, Build-Befehle für iOS/macOS, Technologie-Stack erweitert

### Fixed
- Login/Registrierung schlug auf Android fehl, weil `cleartextTrafficPermitted=false` alle HTTP-Verbindungen blockierte
- iOS/macOS: App Transport Security blockierte HTTP-Verbindungen mangels `NSAppTransportSecurity`-Konfiguration

---

## [1.2.0] - 2026-04-04

### Added
- GitHub Actions Workflow: Android APK wird jetzt parallel zum Server-ZIP gebaut und als `vera-android.apk` ins GitHub Release hochgeladen
- Workflow: drei Jobs (`read-version` → `build-server` + `build-android` parallel → `release`)
- Workflow: APK-Signierung via GitHub Secrets (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`, `ANDROID_KEYSTORE_PASSWORD`)
- `UpdateService` — prüft GitHub Releases API auf neue Versionen (`https://api.github.com/repos/Chaosnico9000/VERA/releases/latest`)
- Automatischer Update-Check beim App-Start: 3 Sekunden nach Start → Alert mit Download-Link wenn neuere Version verfügbar
- `UpdateInfo`-Record (`LatestVersion`, `DownloadUrl`, `IsNewer`)

### Changed
- Workflow umbenannt von „Release VERA Server" zu „Release VERA"
- `AGENTS.md` aktualisiert: `UpdateService`, Workflow-Struktur, Keystore-Secrets

---

## [1.1.0] - 2026-04-04

### Added
- `VERA.Shared/AppVersion.cs` — zentrale Versionskonstanten (`Current`, `MinServerVersion`, `MinClientVersion`) als Single Source of Truth für Client und Server
- `ServerInfoResponse`-DTO in `VERA.Shared/Dto/ApiDtos.cs`
- `GET /api/info` Endpoint (`InfoController`) — liefert Server-Version und Min-Client-Version ohne Authentifizierung
- `ServerCompatibility`-Enum in `ApiClient` (`Ok`, `ServerTooOld`, `ClientTooOld`, `Unreachable`)
- `ApiClient.CheckServerAsync()` — prüft Erreichbarkeit und beidseitige Versionskompatibilität
- `X-Client-Version`-Header wird bei jedem HTTP-Request automatisch mitgesendet
- Server-Middleware: veraltete Clients (Header `< MinClientVersion`) erhalten `426 Upgrade Required`
- Login-Guard in `LoginPage`: Versionscheck vor jedem Login-Versuch mit klarer Fehlermeldung
- „🔌 Verbindung testen"-Button in den Einstellungen — testet Erreichbarkeit und Versionskompatibilität und zeigt Ergebnis als Alert
- Startup-Log zeigt Server-Version und Min-Client-Version zusätzlich zu den Endpoints

### Changed
- Pelican Egg Installationsskript: `set -e` entfernt, `rm`-Befehle zusammengefasst, `chmod +x VERA.Server` hinzugefügt

---

## [1.0.8] - 2025-07-06

### Changed
- MAUI-Projekt von `VERA/` nach `VERA.Client/` umbenannt (Ordner + `.csproj`)
- `VERA.slnx` Solution-Referenz aktualisiert
- GitHub Actions Workflow: Versionspfad auf `VERA.Client/VERA.Client.csproj` aktualisiert
- `AGENTS.md` Build-Befehle und Projektstruktur aktualisiert
- `RootNamespace` bleibt `VERA` (kein Breaking Change in bestehenden `.cs`-Dateien)

---

## [1.0.7]

### Fixed
- Pelican Egg: `set -e` hinzugefügt — Skript bricht bei jedem Fehler sofort ab statt mit Exit 0 weiterzulaufen
- `exit 0` explizit am Ende des Installationsskripts — Pelican erkennt erfolgreiche Installation zuverlässig und schaltet auf "installed"
- `echo "Installation abgeschlossen."` als finales Signal im Install-Log

---

## [1.0.6]

### Fixed
- Pelican Egg Installationsskript: `mv vera-server/* .` bleibt in `/mnt/server` (während Installation existiert `/home/container` noch nicht — das ist ein Laufzeit-Mount)
- Überflüssiges `chmod +x` entfernt (`.dll`-Dateien brauchen kein Execute-Bit für `dotnet`)
- Startup-Befehl `dotnet /home/container/VERA.Server.dll` korrekt — zur Laufzeit ist `/mnt/server` als `/home/container` gemountet

---

## [1.0.5]

### Fixed
- Pelican Egg: Startup-Befehl von `dotnet VERA.Server.dll` auf `dotnet /home/container/VERA.Server.dll` geändert (DLL liegt in `/home/container/`, Prozess startet von `/` aus)
- Installationsskript: Dateien werden jetzt direkt nach `/home/container/` verschoben statt in `/mnt/server/`
- `chmod +x` ebenfalls auf absoluten Pfad `/home/container/VERA.Server.dll` korrigiert

---

## [1.0.4]

### Fixed
- Datenbankpfad von `/data` (read-only im Pelican-Container) auf `/home/container/data` geändert
- `appsettings.json` und `Program.cs` Fallback-Wert aktualisiert
- Pelican Egg: neue Umgebungsvariable `DataDirectory` (Standard: `/home/container/data`) — ermöglicht flexiblen Pfad je nach Hosting-Umgebung

---

## [1.0.3]

### Fixed
- Pelican Egg Installationsskript: `mv vera-server/* .` hinzugefügt, damit Dateien direkt in `/mnt/server/` liegen statt im Unterordner `vera-server/`
- `chmod +x VERA.Server.dll` schlug fehl, weil die DLL im falschen Verzeichnis gesucht wurde

---

## [1.0.2]

### Fixed
- GitHub Actions 403-Fehler beim Release-Erstellen: `actions: read`-Permission ergänzt
- GitHub Repository muss unter Settings → Actions → General auf "Read and write permissions" gestellt sein

---

## [1.0.1]

### Added
- GitHub Actions Workflow (`.github/workflows/release.yml`) — automatischer Build und Release von `vera-server.zip` bei jedem `v*.*.*`-Tag
- DataAnnotations-Validierung auf allen VERA.Shared DTOs (`[Required]`, `[StringLength]`, `[Range]`)
- `EndTime > StartTime`-Validierung im `TimeEntriesController.Upsert`
- Leer-Prüfung im `TimeEntriesController.Sync`
- Dokumentation: `AGENTS.md`, `CHANGELOG.md`, `README.md`, `docs/DEPLOYMENT.md`, `docs/SETUP.md`, `docs/ARCHITECTURE.md`

### Changed
- `ApplicationDisplayVersion` von `"1.0"` auf `"1.0.0"` (SemVer-konform)
- Redundante manuelle Passwortlängen-Prüfung in `AuthController` entfernt (jetzt via DataAnnotations)

### Fixed
- `VERA.csproj.Backup.tmp` Build-Artefakt entfernt

---

## [1.0.0] - 2025-07-01

### Added
- **VERA.Server** — vollständiger ASP.NET Core 10 Backend-Server
  - JWT-Authentifizierung (Access Token 15 Min., Refresh Token 7 Tage mit Rotation)
  - SQLite-Datenbank via EF Core 9 mit automatischen Migrationen
  - `POST /api/auth/register` — Benutzerregistrierung
  - `POST /api/auth/login` — Login mit Lockout nach 5 Fehlversuchen
  - `POST /api/auth/refresh` — Token-Erneuerung mit Rotation
  - `POST /api/auth/logout` — Refresh-Token-Invalidierung
  - `POST /api/auth/change-password` — Passwortänderung (invalidiert alle Sessions)
  - `GET/POST/DELETE /api/entries` — Zeiteinträge-CRUD
  - `POST /api/entries/sync` — Bulk-Sync aller Einträge
  - Benutzerdefinierter IP-Rate-Limiter (20 Req/Min auf Auth-Endpunkten)
  - Security-Header: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy
- **VERA.Shared** — gemeinsame Class Library für DTOs und Enums
  - `RegisterRequest`, `LoginRequest`, `RefreshRequest`, `AuthResponse`, `ChangePasswordRequest`
  - `TimeEntryDto`, `UpsertTimeEntryRequest`, `ApiError`
  - `LoginResult`-Enum (Success, InvalidPassword, NoAccountFound, AccountLocked)
- **ApiClient** in MAUI — transparente JWT-Erneuerung, Token-Speicherung in Android Preferences
- **Dockerfile** — Multi-Stage-Build, Non-Root-User `vera` (uid 1001), Volume `/data`, Port 8080
- **pelican-egg.json** — Pterodactyl/Pelican-Egg für One-Click-Deployment
  - Umgebungsvariablen: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `ASPNETCORE_HTTP_PORTS`

### Changed
- `LoginPage` und `RegisterPage` verwenden nun `ApiClient` statt lokalem `AccountService`
- `App.xaml.cs` prüft Server-URL beim Start → leitet zu `RegisterPage` oder `LoginPage` weiter
- `LoginResult`-Enum in `VERA.Shared` verschoben (Single Source of Truth)

---

## [0.8.0] - 2025-06-01

### Added
- **Account-System** — lokaler `AccountService` mit PBKDF2-SHA256 (300.000 Iterationen, 32-Byte-Salt/Hash)
- `RegisterPage` — Registrierungsformular mit Passwort-Bestätigung
- `LoginPage` — Username/Passwort-Login + optionale biometrische Authentifizierung
- `LockPage` — App-Sperr-Seite bei Inaktivität oder manueller Sperrung
- Biometrische Authentifizierung via `Plugin.Maui.Biometric` (Android-only)

### Security
- Passwörter werden ausschließlich als PBKDF2-Hash gespeichert, niemals im Klartext
- Biometrischer Fallback auf PIN/Passwort wenn Biometrie nicht verfügbar

---

## [0.7.0] - 2025-05-15

### Added
- **LevelingPage** — dedizierte Gamification-Detailseite
  - Hero-Card mit aktuellem Level und Rang
  - XP-Fortschrittsbalken (aktuell/nächstes Level)
  - Stats-Grid (Gesamtstunden, Streak, Achievements)
  - Achievement-Liste mit Unlock-Status und Fortschrittsbalken
  - XP-Aufschlüsselung (Woher kommt die XP?)
  - Nächste Meilensteine (Level-Vorschau)
- Navigation zu LevelingPage aus Einstellungen/Menü

---

## [0.6.0] - 2025-05-01

### Changed
- **Gamification-System komplett neu geschrieben** (v2)
  - 30 Level (vorher: 10)
  - Exponentielle XP-Kurve: `XP(n) = 2000 × 1.55^(n-1)` (~8 Mio. XP für Level 29 ≈ 4 Jahre aktive Nutzung)
  - 18 Achievements (vorher: 8) mit detaillierter Fortschrittsanzeige
  - 5 Ränge (je 6 Level): Neuling → Lehrling → Geselle → Meister → Experte
  - Jubiläums-XP für besondere Meilensteine
  - Erweiterte Streak-Erkennung (260-Wochen-Rückblick)
  - `CheckVollerMonat()` — Bonus-XP für vollständig erfasste Monate

---

## [0.5.0] - 2025-04-15

### Added
- **Kalenderansicht** — Monatsübersicht mit markierten Arbeitstagen
- Pro-Tag-Delta-Anzeige (Über-/Unterstunden je Tag)
- Visuelles Highlighting von Feiertagen in der Kalenderansicht

---

## [0.4.0] - 2025-04-01

### Added
- **Sicherheits-Features**
  - `network_security_config.xml` — Nur HTTPS, nur System-CAs (kein User-Cert-Pinning)
  - Biometrische Entsperrung (Fingerprint/Face ID auf Android)
  - Atomare Schreiboperationen in `TimeTrackingService` (`.tmp` → `File.Replace`)
- **ErsterArbeitstag**-Einstellung — korrekte Berechnung ab tatsächlichem Arbeitsbeginn

### Fixed
- Feiertags-Erkennung korrigiert (bundeslandspezifische Feiertage)
- `JavaProxyThrowable`-Crash bei Aktivitätswechsel behoben
- Namespace-Konflikt in Service-Klassen behoben

### Security
- Alle lokalen Datenschreibvorgänge sind nun atomar (kein Datenverlust bei App-Absturz)

---

## [0.3.0] - 2025-03-15

### Added
- **Gamification v1** — erstes Belohnungssystem
  - 10 Level mit linearer XP-Kurve
  - 8 Achievements (Erste Stunde, Erster Tag, etc.)
  - XP-Anzeige in der App

---

## [0.2.0] - 2025-03-01

### Added
- **Verlaufsansicht** — Liste aller bisherigen Zeiteinträge
- **Statistiken** — Wochenübersicht, Gesamtstunden, Durchschnitt
- Per-Tag-Delta (geplante vs. tatsächliche Stunden)
- Kategorien für Zeiteinträge

---

## [0.1.0] - 2025-02-01

### Added
- Erste Version der VERA Zeiterfassungs-App
- Basis-Zeiterfassung (Start/Stop-Timer)
- Lokale Datenspeicherung als JSON
- Einfache Übersichtsseite
- .NET 10 MAUI Projekt-Grundstruktur

---

[Unreleased]: https://github.com/Chaosnico9000/VERA/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Chaosnico9000/VERA/compare/v0.8.0...v1.0.0
[0.8.0]: https://github.com/Chaosnico9000/VERA/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/Chaosnico9000/VERA/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/Chaosnico9000/VERA/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/Chaosnico9000/VERA/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/Chaosnico9000/VERA/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/Chaosnico9000/VERA/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/Chaosnico9000/VERA/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/Chaosnico9000/VERA/releases/tag/v0.1.0
