# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:11.0.100-resolute AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["NuGet.Config", "global.json", "./"]
COPY ["exemplos/Directory.Build.props", "exemplos/Directory.Build.targets", "exemplos/Directory.Packages.props", "exemplos/"]
COPY ["exemplos/c27-c28-observabilidade/C27C28.Observabilidade.csproj", "exemplos/c27-c28-observabilidade/"]
RUN dotnet restore "exemplos/c27-c28-observabilidade/C27C28.Observabilidade.csproj"

COPY ["exemplos/c27-c28-observabilidade/", "exemplos/c27-c28-observabilidade/"]
RUN dotnet publish "exemplos/c27-c28-observabilidade/C27C28.Observabilidade.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0.0-resolute-chiseled-extra AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Dependencias__Catalogo=http://127.0.0.1:8080 \
    Dependencias__CatalogoTimeout=00:00:03 \
    HostOptions__ShutdownTimeout=00:00:08 \
    Telemetry__ConsoleExporter=false \
    Telemetry__SamplingRatio=0.25

EXPOSE 8080

COPY --from=build /app/publish .

STOPSIGNAL SIGTERM
USER $APP_UID
ENTRYPOINT ["dotnet", "C27C28.Observabilidade.dll"]
