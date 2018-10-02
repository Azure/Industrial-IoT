FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.OpcUa.Services.Vault.csproj src/
COPY common/Microsoft.Azure.IIoT.OpcUa.Services.Vault.Common.csproj common/
COPY .nuget .nuget
RUN dotnet restore --configfile .nuget/NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.OpcUa.Services.Vault.csproj
COPY . .
WORKDIR /src/src
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Services.Vault.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.Vault.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.dll"]
