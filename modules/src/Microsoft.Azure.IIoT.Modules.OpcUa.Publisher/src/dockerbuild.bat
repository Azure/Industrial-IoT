REM build a docker container of the console reference server
dotnet build Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj
dotnet publish Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.csproj -o ./publish
docker build -f .\Dockerfile.standalone -t opcpublisher .
