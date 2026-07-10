# UA-.NETStandard 2.0 stack migration — work in progress

This repository is mid-way through migrating OPC Publisher 3.0 from the published
`OPCFoundation.NetStandard.Opc.Ua.*` 1.5.x NuGet packages to the **UA-.NETStandard
2.0** stack vendored as the git submodule at `external/UA-.NETStandard`
(v2.0.64-preview).

The migration is **staged**. Each stage lands as a durable, whole-solution-green
commit. To keep every commit green, projects that have not yet been migrated are
**temporarily removed from `Industrial-IoT.slnx`** and re-added + migrated in a
later stage. Nothing is deleted from disk — only the solution membership changes.

## Stage 1 (this commit) — `Azure.IIoT.OpcUa` (Core) on 2.0

**Done:**

- The four `OPCFoundation.NetStandard.Opc.Ua.*` PackageReferences (`Core`,
  `Client`, `Client.ComplexTypes`, `Server`) are replaced by ProjectReferences
  into the `external/UA-.NETStandard` submodule; the OPCFoundation group in
  `Directory.Packages.props` is emptied and `Apache.Avro` is removed.
- The 6 forked codecs (`JsonEncoderEx`, `JsonDecoderEx`, `BaseAvroEncoder`,
  `BaseAvroDecoder`, `AvroEncoder`, `AvroDecoder`) and **all Avro** (source,
  schemas, PubSub Avro message types, `AvroFileWriter`, and every Avro test) are
  deleted. Avro will be re-added upstream in the 2.0 stack later and consumed then.
- The value API (`JsonVariantEncoder`, `EncodeableEx.AsJson`) is rewired onto the
  2.0 stack's own `Opc.Ua.JsonEncoder`/`Opc.Ua.JsonDecoder` using
  `JsonEncoderOptions.Compact` (reversible) / `.Verbose` (non-reversible).
- The **UADP** PubSub encoders are fully rewired onto the 2.0
  `BinaryEncoder`/`BinaryDecoder`.
- The **JSON** PubSub encoders (`JsonNetworkMessage`, `JsonDataSetMessage`,
  `MonitoredItemMessage`, `JsonMetadataMessage`, and the Avro/JSON dispatch in
  `PubSubMessage`/`EncodeableDictionary.Decode`) are reduced to minimal compiling
  stubs that throw `NotSupportedException` and are tagged `TODO(Phase 5)`. They
  will be replaced by the 2.0 stack `Opc.Ua.PubSub` implementation in Phase 5.
- Struct-redesign API deltas across the encoders are adopted as-is (2.0 made many
  OPC UA types — `NodeId`, `ExpandedNodeId`, `LocalizedText`, `QualifiedName`,
  `DataValue`, `TypeInfo`, `ExtensionObject`, `ArrayOf<T>`, `DateTimeUtc`,
  `ByteString` — value types / immutable).

**In scope this stage (build green in `Industrial-IoT.slnx`):**

- `src/Azure.IIoT.OpcUa/src` (Core of the migration)
- `src/Azure.IIoT.OpcUa.Core/src` + `tests`
- `src/Azure.IIoT.OpcUa.Publisher.Models/src` + `tests`
- `src/Azure.IIoT.OpcUa.Publisher.Sdk/src`

## Stage 2 (in progress) — `Azure.IIoT.OpcUa.Publisher` (+ Module.src) on 2.0

Stage 2 migrates the Publisher engine, Module host, and SDK onto the 2.0 stack.
The **architectural** 2.0 API deltas have landed on disk (see below), but the
**body-level value-type ripple** (2.0 turned `NodeId`, `ExpandedNodeId`,
`QualifiedName`, `LocalizedText`, `DataValue`, `StatusCode`, `ByteString` into
readonly value types, and replaced every generated `XxxCollection` with the
`ArrayOf<T>` readonly struct) is **not yet fully resolved** — `Azure.IIoT.OpcUa.Publisher`
still has a large residual error count. Because it does not yet build green,
`Azure.IIoT.OpcUa.Publisher/src` and `Azure.IIoT.OpcUa.Publisher.Module/src`
remain **descoped** from `Industrial-IoT.slnx` so every commit stays green.
The `Azure.IIoT.OpcUa.Publisher.Sdk` continues to build green (it depends on
Models, not the Publisher engine) and stays in scope.

**Landed on disk this stage (architectural deltas — reusable, resumable):**

- `Stack/OpcUaCollectionCompat.cs` (new) — a compat shim declaring the removed
  `XxxCollection : List<Xxx>` types in `namespace Opc.Ua` (exploiting the
  `List<T>` → `ArrayOf<T>` implicit conversion for the request direction) plus a
  `NodeIdCompat.IsNull(...)` helper (the static `NodeId.IsNull` was removed in
  favour of the instance `.IsNull` property).
- `Stack/LruNodeCacheCompat.cs` (new) — a Publisher-local `ILruNodeCache` /
  `LruNodeCache` wrapper over the 2.0 `INodeCache` / `NodeCache`, re-exposing the
  old surface (`IsTypeOf`, `GetNodeAsync`, `GetSuperTypeAsync`,
  `GetBuiltInTypeAsync`, `GetReferencesAsync`, `Clear`, `Inner`). Isolates the
  node-cache churn (the 2.0 `ILruNodeCache` was removed) to one file.
- `Stack/CertificateValidationCompat.cs` (new) — a
  `CertificateValidationEventHandler` / `CertificateValidationEventArgs` compat
  pair bridging the removed `CertificateValidator` event model onto the 2.0
  `ICertificateManager.AcceptError` hook.
- `Stack/Runtime/FlatCertificateStore.cs` — migrated the 7 members to the 2.0
  async `ICertificateStore` (`Certificate` / `CertificateCollection` in
  `Opc.Ua.Security.Certificates`).
- `Stack/Services/OpcUaApplication.cs` — `Validate` event rewired onto the 2.0
  `ICertificateManager.AcceptError` hook.
- `Stack/Services/OpcUaClient.cs` / `OpcUaSession.cs` — `Create` / `CreateAsync`
  overrides + session ctor migrated to the 2.0 `SessionFactory` signatures
  (`Certificate?` / `CertificateCollection?` / `ArrayOf<EndpointDescription>` /
  `ArrayOf<string>` / `IUserIdentity?` / non-nullable `ReverseConnectManager`).
  Classic sessions kept (no `ManagedSession` — that is Phase 4b).
- `Stack/Extensions/ServiceResponseEx.cs` — added `ArrayOf<T>` overloads to the
  `Validate` extension (response `.Results` / `.DiagnosticInfos` are now
  `ArrayOf<T>`, which is not `IEnumerable<T>`).
- Bulk mechanical: `NodeId.IsNull(x)` → `NodeIdCompat.IsNull(x)` (57 files);
  `.Item1?` → `.Item1` on now-non-null `DataValue` tuples in `SessionEx`.

**Remaining for Stage 2 completion (body-wave, context-sensitive — must be hand-fixed):**

- Value-type `?.` removal on `DataValue` / `NodeId` / `QualifiedName` /
  `LocalizedText` (CS0023, ~100).
- `StatusCode` ↔ `uint` (`switch` on `StatusCode`, `(uint)` casts) (CS9135/CS0266, ~160).
- `ArrayOf<T>` mutation sites (`.Add` / `.Insert` / `.RemoveAt` on
  `RelativePath.Elements`, `EventFilter.SelectClauses`) → build a `List<T>` and assign (CS1061).
- `ByteString` explicit casts; `DataValue` readonly-property construction via ctor;
  `null` → `NodeId.Null` / `QualifiedName.Null`; `ExpandedNodeId.ToNodeId(NamespaceTable)`;
  `PropertyState<T>` now abstract (NodeServices node construction, CS0144 ~62).
- The hidden `ComplexTypeSystem` wave (~62 uses) still behind the current errors.
- `Module.src` (controllers / Startup / runtime) + any remaining `Sdk` deltas.

## Temporarily descoped from `Industrial-IoT.slnx` (to be migrated in later stages)

| Project | Reason | Target stage |
| --- | --- | --- |
| `src/Azure.IIoT.OpcUa.Publisher/src` | Architectural 2.0 deltas landed (certificates/node-cache/session-factory/collection shims — see Stage 2 section); residual body-wave value-type ripple (`ArrayOf<T>`, struct `NodeId`/`DataValue`/`StatusCode`, `PropertyState<T>` abstract, `ComplexTypeSystem`) still to hand-fix | Stage 2 (Publisher) |
| `src/Azure.IIoT.OpcUa.Publisher/tests` | depends on Publisher.src | Stage 2 |
| `src/Azure.IIoT.OpcUa.Publisher.Module/src` | depends on Publisher.src; ASP.NET host wiring not yet adapted | Stage 2 |
| `src/Azure.IIoT.OpcUa.Publisher.Module/tests` | depends on Module.src | Stage 2 |
| `src/Azure.IIoT.OpcUa.Publisher.Module/cli` | depends on Module.src | Stage 2 |
| `src/Azure.IIoT.OpcUa.Publisher.Testing/src` (Servers) | ~388 errors against the 2.0 `Opc.Ua.Server` NodeManager API (node-states, `INodeManager3`, clone/read/write) | Stage 3 (Testing.Servers / Phase 9A) |
| `src/Azure.IIoT.OpcUa.Publisher.Testing/tests` | depends on Testing.Servers | Stage 3 |
| `src/Azure.IIoT.OpcUa.Publisher.Testing/cli` | depends on Testing.Servers | Stage 3 |
| `src/Azure.IIoT.OpcUa/tests` | `EncodeableDictionaryTests` / `JsonDataSetTests` reference the deleted forked codecs; JSON PubSub tests exercise the `TODO(Phase 5)` stubs; expected values need rewiring to the 2.0 codec output | Stage 2/5 (test rewiring) |

## Constraints (unchanged across stages)

- Never modify the `external/UA-.NETStandard` submodule.
- Do not adopt `ManagedSession` (that is Phase 4b, a later pass) — keep the classic
  `Session`/`Subscription`/`MonitoredItem` APIs.
- Accept the 2.0 stack codec output as-is (behavioral compat bar; do not re-fork to
  match the old byte-for-byte output).
