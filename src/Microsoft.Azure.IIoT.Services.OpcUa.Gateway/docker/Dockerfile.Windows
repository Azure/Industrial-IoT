FROM microsoft/dotnet:2.1-runtime-nanoserver-1809 AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk-nanoserver-1809 AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src
RUN dotnet build Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.OpcUa.Gateway.dll"]
