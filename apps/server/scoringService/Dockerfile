FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar shared primero (dependencia del proyecto) para aprovechar la cache de capas.
COPY apps/server/shared/ apps/server/shared/
COPY apps/server/scoringService/scoringService.csproj apps/server/scoringService/
RUN dotnet restore apps/server/scoringService/scoringService.csproj

COPY apps/server/scoringService/ apps/server/scoringService/
WORKDIR /src/apps/server/scoringService
RUN dotnet publish -c Release -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5030
EXPOSE 5030

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "scoringService.dll"]
