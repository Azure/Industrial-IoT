dotnet build Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Edge.csproj
dotnet publish Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Edge.csproj -o ./publish
docker build -t edgegds .
