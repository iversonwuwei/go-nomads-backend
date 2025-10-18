# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/Gateway/Gateway/Gateway.csproj", "src/Gateway/Gateway/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]

# Restore dependencies
RUN dotnet restore "src/Gateway/Gateway/Gateway.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/Gateway/Gateway"
RUN dotnet build "Gateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 50003

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]
