FROM mcr.microsoft.com/dotnet/core/runtime:2.2-alpine AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
ENV PROJECT_ROOT=modules/opc-publisher

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
# ADD .git .git

WORKDIR /app/${PROJECT_ROOT}
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/opcpublisher/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj opcpublisher/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 opcpublisher/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj

COPY ${PROJECT_ROOT}/opcpublisher ./opcpublisher
WORKDIR /app/${PROJECT_ROOT}/opcpublisher
RUN dotnet publish "Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build /out .
WORKDIR /appdata
ENTRYPOINT ["dotnet", "/app/opcpublisher.dll"]
