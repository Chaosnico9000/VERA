# VERA — Zeiterfassungs-App

> **V**irtualer **E**rfassungs- und **R**apportierungs-**A**ssistent

Eine .NET MAUI Zeiterfassungs-App für Android mit vollständigem Server-Backend, Gamification und biometrischer Sicherheit.

---

## Features

| Bereich | Features |
|---|---|
| ⏱ **Zeiterfassung** | Start/Stop-Timer, Kategorien, Kalenderansicht, Pro-Tag-Delta |
| 🏆 **Gamification** | 30 Level, exponentielle XP-Kurve, 18 Achievements, 5 Ränge |
| 🔒 **Sicherheit** | PBKDF2-Passwörter, JWT-Auth, Biometrie, Rate-Limiting |
| ☁️ **Server-Sync** | ASP.NET Core 10 Backend, SQLite, JWT mit Refresh-Rotation |
| 🐳 **Deployment** | Docker + Pelican/Pterodactyl Egg für einfaches Hosting |

---

## Schnellstart

### App (Android)

```bash
# Voraussetzungen: .NET 10 SDK + Android SDK
git clone https://github.com/Chaosnico9000/VERA.git
cd VERA
dotnet build VERA/VERA.csproj -f net10.0-android
```

### Server (lokal)

```bash
cd VERA
dotnet run --project VERA.Server/VERA.Server.csproj
# Server läuft auf http://localhost:8080
```

### Server (Docker)

```bash
docker build -t vera-server .
docker run -d -p 8080:8080 \
  -e Jwt__Secret="dein-geheimes-secret-mindestens-32-zeichen" \
  -e Jwt__Issuer="vera-server" \
  -e Jwt__Audience="vera-app" \
  -v vera-data:/data \
  vera-server
```

---

## Dokumentation

| Dokument | Beschreibung |
|---|---|
| [📦 Deployment](docs/DEPLOYMENT.md) | Pelican/Pterodactyl & Docker Deployment-Guide |
| [🛠 Setup](docs/SETUP.md) | Lokale Entwicklungsumgebung einrichten |
| [🏛 Architektur](docs/ARCHITECTURE.md) | System-Architektur, Datenfluss, Sicherheit |
| [📋 Changelog](CHANGELOG.md) | Versionshistorie (SemVer) |

---

## Technologie-Stack

- **App:** .NET 10 MAUI (Android, iOS, Windows, macOS)
- **Backend:** ASP.NET Core 10 Web API
- **Datenbank:** SQLite via EF Core 9
- **Auth:** JWT Bearer (HmacSha256) + PBKDF2-SHA256
- **Container:** Docker (`mcr.microsoft.com/dotnet/aspnet:10.0`)
- **Deployment:** Pelican/Pterodactyl Panel

---

## Versionen

Aktuelle Version: **v1.0.0** — Siehe [CHANGELOG.md](CHANGELOG.md)

---

## Lizenz

Privates Projekt — Alle Rechte vorbehalten.
