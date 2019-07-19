ARG runtime_base_tag=2.1-runtime-stretch-slim
ARG build_base_tag=2.1-sdk-stretch

FROM microsoft/dotnet:${build_base_tag} AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY opcpublisher/*.csproj ./opcpublisher/
WORKDIR /app/opcpublisher
RUN dotnet restore

# copy and publish app
WORKDIR /app
COPY opcpublisher/. ./opcpublisher/
WORKDIR /app/opcpublisher
RUN dotnet publish -c Debug -o out

# start it up
FROM microsoft/dotnet:${runtime_base_tag} AS runtime

RUN apt-get update && \
    apt-get install -y --no-install-recommends unzip curl procps && \
    rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

WORKDIR /app
COPY --from=build /app/opcpublisher/out ./
WORKDIR /appdata
ENTRYPOINT ["dotnet", "/app/opcpublisher.dll"]