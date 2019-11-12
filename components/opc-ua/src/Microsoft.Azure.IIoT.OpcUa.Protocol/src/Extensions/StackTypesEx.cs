// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using UaApplicationType = Opc.Ua.ApplicationType;
    using UaSecurityMode = Opc.Ua.MessageSecurityMode;
    using UaBrowseDirection = Opc.Ua.BrowseDirection;
    using UaTokenType = Opc.Ua.UserTokenType;
    using UaNodeClass = Opc.Ua.NodeClass;
    using UaPermissionType = Opc.Ua.PermissionType;
    using UaDiagnosticsLevel = Opc.Ua.DiagnosticsMasks;
    using UaMonitoredItemSampleContentMask = Opc.Ua.MonitoredItemSampleContentMask;
    using JsonDataSetMessageContentMask = Opc.Ua.JsonDataSetMessageContentMask;
    using JsonNetworkMessageContentMask = Opc.Ua.JsonNetworkMessageContentMask;
    using UadpDataSetMessageContentMask = Opc.Ua.UadpDataSetMessageContentMask;
    using UadpNetworkMessageContentMask = Opc.Ua.UadpNetworkMessageContentMask;
    using UaDataSetFieldContentMask = Opc.Ua.DataSetFieldContentMask;
    using System.Collections.Generic;

    /// <summary>
    /// Stack types conversions
    /// </summary>
    public static class StackTypesEx {

        /// <summary>
        /// Convert node class
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        public static NodeClass? ToServiceType(this UaNodeClass nodeClass) {
            switch (nodeClass) {
                case UaNodeClass.Object:
                    return NodeClass.Object;
                case UaNodeClass.ObjectType:
                    return NodeClass.ObjectType;
                case UaNodeClass.Variable:
                    return NodeClass.Variable;
                case UaNodeClass.VariableType:
                    return NodeClass.VariableType;
                case UaNodeClass.Method:
                    return NodeClass.Method;
                case UaNodeClass.DataType:
                    return NodeClass.DataType;
                case UaNodeClass.ReferenceType:
                    return NodeClass.ReferenceType;
                case UaNodeClass.View:
                    return NodeClass.View;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert node class
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        public static UaNodeClass ToStackType(this NodeClass nodeClass) {
            switch (nodeClass) {
                case NodeClass.Object:
                    return UaNodeClass.Object;
                case NodeClass.ObjectType:
                    return UaNodeClass.ObjectType;
                case NodeClass.Variable:
                    return UaNodeClass.Variable;
                case NodeClass.VariableType:
                    return UaNodeClass.VariableType;
                case NodeClass.Method:
                    return UaNodeClass.Method;
                case NodeClass.DataType:
                    return UaNodeClass.DataType;
                case NodeClass.ReferenceType:
                    return UaNodeClass.ReferenceType;
                case NodeClass.View:
                    return UaNodeClass.View;
                default:
                    return UaNodeClass.Unspecified;
            }
        }

        /// <summary>
        /// Convert mask to a list of node classes
        /// </summary>
        /// <param name="nodeClasses"></param>
        /// <returns></returns>
        public static List<NodeClass> ToServiceMask(this UaNodeClass nodeClasses) {
            if (nodeClasses == UaNodeClass.Unspecified) {
                return null;
            }
            var result = new List<NodeClass>();
            var mask = (uint)nodeClasses;
            if (0 != (mask & (uint)UaNodeClass.Object)) {
                result.Add(NodeClass.Object);
            }
            if (0 != (mask & (uint)UaNodeClass.Variable)) {
                result.Add(NodeClass.Variable);
            }
            if (0 != (mask & (uint)UaNodeClass.Method)) {
                result.Add(NodeClass.Method);
            }
            if (0 != (mask & (uint)UaNodeClass.ObjectType)) {
                result.Add(NodeClass.ObjectType);
            }
            if (0 != (mask & (uint)UaNodeClass.VariableType)) {
                result.Add(NodeClass.VariableType);
            }
            if (0 != (mask & (uint)UaNodeClass.ReferenceType)) {
                result.Add(NodeClass.ReferenceType);
            }
            if (0 != (mask & (uint)UaNodeClass.DataType)) {
                result.Add(NodeClass.DataType);
            }
            if (0 != (mask & (uint)UaNodeClass.View)) {
                result.Add(NodeClass.View);
            }
            return result;
        }

        /// <summary>
        /// Convert mask to a list of node classes
        /// </summary>
        /// <param name="nodeClasses"></param>
        /// <returns></returns>
        public static UaNodeClass ToStackMask(this List<NodeClass> nodeClasses) {
            var mask = 0u;
            if (nodeClasses != null) {
                foreach (var nodeClass in nodeClasses) {
                    mask |= (uint)nodeClass.ToStackType();
                }
            }
            return (UaNodeClass)mask;
        }

        /// <summary>
        /// Convert browse direction
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static BrowseDirection? ToServiceType(this UaBrowseDirection mode) {
            switch (mode) {
                case UaBrowseDirection.Forward:
                    return BrowseDirection.Forward;
                case UaBrowseDirection.Inverse:
                    return BrowseDirection.Backward;
                case UaBrowseDirection.Both:
                    return BrowseDirection.Both;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert browse direction
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaBrowseDirection ToStackType(this BrowseDirection mode) {
            switch (mode) {
                case BrowseDirection.Forward:
                    return UaBrowseDirection.Forward;
                case BrowseDirection.Backward:
                    return UaBrowseDirection.Inverse;
                case BrowseDirection.Both:
                    return UaBrowseDirection.Both;
                default:
                    return UaBrowseDirection.Forward;
            }
        }

        /// <summary>
        /// Convert Permissions
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public static UaPermissionType ToStackType(this RolePermissions? permissions) {
            if (permissions == null) {
                return UaPermissionType.None;
            }
            return (UaPermissionType)permissions;
        }

        /// <summary>
        /// Convert permissions
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public static RolePermissions? ToServiceType(this UaPermissionType permissions) {
            if (permissions == UaPermissionType.None) {
                return null;
            }
            return (RolePermissions)permissions;
        }

        /// <summary>
        /// Convert security mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static SecurityMode? ToServiceType(this UaSecurityMode mode) {
            switch (mode) {
                case UaSecurityMode.None:
                    return SecurityMode.None;
                case UaSecurityMode.Sign:
                    return SecurityMode.Sign;
                case UaSecurityMode.SignAndEncrypt:
                    return SecurityMode.SignAndEncrypt;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert security mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaSecurityMode ToStackType(this SecurityMode mode) {
            switch (mode) {
                case SecurityMode.Sign:
                    return UaSecurityMode.Sign;
                case SecurityMode.SignAndEncrypt:
                    return UaSecurityMode.SignAndEncrypt;
                default:
                    return UaSecurityMode.None;
            }
        }

        /// <summary>
        /// Convert token type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static CredentialType? ToServiceType(this UaTokenType type) {
            switch (type) {
                case UaTokenType.Anonymous:
                    return CredentialType.None;
                case UaTokenType.Certificate:
                    return CredentialType.X509Certificate;
                case UaTokenType.UserName:
                case UaTokenType.IssuedToken:
                    return CredentialType.UserName;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert application type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApplicationType? ToServiceType(this UaApplicationType type) {
            switch (type) {
                case UaApplicationType.Client:
                    return ApplicationType.Client;
                case UaApplicationType.DiscoveryServer:
                    return ApplicationType.DiscoveryServer;
                case UaApplicationType.Server:
                    return ApplicationType.Server;
                case UaApplicationType.ClientAndServer:
                    return ApplicationType.ClientAndServer;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert application type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static UaApplicationType ToStackType(this ApplicationType type) {
            switch (type) {
                case ApplicationType.Client:
                    return UaApplicationType.Client;
                case ApplicationType.ClientAndServer:
                    return UaApplicationType.ClientAndServer;
                case ApplicationType.DiscoveryServer:
                    return UaApplicationType.DiscoveryServer;
                default:
                    return UaApplicationType.Server;
            }
        }

        /// <summary>
        /// Convert to diagnostics level
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static DiagnosticsLevel ToServiceType(this UaDiagnosticsLevel level) {
            switch (level) {
                case UaDiagnosticsLevel.SymbolicIdAndText | UaDiagnosticsLevel.InnerDiagnostics:
                    return DiagnosticsLevel.Diagnostics;
                case UaDiagnosticsLevel.All:
                    return DiagnosticsLevel.Verbose;
                default:
                    return DiagnosticsLevel.None;
            }
        }

        /// <summary>
        /// Convert to diagnostics mask
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static UaDiagnosticsLevel ToStackType(this DiagnosticsLevel level) {
            switch (level) {
                case DiagnosticsLevel.Diagnostics:
                    return UaDiagnosticsLevel.SymbolicIdAndText | UaDiagnosticsLevel.InnerDiagnostics;
                case DiagnosticsLevel.Verbose:
                    return UaDiagnosticsLevel.All;
                default:
                    return UaDiagnosticsLevel.None;
            }
        }

        /// <summary>
        /// Get message content mask
        /// </summary>
        /// <returns></returns>
        public static uint ToStackType(this MonitoredItemMessageContentMask? mask,
            MonitoredItemMessageEncoding? encoding) {
            switch (encoding) {
                case MonitoredItemMessageEncoding.Json:
                    return (uint)mask.ToJsonStackType();
                // Binary?
            }
            return (uint)mask.ToJsonStackType();
        }

        /// <summary>
        /// Get message content mask
        /// </summary>
        /// <returns></returns>
        public static UaMonitoredItemSampleContentMask ToJsonStackType(this MonitoredItemMessageContentMask? mask) {
            if (mask == null) {
                mask =
                    MonitoredItemMessageContentMask.DisplayName |
                    MonitoredItemMessageContentMask.StatusCode |
                    MonitoredItemMessageContentMask.Status |
                    MonitoredItemMessageContentMask.ExtraFields |
                    MonitoredItemMessageContentMask.SubscriptionId |
                    MonitoredItemMessageContentMask.NodeId |
                    MonitoredItemMessageContentMask.Timestamp |
                    MonitoredItemMessageContentMask.ServerTimestamp |
                    MonitoredItemMessageContentMask.SourceTimestamp;
            }
            UaMonitoredItemSampleContentMask result = 0;
            if (0 != (mask & MonitoredItemMessageContentMask.SourceTimestamp)) {
                result |= UaMonitoredItemSampleContentMask.SourceTimestamp;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.SourcePicoSeconds)) {
                result |= UaMonitoredItemSampleContentMask.SourcePicoSeconds;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.ServerTimestamp)) {
                result |= UaMonitoredItemSampleContentMask.ServerTimestamp;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.ServerPicoSeconds)) {
                result |= UaMonitoredItemSampleContentMask.ServerPicoSeconds;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.Timestamp)) {
                result |= UaMonitoredItemSampleContentMask.Timestamp;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.PicoSeconds)) {
                result |= UaMonitoredItemSampleContentMask.PicoSeconds;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.StatusCode)) {
                result |= UaMonitoredItemSampleContentMask.StatusCode;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.Status)) {
                result |= UaMonitoredItemSampleContentMask.Status;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.NodeId)) {
                result |= UaMonitoredItemSampleContentMask.NodeId;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.EndpointUrl)) {
                result |= UaMonitoredItemSampleContentMask.EndpointUrl;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.ApplicationUri)) {
                result |= UaMonitoredItemSampleContentMask.ApplicationUri;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.DisplayName)) {
                result |= UaMonitoredItemSampleContentMask.DisplayName;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.ExtraFields)) {
                result |= UaMonitoredItemSampleContentMask.ExtraFields;
            }
            if (0 != (mask & MonitoredItemMessageContentMask.SubscriptionId)) {
                result |= UaMonitoredItemSampleContentMask.SubscriptionId;
            }
            return result;
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static uint ToStackType(this NetworkMessageContentMask? mask, NetworkMessageEncoding? encoding) {
            switch (encoding) {
                case NetworkMessageEncoding.Uadp:
                    return (uint)mask.ToUadpStackType();
                case NetworkMessageEncoding.Json:
                    return (uint)mask.ToJsonStackType();
            }
            return (uint)mask.ToJsonStackType();
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static uint ToStackType(this DataSetContentMask? mask, NetworkMessageEncoding? encoding) {
            switch (encoding) {
                case NetworkMessageEncoding.Uadp:
                    return (uint)mask.ToUadpStackType();
                case NetworkMessageEncoding.Json:
                    return (uint)mask.ToJsonStackType();
            }
            return (uint)mask.ToJsonStackType();
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static JsonNetworkMessageContentMask ToJsonStackType(this NetworkMessageContentMask? mask) {
            if (mask == null) {
                mask =
                    NetworkMessageContentMask.NetworkMessageHeader |
                    NetworkMessageContentMask.DataSetMessageHeader |
                    NetworkMessageContentMask.PublisherId |
                    NetworkMessageContentMask.DataSetClassId;
            }
            JsonNetworkMessageContentMask result = 0;
            if (0 != (mask & NetworkMessageContentMask.PublisherId)) {
                result |= JsonNetworkMessageContentMask.PublisherId;
            }
            if (0 != (mask & NetworkMessageContentMask.DataSetClassId)) {
                result |= JsonNetworkMessageContentMask.DataSetClassId;
            }
            if (0 != (mask & NetworkMessageContentMask.NetworkMessageHeader)) {
                result |= JsonNetworkMessageContentMask.NetworkMessageHeader;
            }
            if (0 != (mask & NetworkMessageContentMask.DataSetMessageHeader)) {
                result |= JsonNetworkMessageContentMask.DataSetMessageHeader;
            }
            if (0 != (mask & NetworkMessageContentMask.SingleDataSetMessage)) {
                result |= JsonNetworkMessageContentMask.SingleDataSetMessage;
            }
            if (0 != (mask & NetworkMessageContentMask.ReplyTo)) {
                result |= JsonNetworkMessageContentMask.ReplyTo;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static JsonDataSetMessageContentMask ToJsonStackType(this DataSetContentMask? mask) {
            if (mask == null) {
                mask =
                    DataSetContentMask.DataSetWriterId |
                    DataSetContentMask.MetaDataVersion |
                    DataSetContentMask.SequenceNumber;
            }
            var result = JsonDataSetMessageContentMask.None;
            if (0 != (mask & DataSetContentMask.Timestamp)) {
                result |= JsonDataSetMessageContentMask.Timestamp;
            }
            if (0 != (mask & DataSetContentMask.Status)) {
                result |= JsonDataSetMessageContentMask.Status;
            }
            if (0 != (mask & DataSetContentMask.MetaDataVersion)) {
                result |= JsonDataSetMessageContentMask.MetaDataVersion;
            }
            if (0 != (mask & DataSetContentMask.SequenceNumber)) {
                result |= JsonDataSetMessageContentMask.SequenceNumber;
            }
            if (0 != (mask & DataSetContentMask.DataSetWriterId)) {
                result |= JsonDataSetMessageContentMask.DataSetWriterId;
            }
            return result;
        }
        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static UadpNetworkMessageContentMask ToUadpStackType(this NetworkMessageContentMask? mask) {
            if (mask == null) {
                mask =
                    NetworkMessageContentMask.NetworkMessageHeader |
                    NetworkMessageContentMask.DataSetMessageHeader |
                    NetworkMessageContentMask.PublisherId |
                    NetworkMessageContentMask.DataSetClassId;
            }
            UadpNetworkMessageContentMask result = 0;
            if (0 != (mask & NetworkMessageContentMask.PublisherId)) {
                result |= UadpNetworkMessageContentMask.PublisherId;
            }
            if (0 != (mask & NetworkMessageContentMask.GroupHeader)) {
                result |= UadpNetworkMessageContentMask.GroupHeader;
            }
            if (0 != (mask & NetworkMessageContentMask.WriterGroupId)) {
                result |= UadpNetworkMessageContentMask.WriterGroupId;
            }
            if (0 != (mask & NetworkMessageContentMask.GroupVersion)) {
                result |= UadpNetworkMessageContentMask.GroupVersion;
            }
            if (0 != (mask & NetworkMessageContentMask.NetworkMessageNumber)) {
                result |= UadpNetworkMessageContentMask.NetworkMessageNumber;
            }
            if (0 != (mask & NetworkMessageContentMask.SequenceNumber)) {
                result |= UadpNetworkMessageContentMask.SequenceNumber;
            }
            if (0 != (mask & NetworkMessageContentMask.PayloadHeader)) {
                result |= UadpNetworkMessageContentMask.PayloadHeader;
            }
            if (0 != (mask & NetworkMessageContentMask.Timestamp)) {
                result |= UadpNetworkMessageContentMask.Timestamp;
            }
            if (0 != (mask & NetworkMessageContentMask.Picoseconds)) {
                result |= UadpNetworkMessageContentMask.PicoSeconds;
            }
            if (0 != (mask & NetworkMessageContentMask.DataSetClassId)) {
                result |= UadpNetworkMessageContentMask.DataSetClassId;
            }
            if (0 != (mask & NetworkMessageContentMask.PromotedFields)) {
                result |= UadpNetworkMessageContentMask.PromotedFields;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static UadpDataSetMessageContentMask ToUadpStackType(this DataSetContentMask? mask) {
            if (mask == null) {
                mask =
                    DataSetContentMask.DataSetWriterId |
                    DataSetContentMask.MetaDataVersion |
                    DataSetContentMask.SequenceNumber;
            }
            var result = UadpDataSetMessageContentMask.None;
            if (0 != (mask & DataSetContentMask.Timestamp)) {
                result |= UadpDataSetMessageContentMask.Timestamp;
            }
            if (0 != (mask & DataSetContentMask.PicoSeconds)) {
                result |= UadpDataSetMessageContentMask.PicoSeconds;
            }
            if (0 != (mask & DataSetContentMask.Status)) {
                result |= UadpDataSetMessageContentMask.Status;
            }
            if (0 != (mask & DataSetContentMask.SequenceNumber)) {
                result |= UadpDataSetMessageContentMask.SequenceNumber;
            }
            if (0 != (mask & DataSetContentMask.MinorVersion)) {
                result |= UadpDataSetMessageContentMask.MinorVersion;
            }
            if (0 != (mask & DataSetContentMask.MajorVersion)) {
                result |= UadpDataSetMessageContentMask.MajorVersion;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static UaDataSetFieldContentMask ToStackType(this DataSetFieldContentMask? mask) {
            if (mask == null) {
                mask =
                    DataSetFieldContentMask.StatusCode |
                    DataSetFieldContentMask.SourceTimestamp |
                    DataSetFieldContentMask.SourcePicoSeconds |
                    DataSetFieldContentMask.ServerPicoSeconds |
                    DataSetFieldContentMask.ServerTimestamp;
            }
            UaDataSetFieldContentMask result = 0;
            if (0 != (mask & DataSetFieldContentMask.StatusCode)) {
                result |= UaDataSetFieldContentMask.StatusCode;
            }
            if (0 != (mask & DataSetFieldContentMask.SourceTimestamp)) {
                result |= UaDataSetFieldContentMask.SourceTimestamp;
            }
            if (0 != (mask & DataSetFieldContentMask.ServerTimestamp)) {
                result |= UaDataSetFieldContentMask.ServerTimestamp;
            }
            if (0 != (mask & DataSetFieldContentMask.SourcePicoSeconds)) {
                result |= UaDataSetFieldContentMask.SourcePicoSeconds;
            }
            if (0 != (mask & DataSetFieldContentMask.ServerPicoSeconds)) {
                result |= UaDataSetFieldContentMask.ServerPicoSeconds;
            }
            if (0 != (mask & DataSetFieldContentMask.RawData)) {
                result |= UaDataSetFieldContentMask.RawData;
            }
            return result;
        }
    }
}
