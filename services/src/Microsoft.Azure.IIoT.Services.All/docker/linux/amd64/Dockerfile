FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
ENV PROJECT_ROOT=services
EXPOSE 9040
EXPOSE 9041
EXPOSE 9042
EXPOSE 9043
EXPOSE 9044
EXPOSE 51111

ARG BUILD_SOURCEVERSION
ARG RELEASE_BUILD
ENV PROJECT_BUILD=1
WORKDIR /app
COPY *.props /
# ADD .git .git

WORKDIR /app/${PROJECT_ROOT}
COPY NuGet.Config NuGet.Config
COPY ${PROJECT_ROOT}/*.props /
COPY ${PROJECT_ROOT}/src ./src
ENV BUILD_SOURCEVERSION=${BUILD_SOURCEVERSION}
ENV RELEASE_BUILD=${RELEASE_BUILD}
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Services.All/src/Microsoft.Azure.IIoT.Services.All.csproj

WORKDIR /app/${PROJECT_ROOT}/src/Microsoft.Azure.IIoT.Services.All/src
RUN dotnet publish "Microsoft.Azure.IIoT.Services.All.csproj" -c Release -o /out

FROM base AS final
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.All.dll"]

