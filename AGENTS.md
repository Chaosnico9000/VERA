# AGENTS.md — AI Agent Working Reference

> ## PFLICHTLEKTÜRE — WIRD JEDE SESSION ZUERST GELESEN
>
> **Dieses Dokument MUSS am Anfang JEDER Session vollständig gelesen werden.**
> Es enthält alle Konventionen, Regeln und Projektstruktur für den AI-Agenten.
> **Keine Datei darf bearbeitet werden, bevor dieses Dokument gelesen wurde.**

---

## 0. Pflichtregeln für jede Session

1. **AGENTS.md zuerst lesen** — vor jeder anderen Aktion.
2. **CHANGELOG.md immer aktualisieren** — bei jeder Änderung, die Features, Fixes oder Security betrifft.
3. **Version immer synchron halten** — AppVersion.Current in VERA.Shared/AppVersion.cs und ApplicationDisplayVersion in VERA.Client/VERA.Client.csproj müssen identisch sein.
4. **Alle betroffenen Dokumente aktualisieren** — bei Architektur-, Deployment- oder Setup-Änderungen auch docs/*.md anpassen.
5. **Vor dem Commit bauen** — run_build aufrufen und Fehler beheben, bevor als erledigt markiert wird.

---

## 1. Pflichtregeln für Dateibearbeitung

| Szenario | Vorgehensweise |
|---|---|
| Vollständige Datei neu schreiben | remove_file + create_file (NIEMALS replace_string_in_file für ganze Dateien) |
| Kleine lokale Änderung | replace_string_in_file mit mind. 3-5 Zeilen Kontext vor/nach der Änderung |
| Mehrere Änderungen in einer Datei | multi_replace_string_in_file in einem einzigen Aufruf |
| Datei löschen | remove_file |
| Neue Datei anlegen | create_file (Verzeichnis wird automatisch erstellt) |

> **Warum:** replace_string_in_file hängt bei großen Rewrites alten Code an → CS9348/CS0102 Buildfehler.

---

## 2. Projektstruktur

VERA.Client/                 <- .NET 10 MAUI App (Android primaer, iOS + macOS + Windows bedingt)
  VERA.Client.csproj         <- ApplicationDisplayVersion muss SemVer-konform und mit AppVersion.Current synchron sein
  App.xaml.cs                <- Startup: Server-URL-Check -> RegisterPage/LoginPage/AppShell
  MauiProgram.cs             <- DI: ApiClient, AccountService, GamificationService, UpdateService
  Services/
    ApiClient.cs             <- HTTP-Client, JWT-Refresh transparent, X-Client-Version Header, CheckServerAsync()
    AccountService.cs        <- Lokale Accounts (PBKDF2, nur Fallback)
    UpdateService.cs         <- GitHub Releases API, Update-Check beim Start
    GamificationService.cs   <- 30 Level, exponentielle XP, 18 Achievements, 5 Raenge
    TimeTrackingService.cs   <- Lokaler JSON-Speicher, atomare Schreiboperationen
  Views/
    LoginPage.xaml(.cs)      <- Server-URL-Feld + Verbindungsstatus-Badge + Biometrie
    RegisterPage.xaml(.cs)   <- Server-Registrierung + Verbindungsstatus-Badge
    LevelingPage.xaml(.cs)   <- Gamification-Detailseite
    EinstellungenPage.xaml(.cs) <- Server-URL, Passwort aendern
  Platforms/
    Android/
      Resources/xml/network_security_config.xml  <- HTTP + HTTPS erlaubt (user-konfigurierte Server-URL)
    iOS/
      Info.plist             <- NSAppTransportSecurity: NSAllowsArbitraryLoads=true (HTTP + HTTPS)
    MacCatalyst/
      Info.plist             <- NSAppTransportSecurity: NSAllowsArbitraryLoads=true (HTTP + HTTPS)
      Entitlements.plist     <- network.client Entitlement

VERA.Server/                 <- ASP.NET Core 10 Web API
  Program.cs                 <- DI, JWT, SQLite, Rate-Limiter, ForwardedHeaders, Security-Header
  Controllers/
    AuthController.cs        <- /api/auth (register, login, refresh, logout, change-password)
    TimeEntriesController.cs <- /api/entries (GET, POST, DELETE, POST sync)
    InfoController.cs        <- /api/info (GET, keine Auth) - Server-Version + MinClientVersion
  Services/
    AuthService.cs           <- PBKDF2-SHA256, JWT-Ausgabe, Refresh-Rotation, Lockout
  Data/
    VeraDbContext.cs         <- EF Core DbContext, Indizes, Cascade-Delete
    Entities.cs              <- User, TimeEntry, RefreshToken
    Migrations/              <- EF Core Migrationen (InitialCreate)
  appsettings.json           <- DataDirectory, Jwt-Konfiguration

VERA.Shared/                 <- .NET 10 Class Library (Single Source of Truth)
  Dto/ApiDtos.cs             <- RegisterRequest, LoginRequest, AuthResponse, TimeEntryDto, ServerInfoResponse, ...
  AppVersion.cs              <- Versionskonstanten: Current, MinServerVersion, MinClientVersion
  LoginResult.cs             <- Enum: Success, InvalidPassword, NoAccountFound, AccountLocked

Dockerfile                   <- Multi-Stage Build, Non-Root User vera (uid 1001), Port 8080
pelican-egg.json             <- Pterodactyl/Pelican Egg-Definition

docs/
  DEPLOYMENT.md              <- Pelican/Docker-Deployment-Guide
  SETUP.md                   <- Lokale Entwicklungsumgebung
  ARCHITECTURE.md            <- Systemarchitektur, Datenfluss, Sicherheitsebenen

CHANGELOG.md                 <- SemVer-Changelog (MUSS bei jeder Version aktualisiert werden)
README.md                    <- Projektueberblick
AGENTS.md                    <- Dieses Dokument

---

## 3. Versionierung (SemVer)

Format: MAJOR.MINOR.PATCH

| Aenderung | Version erhoehen |
|---|---|
| Breaking API-Change, grosse Architekturveraenderung | MAJOR |
| Neue Funktion (abwaertskompatibel) | MINOR |
| Bugfix, Sicherheitspatch, Doku-Update | PATCH |

Pflichten bei jeder Versionsaenderung:
1. VERA.Client/VERA.Client.csproj -> ApplicationDisplayVersion aktualisieren (z.B. "1.3.0")
2. VERA.Client/VERA.Client.csproj -> ApplicationVersion erhoehen (Integer, Android-Build-Nummer)
3. VERA.Shared/AppVersion.cs -> Current auf dieselbe Version setzen
4. CHANGELOG.md -> neuen Eintrag unter ## [X.Y.Z] - YYYY-MM-DD hinzufuegen
5. Commit + Push auf master — GitHub Actions baut und veroeffentlicht den Release automatisch.
   - Version wird aus ApplicationDisplayVersion in VERA.Client/VERA.Client.csproj gelesen
   - Git-Tag vX.Y.Z wird automatisch gesetzt
   - vera-server.zip und vera-android.apk landen unter releases/latest/download/

Kein manuelles git tag noetig.

**Aktuelle Version: 1.3.0**

---

## 4. Build-Befehle

# MAUI App (Android) - laeuft auf Linux/Windows/macOS CI
dotnet build VERA.Client\VERA.Client.csproj -f net10.0-android

# MAUI App (iOS) - NUR auf macOS moeglich (Xcode erforderlich)
dotnet build VERA.Client\VERA.Client.csproj -f net10.0-ios

# MAUI App (macOS Catalyst) - NUR auf macOS moeglich (Xcode erforderlich)
dotnet build VERA.Client\VERA.Client.csproj -f net10.0-maccatalyst

# VERA.Server
dotnet build VERA.Server\VERA.Server.csproj

# Gesamte Solution
dotnet build VERA.slnx

# EF Core Migration hinzufuegen (im VERA.Server-Verzeichnis)
dotnet ef migrations add <MigrationName> --project VERA.Server

# EF Core Datenbank aktualisieren (lokal)
dotnet ef database update --project VERA.Server

Hinweis iOS/macOS: GitHub Actions baut nur Android (ubuntu-runner). iOS/macOS-Builds erfordern einen macOS-Runner mit Xcode.

---

## 5. HTTPS-Policy und Netzwerksicherheit

Das VERA-Backend laeuft auf HTTP hinter dem Pelican-Reverse-Proxy. HTTPS wird am Proxy terminiert.

| Ebene | Regel |
|---|---|
| Server (Program.cs) | UseForwardedHeaders aktiviert. UseHsts und UseHttpsRedirection sind VERBOTEN. |
| Android | network_security_config.xml: cleartextTrafficPermitted=true — HTTP wird toleriert, da die Server-URL vom Nutzer konfiguriert wird. |
| iOS | Info.plist: NSAppTransportSecurity mit NSAllowsArbitraryLoads=true — notwendig fuer user-konfigurierte HTTP-Server. |
| macOS Catalyst | Info.plist: identisch mit iOS — NSAllowsArbitraryLoads=true. |
| Empfehlung an Nutzer | HTTPS-URL verwenden wenn moeglich. HTTP bleibt fuer lokale/private Server supportet. |

NIEMALS UseHsts() oder UseHttpsRedirection() in Program.cs einfuegen — der Container kommuniziert nur ueber HTTP.

---

## 6. Sicherheitskonventionen

- Passwort-Hashing: PBKDF2-SHA256, 300.000 Iterationen, 32-Byte-Salt, 32-Byte-Hash (nur Server)
- JWT Access Token: 15 Minuten, HmacSha256
- JWT Refresh Token: 7 Tage, Rotation bei jedem Refresh (altes Token wird sofort invalidiert)
- Lockout: 5 fehlgeschlagene Logins -> 5 Minuten gesperrt (IP-basiert im Rate-Limiter)
- Rate-Limiting: 20 Requests/Minute pro IP auf /api/auth/* (Custom ConcurrentDictionary-Middleware)
- Security-Header: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy
- ForwardedHeaders: XForwardedFor + XForwardedProto — KnownNetworks/KnownProxies gecleart fuer Pelican-Kompatibilitaet
- Secrets: Jwt__Secret NIEMALS in Quellcode — nur via Umgebungsvariable oder Pelican-Panel
- Atomare Schreiboperationen: TimeTrackingService schreibt in .tmp-Datei, dann File.Replace
- Keystore: *.keystore / *.jks sind in .gitignore — niemals committen

---

## 7. CHANGELOG.md Pflege

Das CHANGELOG.md folgt dem Keep a Changelog Format:

## [X.Y.Z] - YYYY-MM-DD
### Added
- Neue Features
### Changed
- Geaenderte Features
### Fixed
- Behobene Bugs
### Security
- Sicherheits-Fixes

CHANGELOG muss IMMER aktualisiert werden, bevor ein Commit als neue Version markiert wird.

---

## 8. VERA.Shared — Cross-Project Types

VERA.Shared ist der EINZIGE Ort fuer Typen, die von MAUI App und Server gemeinsam genutzt werden:
- DTOs: VERA.Shared.Dto.ApiDtos.cs
- Enums: VERA.Shared.LoginResult
- Versionskonstanten: VERA.Shared.AppVersion

NIEMALS Duplikate dieser Typen in VERA.Client.* oder VERA.Server.* anlegen.

---

## 9. Bekannte Fallstricke

1. replace_string_in_file bei grossen Dateien -> Haengt alten Code an -> delete+create verwenden
2. Rate-Limiter: AddFixedWindowLimiter benoetigt extra NuGet in .NET 10 -> Custom ConcurrentDictionary-Middleware verwenden
3. LoginResult Enum: lebt in VERA.Shared, NICHT in VERA.Services
4. EF Core Migrations: Aenderungen an Entities.cs erfordern neue Migration + db.Database.Migrate() laeuft automatisch
5. pelican-egg.json Install-Script: Referenziert GitHub-Releases-URL -> muss aktuell sein
6. Android minSdkVersion: 23 (Android 6.0) — Biometric-NuGet ist Android-only
7. AppVersion.Current muss mit ApplicationDisplayVersion in VERA.Client/VERA.Client.csproj uebereinstimmen — BEIDE aktualisieren
8. GET /api/info ist bewusst ohne [Authorize] — wird vor dem Login aufgerufen
9. iOS/macOS builds erfordern Xcode auf macOS — GitHub Actions baut nur Android
10. UseHsts() / UseHttpsRedirection() NIEMALS in Program.cs einfuegen — Server laeuft HTTP hinter Pelican-Proxy
11. NSAppTransportSecurity in iOS/macOS Info.plist ist bewusst NSAllowsArbitraryLoads=true — fuer user-konfigurierte HTTP-Server

---

## 10. Dokumentationspflichten

Bei jeder Session, die neue Features hinzufuegt oder bestehende aendert:

- [ ] CHANGELOG.md aktualisieren
- [ ] Relevante docs/*.md aktualisieren (falls Deployment/Setup/Architektur betroffen)
- [ ] ApplicationDisplayVersion in VERA.Client/VERA.Client.csproj pruefen/erhoehen
- [ ] ApplicationVersion (Integer) in VERA.Client/VERA.Client.csproj erhoehen
- [ ] AppVersion.Current in VERA.Shared/AppVersion.cs synchron halten
- [ ] AGENTS.md aktualisieren wenn sich Architektur, Plattformen oder Konventionen aendern
- [ ] run_build erfolgreich ausfuehren bevor Session als abgeschlossen gilt

---

## 11. Technologie-Stack (Kurzreferenz)

| Komponente | Technologie | Version / Details |
|---|---|---|
| Mobile App | .NET MAUI | .NET 10 |
| Android | Android SDK | minSdk 23 (6.0) |
| iOS | iOS | min 15.0, Xcode erforderlich |
| macOS | Mac Catalyst | min 15.0, Xcode erforderlich |
| Windows | Windows App SDK | min 10.0.17763.0 (optional) |
| Backend | ASP.NET Core | .NET 10, HTTP hinter Pelican-Proxy |
| Datenbank | SQLite + EF Core | 9.0.x, Migrations auto-apply |
| Auth | JWT Bearer | HmacSha256, 15min/7d Rotation |
| CI/CD | GitHub Actions | 3-Job Workflow: read-version + build-server + build-android -> release |
| Hosting | Pelican / Docker | panel.chaosfritten.de, Port 27022 |
