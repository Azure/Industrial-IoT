#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj", "modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src/"]
RUN dotnet restore "modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj"
COPY . .
WORKDIR "/src/modules/src/Microsoft.Azure.IIoT.Modules.OpcUa.Publisher/src"
RUN dotnet build "Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "opcpublisher.dll"]