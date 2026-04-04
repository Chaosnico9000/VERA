# VERA — Zeiterfassungs-App

> **V**irtualer **E**rfassungs- und **R**apportierungs-**A**ssistent

Eine .NET MAUI Zeiterfassungs-App fuer Android, iOS und macOS mit vollstaendigem Server-Backend, Gamification und biometrischer Sicherheit.

Aktuelle Version: **v1.3.0** — Siehe [CHANGELOG.md](CHANGELOG.md)

---

## Features

| Bereich | Features |
|---|---|
| Zeiterfassung | Start/Stop-Timer, Kategorien, Kalenderansicht, Pro-Tag-Delta |
| Gamification | 30 Level, exponentielle XP-Kurve, 18 Achievements, 5 Raenge |
| Sicherheit | PBKDF2-Passwoerter, JWT-Auth, Biometrie (Android), Rate-Limiting |
| Server-Sync | ASP.NET Core 10 Backend, SQLite, JWT mit Refresh-Rotation |
| Deployment | Docker + Pelican/Pterodactyl Egg fuer einfaches Hosting |
| Cross-Platform | Android (primär), iOS/iPadOS, macOS Catalyst, Windows (optional) |

---

## App herunterladen und installieren

### Android

Die neueste APK ist immer unter [Releases](https://github.com/Chaosnico9000/VERA/releases/latest) verfuegbar.

1. `vera-android.apk` herunterladen
2. Auf dem Geraet oeffnen — „Unbekannte Quellen" erlauben falls gefragt
3. Installieren — die App prueft beim Start automatisch auf neue Versionen

---

### iOS / iPadOS

> **Wichtig:** Apple erlaubt keine direkte APK-aehnliche Installation. Es gibt zwei Wege ohne
> kostenpflichtigen Apple Developer Account:

#### Option A — AltStore (empfohlen, kostenlos)

1. [AltStore](https://altstore.io) auf dem Mac oder Windows-PC installieren
2. AltServer im System-Tray starten
3. iPhone/iPad per USB verbinden
4. `vera-ios.ipa` aus dem [Release](https://github.com/Chaosnico9000/VERA/releases/latest) herunterladen
5. In AltStore: „+" → IPA-Datei auswaehlen → Installieren
6. iPhone: **Einstellungen → Allgemein → VPN & Geraeteverwaltung → eigene Apple-ID → Vertrauen**
7. App laeuft 7 Tage, danach mit AltStore verlaengern (solange iPhone am PC ist)

#### Option B — Sideloadly (kostenlos)

1. [Sideloadly](https://sideloadly.io) herunterladen und installieren
2. `vera-ios.ipa` aus dem Release herunterladen
3. Sideloadly oeffnen → IPA per Drag & Drop hineinziehen → Apple-ID eingeben → Start
4. iPhone: **Einstellungen → Allgemein → VPN & Geraeteverwaltung → eigene Apple-ID → Vertrauen**

#### Option C — TestFlight (Apple Developer Account erforderlich)

Erfordert einen Apple Developer Account ($99/Jahr). Siehe [Deployment-Guide](docs/DEPLOYMENT.md).

> **Hinweis iOS-Build:** Ein iOS-IPA wird nur gebaut wenn die GitHub-Secrets
> `APPLE_CERTIFICATE_P12_BASE64`, `APPLE_PROVISIONING_PROFILE_BASE64` und `APPLE_TEAM_ID`
> im Repository gesetzt sind. Ohne diese Secrets wird der Android-Release normal erstellt —
> der iOS-Job wird still uebersprungen.

---

### macOS Catalyst

Der macOS-Build erfordert lokale Entwicklung mit Xcode. Siehe [Setup-Guide](docs/SETUP.md).

---

## Server einrichten

### Pelican / Pterodactyl (empfohlen)

Das mitgelieferte Egg installiert den Server automatisch:
1. `pelican-egg.json` ins Panel importieren
2. Server erstellen — der Installationsscript laedt `vera-server.zip` vom neuesten Release
3. Umgebungsvariablen setzen (siehe unten)
4. Server starten

### Docker

```bash
docker build -t vera-server .
docker run -d -p 8080:8080 \
  -e Jwt__Secret="mindestens-32-zeichen-langes-secret" \
  -e Jwt__Issuer="vera-server" \
  -e Jwt__Audience="vera-app" \
  -v vera-data:/data \
  vera-server
```

### Umgebungsvariablen (Server)

| Variable | Beschreibung | Pflicht |
|---|---|---|
| `Jwt__Secret` | JWT-Signing-Secret (mind. 32 Zeichen) | Ja |
| `Jwt__Issuer` | JWT-Issuer (z.B. `vera-server`) | Ja |
| `Jwt__Audience` | JWT-Audience (z.B. `vera-app`) | Ja |
| `DataDirectory` | Pfad zur SQLite-Datenbank | Nein (Standard: `/home/container/data`) |
| `ASPNETCORE_HTTP_PORTS` | HTTP-Port | Nein (Standard: `8080`) |

---

## Lokale Entwicklung

```bash
git clone https://github.com/Chaosnico9000/VERA.git
cd VERA

# Server starten
dotnet run --project VERA.Server/VERA.Server.csproj

# Android-App bauen (funktioniert auf Linux/Windows/macOS)
dotnet build VERA.Client/VERA.Client.csproj -f net10.0-android

# iOS-App bauen (nur auf macOS mit Xcode)
dotnet build VERA.Client/VERA.Client.csproj -f net10.0-ios

# macOS-App bauen (nur auf macOS mit Xcode)
dotnet build VERA.Client/VERA.Client.csproj -f net10.0-maccatalyst
```

---

## CI/CD — GitHub Actions

Der Workflow `release.yml` baut bei jedem Push auf `master` automatisch:

| Job | Runner | Output |
|---|---|---|
| `build-server` | ubuntu-latest | `vera-server.zip` |
| `build-android` | ubuntu-latest | `vera-android.apk` (signiert) |
| `build-ios` | macos-latest | `vera-ios.ipa` (nur wenn Apple-Secrets gesetzt) |

### GitHub Secrets fuer Android-Signierung

| Secret | Beschreibung |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | Keystore-Datei als Base64 |
| `ANDROID_KEY_ALIAS` | Key-Alias im Keystore |
| `ANDROID_KEY_PASSWORD` | Key-Passwort |
| `ANDROID_KEYSTORE_PASSWORD` | Keystore-Passwort |

### GitHub Secrets fuer iOS-Signierung (optional)

| Secret | Beschreibung |
|---|---|
| `APPLE_CERTIFICATE_P12_BASE64` | Signing-Zertifikat als Base64-P12 |
| `APPLE_CERTIFICATE_PASSWORD` | P12-Zertifikat-Passwort |
| `APPLE_PROVISIONING_PROFILE_BASE64` | Provisioning Profile als Base64 |
| `APPLE_TEAM_ID` | Apple Developer Team-ID |
| `APPLE_BUNDLE_ID` | Bundle-ID (Standard: `com.companyname.vera`) |

---

## Dokumentation

| Dokument | Beschreibung |
|---|---|
| [Deployment](docs/DEPLOYMENT.md) | Pelican/Pterodactyl, Docker, TestFlight |
| [Setup](docs/SETUP.md) | Lokale Entwicklungsumgebung |
| [Architektur](docs/ARCHITECTURE.md) | System-Architektur, Datenfluss, Sicherheit |
| [Changelog](CHANGELOG.md) | Versionshistorie (SemVer) |

---

## Technologie-Stack

| Komponente | Technologie |
|---|---|
| Mobile App | .NET 10 MAUI |
| Android | min. Android 6.0 (SDK 23) |
| iOS / iPadOS | min. iOS 15.0 |
| macOS | min. macOS Catalyst 15.0 |
| Backend | ASP.NET Core 10 |
| Datenbank | SQLite via EF Core 9 |
| Auth | JWT Bearer (HmacSha256) + PBKDF2-SHA256 |
| Container | Docker (mcr.microsoft.com/dotnet/aspnet:10.0) |
| Hosting | Pelican / Pterodactyl Panel |

---

## Lizenz

Privates Projekt — Alle Rechte vorbehalten.
