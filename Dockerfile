FROM microsoft/dotnet:1.1-sdk

COPY /src /build

WORKDIR /build
RUN dotnet restore
RUN dotnet publish -c Release -o out

WORKDIR /build/out
ENTRYPOINT ["dotnet", "Opc.Ua.Publisher.dll"]
