# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# Copy the project files
COPY ["Server/.", "Server/"]
COPY ["TopDownVoxelsEngineUnity/Assets/Shared/.", "TopDownVoxelsEngineUnity/Assets/Shared/"]
COPY ["TopDownVoxelsEngineUnity/Assets/StreamingAssets/.", "TopDownVoxelsEngineUnity/Assets/StreamingAssets/"]
COPY ["TopDownVoxelsEngineUnity/Assets/Plugins/Sirenix/Assemblies/Sirenix.OdinInspector.Attributes.dll", "TopDownVoxelsEngineUnity/Assets/Plugins/Sirenix/Assemblies/"]

# Restore dependencies
RUN dotnet restore "Server/Server.csproj"

# Build the application
RUN dotnet build "Server/Server.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Server/Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS final
WORKDIR /app

# Copy the published output
COPY --from=publish /app/publish .

# Create non-root user for security
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "Server.dll"]