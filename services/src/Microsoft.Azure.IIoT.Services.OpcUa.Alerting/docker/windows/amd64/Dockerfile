FROM microsoft/dotnet:2.2-runtime-nanoserver-1809 AS base
FROM microsoft/dotnet:2.2-sdk-nanoserver-1809 AS build
ARG PROJECT=Microsoft.Azure.IIoT.Services.OpcUa.Alerting
ARG PROJECT_ROOT=services

ARG BUILD_SOURCEVERSION
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/${PROJECT}/src/${PROJECT}.csproj src/${PROJECT}/src/${PROJECT}.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 "src/${PROJECT}/src/${PROJECT}.csproj"

FROM build AS publish
COPY ${PROJECT_ROOT}/src "."
WORKDIR /app/project/src/${PROJECT}/src
RUN dotnet publish "${PROJECT}.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["dotnet", ${PROJECT}".dll"]

