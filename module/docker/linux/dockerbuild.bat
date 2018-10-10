dotnet build Microsoft.Azure.IIoT.OpcUa.Modules.Vault.csproj
dotnet publish Microsoft.Azure.IIoT.OpcUa.Modules.Vault.csproj -o ./publish
docker build -t edgegds .
