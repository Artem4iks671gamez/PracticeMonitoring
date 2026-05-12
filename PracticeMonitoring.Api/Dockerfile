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
    && apt-get install -y --no-install-recommends postgresql-client \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ["sh", "-c", "dotnet PracticeMonitoring.Api.dll --urls http://0.0.0.0:${PORT:-8080}"]
