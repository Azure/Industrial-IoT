FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Api/cli/Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Api/cli/
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Api/src/Microsoft.Azure.IIoT.OpcUa.Api.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Api/src/
COPY Common/Microsoft.Azure.IIoT/src/Microsoft.Azure.IIoT.csproj Common/Microsoft.Azure.IIoT/src/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503 OpcUa/Microsoft.Azure.IIoT.OpcUa.Api/cli/Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj
COPY . .
WORKDIR /src/OpcUa/Microsoft.Azure.IIoT.OpcUa.Api/cli
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OpcTwinCtrl.dll"]
