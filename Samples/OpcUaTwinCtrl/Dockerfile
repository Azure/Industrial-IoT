FROM microsoft/dotnet:runtime AS base
WORKDIR /app

FROM microsoft/dotnet:latest AS build
WORKDIR /src
COPY iot-opc-twin-service.sln ./
COPY Samples/OpcUaTwinCtrl/OpcUaTwinCtrl.csproj Samples/OpcUaTwinCtrl/
COPY WebService.Client/WebService.Client.csproj WebService.Client/
COPY Services.Common/Services.Common.csproj Services.Common/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Samples/OpcUaTwinCtrl
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OpcUaTwinCtrl.dll"]
