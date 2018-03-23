FROM microsoft/aspnetcore:latest AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:latest AS build
WORKDIR /src
COPY iot-opc-twin-service.sln ./
COPY WebService/WebService.csproj WebService/
COPY Services/Services.csproj Services/
COPY Services.Common/Services.Common.csproj Services.Common/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/WebService
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IoTSolutions.OpcTwin.WebService.dll"]
