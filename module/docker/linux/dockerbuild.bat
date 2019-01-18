dotnet build ..\..\Microsoft.Azure.IIoT.OpcUa.Modules.Vault.csproj
dotnet publish ..\..\Microsoft.Azure.IIoT.OpcUa.Modules.Vault.csproj -o ./docker/linux/publish
docker build -t edgeopcvault .

