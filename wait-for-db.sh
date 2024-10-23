#!/bin/sh

# Ожидание доступности базы данных
echo "Waiting for the database to be ready..."
until dotnet ef database update; do
  >&2 echo "Database is unavailable - sleeping"
  sleep 5
done

# Запуск приложения после успешного применения миграций
echo "Database is up - starting the application"
exec dotnet Comments-app.dll
