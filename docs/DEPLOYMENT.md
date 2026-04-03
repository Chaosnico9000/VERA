# VERA Server — Deployment-Guide

Dieser Guide beschreibt das Deployment des VERA.Server-Backends auf:
- [Pelican/Pterodactyl Panel](#pelican--pterodactyl)
- [Docker (manuell)](#docker-manuell)
- [Docker Compose](#docker-compose)

---

## Voraussetzungen

- Docker oder ein Pelican/Pterodactyl-Panel
- Einen sicheren JWT-Secret (mind. 32 zufällige Zeichen)
- Einen offenen Port (Standard: `8080`)

---

## Pelican / Pterodactyl

### Schritt 1: Egg importieren

1. Im Panel-Admin zu **Nests** → **Import Egg** navigieren
2. Die Datei `pelican-egg.json` aus dem Repository-Root hochladen
3. Das Egg wird als **"VERA Backend"** im gewählten Nest registriert

### Schritt 2: Server erstellen

1. Neuen Server erstellen und das **VERA Backend** Egg auswählen
2. Folgende Umgebungsvariablen konfigurieren:

| Variable | Beschreibung | Pflicht | Standard |
|---|---|---|---|
| `Jwt__Secret` | JWT-Signierungsschlüssel (mind. 32 Zeichen, zufällig!) | ✅ Ja | — |
| `Jwt__Issuer` | JWT-Aussteller (z.B. deine Domain) | ✅ Ja | `vera-server` |
| `Jwt__Audience` | JWT-Zielgruppe | ✅ Ja | `vera-app` |
| `ASPNETCORE_HTTP_PORTS` | Port des Servers | ✅ Ja | `8080` |

> ⚠️ **Sicherheitshinweis:** `Jwt__Secret` niemals teilen, in Logs ausgeben oder in Quellcode schreiben.  
> Generierung: `openssl rand -base64 48` oder ein Passwortmanager.

### Schritt 3: Server starten

1. Server im Panel starten
2. In der Konsole erscheint: `Now listening on http://[::]:8080`
3. Der Server ist betriebsbereit

### Schritt 4: Datenbank

- Die SQLite-Datenbank wird beim ersten Start **automatisch** unter `/data/vera.db` angelegt
- EF Core Migrationen werden beim Start automatisch angewendet
- Das `/data`-Verzeichnis ist als persistentes Volume gemountet — Daten bleiben bei Server-Neustarts erhalten

### Schritt 5: App verbinden

1. VERA-App öffnen
2. Beim ersten Start wird nach der **Server-URL** gefragt
3. Server-URL eingeben (z.B. `https://mein-server.example.com:8080`)
4. Registrieren und loslegen

---

## Docker (manuell)

### Image bauen

```bash
# Aus dem Repository-Root
docker build -t vera-server:latest .
```

### Container starten

```bash
docker run -d \
  --name vera-server \
  --restart unless-stopped \
  -p 8080:8080 \
  -e Jwt__Secret="HIER_SICHERES_SECRET_MIN_32_ZEICHEN" \
  -e Jwt__Issuer="vera-server" \
  -e Jwt__Audience="vera-app" \
  -e ASPNETCORE_HTTP_PORTS="8080" \
  -v vera-data:/data \
  vera-server:latest
```

### Logs prüfen

```bash
docker logs vera-server -f
```

### Container stoppen/aktualisieren

```bash
docker stop vera-server
docker rm vera-server
# Neues Image bauen (siehe oben)
docker run ...  # Gleicher Befehl wie oben
```

---

## Docker Compose

Erstelle eine `docker-compose.yml` (nicht im Repository, da sie Secrets enthält):

```yaml
services:
  vera-server:
    build: .
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      Jwt__Secret: "HIER_SICHERES_SECRET_MIN_32_ZEICHEN"
      Jwt__Issuer: "vera-server"
      Jwt__Audience: "vera-app"
      ASPNETCORE_HTTP_PORTS: "8080"
    volumes:
      - vera-data:/data

volumes:
  vera-data:
```

```bash
docker compose up -d
docker compose logs -f
```

---

## Reverse Proxy (empfohlen)

Für HTTPS (von der VERA-App **zwingend erforderlich** in Production) einen Reverse Proxy vorschalten:

### Nginx Beispiel

```nginx
server {
    listen 443 ssl;
    server_name mein-server.example.com;

    ssl_certificate     /etc/ssl/certs/cert.pem;
    ssl_certificate_key /etc/ssl/private/key.pem;

    location / {
        proxy_pass         http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

> **Hinweis:** Die VERA Android-App erzwingt HTTPS durch `network_security_config.xml`.  
> Nur für lokale Entwicklung/Tests ist HTTP erlaubt (via `cleartextTrafficPermitted`).

---

## Troubleshooting

| Problem | Lösung |
|---|---|
| `Jwt__Secret` zu kurz | Secret muss mind. 32 ASCII-Zeichen lang sein |
| Datenbankfehler beim Start | Prüfe, ob `/data` schreibbar ist (Owner: `vera`, uid 1001) |
| App verbindet sich nicht | Server-URL ohne abschließenden `/` eingeben, Port prüfen |
| `401 Unauthorized` bei allen Anfragen | Access Token abgelaufen → App neu starten (Auto-Refresh) |
| Rate-Limit erreicht (429) | 1 Minute warten (20 Req/Min auf Auth-Endpunkten) |

---

## Datenpfade im Container

| Pfad | Inhalt |
|---|---|
| `/data/vera.db` | SQLite-Datenbank (persistentes Volume) |
| `/app/` | Anwendungsdateien (unveränderlich) |
| `/app/appsettings.json` | Basis-Konfiguration (Defaults) |
