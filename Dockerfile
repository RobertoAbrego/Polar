# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copiar todo el proyecto
COPY . .

# Restaurar dependencias
RUN dotnet restore

# Publicar aplicación
RUN dotnet publish -c Release -o /app

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiar app publicada
COPY --from=build /app .

# =========================
# DB2 CLI DRIVER (clidriver)
# =========================

# Copia el driver IBM DB2 al contenedor
COPY clidriver /opt/ibm/clidriver

# Variables de entorno necesarias para ODBC DB2
ENV DB2DIR=/opt/ibm/clidriver
ENV PATH=$PATH:/opt/ibm/clidriver/bin
ENV LD_LIBRARY_PATH=/opt/ibm/clidriver/lib

# =========================
# CONFIGURACIÓN ASP.NET CORE
# =========================

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Polar.dll"]