# VERA — Systemarchitektur

---

## Überblick

VERA ist ein Client-Server-System bestehend aus:

```
┌─────────────────────────────────────────────────────────────┐
│                    VERA MAUI App (Android)                   │
│  ┌──────────────┐  ┌─────────────────┐  ┌───────────────┐  │
│  │  TimeTracking │  │  Gamification   │  │  Auth / UI    │  │
│  │  Service      │  │  Service        │  │  Pages        │  │
│  └──────┬───────┘  └────────┬────────┘  └───────┬───────┘  │
│         │                   │                    │          │
│         └───────────────────┴────────────────────┘          │
│                             │                               │
│                        ApiClient                            │
│                  (JWT-Bearer, Auto-Refresh)                  │
└─────────────────────────────┬───────────────────────────────┘
                              │ HTTPS (JWT Bearer Token)
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      VERA.Server                            │
│               (ASP.NET Core 10 Web API)                     │
│                                                             │
│  ┌──────────────────┐        ┌──────────────────────────┐  │
│  │  AuthController   │        │  TimeEntriesController    │  │
│  │  /api/auth/*      │        │  /api/entries/*           │  │
│  └────────┬─────────┘        └───────────┬──────────────┘  │
│           │                               │                  │
│  ┌────────▼─────────┐        ┌───────────▼──────────────┐  │
│  │   AuthService    │        │      VeraDbContext         │  │
│  │  PBKDF2, JWT     │        │   (EF Core + SQLite)       │  │
│  └──────────────────┘        └──────────────────────────┘  │
│                                                             │
│  ── Middleware ─────────────────────────────────────────── │
│  Rate-Limiter (20/min/IP) │ Security-Header │ JWT-Bearer   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │   SQLite DB     │
                    │  /data/vera.db  │
                    │  (persistentes  │
                    │   Volume)       │
                    └─────────────────┘
```

---

## Komponenten

### VERA MAUI App

| Klasse | Verantwortung |
|---|---|
| `App.xaml.cs` | Startup-Logik: Server-URL prüfen → Register/Login/AppShell |
| `MauiProgram.cs` | DI-Container: Services registrieren |
| `ApiClient` | HTTP-Client mit transparenter JWT-Erneuerung |
| `AccountService` | Lokaler PBKDF2-Account (Fallback ohne Server) |
| `GamificationService` | XP-Berechnung, Level, Achievements, Ränge |
| `TimeTrackingService` | Lokale JSON-Speicherung mit atomaren Schreibvorgängen |
| `LoginPage` | Server-Login + biometrische Entsperrung |
| `RegisterPage` | Erstregistrierung am Server |
| `LevelingPage` | Gamification-Detailansicht |
| `EinstellungenPage` | Server-URL, Passwort ändern |

### VERA.Server

| Klasse | Verantwortung |
|---|---|
| `Program.cs` | DI, Middleware-Pipeline, DB-Migration beim Start |
| `AuthController` | REST-Endpunkte für Authentifizierung |
| `TimeEntriesController` | REST-Endpunkte für Zeiteinträge (CRUD + Sync) |
| `AuthService` | PBKDF2-Hashing, JWT-Ausgabe, Lockout-Logik |
| `VeraDbContext` | EF Core DbContext mit Indizes und Cascade-Delete |
| `Entities` | `User`, `TimeEntry`, `RefreshToken` EF-Entitäten |

### VERA.Shared

Gemeinsame Class Library — Single Source of Truth für cross-project Typen:

| Typ | Beschreibung |
|---|---|
| `RegisterRequest` | DTO: Registrierungsanfrage |
| `LoginRequest` | DTO: Login-Anfrage |
| `RefreshRequest` | DTO: Token-Erneuerungsanfrage |
| `AuthResponse` | DTO: Server-Antwort mit Access+Refresh-Token |
| `ChangePasswordRequest` | DTO: Passwortänderung |
| `TimeEntryDto` | DTO: Zeiteintrag (lesend) |
| `UpsertTimeEntryRequest` | DTO: Zeiteintrag anlegen/aktualisieren |
| `ApiError` | DTO: Fehlerantwort mit Code und Nachricht |
| `LoginResult` | Enum: Success, InvalidPassword, NoAccountFound, AccountLocked |

---

## Authentifizierungs-Datenfluss

```
App                         Server
 │                            │
 ├─── POST /api/auth/login ──►│
 │    { username, password }  │
 │                            ├── PBKDF2-Verify
 │                            ├── Lockout prüfen
 │◄── 200 OK ─────────────────┤
 │    { accessToken (15min),  │
 │      refreshToken (7d),    │
 │      expiresAt }           │
 │                            │
 │  [Token läuft bald ab]     │
 ├─── POST /api/auth/refresh ►│
 │    { refreshToken }        │
 │                            ├── Token validieren
 │                            ├── Altes Token invalidieren
 │                            ├── Neues Refresh-Token ausgeben
 │◄── 200 OK ─────────────────┤
 │    { neuer accessToken,    │
 │      neuer refreshToken }  │
 │                            │
 │  [API-Aufruf]              │
 ├─── GET /api/entries ──────►│
 │    Authorization: Bearer   │
 │    <accessToken>           │
 │◄── 200 OK ─────────────────┤
 │    [ ...entries ]          │
```

---

## Sicherheitsebenen

### Transport
- HTTPS erzwungen durch `network_security_config.xml` (Android)
- Nur System-CAs akzeptiert (kein User-Cert-Trust)

### Authentifizierung
- PBKDF2-SHA256, 300.000 Iterationen, 32-Byte-Salt + 32-Byte-Hash
- JWT Access Token: 15 Minuten, HmacSha256
- JWT Refresh Token: 7 Tage, Rotation bei jedem Refresh
- Lockout nach 5 Fehlversuchen: 5 Minuten gesperrt

### API-Schutz
- Rate-Limiter: 20 Requests/Minute pro IP auf `/api/auth/*`
- `[Authorize]`-Attribut auf allen geschützten Endpunkten
- Security-Header bei jeder Response

### Datenspeicherung
- Server: SQLite unter `/data/vera.db` (persistentes Volume, non-root Owner)
- Client: Lokale JSON-Datei mit atomaren Schreibvorgängen (`.tmp` → `File.Replace`)
- Refresh-Token im Android `Preferences`-Store (Android KeyStore-geschützt)

### Container-Sicherheit
- Non-root User `vera` (uid/gid 1001) im Docker-Container
- Read-only Anwendungsdateien, nur `/data` ist schreibbar

---

## Gamification-System

### XP-Kurve

```
XP für Level n = 2000 × 1.55^(n-1)

Level  1:         2.000 XP  (Startwert)
Level  5:        22.800 XP
Level 10:       310.000 XP
Level 15:     3.500.000 XP
Level 20:    40.000.000 XP
Level 29: ~8.000.000.000 XP (≈ 4 Jahre tägliche Nutzung)
```

### Ränge

| Rang | Level | Beschreibung |
|---|---|---|
| Neuling | 1–6 | Einstieg |
| Lehrling | 7–12 | Grundlagen |
| Geselle | 13–18 | Fortgeschritten |
| Meister | 19–24 | Experte |
| Experte | 25–30 | Elite |

### XP-Quellen

- Erfasste Arbeitsstunden
- Vollständige Monate (Bonus)
- Streaks (Wochen-Berechnung, 260-Wochen-Rückblick)
- Achievement-Unlocks
- Jubiläen (besondere Meilensteine)

---

## Datenbank-Schema (SQLite)

```sql
Users
  Id          GUID (PK)
  Username    TEXT UNIQUE
  PasswordHash TEXT
  PasswordSalt TEXT
  FailedLoginAttempts INT
  LockoutUntil DATETIME?
  CreatedAt   DATETIME

TimeEntries
  Id          GUID (PK)
  UserId      GUID (FK → Users.Id, CASCADE DELETE)
  Title       TEXT
  Category    TEXT
  StartTime   DATETIME
  EndTime     DATETIME?
  Type        INT

RefreshTokens
  Id          GUID (PK)
  UserId      GUID (FK → Users.Id, CASCADE DELETE)
  Token       TEXT UNIQUE
  ExpiresAt   DATETIME
  CreatedAt   DATETIME
  CreatedByIp TEXT
  RevokedAt   DATETIME?
  RevokedByIp TEXT?
```

---

## API-Endpunkte

### Auth (`/api/auth`)

| Method | Endpoint | Auth | Beschreibung |
|---|---|---|---|
| POST | `/register` | — | Benutzer registrieren |
| POST | `/login` | — | Login, gibt Tokens zurück |
| POST | `/refresh` | — | Access Token erneuern |
| POST | `/logout` | ✅ Bearer | Refresh Token invalidieren |
| POST | `/change-password` | ✅ Bearer | Passwort ändern |

### Zeiteinträge (`/api/entries`)

| Method | Endpoint | Auth | Beschreibung |
|---|---|---|---|
| GET | `/` | ✅ Bearer | Alle Einträge des Benutzers |
| POST | `/` | ✅ Bearer | Eintrag anlegen oder aktualisieren (Upsert) |
| DELETE | `/{id}` | ✅ Bearer | Eintrag löschen |
| POST | `/sync` | ✅ Bearer | Bulk-Sync aller Einträge |
