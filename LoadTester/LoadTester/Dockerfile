# ���������� .NET runtime ��� ���������� ����������
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

# ���������� .NET SDK ��� ������ ����������
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# �������� ������ csproj ���� � ��������������� �����������
COPY ["LoadTester/LoadTester.csproj", "LoadTester/"]
RUN dotnet restore "./LoadTester/LoadTester.csproj"

# �������� ��� ����� ������� � �������� ����������
COPY . .
WORKDIR "/src/LoadTester"
RUN dotnet build "./LoadTester.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ��������� ����������
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LoadTester.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ��������� ����� ��� �������
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ��������� ������� ������� ����������
ENTRYPOINT ["dotnet", "LoadTester.dll"]
