FROM microsoft/dotnet:2.2-runtime AS base
FROM microsoft/dotnet:2.2-sdk AS build
ENV PROJECT=Microsoft.Azure.IIoT.OpcUa.Api
ENV PROJECT_ROOT=api
ENV _HOST=host.docker.internal

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/${PROJECT}/cli/${PROJECT}.Cli.csproj src/${PROJECT}/cli/${PROJECT}.Cli.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/${PROJECT}/cli/${PROJECT}.Cli.csproj

FROM build AS publish
COPY ${PROJECT_ROOT}/src ./src
WORKDIR /app/project/src/${PROJECT}/cli
RUN dotnet publish "${PROJECT}.Cli.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["dotnet", "${PROJECT}.Cli.dll"]

