FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.2-bionic-arm32v7 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
ENV PROJECT=Microsoft.Azure.IIoT.Modules.OpcUa.Publisher
ENV PROJECT_ROOT=modules/opc-publisher

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/opcpublisher/${PROJECT}.csproj opcpublisher/${PROJECT}.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 opcpublisher/${PROJECT}.csproj

FROM build AS publish
COPY ${PROJECT_ROOT}/opcpublisher ./opcpublisher
WORKDIR /app/project/opcpublisher
RUN dotnet publish "${PROJECT}.csproj" -c Release -o /out -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
WORKDIR /appdata
ENTRYPOINT ["./app/opcpublisher"]
