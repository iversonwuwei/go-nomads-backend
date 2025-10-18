# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/Services/ProductService/ProductService/ProductService.csproj", "src/Services/ProductService/ProductService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]

# Restore dependencies
RUN dotnet restore "src/Services/ProductService/ProductService/ProductService.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/Services/ProductService/ProductService"
RUN dotnet build "ProductService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 50001

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
