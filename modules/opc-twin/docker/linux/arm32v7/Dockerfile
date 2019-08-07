FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.2-bionic-arm32v7 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
ENV PROJECT_ROOT=modules/opc-twin

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
# ADD .git .git

WORKDIR /app/${PROJECT_ROOT}
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj

COPY ${PROJECT_ROOT}/src ./src
WORKDIR /app/${PROJECT_ROOT}/src
RUN dotnet publish "Microsoft.Azure.IIoT.Modules.OpcUa.Twin.csproj" -c Release -o /out -r linux-arm

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["./Microsoft.Azure.IIoT.Modules.OpcUa.Twin"]
