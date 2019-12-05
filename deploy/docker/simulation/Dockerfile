FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
ENV NO_GIT=1
WORKDIR /src
COPY . .
RUN dotnet restore "components/opc-ua/src/Microsoft.Azure.IIoT.OpcUa.Testing/cli/Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj"
WORKDIR "/src/components/opc-ua/src/Microsoft.Azure.IIoT.OpcUa.Testing/cli"
RUN dotnet build "Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj" -c Release -o /app/build

FROM build AS publish
ENV NO_GIT=1
RUN dotnet publish "Microsoft.Azure.IIoT.OpcUa.Testing.Cli.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Testing.Cli.dll"]