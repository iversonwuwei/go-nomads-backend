FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5213
ENV ASPNETCORE_URLS=http://+:5213

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/ConfigService/ConfigService/ConfigService.csproj", "src/Services/ConfigService/ConfigService/"]
COPY ["src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults.csproj", "src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/ConfigService/ConfigService/ConfigService.csproj"
COPY . .
WORKDIR "/src/src/Services/ConfigService/ConfigService"
RUN dotnet build "ConfigService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConfigService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConfigService.dll"]
