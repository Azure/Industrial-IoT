FROM microsoft/dotnet:2.0.0-runtime

COPY ./publish /publish
WORKDIR /publish

ENTRYPOINT ["dotnet", "Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.Edge.dll"]
