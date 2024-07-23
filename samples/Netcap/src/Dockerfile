ARG BRANCH=main
ARG VERSION
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
RUN DEBIAN_FRONTEND=noninteractive apt update -y && apt install -y libpcap-dev bash net-tools
USER root:root
ENV BRANCH=${BRANCH}
ENV VERSION=${VERSION}
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Netcap.csproj", "."]
RUN dotnet restore "./Netcap.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Netcap.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Netcap.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Netcap.dll"]