FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Comments-app/Comments-app.csproj", "Comments-app/"]
RUN dotnet restore "./Comments-app/Comments-app.csproj"
COPY . .
WORKDIR "/src/Comments-app"
RUN dotnet build "./Comments-app.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этап публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Comments-app.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальный образ с SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Локальная установка dotnet-ef
WORKDIR /app
RUN dotnet new tool-manifest
RUN dotnet tool install dotnet-ef
RUN dotnet tool restore
RUN dotnet tool run ef --help

# Копируем скрипт ожидания базы данных
COPY Comments-app/wait-for-db.sh .
RUN chmod +x wait-for-db.sh

# Устанавливаем рабочие порты
EXPOSE 8080
EXPOSE 8081

# ENTRYPOINT для применения миграций и запуска приложения
ENTRYPOINT ["./wait-for-db.sh"]
