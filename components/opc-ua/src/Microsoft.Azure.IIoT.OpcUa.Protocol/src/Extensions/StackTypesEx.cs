// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using UaApplicationType = Opc.Ua.ApplicationType;
    using UaSecurityMode = Opc.Ua.MessageSecurityMode;
    using UaBrowseDirection = Opc.Ua.BrowseDirection;
    using UaTokenType = Opc.Ua.UserTokenType;
    using UaNodeClass = Opc.Ua.NodeClass;
    using UaFilterOperator = Opc.Ua.FilterOperator;
    using UaMonitoringMode = Opc.Ua.MonitoringMode;
    using UaDeadbandType = Opc.Ua.DeadbandType;
    using UaDataChangeTrigger = Opc.Ua.DataChangeTrigger;
    using UaPermissionType = Opc.Ua.PermissionType;
    using UaDiagnosticsLevel = Opc.Ua.DiagnosticsMasks;
    using JsonDataSetMessageContentMask = Opc.Ua.JsonDataSetMessageContentMask;
    using JsonNetworkMessageContentMask = Opc.Ua.JsonNetworkMessageContentMask;
    using UadpDataSetMessageContentMask = Opc.Ua.UadpDataSetMessageContentMask;
    using UadpNetworkMessageContentMask = Opc.Ua.UadpNetworkMessageContentMask;
    using UaDataSetFieldContentMask = Opc.Ua.DataSetFieldContentMask;
    using System.Collections.Generic;
    using System;
    using Opc.Ua.PubSub;

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
        /// Convert monitoring mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static MonitoringMode? ToServiceType(this UaMonitoringMode mode) {
            switch (mode) {
                case UaMonitoringMode.Disabled:
                    return MonitoringMode.Disabled;
                case UaMonitoringMode.Sampling:
                    return MonitoringMode.Sampling;
                case UaMonitoringMode.Reporting:
                    return MonitoringMode.Reporting;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert monitoring mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaMonitoringMode? ToStackType(this MonitoringMode? mode) {
            if (mode == null) {
                return null;
            }
            switch (mode) {
                case MonitoringMode.Disabled:
                    return UaMonitoringMode.Disabled;
                case MonitoringMode.Sampling:
                    return UaMonitoringMode.Sampling;
                case MonitoringMode.Reporting:
                    return UaMonitoringMode.Reporting;
                default:
                    return UaMonitoringMode.Reporting;
            }
        }

        /// <summary>
        /// Convert deadband type
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaDeadbandType ToStackType(this DeadbandType? mode) {
            if (mode == null) {
                return UaDeadbandType.None;
            }
            switch (mode.Value) {
                case DeadbandType.Absolute:
                    return UaDeadbandType.Absolute;
                case DeadbandType.Percent:
                    return UaDeadbandType.Percent;
                default:
                    return UaDeadbandType.None;
            }
        }

        /// <summary>
        /// Convert deadband type
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static DeadbandType? ToServiceType(this UaDeadbandType mode) {
            switch (mode) {
                case UaDeadbandType.None:
                    return null;
                case UaDeadbandType.Absolute:
                    return DeadbandType.Absolute;
                case UaDeadbandType.Percent:
                    return DeadbandType.Percent;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert deadband type
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaDataChangeTrigger ToStackType(this DataChangeTriggerType? mode) {
            if (mode == null) {
                // Default is status and value change triggering
                return UaDataChangeTrigger.StatusValue;
            }
            switch (mode.Value) {
                case DataChangeTriggerType.Status:
                    return UaDataChangeTrigger.Status;
                case DataChangeTriggerType.StatusValue:
                    return UaDataChangeTrigger.StatusValue;
                case DataChangeTriggerType.StatusValueTimestamp:
                    return UaDataChangeTrigger.StatusValueTimestamp;
                default:
                    return UaDataChangeTrigger.StatusValue;
            }
        }

        /// <summary>
        /// Convert deadband type
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static DataChangeTriggerType? ToServiceType(this UaDataChangeTrigger mode) {
            switch (mode) {
                case UaDataChangeTrigger.Status:
                    return DataChangeTriggerType.Status;
                case UaDataChangeTrigger.StatusValue:
                    return DataChangeTriggerType.StatusValue;
                case UaDataChangeTrigger.StatusValueTimestamp:
                    return DataChangeTriggerType.StatusValueTimestamp;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert to stack type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static UaFilterOperator ToStackType(this FilterOperatorType type) {
            switch (type) {
                case FilterOperatorType.Equals:
                    return UaFilterOperator.Equals;
                case FilterOperatorType.IsNull:
                    return UaFilterOperator.IsNull;
                case FilterOperatorType.GreaterThan:
                    return UaFilterOperator.GreaterThan;
                case FilterOperatorType.LessThan:
                    return UaFilterOperator.LessThan;
                case FilterOperatorType.GreaterThanOrEqual:
                    return UaFilterOperator.GreaterThanOrEqual;
                case FilterOperatorType.LessThanOrEqual:
                    return UaFilterOperator.LessThanOrEqual;
                case FilterOperatorType.Like:
                    return UaFilterOperator.Like;
                case FilterOperatorType.Not:
                    return UaFilterOperator.Not;
                case FilterOperatorType.Between:
                    return UaFilterOperator.Between;
                case FilterOperatorType.InList:
                    return UaFilterOperator.InList;
                case FilterOperatorType.And:
                    return UaFilterOperator.And;
                case FilterOperatorType.Or:
                    return UaFilterOperator.Or;
                case FilterOperatorType.Cast:
                    return UaFilterOperator.Cast;
                case FilterOperatorType.InView:
                    return UaFilterOperator.InView;
                case FilterOperatorType.OfType:
                    return UaFilterOperator.OfType;
                case FilterOperatorType.RelatedTo:
                    return UaFilterOperator.RelatedTo;
                case FilterOperatorType.BitwiseAnd:
                    return UaFilterOperator.BitwiseAnd;
                case FilterOperatorType.BitwiseOr:
                    return UaFilterOperator.BitwiseOr;
                default:
                    throw new NotSupportedException($"{type} not supported");
            }
        }

        /// <summary>
        /// Convert to stack type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FilterOperatorType ToServiceType(this UaFilterOperator type) {
            switch (type) {
                case UaFilterOperator.Equals:
                    return FilterOperatorType.Equals;
                case UaFilterOperator.IsNull:
                    return FilterOperatorType.IsNull;
                case UaFilterOperator.GreaterThan:
                    return FilterOperatorType.GreaterThan;
                case UaFilterOperator.LessThan:
                    return FilterOperatorType.LessThan;
                case UaFilterOperator.GreaterThanOrEqual:
                    return FilterOperatorType.GreaterThanOrEqual;
                case UaFilterOperator.LessThanOrEqual:
                    return FilterOperatorType.LessThanOrEqual;
                case UaFilterOperator.Like:
                    return FilterOperatorType.Like;
                case UaFilterOperator.Not:
                    return FilterOperatorType.Not;
                case UaFilterOperator.Between:
                    return FilterOperatorType.Between;
                case UaFilterOperator.InList:
                    return FilterOperatorType.InList;
                case UaFilterOperator.And:
                    return FilterOperatorType.And;
                case UaFilterOperator.Or:
                    return FilterOperatorType.Or;
                case UaFilterOperator.Cast:
                    return FilterOperatorType.Cast;
                case UaFilterOperator.InView:
                    return FilterOperatorType.InView;
                case UaFilterOperator.OfType:
                    return FilterOperatorType.OfType;
                case UaFilterOperator.RelatedTo:
                    return FilterOperatorType.RelatedTo;
                case UaFilterOperator.BitwiseAnd:
                    return FilterOperatorType.BitwiseAnd;
                case UaFilterOperator.BitwiseOr:
                    return FilterOperatorType.BitwiseOr;
                default:
                    throw new NotSupportedException($"{type} not supported");
            }
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static uint ToStackType(this NetworkMessageContentMask? mask, MessageEncoding? encoding) {
            if (mask == null) {
                mask =
                    NetworkMessageContentMask.NetworkMessageHeader |
                    NetworkMessageContentMask.NetworkMessageNumber |
                    NetworkMessageContentMask.DataSetMessageHeader |
                    NetworkMessageContentMask.PublisherId |
                    NetworkMessageContentMask.DataSetClassId;
            }
            switch (encoding) {
                case MessageEncoding.Uadp:
                    return (uint)ToUadpStackType(mask.Value);
                case MessageEncoding.Json:
                    return (uint)ToJsonStackType(mask.Value);
            }
            return (uint)ToJsonStackType(mask.Value);
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="fieldMask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static uint ToStackType(this DataSetContentMask? mask,
            DataSetFieldContentMask? fieldMask, MessageEncoding? encoding) {
            if (mask == null) {
                mask =
                    DataSetContentMask.DataSetWriterId |
                    DataSetContentMask.DataSetWriterName |
                    DataSetContentMask.MetaDataVersion |
                    DataSetContentMask.MajorVersion |
                    DataSetContentMask.MinorVersion |
                    DataSetContentMask.SequenceNumber |
                    DataSetContentMask.Timestamp |
                    DataSetContentMask.MessageType |
                    DataSetContentMask.Status;
            }
            switch (encoding) {
                case MessageEncoding.Uadp:
                    return (uint)ToUadpStackType(mask.Value);
                case MessageEncoding.Json:
                    return (uint)ToJsonStackType(mask.Value, fieldMask);
            }
            return (uint)ToJsonStackType(mask.Value, fieldMask);
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static JsonNetworkMessageContentMask ToJsonStackType(this NetworkMessageContentMask mask) {
            var result = JsonNetworkMessageContentMask.None;
            if (0 != (mask & NetworkMessageContentMask.PublisherId)) {
                result |= JsonNetworkMessageContentMask.PublisherId;
            }
            if (0 != (mask & NetworkMessageContentMask.DataSetClassId)) {
                result |= JsonNetworkMessageContentMask.DataSetClassId;
            }
            if (0 != (mask & NetworkMessageContentMask.ReplyTo)) {
                result |= JsonNetworkMessageContentMask.ReplyTo;
            }
            if (0 != (mask & NetworkMessageContentMask.NetworkMessageHeader)) {
                result |= JsonNetworkMessageContentMask.NetworkMessageHeader;
            }
            else {
                // If not set, bits 3, 4 and 5 can also not be set
                result = JsonNetworkMessageContentMask.None;
            }
            if (0 != (mask & NetworkMessageContentMask.MonitoredItemMessage)) {
                // If monitored item message, then no network message header
                result = JsonNetworkMessageContentMask.None;
            }
            if (0 != (mask & NetworkMessageContentMask.DataSetMessageHeader)) {
                result |= JsonNetworkMessageContentMask.DataSetMessageHeader;
            }
            if (0 != (mask & NetworkMessageContentMask.SingleDataSetMessage)) {
                result |= JsonNetworkMessageContentMask.SingleDataSetMessage;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="fieldMask"></param>
        /// <returns></returns>
        private static JsonDataSetMessageContentMask ToJsonStackType(this DataSetContentMask mask,
            DataSetFieldContentMask? fieldMask) {
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
            if (0 != (mask & DataSetContentMask.MessageType)) {
                result |= JsonDataSetMessageContentMask.MessageType;
            }
            if (0 != (mask & DataSetContentMask.DataSetWriterName)) {
                result |= JsonDataSetMessageContentMask2.DataSetWriterName;
            }
            if (0 != (mask & DataSetContentMask.ReversibleFieldEncoding)) {
                result |= JsonDataSetMessageContentMask2.ReversibleFieldEncoding;
            }

            if (fieldMask != null) {
                if (0 != (fieldMask & DataSetFieldContentMask.NodeId)) {
                    result |= JsonDataSetMessageContentMaskEx.NodeId;
                }
                if (0 != (fieldMask & DataSetFieldContentMask.DisplayName)) {
                    result |= JsonDataSetMessageContentMaskEx.DisplayName;
                }
                if (0 != (fieldMask & DataSetFieldContentMask.ExtensionFields)) {
                    result |= JsonDataSetMessageContentMaskEx.ExtensionFields;
                }
                if (0 != (fieldMask & DataSetFieldContentMask.EndpointUrl)) {
                    result |= JsonDataSetMessageContentMaskEx.EndpointUrl;
                }
                if (0 != (fieldMask & DataSetFieldContentMask.ApplicationUri)) {
                    result |= JsonDataSetMessageContentMaskEx.ApplicationUri;
                }
            }
            return result;
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static UadpNetworkMessageContentMask ToUadpStackType(this NetworkMessageContentMask mask) {
            var result = UadpNetworkMessageContentMask.None;
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
        private static UadpDataSetMessageContentMask ToUadpStackType(this DataSetContentMask mask) {
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
            var result = UaDataSetFieldContentMask.None;
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
