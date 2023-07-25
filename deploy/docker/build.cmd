@echo off
setlocal

set BASEIMAGE=/p:ContainerBaseImage=mcr.microsoft.com/dotnet/aspnet:7.0-alpine-amd64
set CMDLINE=-r linux-musl-x64 --self-contained false /t:PublishContainer
set PROJECT=../../src/Azure.IIoT.OpcUa.Publisher.Module/src/Azure.IIoT.OpcUa.Publisher.Module.csproj

dotnet publish %PROJECT% -c Release %CMDLINE% %BASEIMAGE% /p:ContainerImageTag=latest
dotnet publish %PROJECT% -c Debug %CMDLINE% %BASEIMAGE% /p:ContainerImageTag=debug

set DOCKER_REGISTRY=""
set OPC_PUBLISHER_TAG=latest