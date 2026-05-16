FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY apps/server/shared/ apps/server/shared/
COPY apps/server/userService/userService.csproj apps/server/userService/
RUN dotnet restore apps/server/userService/userService.csproj

COPY apps/server/userService/ apps/server/userService/
WORKDIR /src/apps/server/userService
RUN dotnet publish -c Release -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5020
EXPOSE 5020

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "userService.dll"]
