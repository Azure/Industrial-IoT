FROM microsoft/dotnet:2.2-runtime-nanoserver-1809 AS base
FROM microsoft/dotnet:2.2-sdk-nanoserver-1809 AS build
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
COPY ["opcpublisher/${PROJECT}.csproj", "opcpublisher/"]
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 "opcpublisher/${PROJECT}.csproj"

FROM build AS publish
COPY ${PROJECT_ROOT}/src ./src
WORKDIR "/app/project/opcpublisher"
RUN dotnet publish "${PROJECT}.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
WORKDIR /appdata
ENTRYPOINT ["dotnet", "/app/opcpublisher.dll"]

