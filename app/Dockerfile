FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 56310
EXPOSE 44342

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY opc-gds-app/Microsoft.Azure.IIoT.OpcUa.Services.Gds.App.csproj Gds.App/
RUN dotnet restore Gds.App/Microsoft.Azure.IIoT.OpcUa.Services.Gds.App.csproj
COPY . .
WORKDIR /src/Gds.App
RUN dotnet build Microsoft.Azure.IIoT.OpcUa.Services.Gds.App.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.Gds.App.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.Gds.App.dll"]
