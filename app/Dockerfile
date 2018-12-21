FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 56310
EXPOSE 44342

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.csproj Vault.App/
RUN dotnet restore Vault.App/Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.csproj
COPY . .
WORKDIR /src/Vault.App
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Services.Gds.Vault.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.Gds.Vault.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.dll"]
