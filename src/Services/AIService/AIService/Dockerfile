# 使用 .NET 9.0 运行时作为基础镜像
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8009

# 使用 .NET 9.0 SDK 镜像进行构建
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制 .csproj 文件并还原依赖
COPY ["src/Services/AIService/AIService/AIService.csproj", "src/Services/AIService/AIService/"]
COPY ["src/Shared/Shared/Shared.csproj", "src/Shared/Shared/"]

RUN dotnet restore "src/Services/AIService/AIService/AIService.csproj"

# 复制所有源代码
COPY . .
WORKDIR "/src/src/Services/AIService/AIService"

# 构建应用程序
RUN dotnet build "AIService.csproj" -c Release -o /app/build

# 发布应用程序
FROM build AS publish
RUN dotnet publish "AIService.csproj" -c Release -o /app/publish

# 最终运行时镜像
FROM base AS final
WORKDIR /app

# 创建日志目录
RUN mkdir -p /app/logs

# 复制发布的应用程序
COPY --from=publish /app/publish .

# 设置环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8009

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8009/health || exit 1

# 启动应用程序
ENTRYPOINT ["dotnet", "AIService.dll"]