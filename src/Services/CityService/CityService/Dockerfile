FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/CityService/CityService/CityService.csproj", "src/Services/CityService/CityService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/CityService/CityService/CityService.csproj"
COPY . .
WORKDIR "/src/src/Services/CityService/CityService"
RUN dotnet build "CityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CityService.dll"]
