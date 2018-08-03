FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Api/cli/Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj src/Microsoft.Azure.IIoT.OpcUa.Api/cli/
COPY src/Microsoft.Azure.IIoT.OpcUa.Api/src/Microsoft.Azure.IIoT.OpcUa.Api.csproj src/Microsoft.Azure.IIoT.OpcUa.Api/src/
COPY NuGet.Config NuGet.Config
RUN dotnet restore --configfile NuGet.Config src/Microsoft.Azure.IIoT.OpcUa.Api/cli/Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.OpcUa.Api/cli
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OpcTwinCtrl.dll"]
