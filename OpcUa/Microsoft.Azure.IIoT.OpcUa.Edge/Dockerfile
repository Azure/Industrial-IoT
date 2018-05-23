FROM microsoft/dotnet:2.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.0-sdk AS build
WORKDIR /src
COPY azure-iiot-twin.sln ./
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge/Microsoft.Azure.IIoT.OpcUa.Edge.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge/
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Services.Edge/Microsoft.Azure.IIoT.OpcUa.Services.Edge.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Services.Edge/
COPY OpcUa/Microsoft.Azure.IIoT.OpcUa.Services/Microsoft.Azure.IIoT.OpcUa.Services.csproj OpcUa/Microsoft.Azure.IIoT.OpcUa.Services/
COPY Common/Microsoft.Azure.IIoT/Microsoft.Azure.IIoT.csproj Common/Microsoft.Azure.IIoT/
COPY Common/Microsoft.Azure.IIoT.Edge/Microsoft.Azure.IIoT.Edge.csproj Common/Microsoft.Azure.IIoT.Edge/
COPY Common/Microsoft.Azure.IIoT.Net/Microsoft.Azure.IIoT.Net.csproj Common/Microsoft.Azure.IIoT.Net/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/OpcUa/Microsoft.Azure.IIoT.OpcUa.Edge
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Edge.dll"]
