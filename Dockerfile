FROM microsoft/dotnet:1.0-sdk-projectjson

COPY / /build

WORKDIR /build
RUN dotnet restore

WORKDIR /build/src/GatewayApp.NetCore
RUN dotnet publish

WORKDIR /build/src/GatewayApp.NetCore/bin/Debug/netcoreapp1.0/publish
ENV LD_LIBRARY_PATH=/build/src/GatewayApp.NetCore/bin/Debug/netcoreapp1.0/publish
ENTRYPOINT ["dotnet", "GatewayApp.NetCore.dll"]
