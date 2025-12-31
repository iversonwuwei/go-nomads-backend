FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8004

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/AccommodationService/AccommodationService/AccommodationService.csproj", "src/Services/AccommodationService/AccommodationService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/AccommodationService/AccommodationService/AccommodationService.csproj"
COPY . .
WORKDIR "/src/src/Services/AccommodationService/AccommodationService"
RUN dotnet build "AccommodationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AccommodationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccommodationService.dll"]
