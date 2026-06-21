# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source
COPY . .
RUN dotnet restore src/EcoMonitor.Web/EcoMonitor.Web.csproj
RUN dotnet publish src/EcoMonitor.Web/EcoMonitor.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5108
EXPOSE 5108
ENTRYPOINT ["dotnet", "EcoMonitor.Web.dll"]
