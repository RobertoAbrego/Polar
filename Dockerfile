FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

RUN apt-get update && apt-get install -y \
    unixodbc \
    unixodbc-dev \
    libstdc++6 \
    libgcc-s1 \
    libc6 \
    libxml2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

COPY clidriver /opt/ibm/clidriver
RUN chmod -R 755 /opt/ibm/clidriver
RUN echo "[IBM DB2 ODBC DRIVER]" > /opt/ibm/clidriver/odbcinst.ini && \
    echo "Description=IBM DB2 ODBC Driver" >> /opt/ibm/clidriver/odbcinst.ini && \
    echo "Driver=/opt/ibm/clidriver/lib/libdb2o.so" >> /opt/ibm/clidriver/odbcinst.ini && \
    ln -sf /opt/ibm/clidriver/lib/libdb2.so /opt/ibm/clidriver/lib/libdb2o.so

ENV DB2DIR=/opt/ibm/clidriver
ENV PATH=$PATH:/opt/ibm/clidriver/bin
ENV LD_LIBRARY_PATH=/opt/ibm/clidriver/lib:/usr/lib
ENV ODBCSYSINI=/opt/ibm/clidriver
ENV ODBCINI=/opt/ibm/clidriver/odbc.ini

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Polar.dll"]