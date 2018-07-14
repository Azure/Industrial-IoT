FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa/cli/Microsoft.Azure.IIoT.OpcUa.Cli.csproj src/Microsoft.Azure.IIoT.OpcUa/cli/
COPY src/Microsoft.Azure.IIoT.OpcUa/src/Microsoft.Azure.IIoT.OpcUa.csproj src/Microsoft.Azure.IIoT.OpcUa/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Edge/src/Microsoft.Azure.IIoT.OpcUa.Edge.csproj src/Microsoft.Azure.IIoT.OpcUa.Edge/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Twin/src/Microsoft.Azure.IIoT.OpcUa.Twin.csproj src/Microsoft.Azure.IIoT.OpcUa.Twin/src/
COPY packages packages
COPY NuGet.Config NuGet.Config
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.OpcUa/cli/Microsoft.Azure.IIoT.OpcUa.Cli.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.OpcUa/cli
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OpcTwinCtrlInternal.dll"]
