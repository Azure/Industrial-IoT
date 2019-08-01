FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine AS build
WORKDIR /src
COPY opcpublisher/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj
COPY . .
WORKDIR /src/src
RUN dotnet publish Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj -c Release -o /app -r linux-arm

FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.2-bionic-arm32v7 AS base
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./opcpublisher"]
