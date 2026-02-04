FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8010

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/CacheService/CacheService/CacheService.csproj", "src/Services/CacheService/CacheService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/CacheService/CacheService/CacheService.csproj"
COPY . .
WORKDIR "/src/src/Services/CacheService/CacheService"
RUN dotnet build "CacheService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CacheService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CacheService.dll"]
