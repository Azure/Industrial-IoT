@echo off
setlocal

set CMDLINE=--self-contained false /t:PublishContainer -r linux-x64
set PROJECT=../../src/Azure.IIoT.OpcUa.Publisher.Module/src/Azure.IIoT.OpcUa.Publisher.Module.csproj

dotnet restore %PROJECT% -s https://api.nuget.org/v3/index.json
dotnet publish %PROJECT% -c Release %CMDLINE% /p:ContainerImageTag=latest
dotnet publish %PROJECT% -c Debug %CMDLINE% /p:ContainerImageTag=debug

k3d image import iotedge/opc-publisher:latest -c k3s-default
kubectl apply -f connector-template.yaml
