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
docker compose -f docker-compose.local.yml up -d
```

Run API:

```bash
dotnet run --project UptimeMonitoring.Api
```

Run Worker (in a separate terminal):

```bash
dotnet run --project UptimeMonitoring.Worker
```

## Basic usage (API)

- **Register**: `POST /api/auth/register`
- **Login**: `POST /api/auth/login` → returns JWT token
- **Add website** (auth required): `POST /api/websites`
- **List websites** (auth required): `GET /api/websites`
- **Dashboard status** (auth required): `GET /api/dashboard/status`

## Email alerts (optional)

Email sending is **disabled by default**. Configure in `appsettings.json` / `appsettings.Development.json` (or via environment variables):

- `Smtp__Enabled=true`
- `Smtp__Host=smtp.example.com`
- `Smtp__Port=587`
- `Smtp__EnableSsl=true`
- `Smtp__Username=...`
- `Smtp__Password=...`
- `Smtp__From=alerts@example.com`

