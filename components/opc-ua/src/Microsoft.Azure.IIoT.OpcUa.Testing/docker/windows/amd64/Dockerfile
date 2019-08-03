ENV PROJECT=Microsoft.Azure.IIoT.OpcUa.Testing.Cli
ENV PROJECT_ROOT=components/opc-ua
FROM microsoft/dotnet:2.2-runtime-nanoserver-1809 AS base
FROM microsoft/dotnet:2.2-sdk-nanoserver-1809 AS build

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/build
COPY ${PROJECT_ROOT}/*.props /
COPY ["${PROJECT_ROOT}/src/**/*.csproj", "src/"]
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/${PROJECT}/src/${PROJECT}.csproj

FROM build AS publish
COPY ["${PROJECT_ROOT}/src", "."]
WORKDIR "/app/build/src/${PROJECT}/src"
RUN dotnet publish "${PROJECT}.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["dotnet", "${PROJECT}.dll"]

