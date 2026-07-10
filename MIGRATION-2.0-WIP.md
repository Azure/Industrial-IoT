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

## Temporarily descoped from `Industrial-IoT.slnx` (to be migrated in later stages)

| Project | Reason | Target stage |
| --- | --- | --- |
| `src/Azure.IIoT.OpcUa.Publisher/src` | `ComplexTypeSystem`, `INodeCache` (was `ILruNodeCache`), `CertificateValidator` (UA0021) + async `ICertificateStore`, session/subscription/monitored-item signature deltas not yet adapted | Stage 2 (Publisher) |
| `src/Azure.IIoT.OpcUa.Publisher/tests` | depends on Publisher.src | Stage 2 |
| `src/Azure.IIoT.OpcUa.Publisher.Module/src` | depends on Publisher.src; ASP.NET host wiring | Stage 2 |
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
