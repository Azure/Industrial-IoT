FROM microsoft/dotnet:2.2-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Api/cli/Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj src/Microsoft.Azure.IIoT.OpcUa.Api/cli/
COPY src/Microsoft.Azure.IIoT.OpcUa.Api/src/Microsoft.Azure.IIoT.OpcUa.Api.csproj src/Microsoft.Azure.IIoT.OpcUa.Api/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Api.Registry/src/Microsoft.Azure.IIoT.OpcUa.Api.Registry.csproj src/Microsoft.Azure.IIoT.OpcUa.Api.Registry/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Api.Twin/src/Microsoft.Azure.IIoT.OpcUa.Api.Twin.csproj src/Microsoft.Azure.IIoT.OpcUa.Api.Twin/src/
COPY src/Microsoft.Azure.IIoT.OpcUa.Api.History/src/Microsoft.Azure.IIoT.OpcUa.Api.History.csproj src/Microsoft.Azure.IIoT.OpcUa.Api.History/src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config src/Microsoft.Azure.IIoT.OpcUa.Api/cli/Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.OpcUa.Api/cli
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Api.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV _HOST=host.docker.internal
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Api.Cli.dll"]
