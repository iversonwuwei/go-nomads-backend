FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/SearchService/SearchService/SearchService.csproj", "src/Services/SearchService/SearchService/"]
COPY ["src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults.csproj", "src/GoNomads.ServiceDefaults/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/SearchService/SearchService/SearchService.csproj"
COPY . .
WORKDIR "/src/src/Services/SearchService/SearchService"
RUN dotnet build "SearchService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SearchService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SearchService.dll"]
