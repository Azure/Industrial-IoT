# Industrial IoT Platform C# Reference

This page describes how to get the C# API reference of all C# projects in the repository (including tests).

## Prerequisites 

Install [docfx](https://dotnet.github.io/docfx/) for example by using [chocolatey](https://chocolatey.org/).

## Build the API Reference

To build the API reference open a PowerShell in the root directory of repository and run
```powershell
 docfx ".\api\csharp-api\docfx.json" --build
```

The build results are stored under `api/csharp-api/_site`

## Run API Reference as local webpage

To run the API reference as local webpage, open PowerShell in the root directory of repository and run
```powershell
 docfx ".\api\csharp-api\docfx.json" --serve
```

