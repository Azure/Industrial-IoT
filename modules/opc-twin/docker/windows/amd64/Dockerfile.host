FROM microsoft/dotnet:2.2-runtime-nanoserver-1809 AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk-nanoserver-1809 AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj src/
COPY cli/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli.csproj cli/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 cli/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli.csproj
COPY . .
WORKDIR /src/cli
RUN dotnet build Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Cli.dll", "--host"]
