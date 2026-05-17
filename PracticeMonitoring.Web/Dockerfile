FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY PracticeMonitoring.Web/PracticeMonitoring.Web.csproj PracticeMonitoring.Web/
RUN dotnet restore PracticeMonitoring.Web/PracticeMonitoring.Web.csproj

COPY PracticeMonitoring.Web/ PracticeMonitoring.Web/
WORKDIR /src/PracticeMonitoring.Web
RUN dotnet publish PracticeMonitoring.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libreoffice-writer fonts-liberation fontconfig \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

CMD ["sh", "-c", "dotnet PracticeMonitoring.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]
