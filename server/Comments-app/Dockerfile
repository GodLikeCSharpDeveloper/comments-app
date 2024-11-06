# Стадия сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Comments-app/CommentApp.csproj", "Comments-app/"]
RUN dotnet restore "./Comments-app/CommentApp.csproj"
COPY . .
WORKDIR "/src/Comments-app"
RUN dotnet build "./CommentApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Стадия публикации
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CommentApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Финальная стадия
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Экспонируем только HTTP-порт
EXPOSE 8080

# Устанавливаем переменную окружения для HTTP
ENV ASPNETCORE_URLS="http://*:8080"


# Копируем опубликованное приложение
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "CommentApp.dll"]
