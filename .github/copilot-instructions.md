# Copilot Instructions for Azure Industrial-IoT

## Build and Test

The repo uses `.slnx` solution files (not legacy `.sln`).

```shell
# Restore (CI uses public nuget.org; local dev uses the Azure Artifacts feed in Nuget.Config)
dotnet restore -s https://api.nuget.org/v3/index.json

# Build — .NET analyzers (Roslyn + Roslynator) run during build, no separate lint step
dotnet build --no-restore

# Tests MUST run serially — OPC UA servers contend on ports and PKI directories
dotnet test src/Azure.IIoT.OpcUa.Publisher.Models/tests/Azure.IIoT.OpcUa.Publisher.Models.Tests.csproj --no-build --verbosity normal --blame-hang-timeout 10m --blame-hang-dump-type none
dotnet test src/Azure.IIoT.OpcUa/tests/Azure.IIoT.OpcUa.Tests.csproj --no-build --verbosity normal --blame-hang-timeout 10m --blame-hang-dump-type none
dotnet test src/Azure.IIoT.OpcUa.Publisher/tests/Azure.IIoT.OpcUa.Publisher.Tests.csproj --no-build --verbosity normal --blame-hang-timeout 10m --blame-hang-dump-type none
dotnet test src/Azure.IIoT.OpcUa.Publisher.Module/tests/Azure.IIoT.OpcUa.Publisher.Module.Tests.csproj --no-build --verbosity normal --blame-hang-timeout 10m --blame-hang-dump-type none

# Run a single test by name
dotnet test src/Azure.IIoT.OpcUa.Publisher/tests/Azure.IIoT.OpcUa.Publisher.Tests.csproj --no-build --blame-hang-timeout 10m --filter "FullyQualifiedName~YourTestName"
```

## Architecture

OPC Publisher is an Azure IoT Edge module that connects to OPC UA servers, subscribes to data changes, and publishes telemetry to Azure IoT Hub (or MQTT/HTTP endpoints). All projects target **.NET 10** (`net10.0`), C# 13.0.

### Project dependency graph

```
Azure.IIoT.OpcUa                          ← Core OPC UA types, encoders, PubSub message formats
  └─ Azure.IIoT.OpcUa.Publisher           ← Publisher engine: subscriptions, storage, discovery, configuration
       └─ Azure.IIoT.OpcUa.Publisher.Module ← ASP.NET Core host (IoT Edge module), REST API, startup/DI
Azure.IIoT.OpcUa.Publisher.Models          ← Shared API/SDK models (standalone, no OPC UA dependency)
Azure.IIoT.OpcUa.Publisher.Sdk            ← Client SDK for the Publisher REST API
Azure.IIoT.OpcUa.Publisher.Testing         ← Test OPC UA servers and fixtures
```

### Key layers

- **Encoders** (`Azure.IIoT.OpcUa/src/Encoders/`): Custom JSON/binary encoders for OPC UA types and PubSub network messages (JSON, UADP)
- **Stack** (`Azure.IIoT.OpcUa.Publisher/src/Stack/`): OPC UA client connection management, session handling, monitored items
- **Services** (`Azure.IIoT.OpcUa.Publisher/src/Services/`): Core publisher business logic — subscription management, asset/device integration
- **Storage** (`Azure.IIoT.OpcUa.Publisher/src/Storage/`): Published nodes configuration file handling
- **Runtime** (`Azure.IIoT.OpcUa.Publisher.Module/src/Runtime/`): Configuration binding and Autofac DI registration
- **Startup** (`Azure.IIoT.OpcUa.Publisher.Module/src/Startup.cs`): ASP.NET Core host with Autofac container, JSON + MessagePack + OpenAPI

## Conventions

### Dependency injection

The project uses **Autofac** (not the built-in Microsoft DI container). Services are registered using `ContainerBuilder` extension methods in files named `*Ex.cs` (e.g., `ContainerBuilderEx.cs`). Register new services with `.AsImplementedInterfaces()` and follow nearby lifetime conventions (`SingleInstance`, `InstancePerLifetimeScope`, etc.).

### Naming (enforced by `.editorconfig`)

- **Fields**: `_camelCase` prefix for all instance fields
- **Private/protected constants and static readonly**: `kPascalCase` prefix
- **Public constants**: `PascalCase`
- **Async methods**: must end in `Async`
- **Block-scoped namespaces** are required (file-scoped namespaces produce warnings)
- **`using` directives** go inside the namespace block

### Models

API models are generally `record class` types annotated with `[DataContract]` and `[DataMember(Name = "...", Order = N)]` for serialization. Use `sealed record class` where existing similar models do. Use the `required` keyword and `[Required]` attribute for mandatory properties.

### Code patterns

- Prefer `var` for local variable declarations
- Source-generated logging via `[LoggerMessage]` attribute on partial methods
- Source-generated regex via `[GeneratedRegex]`
- OPC UA type conversions are static extension methods (e.g., `NodeIdEx`, `ExpandedNodeIdEx`)
- Configuration uses `IOptions<T>` pattern (e.g., `PublisherConfig`, `PublisherOptions`)

### Testing

- **xUnit** with **Moq** for mocking (`new Mock<T>()` pattern)
- Test projects mirror source projects (e.g., `Azure.IIoT.OpcUa.Publisher` → `Azure.IIoT.OpcUa.Publisher.Tests`)
- Shared test base classes provide fixtures and utilities (e.g., `TempFileProviderBase`, `OpcUaMonitoredItemTestsBase`)
- **Tests must run serially** across projects — do not use `dotnet test` on the solution file

### Versioning

Uses **Nerdbank.GitVersioning** (`version.json`). Do not manually set assembly or package versions — they are computed from git height.
