# OPC Twin service

## Supported OPC UA Services

The OPC Twin cloud service exposes a [REST API](../api/twin/readme.md) to call the following OPC UA services on activated endpoints in an OPC Twin edge module:

- **Read** and **Write** a “Value” on a Variable node
  Write is also exposed as Desired/Reported Property on the endpoint identity
- **Call** a “Method Node”
- **Read** and **Write** Node “Attributes
- **History Read** and **Update** service calls to interact with Historians
- **Batching** of any of the above calls.
- **Browse** first / next (with and without reading the browsed target nodes)
- Get **meta data** of methods (to display input arguments to a user)
- **Publish/Unpublish** variable and event (see OPC Publisher integration)

## OPC UA JSON

The OPC Twin service REST API uses OPC UA JSON reversible encoding as per standard defined in OPC UA specification 1.04, Part 6, with the following exceptions or enhancements:

1. `null` values are not encoded.  A missing value implies `null`.

2. In addition to the standard string encoding using a namespace index (e.g. `ns=4;i=3`) or the expanded format (e.g. `nsu=http://opcfoundation.org/UA/;i=3523`) the API supports and encourages the use of URI encoded Node Ids (see [RFC 3986](http://tools.ietf.org/html/rfc3986)).

   ```bash
   <namespace-uri>#<id-type>=<URL-encoded-id-value>
   ```

   Examples are: `http://opcfoundation.org/UA/#i=3523` or `http://opcfoundation.org/UA/#s=tag1`. 

   The API itself does not support specifying JSON Object encoded Node Id, however, values can contain them.  All responses include node identifiers in the Uri format.

   It is recommended to always use the URI form of Node Id since it is independent of the server and session's namespace table state.

3. Expanded Node Identifiers can also be encoded using the OPC UA defined syntax (e.g. `src=opc.tcp://test;nsu=http://opcfoundation.org/UA/;i=3523`).  However, all requests accept and responses return a URI encoded Expanded Node Id, which differs from the regular Node Id URI format only if a server URI is specified.  In this case the server URI is appended as 

   ```bash
   <namespace-uri>&srv=<URL-encoded-server-uri>#<id-type>=<URL-encoded-id-value>
   ```

4. All *primitive built-in* values (`integer`, `string`, `int32`, `double`, etc.) and *Arrays* of them can be passed as JSON encoded Variant objects (as per standard) or as JSON Token.  The twin module attempts to coerce the JSON Token in the payload to the expected built-in type of the Variable or Input argument.

5. The decoder will match JSON variable names case-**in**sensitively.  This means you can write a JSON object property name as `"tyPeiD": ""`, `"typeid": ""`, or `"TYPEID": ""` and all are decoded into a OPC UA structure's `"TypeId"` member.

6. Qualified Names are encoded as a single string the same way as Node Ids, where the name is the ID element of the URI, URL encoded, e.g. `http://opcfoundation.org/UA/#Browse%20Name`

While not always enforced, ensure you **URL encode** the id value or name of Qualified Names, Node Ids and Expanded Node Ids.

## Next steps

- [Learn more about the OPC Twin module](module.md)
- [Learn more about the overall Architecture](architecture.md)
- [Explore the OPC Twin REST API](../api/twin/readme.md)
