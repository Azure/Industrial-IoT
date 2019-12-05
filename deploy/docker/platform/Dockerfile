FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
ENV NO_GIT=1
WORKDIR /src
COPY . .
RUN dotnet restore "services/src/Microsoft.Azure.IIoT.Services.All/src/Microsoft.Azure.IIoT.Services.All.csproj"
WORKDIR "/src/services/src/Microsoft.Azure.IIoT.Services.All/src"
RUN dotnet build "Microsoft.Azure.IIoT.Services.All.csproj" -c Release -o /app/build

FROM build AS publish
ENV NO_GIT=1
RUN dotnet publish "Microsoft.Azure.IIoT.Services.All.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.Services.All.dll"]