FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8007

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/TravelPlanningService/TravelPlanningService/TravelPlanningService.csproj", "src/Services/TravelPlanningService/TravelPlanningService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/TravelPlanningService/TravelPlanningService/TravelPlanningService.csproj"
COPY . .
WORKDIR "/src/src/Services/TravelPlanningService/TravelPlanningService"
RUN dotnet build "TravelPlanningService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TravelPlanningService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelPlanningService.dll"]
