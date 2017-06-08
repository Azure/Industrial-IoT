FROM microsoft/dotnet:1.1-sdk

COPY / /build

WORKDIR /build
RUN dotnet restore

WORKDIR /build/src/GatewayApp.NetCore
RUN dotnet publish -c Release -f netcoreapp1.1 -r debian.8-x64 -o bin/Debug/netcoreapp1.1

WORKDIR /build/src/GatewayApp.NetCore/bin/Debug/netcoreapp1.1
ENV LD_LIBRARY_PATH=/build/src/GatewayApp.NetCore/bin/Debug/netcoreapp1.1
ENTRYPOINT ["./GatewayApp.NetCore"]
