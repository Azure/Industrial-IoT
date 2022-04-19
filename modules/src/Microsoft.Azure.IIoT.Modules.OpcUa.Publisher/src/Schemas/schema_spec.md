# Published Nodes Validation - Design documentation

There are two default schema availabe: one stric and one more backward compatible. The currently enabled, 
backward compatible version does not perform value validation checks for OPC Node Ids and allows both 
the `Id` and `ExpandedNodeId` properties in the most recent subschema. In the future and perhaps in lockstep
with the next 'major' release, the strict schema (which performs Node Id validation) should be re-enabled.

## Value Validation Approaches

The design approaches for the regular expressions included in the schema require explanation. 

### Regex Design for Endpoint URLs

In the generated schema, the Endpoint URLs are format checked against the Json-Schema URL
type as well as a regex pattern that enforces a `opc.tcp` prefix, e.g. `opc.tcp://{well formed url}`.
The regular expression is as follows: `opc.tcp://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*\\(\\),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+`.

### Regex Design for Node ID

There are three (3) styles of NodeID address formats, each paired with four (4) distinct types
of NodeID.

The NodeID address formats are as follows:

- `{nodeId type}={nodeId}`
- `nsu={url/urn resource path}/;{nodeId type}={nodeId}`
- `ns={namespace index based on OPC UA IM}/;{nodeId type}={nodeId}`

The `nsu={url/urn resource path}/;{nodeId type}={nodeId}` format is the encouraged option as
`{nodeId type}={nodeId}` and `ns={namespace index based on OPC UA IM}/;{nodeId type}={nodeId}`
can cause collisions where nodeIds are the same across namespace or potentially collect the
wrong data if the namespace array index position changes during an information model rebuild.

To maintain backwards compatibility, this schema supports all three addressing formats.

The NodeId types allowable by the OPC UA Information Model are as follows:

- `b` - ByteStrings - (RegEx matches base64 encoded strings)
- `g` - GUID based NodeIds - (RegEx matches GUIDs)
- `i` - unsigned integer based NodeIds - (RegEx matches integers)
- `s` - string based NodeIds -  (RegEx matches UTF-8 character set)

Each of these NodeId types has a discrete regular expression to ensure a well formed document.
Note that the `string` type NodeId regular expression includes all UTF-8 characters, only
excluding a few common control characters; it is likely that this check is NOT comprehensive
and edge cases should be submitted as issues against this repository.

Example Generated NodeID Validation Regex is as follows. You can customize the generator tool
(https://github.com/WilliamBerryiii/opcpublisherschemavalidator)
to produce domain/implementation specific regular expressions as you see fit.

```text
(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(i=(\\d+)$))|(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(s=([\\x00-\\x7F]|([\\xC2-\\xDF]|\\xE0[\\xA0-\\xBF]|\\xED[\\x80-\\x9F]|(|[\\xE1-\\xEC]|[\\xEE-\\xEF]|\\xF0[\\x90-\\xBF]|\\xF4[\\x80-\\x8F]|[\\xF1-\\xF3][\\x80-\\xBF])[\\x80-\\xBF])[\\x80-\\xBF])+$))|(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(g=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$))|(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(b=(?:[A-Za-z\\d+/]{4})*(?:[A-Za-z\\d+/]{3}=|[A-Za-z\\d+/]{2}==)$))|(^ns=(\\d+);(i=(\\d+)$))|(^ns=(\\d+);(s=([\\x00-\\x7F]|([\\xC2-\\xDF]|\\xE0[\\xA0-\\xBF]|\\xED[\\x80-\\x9F]|(|[\\xE1-\\xEC]|[\\xEE-\\xEF]|\\xF0[\\x90-\\xBF]|\\xF4[\\x80-\\x8F]|[\\xF1-\\xF3][\\x80-\\xBF])[\\x80-\\xBF])[\\x80-\\xBF])+$))|(^ns=(\\d+);(g=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$))|(^ns=(\\d+);(b=(?:[A-Za-z\\d+/]{4})*(?:[A-Za-z\\d+/]{3}=|[A-Za-z\\d+/]{2}==)$))|(^i=(\\d+)$)|(^s=([\\x00-\\x7F]|([\\xC2-\\xDF]|\\xE0[\\xA0-\\xBF]|\\xED[\\x80-\\x9F]|(|[\\xE1-\\xEC]|[\\xEE-\\xEF]|\\xF0[\\x90-\\xBF]|\\xF4[\\x80-\\x8F]|[\\xF1-\\xF3][\\x80-\\xBF])[\\x80-\\xBF])[\\x80-\\xBF])+$)|(^g=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$)|(^b=(?:[A-Za-z\\d+/]{4})*(?:[A-Za-z\\d+/]{3}=|[A-Za-z\\d+/]{2}==)$)
```

NodeId Regex Components

```text
(^i=(\\d+)$)|
(^s=([\\x00-\\x7F]|([\\xC2-\\xDF]|\\xE0[\\xA0-\\xBF]|\\xED[\\x80-\\x9F]|(|[\\xE1-\\xEC]|[\\xEE-\\xEF]|\\xF0[\\x90-\\xBF]|\\xF4[\\x80-\\x8F]|[\\xF1-\\xF3][\\x80-\\xBF])[\\x80-\\xBF])[\\x80-\\xBF])+$)|
(^g=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$)|
(^b=(?:[A-Za-z\\d+/]{4})*(?:[A-Za-z\\d+/]{3}=|[A-Za-z\\d+/]{2}==)$)
```

Expanded Node Id Regex Components

```text
(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(i=(\\d+)$))|
(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(s=([\\x00-\\x7F]|([\\xC2-\\xDF]|\\xE0[\\xA0-\\xBF]|\\xED[\\x80-\\x9F]|(|[\\xE1-\\xEC]|[\\xEE-\\xEF]|\\xF0[\\x90-\\xBF]|\\xF4[\\x80-\\x8F]|[\\xF1-\\xF3][\\x80-\\xBF])[\\x80-\\xBF])[\\x80-\\xBF])+$))|
(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(g=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$))|
(^nsu=http[s]?://(?:[a-zA-Z]|[0-9]|[$-_@.&+]|[!*(),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+/;(b=(?:[A-Za-z\\d+/]{4})*(?:[A-Za-z\\d+/]{3}=|[A-Za-z\\d+/]{2}==)$))|
```

Namespace Index Regex Components

```text
(^ns=(\\d+);(i=(\\d+)$))|
(^ns=(\\d+);(s=([\\x00-\\x7F]|([\\xC2-\\xDF]|\\xE0[\\xA0-\\xBF]|\\xED[\\x80-\\x9F]|(|[\\xE1-\\xEC]|[\\xEE-\\xEF]|\\xF0[\\x90-\\xBF]|\\xF4[\\x80-\\x8F]|[\\xF1-\\xF3][\\x80-\\xBF])[\\x80-\\xBF])[\\x80-\\xBF])+$))|
(^ns=(\\d+);(g=([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$))|
(^ns=(\\d+);(b=(?:[A-Za-z\\d+/]{4})*(?:[A-Za-z\\d+/]{3}=|[A-Za-z\\d+/]{2}==)$))|
```
