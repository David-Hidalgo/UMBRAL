FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY apps/server/shared/ apps/server/shared/
COPY apps/server/liveSessionService/liveSessionService.csproj apps/server/liveSessionService/
RUN dotnet restore apps/server/liveSessionService/liveSessionService.csproj

COPY apps/server/liveSessionService/ apps/server/liveSessionService/
WORKDIR /src/apps/server/liveSessionService
RUN dotnet publish -c Release -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5026
EXPOSE 5026

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "liveSessionService.dll"]
