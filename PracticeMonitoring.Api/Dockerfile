FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PracticeMonitoring.Api/PracticeMonitoring.Api.csproj PracticeMonitoring.Api/
RUN dotnet restore PracticeMonitoring.Api/PracticeMonitoring.Api.csproj

COPY PracticeMonitoring.Api/ PracticeMonitoring.Api/
WORKDIR /src/PracticeMonitoring.Api
RUN dotnet publish PracticeMonitoring.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl gnupg \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL https://www.postgresql.org/media/keys/ACCC4CF8.asc -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc \
    && . /etc/os-release \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt ${VERSION_CODENAME}-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-18 \
    && ln -sf /usr/lib/postgresql/18/bin/pg_dump /usr/local/bin/pg_dump \
    && ln -sf /usr/lib/postgresql/18/bin/pg_restore /usr/local/bin/pg_restore \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ["sh", "-c", "dotnet PracticeMonitoring.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
