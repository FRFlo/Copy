# See https://aka.ms/customizecontainer to learn how to customize your debug container
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
WORKDIR /app
# Configuration des variables d'environnement
ENV CONFIG_PATH=/app/config/config.json \
    SCHEME_PATH=/app/config/scheme.json \
    DEBUG=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Copy/Copy.csproj", "Copy/"]
RUN dotnet restore "./Copy/Copy.csproj"
COPY . .
WORKDIR "/src/Copy"
RUN dotnet build "./Copy.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Copy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Création du répertoire pour la configuration
RUN mkdir -p /app/config

# Volume pour les fichiers de configuration
VOLUME /app/config

ENTRYPOINT ["dotnet", "Copy.dll"]