FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
ENV PROJECT_ROOT=services
EXPOSE 9040
EXPOSE 51111

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
WORKDIR /app/project
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj

FROM build AS publish
COPY ${PROJECT_ROOT}/src ./src
WORKDIR /app/project/src/Microsoft.Azure.IIoT.Services.OpcUa.Gateway/src
RUN dotnet publish "Microsoft.Azure.IIoT.Services.OpcUa.Gateway.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=publish /out .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.OpcUa.Gateway.dll"]

