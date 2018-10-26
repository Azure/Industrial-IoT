FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Services.Gateway/src/Microsoft.Azure.IIoT.OpcUa.Services.Gateway.csproj src/
COPY NuGet.Config NuGet.Config
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.OpcUa.Services.Gateway/src/Microsoft.Azure.IIoT.OpcUa.Services.Gateway.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.OpcUa.Services.Gateway/src
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Services.Gateway.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.Gateway.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.Gateway.dll"]
