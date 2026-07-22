# ==========================
# Build Stage
# ==========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY backend/ .

WORKDIR /src

RUN dotnet restore BlueprintOS.sln

RUN dotnet publish src/BlueprintOS.Api/BlueprintOS.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ==========================
# Runtime Stage
# ==========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BlueprintOS.Api.dll"]