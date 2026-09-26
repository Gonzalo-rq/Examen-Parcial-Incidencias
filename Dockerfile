# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["IncidenciasApp.csproj", "./"]
RUN dotnet restore "IncidenciasApp.csproj"

COPY . .
RUN dotnet publish "IncidenciasApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Crear directorios con permisos para SQLite
RUN mkdir -p /data && chmod -R 777 /data

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/data/app.db;Cache=Shared"

ENTRYPOINT ["sh", "-c", "exec dotnet IncidenciasApp.dll --urls http://0.0.0.0:${PORT:-8080}"]
