FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

RUN apt-get update && apt-get install -y \
    libstdc++6 \
    libgcc-s1 \
    libc6 \
    libxml2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

COPY clidriver /opt/ibm/clidriver

RUN chmod -R 755 /opt/ibm/clidriver && \
    ln -sf /opt/ibm/clidriver/lib/libdb2.so /usr/lib/libdb2.so && \
    ldconfig

ENV DB2DIR=/opt/ibm/clidriver
ENV PATH=$PATH:/opt/ibm/clidriver/bin
ENV LD_LIBRARY_PATH=/opt/ibm/clidriver/lib:/usr/lib:/lib

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Polar.dll"]