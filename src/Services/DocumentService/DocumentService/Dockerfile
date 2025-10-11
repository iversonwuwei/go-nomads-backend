FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 50004

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy Shared project first
COPY ["src/Shared/Shared/Shared.csproj", "Shared/Shared/"]

# Copy project files
COPY ["src/Services/DocumentService/DocumentService/DocumentService.csproj", "Services/DocumentService/DocumentService/"]
RUN dotnet restore "Services/DocumentService/DocumentService/DocumentService.csproj"

# Copy source files
COPY src/Shared/Shared/ Shared/Shared/
COPY src/Services/DocumentService/DocumentService/ Services/DocumentService/DocumentService/

# Build
WORKDIR "/src/Services/DocumentService/DocumentService"
RUN dotnet build "DocumentService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DocumentService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentService.dll"]
