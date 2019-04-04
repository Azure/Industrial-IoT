FROM microsoft/dotnet:2.1-aspnetcore-runtime-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 9043

FROM microsoft/dotnet:2.1-sdk-nanoserver-1809 AS build
WORKDIR /src
COPY src/Microsoft.Azure.IIoT.Services.OpcUa.History/src/Microsoft.Azure.IIoT.Services.OpcUa.History.csproj src/Microsoft.Azure.IIoT.Services.OpcUa.History/src/
COPY NuGet.Config NuGet.Config
COPY *.props /
RUN dotnet restore --configfile NuGet.Config -nowarn:msb3202,nu1503 src/Microsoft.Azure.IIoT.Services.OpcUa.History/src/Microsoft.Azure.IIoT.Services.OpcUa.History.csproj
COPY . .
WORKDIR /src/src/Microsoft.Azure.IIoT.Services.OpcUa.History/src
RUN dotnet build Microsoft.Azure.IIoT.Services.OpcUa.History.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Microsoft.Azure.IIoT.Services.OpcUa.History.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.OpcUa.History.dll"]
