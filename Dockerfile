FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Modules.Twin.csproj src/
COPY cli/Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli.csproj cli/
COPY NuGet.Config NuGet.Config
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 cli/Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli.csproj
COPY . .
WORKDIR /src/cli
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Modules.Twin.Cli.dll", "--host"]
