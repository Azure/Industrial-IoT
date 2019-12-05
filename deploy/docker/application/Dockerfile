FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
ENV NO_GIT=1
WORKDIR /src
COPY . .
RUN dotnet restore "samples/src/Microsoft.Azure.IIoT.App/src/Microsoft.Azure.IIoT.App.csproj"
WORKDIR "/src/samples/src/Microsoft.Azure.IIoT.App/src"
RUN dotnet build "Microsoft.Azure.IIoT.App.csproj" -c Release -o /app/build

FROM build AS publish
ENV NO_GIT=1
RUN dotnet publish "Microsoft.Azure.IIoT.App.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.App.dll"]