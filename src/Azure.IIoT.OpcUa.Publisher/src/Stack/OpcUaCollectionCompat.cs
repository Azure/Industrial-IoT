// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System.Collections.Generic;

    // TODO(Phase 4b/5): In UA-.NETStandard 2.0 NodeId / ExpandedNodeId became
    // readonly value types and the static NodeId.IsNull(x) helper was removed in
    // favour of the instance x.IsNull property. This shim restores the classic
    // static call shape used pervasively by the Publisher stack layer.
    /// <summary> Compat null-check helpers for the value-type node ids. </summary>
    public static class NodeIdCompat
    {
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(NodeId nodeId) => nodeId.IsNull;
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(NodeId? nodeId) => nodeId is null || nodeId.Value.IsNull;
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(ExpandedNodeId nodeId) => nodeId.IsNull;
        /// <summary> True if the node id is null. </summary>
        public static bool IsNull(ExpandedNodeId? nodeId) => nodeId is null || nodeId.Value.IsNull;
    }

    // TODO(Phase 4b/5): In 2.0 RelativePath.Elements is an immutable ArrayOf<T>
    // with no Add/Insert/RemoveAt. These helpers restore the classic in-place
    // mutation shape used by the browse-path resolver by rebuilding the list.
    /// <summary> Compat mutation helpers for RelativePath. </summary>
    public static class RelativePathCompat
    {
        /// <summary> Append an element. </summary>
        public static void Add(this RelativePath path, RelativePathElement element)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.Add(element);
            path.Elements = elements;
        }

        /// <summary> Insert an element. </summary>
        public static void Insert(this RelativePath path, int index,
            RelativePathElement element)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.Insert(index, element);
            path.Elements = elements;
        }

        /// <summary> Remove an element at index. </summary>
        public static void RemoveAt(this RelativePath path, int index)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.RemoveAt(index);
            path.Elements = elements;
        }

        /// <summary> Append a range of elements. </summary>
        public static void AddRange(this RelativePath path,
            ArrayOf<RelativePathElement> range)
        {
            List<RelativePathElement> elements = [.. path.Elements];
            elements.AddRange([.. range]);
            path.Elements = elements;
        }
    }

    // TODO(Phase 4b/5): The UA-.NETStandard 2.0 stack removed the generated
    // typed "XxxCollection" classes in favour of ArrayOf<Xxx>. To keep the
    // Publisher stack layer (which constructs and mutates these collections)
    // compiling without a sweeping rewrite, we resurrect the classic
    // List<Xxx>-based collection types here. List<T> converts implicitly to
    // ArrayOf<T>, so instances still flow into the 2.0 session/service APIs.
    // These shims can be removed once the stack layer is ported to ArrayOf<T>
    // (or replaced by the 2.0 ManagedSession client in Phase 4b).

    /// <summary> Compat collection. </summary>
    public class StringCollection : List<string>
    {
        /// <summary> Create empty. </summary>
        public StringCollection() { }
        /// <summary> Create with capacity. </summary>
        public StringCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public StringCollection(IEnumerable<string> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class NodeIdCollection : List<NodeId>
    {
        /// <summary> Create empty. </summary>
        public NodeIdCollection() { }
        /// <summary> Create with capacity. </summary>
        public NodeIdCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public NodeIdCollection(IEnumerable<NodeId> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class QualifiedNameCollection : List<QualifiedName>
    {
        /// <summary> Create empty. </summary>
        public QualifiedNameCollection() { }
        /// <summary> Create with capacity. </summary>
        public QualifiedNameCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public QualifiedNameCollection(IEnumerable<QualifiedName> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class ByteStringCollection : List<ByteString>
    {
        /// <summary> Create empty. </summary>
        public ByteStringCollection() { }
        /// <summary> Create with capacity. </summary>
        public ByteStringCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public ByteStringCollection(IEnumerable<ByteString> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class DiagnosticInfoCollection : List<DiagnosticInfo>
    {
        /// <summary> Create empty. </summary>
        public DiagnosticInfoCollection() { }
        /// <summary> Create with capacity. </summary>
        public DiagnosticInfoCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public DiagnosticInfoCollection(IEnumerable<DiagnosticInfo> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class ExtensionObjectCollection : List<ExtensionObject>
    {
        /// <summary> Create empty. </summary>
        public ExtensionObjectCollection() { }
        /// <summary> Create with capacity. </summary>
        public ExtensionObjectCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public ExtensionObjectCollection(IEnumerable<ExtensionObject> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public sealed class StructureFieldCollection : List<StructureField>
    {
        /// <summary> Create empty. </summary>
        public StructureFieldCollection() { }
        /// <summary> Create with capacity. </summary>
        public StructureFieldCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public StructureFieldCollection(IEnumerable<StructureField> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class ReadValueIdCollection : List<ReadValueId>
    {
        /// <summary> Create empty. </summary>
        public ReadValueIdCollection() { }
        /// <summary> Create with capacity. </summary>
        public ReadValueIdCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public ReadValueIdCollection(IEnumerable<ReadValueId> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class HistoryReadValueIdCollection : List<HistoryReadValueId>
    {
        /// <summary> Create empty. </summary>
        public HistoryReadValueIdCollection() { }
        /// <summary> Create with capacity. </summary>
        public HistoryReadValueIdCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public HistoryReadValueIdCollection(IEnumerable<HistoryReadValueId> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class WriteValueCollection : List<WriteValue>
    {
        /// <summary> Create empty. </summary>
        public WriteValueCollection() { }
        /// <summary> Create with capacity. </summary>
        public WriteValueCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public WriteValueCollection(IEnumerable<WriteValue> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class BrowseDescriptionCollection : List<BrowseDescription>
    {
        /// <summary> Create empty. </summary>
        public BrowseDescriptionCollection() { }
        /// <summary> Create with capacity. </summary>
        public BrowseDescriptionCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public BrowseDescriptionCollection(IEnumerable<BrowseDescription> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class BrowsePathCollection : List<BrowsePath>
    {
        /// <summary> Create empty. </summary>
        public BrowsePathCollection() { }
        /// <summary> Create with capacity. </summary>
        public BrowsePathCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public BrowsePathCollection(IEnumerable<BrowsePath> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class BrowsePathTargetCollection : List<BrowsePathTarget>
    {
        /// <summary> Create empty. </summary>
        public BrowsePathTargetCollection() { }
        /// <summary> Create with capacity. </summary>
        public BrowsePathTargetCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public BrowsePathTargetCollection(IEnumerable<BrowsePathTarget> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class ReferenceDescriptionCollection : List<ReferenceDescription>
    {
        /// <summary> Create empty. </summary>
        public ReferenceDescriptionCollection() { }
        /// <summary> Create with capacity. </summary>
        public ReferenceDescriptionCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public ReferenceDescriptionCollection(IEnumerable<ReferenceDescription> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class NodeTypeDescriptionCollection : List<NodeTypeDescription>
    {
        /// <summary> Create empty. </summary>
        public NodeTypeDescriptionCollection() { }
        /// <summary> Create with capacity. </summary>
        public NodeTypeDescriptionCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public NodeTypeDescriptionCollection(IEnumerable<NodeTypeDescription> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class AddNodesItemCollection : List<AddNodesItem>
    {
        /// <summary> Create empty. </summary>
        public AddNodesItemCollection() { }
        /// <summary> Create with capacity. </summary>
        public AddNodesItemCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public AddNodesItemCollection(IEnumerable<AddNodesItem> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class AddReferencesItemCollection : List<AddReferencesItem>
    {
        /// <summary> Create empty. </summary>
        public AddReferencesItemCollection() { }
        /// <summary> Create with capacity. </summary>
        public AddReferencesItemCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public AddReferencesItemCollection(IEnumerable<AddReferencesItem> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class DeleteNodesItemCollection : List<DeleteNodesItem>
    {
        /// <summary> Create empty. </summary>
        public DeleteNodesItemCollection() { }
        /// <summary> Create with capacity. </summary>
        public DeleteNodesItemCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public DeleteNodesItemCollection(IEnumerable<DeleteNodesItem> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class DeleteReferencesItemCollection : List<DeleteReferencesItem>
    {
        /// <summary> Create empty. </summary>
        public DeleteReferencesItemCollection() { }
        /// <summary> Create with capacity. </summary>
        public DeleteReferencesItemCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public DeleteReferencesItemCollection(IEnumerable<DeleteReferencesItem> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class CallMethodRequestCollection : List<CallMethodRequest>
    {
        /// <summary> Create empty. </summary>
        public CallMethodRequestCollection() { }
        /// <summary> Create with capacity. </summary>
        public CallMethodRequestCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public CallMethodRequestCollection(IEnumerable<CallMethodRequest> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class EndpointDescriptionCollection : List<EndpointDescription>
    {
        /// <summary> Create empty. </summary>
        public EndpointDescriptionCollection() { }
        /// <summary> Create with capacity. </summary>
        public EndpointDescriptionCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public EndpointDescriptionCollection(IEnumerable<EndpointDescription> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class ServerOnNetworkCollection : List<ServerOnNetwork>
    {
        /// <summary> Create empty. </summary>
        public ServerOnNetworkCollection() { }
        /// <summary> Create with capacity. </summary>
        public ServerOnNetworkCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public ServerOnNetworkCollection(IEnumerable<ServerOnNetwork> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class ApplicationDescriptionCollection : List<ApplicationDescription>
    {
        /// <summary> Create empty. </summary>
        public ApplicationDescriptionCollection() { }
        /// <summary> Create with capacity. </summary>
        public ApplicationDescriptionCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public ApplicationDescriptionCollection(IEnumerable<ApplicationDescription> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class CertificateIdentifierCollection : List<CertificateIdentifier>
    {
        /// <summary> Create empty. </summary>
        public CertificateIdentifierCollection() { }
        /// <summary> Create with capacity. </summary>
        public CertificateIdentifierCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public CertificateIdentifierCollection(IEnumerable<CertificateIdentifier> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class UserTokenPolicyCollection : List<UserTokenPolicy>
    {
        /// <summary> Create empty. </summary>
        public UserTokenPolicyCollection() { }
        /// <summary> Create with capacity. </summary>
        public UserTokenPolicyCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public UserTokenPolicyCollection(IEnumerable<UserTokenPolicy> collection) : base(collection) { }
    }

}
