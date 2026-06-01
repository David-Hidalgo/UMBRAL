FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY apps/server/shared/ apps/server/shared/
COPY apps/server/api/api.csproj apps/server/api/
RUN dotnet restore apps/server/api/api.csproj

COPY apps/server/api/ apps/server/api/
WORKDIR /src/apps/server/api
RUN dotnet publish -c Release -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5018 
EXPOSE 5018 

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "api.dll"]
