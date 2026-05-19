FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5203
ENV ASPNETCORE_URLS=http://+:5203

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src
COPY NuGet.Config .
COPY ["src/Services/CoworkingService/CoworkingService/CoworkingService.csproj", "src/Services/CoworkingService/CoworkingService/"]
COPY ["src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults.csproj", "src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/CoworkingService/CoworkingService/CoworkingService.csproj"
COPY . .
WORKDIR "/src/src/Services/CoworkingService/CoworkingService"
RUN dotnet build "CoworkingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoworkingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoworkingService.dll"]
