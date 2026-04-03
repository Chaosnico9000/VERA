# Changelog

Alle wichtigen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.

Das Format basiert auf [Keep a Changelog](https://keepachangelog.com/de/1.0.0/),
dieses Projekt hält sich an [Semantic Versioning](https://semver.org/lang/de/).

---

## [Unreleased]

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
