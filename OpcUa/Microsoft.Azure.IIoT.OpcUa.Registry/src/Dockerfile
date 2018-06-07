FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Registry/src/Microsoft.Azure.IIoT.OpcUa.Registry.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Registry/src/
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Services/src/Microsoft.Azure.IIoT.OpcUa.Services.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Services/src/
COPY Common/Microsoft.Azure.IIoT/src/Microsoft.Azure.IIoT.csproj Common/Microsoft.Azure.IIoT/src/
COPY Common/Microsoft.Azure.IIoT.Hub/src/Microsoft.Azure.IIoT.Hub.csproj Common/Microsoft.Azure.IIoT.Hub/src/
COPY Common/Microsoft.Azure.IIoT.Web/src/Microsoft.Azure.IIoT.Web.csproj Common/Microsoft.Azure.IIoT.Web/src/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503 OpcUa/Microsoft.Azure.IIoT.OpcUa.Registry/src/Microsoft.Azure.IIoT.OpcUa.Registry.csproj
COPY . .
WORKDIR /src/OpcUa/Microsoft.Azure.IIoT.OpcUa.Registry/src
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Registry.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Registry.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Registry.dll"]
