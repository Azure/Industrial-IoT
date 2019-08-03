FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.2-bionic-arm32v7 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
ENV PROJECT=Microsoft.Azure.IIoT.Modules.OpcUa.Twin
ENV PROJECT_ROOT=modules/opc-twin

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/${PROJECT}.csproj src/${PROJECT}.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/${PROJECT}.csproj

FROM build AS publish
COPY ${PROJECT_ROOT}/src ./src
WORKDIR /app/project/src
RUN dotnet publish "${PROJECT}.csproj" -c Release -o /out -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["./${PROJECT}"]
