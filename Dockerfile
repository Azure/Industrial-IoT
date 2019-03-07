FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Testing/cli/Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj src/Microsoft.Azure.IIoT.OpcUa.Testing/cli/
COPY src/Microsoft.Azure.IIoT.OpcUa.Testing/src/Microsoft.Azure.IIoT.OpcUa.Testing.csproj src/Microsoft.Azure.IIoT.OpcUa.Testing/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Edge/src/Microsoft.Azure.IIoT.OpcUa.Edge.csproj src/Microsoft.Azure.IIoT.OpcUa.Edge/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Graph/src/Microsoft.Azure.IIoT.OpcUa.Graph.csproj src/Microsoft.Azure.IIoT.OpcUa.Graph/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Twin/src/Microsoft.Azure.IIoT.OpcUa.Twin.csproj src/Microsoft.Azure.IIoT.OpcUa.Twin/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Registry/src/Microsoft.Azure.IIoT.OpcUa.Registry.csproj src/Microsoft.Azure.IIoT.OpcUa.Registry/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Protocol/src/Microsoft.Azure.IIoT.OpcUa.Protocol.csproj src/Microsoft.Azure.IIoT.OpcUa.Protocol/src/
COPY src/Microsoft.Azure.IIoT.OpcUa/src/Microsoft.Azure.IIoT.OpcUa.csproj src/Microsoft.Azure.IIoT.OpcUa/src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.OpcUa.Testing/cli/Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.OpcUa.Testing/cli
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Testing.Cli.dll"]
