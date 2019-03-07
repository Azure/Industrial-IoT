FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 9042

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.Services.OpcUa.Registry/src/Microsoft.Azure.IIoT.Services.OpcUa.Registry.csproj src/Microsoft.Azure.IIoT.Services.OpcUa.Registry/src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Services.OpcUa.Registry/src/Microsoft.Azure.IIoT.Services.OpcUa.Registry.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.Services.OpcUa.Registry/src
RUN dotnet build Microsoft.Azure.IIoT.Services.OpcUa.Registry.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.Services.OpcUa.Registry.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.OpcUa.Registry.dll"]
