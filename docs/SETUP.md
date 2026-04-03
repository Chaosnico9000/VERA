# VERA — Lokale Entwicklungsumgebung

---

## Voraussetzungen

| Tool | Version | Zweck |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0 | MAUI App + Server |
| Android SDK | API 23+ | MAUI Android-Build |
| [Visual Studio](https://visualstudio.microsoft.com/) | 2022/2026 | IDE (empfohlen) |
| [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) | 9.x | Datenbankmigrationen |
| Docker | (optional) | Server-Container |

### EF Core CLI installieren

```bash
dotnet tool install --global dotnet-ef
```

---

## Repository klonen

```bash
git clone https://github.com/Chaosnico9000/VERA.git
cd VERA
```

---

## Solution-Struktur

```
VERA/           ← MAUI App
VERA.Server/    ← ASP.NET Core Backend
VERA.Shared/    ← Gemeinsame DTOs + Enums
VERA.slnx       ← Solution-Datei
```

---

## VERA.Server lokal starten

### 1. Umgebungsvariablen konfigurieren

Erstelle eine `appsettings.Development.json` im `VERA.Server/`-Verzeichnis  
(diese Datei ist in `.gitignore` — niemals einchecken!):

```json
{
  "DataDirectory": "./data",
  "Jwt": {
    "Secret": "lokales-entwicklungs-secret-mindestens-32-zeichen",
    "Issuer": "vera-dev",
    "Audience": "vera-app",
    "ExpiresInMinutes": 15,
    "RefreshExpiresInDays": 7
  }
}
```

Alternativ via PowerShell-Umgebungsvariablen:

```powershell
$env:Jwt__Secret = "lokales-entwicklungs-secret-mindestens-32-zeichen"
$env:Jwt__Issuer = "vera-dev"
$env:Jwt__Audience = "vera-app"
```

### 2. Server starten

```bash
dotnet run --project VERA.Server/VERA.Server.csproj
```

Server läuft auf: `http://localhost:5000` (Development-Port)

> In Development wird HTTP akzeptiert. Die SQLite-Datenbank wird in `VERA.Server/data/vera.db` angelegt.

---

## MAUI App bauen

### Android (Emulator/Gerät)

```bash
dotnet build VERA/VERA.csproj -f net10.0-android
```

### Android im Emulator starten (Visual Studio)

1. Android-Emulator starten (AVD Manager)
2. `VERA` als Startprojekt setzen
3. `F5` drücken

### Server-URL in der App

Beim ersten App-Start → Server-URL eingeben: `http://10.0.2.2:5000`  
(Android-Emulator-Alias für `localhost` des Host-Rechners)

---

## EF Core Migrationen

### Neue Migration erstellen

```bash
# Aus dem Repository-Root
dotnet ef migrations add <MigrationName> --project VERA.Server/VERA.Server.csproj
```

### Datenbank manuell aktualisieren

```bash
dotnet ef database update --project VERA.Server/VERA.Server.csproj
```

> Im Normalbetrieb wird die Datenbank beim Serverstart automatisch migriert (`db.Database.Migrate()`).

### Migrationsstatus prüfen

```bash
dotnet ef migrations list --project VERA.Server/VERA.Server.csproj
```

---

## Solution bauen (gesamt)

```bash
dotnet build VERA.slnx
```

---

## Bekannte Entwicklungshinweise

- **HTTP im Emulator:** `network_security_config.xml` erlaubt `cleartextTrafficPermitted` in Debug-Builds explizit — prüfe dies, wenn HTTP-Verbindungen fehlschlagen
- **Biometrie:** Nur auf echten Android-Geräten mit registriertem Fingerprint/Face testbar
- **Hot Reload:** MAUI Hot Reload funktioniert mit `MauiXamlInflator=SourceGen` (aktiviert)
- **SQLite-DB löschen:** `VERA.Server/data/vera.db` löschen für einen sauberen Neustart
- **VERA.csproj.Backup.tmp:** Kann gefahrlos gelöscht werden (Build-Artefakt)

---

## Projektkonventionen

Siehe [AGENTS.md](../AGENTS.md) für alle Coding-Konventionen, Build-Regeln und Versionierungsregeln.
