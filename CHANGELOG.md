# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
dieses Projekt hält sich an [Semantic Versioning](https://semver.org/lang/de/).

---

## [Unreleased]

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
