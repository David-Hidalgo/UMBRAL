FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY apps/server/shared/ apps/server/shared/
COPY apps/server/missionService/missionService.csproj apps/server/missionService/
RUN dotnet restore apps/server/missionService/missionService.csproj

COPY apps/server/missionService/ apps/server/missionService/
WORKDIR /src/apps/server/missionService
RUN dotnet publish -c Release -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5022
EXPOSE 5022

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "missionService.dll"]
