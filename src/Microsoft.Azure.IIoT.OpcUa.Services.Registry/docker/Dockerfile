FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 9042

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Services.Registry/src/Microsoft.Azure.IIoT.OpcUa.Services.Registry.csproj src/
COPY NuGet.Config NuGet.Config
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.OpcUa.Services.Registry/src/Microsoft.Azure.IIoT.OpcUa.Services.Registry.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.OpcUa.Services.Registry/src
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Services.Registry.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.Registry.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.Registry.dll"]
