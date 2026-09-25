FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Full backend tree (main = CareHome.Api only; revenue-cycle-v2 = modular projects).
COPY backend/ backend/
RUN dotnet restore backend/CareHome.Api/CareHome.Api.csproj
RUN dotnet publish backend/CareHome.Api/CareHome.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=40s --retries=10 \
    CMD curl -fsS http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "CareHome.Api.dll"]
