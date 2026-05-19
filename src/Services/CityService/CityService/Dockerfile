FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5202
ENV ASPNETCORE_URLS=http://+:5202

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/CityService/CityService/CityService.csproj", "src/Services/CityService/CityService/"]
COPY ["src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults.csproj", "src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults/"]
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
