ENV PROJECT=Microsoft.Azure.IIoT.Services.OpcUa.Twin
FROM microsoft/dotnet:2.2-aspnetcore-runtime-nanoserver-1809 AS base
FROM microsoft/dotnet:2.2-sdk-nanoserver-1809 AS build
ENV PROJECT_ROOT=services
EXPOSE 9041

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/${PROJECT}/src/${PROJECT}.csproj src/${PROJECT}/src/${PROJECT}.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/${PROJECT}/src/${PROJECT}.csproj

FROM build AS publish
COPY ${PROJECT_ROOT}/src ./src
WORKDIR /app/project/src/${PROJECT}/src
RUN dotnet publish "${PROJECT}.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["dotnet", "${PROJECT}.dll"]

