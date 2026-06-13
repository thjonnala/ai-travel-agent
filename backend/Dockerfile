# Multi-stage build for the .NET 10 Web API, deployed as a Docker service on Render.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against the project files first for better layer caching.
COPY backend/src/TravelAgent.Domain/TravelAgent.Domain.csproj backend/src/TravelAgent.Domain/
COPY backend/src/TravelAgent.Application/TravelAgent.Application.csproj backend/src/TravelAgent.Application/
COPY backend/src/TravelAgent.Infrastructure/TravelAgent.Infrastructure.csproj backend/src/TravelAgent.Infrastructure/
COPY backend/src/TravelAgent.Api/TravelAgent.Api.csproj backend/src/TravelAgent.Api/
RUN dotnet restore backend/src/TravelAgent.Api/TravelAgent.Api.csproj

# Copy the rest of the sources and publish.
COPY backend/ backend/
RUN dotnet publish backend/src/TravelAgent.Api/TravelAgent.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Render injects PORT; the app reads it (see Program.cs). EXPOSE is informational.
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["dotnet", "TravelAgent.Api.dll"]
