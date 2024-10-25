# Используем .NET runtime для финального контейнера
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# Используем .NET SDK для сборки приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем только csproj файл и восстанавливаем зависимости
COPY ["LoadTester/LoadTester.csproj", "LoadTester/"]
RUN dotnet restore "./LoadTester/LoadTester.csproj"

# Копируем все файлы проекта и собираем приложение
COPY . .
WORKDIR "/src/LoadTester"
RUN dotnet build "./LoadTester.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикуем приложение
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LoadTester.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ для запуска
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Указываем команду запуска приложения
ENTRYPOINT ["dotnet", "LoadTester.dll"]
