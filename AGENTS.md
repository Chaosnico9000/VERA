# AGENTS.md — AI Agent Working Reference

> **Dieses Dokument MUSS am Anfang jeder Session gelesen werden.**  
> Es enthält alle Konventionen, Regeln und Projektstruktur für den AI-Agenten.

---

## 1. Pflichtregeln für Dateibearbeitung

| Szenario | Vorgehensweise |
|---|---|
| **Vollständige Datei neu schreiben** | `remove_file` → `create_file` (NIEMALS `replace_string_in_file` für ganze Dateien) |
| **Kleine, lokale Änderung** | `replace_string_in_file` mit mind. 3–5 Zeilen Kontext vor/nach der Änderung |
| **Mehrere Änderungen in einer Datei** | `multi_replace_string_in_file` in einem einzigen Aufruf |
| **Datei löschen** | `remove_file` |
| **Neue Datei anlegen** | `create_file` (Verzeichnis wird automatisch erstellt) |

> **Warum:** `replace_string_in_file` hängt bei großen Rewrites alten Code an → CS9348/CS0102 Buildfehler.

---

## 2. Projektstruktur

```
VERA/                        ← .NET 10 MAUI App (Android primär)
  VERA.csproj                ← ApplicationDisplayVersion muss SemVer-konform sein
  App.xaml.cs                ← Startup: Server-URL-Check → RegisterPage/LoginPage/AppShell
  MauiProgram.cs             ← DI: ApiClient, AccountService, GamificationService
  Services/
    ApiClient.cs             ← HTTP-Client, JWT-Refresh transparent
    AccountService.cs        ← Lokale Accounts (PBKDF2, nur Fallback)
    GamificationService.cs   ← 30 Level, exponentielle XP, 18 Achievements, 5 Ränge
    TimeTrackingService.cs   ← Lokaler JSON-Speicher, atomare Schreiboperationen
  Views/
    LoginPage.xaml(.cs)      ← Server-Login + Biometrie
    RegisterPage.xaml(.cs)   ← Server-Registrierung
    LevelingPage.xaml(.cs)   ← Gamification-Detailseite
    EinstellungenPage.xaml(.cs) ← Server-URL, Passwort ändern
  Platforms/Android/
    Resources/xml/network_security_config.xml ← Nur HTTPS, System-CAs

VERA.Server/                 ← ASP.NET Core 10 Web API
  Program.cs                 ← DI, JWT, SQLite, Rate-Limiter, Security-Header
  Controllers/
    AuthController.cs        ← /api/auth (register, login, refresh, logout, change-password)
    TimeEntriesController.cs ← /api/entries (GET, POST, DELETE, POST sync)
  Services/
    AuthService.cs           ← PBKDF2-SHA256, JWT-Ausgabe, Refresh-Rotation, Lockout
  Data/
    VeraDbContext.cs         ← EF Core DbContext, Indizes, Cascade-Delete
    Entities.cs              ← User, TimeEntry, RefreshToken
    Migrations/              ← EF Core Migrationen (InitialCreate)
  appsettings.json           ← DataDirectory, Jwt-Konfiguration

VERA.Shared/                 ← .NET 10 Class Library (Single Source of Truth)
  Dto/ApiDtos.cs             ← RegisterRequest, LoginRequest, AuthResponse, TimeEntryDto, ...
  LoginResult.cs             ← Enum: Success, InvalidPassword, NoAccountFound, AccountLocked

Dockerfile                   ← Multi-Stage Build, Non-Root User vera (uid 1001), Port 8080
pelican-egg.json             ← Pterodactyl/Pelican Egg-Definition

docs/
  DEPLOYMENT.md              ← Pelican/Docker-Deployment-Guide
  SETUP.md                   ← Lokale Entwicklungsumgebung
  ARCHITECTURE.md            ← Systemarchitektur, Datenfluss, Sicherheitsebenen

CHANGELOG.md                 ← SemVer-Changelog (MUSS bei jeder Version aktualisiert werden)
README.md                    ← Projektübersicht
AGENTS.md                    ← Dieses Dokument
```

---

## 3. Versionierung (SemVer)

Format: **`MAJOR.MINOR.PATCH`**

| Änderung | Version erhöhen |
|---|---|
| Breaking API-Change, große Architekturänderung | MAJOR |
| Neue Funktion (abwärtskompatibel) | MINOR |
| Bugfix, Sicherheitspatch, Doku-Update | PATCH |

**Pflichten bei jeder Versionsänderung:**
1. `VERA/VERA.csproj` → `ApplicationDisplayVersion` aktualisieren (z.B. `"1.1.0"`)
2. `VERA/VERA.csproj` → `ApplicationVersion` erhöhen (Integer, Android-Build-Nummer)
3. `CHANGELOG.md` → neuen Eintrag unter `## [X.Y.Z] - YYYY-MM-DD` hinzufügen
4. Commit + Push auf `master` — GitHub Actions baut und veröffentlicht den Release automatisch.
   - Version wird aus `ApplicationDisplayVersion` in `VERA/VERA.csproj` gelesen
   - Git-Tag `vX.Y.Z` wird automatisch gesetzt
   - `vera-server.zip` landet unter `releases/latest/download/vera-server.zip`

> Kein manuelles `git tag` nötig.

**Aktuelle Version: `1.0.0`**

---

## 4. Build-Befehle

```powershell
# MAUI App (Android)
dotnet build VERA\VERA.csproj -f net10.0-android

# VERA.Server
dotnet build VERA.Server\VERA.Server.csproj

# Gesamte Solution
dotnet build VERA.slnx

# EF Core Migration hinzufügen (im VERA.Server-Verzeichnis)
dotnet ef migrations add <MigrationName> --project VERA.Server

# EF Core Datenbank aktualisieren (lokal)
dotnet ef database update --project VERA.Server
```

---

## 5. Sicherheitskonventionen

- **Passwort-Hashing:** PBKDF2-SHA256, 300.000 Iterationen, 32-Byte-Salt, 32-Byte-Hash (nur Server)
- **JWT Access Token:** 15 Minuten, HmacSha256
- **JWT Refresh Token:** 7 Tage, Rotation bei jedem Refresh (altes Token wird sofort invalidiert)
- **Lockout:** 5 fehlgeschlagene Logins → 5 Minuten gesperrt (IP-basiert im Rate-Limiter)
- **Rate-Limiting:** 20 Requests/Minute pro IP auf `/api/auth/*`
- **Security-Header:** X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy
- **Kein HTTP in Production:** `network_security_config.xml` erlaubt nur HTTPS + System-CAs
- **Secrets:** `Jwt__Secret` NIEMALS in Quellcode — nur via Umgebungsvariable oder Pelican-Panel
- **Atomare Schreiboperationen:** TimeTrackingService schreibt in `.tmp`-Datei, dann `File.Replace`

---

## 6. CHANGELOG.md Pflege

Das `CHANGELOG.md` folgt dem [Keep a Changelog](https://keepachangelog.com/de/1.0.0/) Format:

```markdown
## [X.Y.Z] - YYYY-MM-DD
### Added
- Neue Features

### Changed
- Geänderte Features

### Fixed
- Behobene Bugs

### Security
- Sicherheits-Fixes
```

**CHANGELOG muss IMMER aktualisiert werden**, bevor ein Commit als neue Version markiert wird.

---

## 7. VERA.Shared — Cross-Project Types

`VERA.Shared` ist der **einzige** Ort für Typen, die von MAUI App und Server gemeinsam genutzt werden:
- DTOs: `VERA.Shared.Dto.ApiDtos.cs`
- Enums: `VERA.Shared.LoginResult`

**NIEMALS** Duplikate dieser Typen in `VERA.*` oder `VERA.Server.*` anlegen.

---

## 8. Bekannte Fallstricke

1. **`replace_string_in_file` bei großen Dateien** → Hängt alten Code an → **delete+create verwenden**
2. **Rate-Limiter:** `AddFixedWindowLimiter` benötigt extra NuGet in .NET 10 → Custom `ConcurrentDictionary`-Middleware in `Program.cs` verwenden
3. **LoginResult Enum:** lebt in `VERA.Shared`, NICHT in `VERA.Services`
4. **EF Core Migrations:** Änderungen an `Entities.cs` erfordern neue Migration + `db.Database.Migrate()` läuft automatisch beim Serverstart
5. **pelican-egg.json Install-Script:** Referenziert GitHub-Releases-URL → muss aktuell sein, wenn Releases veröffentlicht werden
6. **Android minSdkVersion:** 23 (Android 6.0) — Biometric-NuGet ist Android-only

---

## 9. Dokumentationspflichten

Bei jeder Session, die **neue Features hinzufügt oder bestehende ändert:**

- [ ] `CHANGELOG.md` aktualisieren
- [ ] Relevante `docs/*.md` aktualisieren (falls Deployment/Setup/Architektur betroffen)
- [ ] `ApplicationDisplayVersion` in `VERA.csproj` prüfen/erhöhen
- [ ] `ApplicationVersion` (Integer) in `VERA.csproj` erhöhen

---

## 10. Technologie-Stack (Kurzreferenz)

| Komponente | Technologie | Version |
|---|---|---|
| Mobile App | .NET MAUI | .NET 10 |
| Android Mindest-SDK | Android | 23 (6.0) |
| Backend | ASP.NET Core | 10 |
| ORM | EF Core | 9.0.5 |
| Datenbank | SQLite | (via EF Core) |
| Auth | JWT Bearer | HmacSha256 |
| Container | Docker | mcr.microsoft.com/dotnet/aspnet:10.0 |
| Deployment | Pelican/Pterodactyl | PTDL_v2 |
| Gamification | Custom | 30 Level, expon. XP (2000×1.55^n) |
