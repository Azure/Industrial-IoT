FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.2-stretch-slim-arm32v7 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch-arm32v7 AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj
COPY . .
WORKDIR /src/src
RUN dotnet add package ILLink.Tasks -v 0.1.5-preview-1841731 -s https://dotnet.myget.org/F/dotnet-core/api/v3/index.json
RUN dotnet build Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj -c Release -o /app -r linux-arm /p:ShowLinkerSizeComparison=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Modules.OpcUa.Twin.dll"]
