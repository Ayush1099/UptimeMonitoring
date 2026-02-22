# UptimeMonitoring

API + background worker that periodically checks your websites, stores results, and (optionally) sends email alerts on UP/DOWN transitions.

## Run with Docker (recommended)

From the repo root:

```bash
docker compose up --build
```

- API: `http://localhost:8080/swagger`
- Postgres is exposed on host port `5000` (optional)
- Redis is exposed on host port `6379` (optional)

The API will automatically apply EF Core migrations on startup.

## Run locally (dotnet) + Docker for dependencies

Start Postgres + Redis:

```bash
docker compose -f docker-compose.yml up -d
```

Run API:

```bash
dotnet run --project UptimeMonitoring.Api
```

Run Worker (in a separate terminal):

```bash
dotnet run --project UptimeMonitoring.Worker
```

Run Web UI (in a third terminal):

```bash
dotnet run --project UptimeMonitoring.Web
```

- Web UI: `http://localhost:5001`
- API: `http://localhost:5248/swagger` (when running locally)

**Note**: API and Web are separate applications. They share the same database and run as two processes/hosts/containers. The Web project uses cookie-based auth; the API uses JWT for other clients (e.g. mobile, SPA).

## Basic usage (API)

- **Register**: `POST /api/auth/register`
- **Login**: `POST /api/auth/login` → returns JWT token
- **Add website** (auth required): `POST /api/websites`
- **List websites** (auth required): `GET /api/websites`
- **Dashboard status** (auth required): `GET /api/dashboard/status`

## Web UI (UptimeMonitoring.Web)

Browser-based UI using Razor Pages and cookie auth. Pages: Login, Register, Dashboard, My websites. Add/remove/pause/resume websites via the UI. Runs on its own port (default 5000); uses the same database and Redis as the API.

## Email alerts (optional)

Email sending is **disabled by default**. Configure in `appsettings.json` / `appsettings.Development.json` (or via environment variables):

- `Smtp__Enabled=true`
- `Smtp__Host=smtp.example.com`
- `Smtp__Port=587`
- `Smtp__EnableSsl=true`
- `Smtp__Username=...`
- `Smtp__Password=...`
- `Smtp__From=alerts@example.com`

