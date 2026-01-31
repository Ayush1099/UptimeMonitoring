FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# Fix Npgsql "libgssapi_krb5.so.2" warning in Debian images
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish UptimeMonitoring.Worker/UptimeMonitoring.Worker.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UptimeMonitoring.Worker.dll"]
