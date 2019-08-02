FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
ARG BUILD_SOURCEVERSION
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
WORKDIR /src
COPY opcpublisher/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj opcpublisher/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 opcpublisher/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj
COPY . .
WORKDIR /src/opcpublisher
RUN dotnet publish Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj -c Release -o /app -r linux-arm

FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.2-bionic-arm32v7 AS base
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./opcpublisher"]
