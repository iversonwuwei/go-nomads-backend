FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8011

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/InnovationService/InnovationService/InnovationService.csproj", "src/Services/InnovationService/InnovationService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/InnovationService/InnovationService/InnovationService.csproj"
COPY . .
WORKDIR "/src/src/Services/InnovationService/InnovationService"
RUN dotnet build "InnovationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InnovationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InnovationService.dll"]
