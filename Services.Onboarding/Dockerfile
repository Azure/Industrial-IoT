FROM microsoft/dotnet:runtime AS base
WORKDIR /app

FROM microsoft/dotnet:latest AS build
WORKDIR /src
COPY iot-opc-twin-service.sln ./
COPY Services.Onboarding/Services.Onboarding.csproj Services.Onboarding/
COPY Services/Services.csproj Services/
COPY Services.Common/Services.Common.csproj Services.Common/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/Services.Onboarding
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IoTSolutions.OpcTwin.Services.Onboarding.dll"]
