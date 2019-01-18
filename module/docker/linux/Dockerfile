FROM microsoft/dotnet:2.2-runtime

COPY ./publish /publish
WORKDIR /publish

ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Modules.Vault.dll"]
