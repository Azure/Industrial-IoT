FROM microsoft/dotnet:2.2-runtime-stretch-slim-arm32v7 AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj
COPY . .
WORKDIR /src/src
RUN dotnet build Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Modules.OpcUa.Twin.dll"]
