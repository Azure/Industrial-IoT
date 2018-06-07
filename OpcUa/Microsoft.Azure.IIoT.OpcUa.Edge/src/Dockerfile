FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge/src/Microsoft.Azure.IIoT.OpcUa.Edge.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge/src/
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Services.Edge/src/Microsoft.Azure.IIoT.OpcUa.Services.Edge.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Services.Edge/src/
COPY Common/Microsoft.Azure.IIoT.Edge/src/Microsoft.Azure.IIoT.Edge.csproj Common/Microsoft.Azure.IIoT.Edge/src/
COPY Common/Microsoft.Azure.IIoT/src/Microsoft.Azure.IIoT.csproj Common/Microsoft.Azure.IIoT/src/
COPY Common/Microsoft.Azure.IIoT.Net/src/Microsoft.Azure.IIoT.Net.csproj Common/Microsoft.Azure.IIoT.Net/src/
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Services/src/Microsoft.Azure.IIoT.OpcUa.Services.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Services/src/
COPY Common/Microsoft.Azure.IIoT.Hub/src/Microsoft.Azure.IIoT.Hub.csproj Common/Microsoft.Azure.IIoT.Hub/src/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503 OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge/src/Microsoft.Azure.IIoT.OpcUa.Edge.csproj
COPY . .
WORKDIR /src/OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge/src
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Edge.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Edge.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Edge.dll"]
