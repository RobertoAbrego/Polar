# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# 🔥 Instalar TODAS las dependencias necesarias
RUN apt-get update && apt-get install -y \
    unixodbc \
    unixodbc-dev \
    libstdc++6 \
    libgcc-s1 \
    libc6 \
    libxml2 \
    && rm -rf /var/lib/apt/lists/*

# Copiar app publicada
COPY --from=build /app .

# =========================
# DB2 CLI DRIVER
# =========================
COPY clidriver /opt/ibm/clidriver

ENV DB2DIR=/opt/ibm/clidriver
ENV PATH=$PATH:/opt/ibm/clidriver/bin
ENV LD_LIBRARY_PATH=/opt/ibm/clidriver/lib

# Opcional pero recomendado
ENV ODBCSYSINI=/opt/ibm/clidriver
ENV ODBCINI=/opt/ibm/clidriver/odbc.ini

# =========================
# ASP.NET
# =========================
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Polar.dll"]