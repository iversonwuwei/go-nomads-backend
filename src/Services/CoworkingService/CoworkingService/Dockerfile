FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8006

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/CoworkingService/CoworkingService/CoworkingService.csproj", "src/Services/CoworkingService/CoworkingService/"]
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
