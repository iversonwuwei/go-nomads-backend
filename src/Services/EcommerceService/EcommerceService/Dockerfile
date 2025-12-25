FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8008

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/EcommerceService/EcommerceService/EcommerceService.csproj", "src/Services/EcommerceService/EcommerceService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]
RUN dotnet restore "src/Services/EcommerceService/EcommerceService/EcommerceService.csproj"
COPY . .
WORKDIR "/src/src/Services/EcommerceService/EcommerceService"
RUN dotnet build "EcommerceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EcommerceService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EcommerceService.dll"]
