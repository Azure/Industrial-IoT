FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 56310
EXPOSE 44342

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY app/Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.csproj app/
COPY api-csharp/Microsoft.Azure.IIoT.OpcUa.Api.Vault.csproj api-csharp/
RUN dotnet restore app/Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.csproj
COPY app app/
COPY api-csharp api-csharp/
WORKDIR /src/app
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.Vault.App.dll"]

