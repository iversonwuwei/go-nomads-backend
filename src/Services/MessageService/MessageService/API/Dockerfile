# 构建阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY NuGet.Config .

# 复制项目文件（保持目录结构）
COPY ["src/Services/MessageService/MessageService/API/MessageService.csproj", "src/Services/MessageService/MessageService/API/"]
COPY ["src/Services/MessageService/MessageService/Application/MessageService.Application.csproj", "src/Services/MessageService/MessageService/Application/"]
COPY ["src/Services/MessageService/MessageService/Infrastructure/MessageService.Infrastructure.csproj", "src/Services/MessageService/MessageService/Infrastructure/"]
COPY ["src/Services/MessageService/MessageService/Domain/MessageService.Domain.csproj", "src/Services/MessageService/MessageService/Domain/"]
COPY ["src/GoNomads.ServiceDefaults/GoNomads.ServiceDefaults.csproj", "src/GoNomads.ServiceDefaults/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]

# 还原依赖
RUN dotnet restore "src/Services/MessageService/MessageService/API/MessageService.csproj"

# 复制所有源代码
COPY . .

# 构建应用
WORKDIR "/src/src/Services/MessageService/MessageService/API"
RUN dotnet build "MessageService.csproj" -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish "MessageService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行时阶段
FROM base AS final
WORKDIR /app

# 安装 curl（用于健康检查）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessageService.dll"]
