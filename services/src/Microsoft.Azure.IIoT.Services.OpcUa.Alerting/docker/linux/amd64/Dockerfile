FROM mcr.microsoft.com/dotnet/core/runtime:2.2 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
ENV PROJECT_ROOT=services

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting/src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting.csproj src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting/src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting/src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting.csproj

FROM build AS publish
COPY ${PROJECT_ROOT}/src ./src
WORKDIR /app/project/src/Microsoft.Azure.IIoT.Services.OpcUa.Alerting/src
RUN dotnet publish "Microsoft.Azure.IIoT.Services.OpcUa.Alerting.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.OpcUa.Alerting.dll"]

