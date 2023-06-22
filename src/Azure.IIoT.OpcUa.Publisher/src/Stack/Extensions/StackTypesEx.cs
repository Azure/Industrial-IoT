// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using UaAggregateBits = Opc.Ua.AggregateBits;
    using UaApplicationType = Opc.Ua.ApplicationType;
    using UaBrowseDirection = Opc.Ua.BrowseDirection;
    using UaDataChangeTrigger = Opc.Ua.DataChangeTrigger;
    using UaDataSetFieldContentMask = Opc.Ua.DataSetFieldContentMask;
    using UaDeadbandType = Opc.Ua.DeadbandType;
    using UaDiagnosticsLevel = Opc.Ua.DiagnosticsMasks;
    using UaExceptionDeviationFormat = Opc.Ua.ExceptionDeviationFormat;
    using UaFilterOperator = Opc.Ua.FilterOperator;
    using JsonDataSetMessageContentMask = Opc.Ua.JsonDataSetMessageContentMask;
    using JsonNetworkMessageContentMask = Opc.Ua.JsonNetworkMessageContentMask;
    using UaSecurityMode = Opc.Ua.MessageSecurityMode;
    using UaMonitoringMode = Opc.Ua.MonitoringMode;
    using UaNodeClass = Opc.Ua.NodeClass;
    using UaPermissionType = Opc.Ua.PermissionType;
    using UaTimestampsToReturn = Opc.Ua.TimestampsToReturn;
    using UadpDataSetMessageContentMask = Opc.Ua.UadpDataSetMessageContentMask;
    using UadpNetworkMessageContentMask = Opc.Ua.UadpNetworkMessageContentMask;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Stack types conversions
    /// </summary>
    internal static class StackTypesEx
    {
        /// <summary>
        /// Convert node class
        /// </summary>
        /// <param name="nodeClass"></param>
        /// <returns></returns>
        public static NodeClass? ToServiceType(this UaNodeClass nodeClass)
        {
            switch (nodeClass)
            {
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
        public static UaNodeClass ToStackType(this NodeClass nodeClass)
        {
            switch (nodeClass)
            {
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
        public static UaNodeClass ToStackMask(this IReadOnlyList<NodeClass>? nodeClasses)
        {
            var mask = 0u;
            if (nodeClasses != null)
            {
                foreach (var nodeClass in nodeClasses)
                {
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
        public static UaBrowseDirection ToStackType(this BrowseDirection mode)
        {
            switch (mode)
            {
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
        /// Convert permissions
        /// </summary>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public static RolePermissions? ToServiceType(this UaPermissionType permissions)
        {
            if (permissions == UaPermissionType.None)
            {
                return null;
            }
            return (RolePermissions)permissions;
        }

        /// <summary>
        /// Convert security mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static SecurityMode? ToServiceType(this UaSecurityMode mode)
        {
            switch (mode)
            {
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
        public static UaSecurityMode ToStackType(this SecurityMode mode)
        {
            switch (mode)
            {
                case SecurityMode.Sign:
                    return UaSecurityMode.Sign;
                case SecurityMode.SignAndEncrypt:
                case SecurityMode.Best:
                    return UaSecurityMode.SignAndEncrypt;
                default:
                    return UaSecurityMode.None;
            }
        }

        /// <summary>
        /// Convert application type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApplicationType? ToServiceType(this UaApplicationType type)
        {
            switch (type)
            {
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
        /// Convert to diagnostics mask
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static UaDiagnosticsLevel ToStackType(this DiagnosticsLevel level)
        {
            var result = UaDiagnosticsLevel.None;
            if (level == DiagnosticsLevel.None)
            {
                return result;
            }
            result |= UaDiagnosticsLevel.SymbolicIdAndText;
            if (level == DiagnosticsLevel.Status)
            {
                return result;
            }
            result |= UaDiagnosticsLevel.AdditionalInfo;
            if (level == DiagnosticsLevel.Information)
            {
                return result;
            }
            result |= UaDiagnosticsLevel.InnerStatusCode;
            result |= UaDiagnosticsLevel.InnerDiagnostics;
            if (level == DiagnosticsLevel.Debug)
            {
                return result;
            }
            result |= UaDiagnosticsLevel.All;
            return result;
        }

        /// <summary>
        /// Convert monitoring mode
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaMonitoringMode? ToStackType(this MonitoringMode? mode)
        {
            if (mode == null)
            {
                return null;
            }
            switch (mode)
            {
                case MonitoringMode.Disabled:
                    return UaMonitoringMode.Disabled;
                case MonitoringMode.Sampling:
                    return UaMonitoringMode.Sampling;
                default:
                    return UaMonitoringMode.Reporting;
            }
        }

        /// <summary>
        /// Convert timestamp to return
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaTimestampsToReturn ToStackType(this TimestampsToReturn? mode)
        {
            switch (mode)
            {
                case TimestampsToReturn.None:
                    return UaTimestampsToReturn.Neither;
                case TimestampsToReturn.Server:
                    return UaTimestampsToReturn.Server;
                case TimestampsToReturn.Source:
                    return UaTimestampsToReturn.Source;
                default:
                    return UaTimestampsToReturn.Both;
            }
        }

        /// <summary>
        /// Convert deadband type
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static UaDeadbandType ToStackType(this DeadbandType? mode)
        {
            if (mode == null)
            {
                return UaDeadbandType.None;
            }
            switch (mode.Value)
            {
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
        public static UaDataChangeTrigger ToStackType(this DataChangeTriggerType? mode)
        {
            if (mode == null)
            {
                // Default is status and value change triggering
                return UaDataChangeTrigger.StatusValue;
            }
            switch (mode.Value)
            {
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
        /// Convert to stack type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static UaFilterOperator ToStackType(this FilterOperatorType type)
        {
            switch (type)
            {
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
        /// <exception cref="NotSupportedException"></exception>
        public static FilterOperatorType ToServiceType(this UaFilterOperator type)
        {
            switch (type)
            {
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
        /// To service type
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static ExceptionDeviationType? ToExceptionDeviationType(
            this UaExceptionDeviationFormat? format)
        {
            switch (format)
            {
                case UaExceptionDeviationFormat.AbsoluteValue:
                    return ExceptionDeviationType.AbsoluteValue;
                case UaExceptionDeviationFormat.PercentOfValue:
                    return ExceptionDeviationType.PercentOfValue;
                case UaExceptionDeviationFormat.PercentOfRange:
                    return ExceptionDeviationType.PercentOfRange;
                case UaExceptionDeviationFormat.PercentOfEURange:
                    return ExceptionDeviationType.PercentOfEURange;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Convert data location
        /// </summary>
        /// <param name="aggregateBits"></param>
        /// <returns></returns>
        public static DataLocation? ToDataLocation(this UaAggregateBits aggregateBits)
        {
            if ((aggregateBits & UaAggregateBits.Calculated) != 0)
            {
                return DataLocation.Calculated;
            }
            else if ((aggregateBits & UaAggregateBits.Interpolated) != 0)
            {
                return DataLocation.Interpolated;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Convert additional data
        /// </summary>
        /// <param name="aggregateBits"></param>
        /// <returns></returns>
        public static AdditionalData? ToAdditionalData(this UaAggregateBits aggregateBits)
        {
            AdditionalData result = 0;
            if ((aggregateBits & UaAggregateBits.ExtraData) != 0)
            {
                result |= AdditionalData.ExtraData;
            }
            if ((aggregateBits & UaAggregateBits.MultipleValues) != 0)
            {
                result |= AdditionalData.MultipleValues;
            }
            if ((aggregateBits & UaAggregateBits.Partial) != 0)
            {
                result |= AdditionalData.Partial;
            }
            if (result == 0)
            {
                return null;
            }
            return result;
        }

        /// <summary>
        /// Get network message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static uint ToStackType(this NetworkMessageContentMask? mask, MessageEncoding? encoding)
        {
            mask ??=
                    NetworkMessageContentMask.NetworkMessageHeader |
                    NetworkMessageContentMask.NetworkMessageNumber |
                    NetworkMessageContentMask.DataSetMessageHeader |
                    NetworkMessageContentMask.PublisherId |
                    NetworkMessageContentMask.DataSetClassId;
            switch (encoding)
            {
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
            DataSetFieldContentMask? fieldMask, MessageEncoding? encoding)
        {
            mask ??=
                    DataSetContentMask.DataSetWriterId |
                    DataSetContentMask.DataSetWriterName |
                    DataSetContentMask.MetaDataVersion |
                    DataSetContentMask.MajorVersion |
                    DataSetContentMask.MinorVersion |
                    DataSetContentMask.SequenceNumber |
                    DataSetContentMask.Timestamp |
                    DataSetContentMask.MessageType |
                    DataSetContentMask.Status;
            switch (encoding)
            {
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
        private static JsonNetworkMessageContentMask ToJsonStackType(this NetworkMessageContentMask mask)
        {
            var result = JsonNetworkMessageContentMask.None;
            if ((mask & NetworkMessageContentMask.PublisherId) != 0)
            {
                result |= JsonNetworkMessageContentMask.PublisherId;
            }
            if ((mask & NetworkMessageContentMask.DataSetClassId) != 0)
            {
                result |= JsonNetworkMessageContentMask.DataSetClassId;
            }
            if ((mask & NetworkMessageContentMask.ReplyTo) != 0)
            {
                result |= JsonNetworkMessageContentMask.ReplyTo;
            }
            if ((mask & NetworkMessageContentMask.NetworkMessageHeader) != 0)
            {
                result |= JsonNetworkMessageContentMask.NetworkMessageHeader;
            }
            else
            {
                // If not set, bits 3, 4 and 5 can also not be set
                result = JsonNetworkMessageContentMask.None;
            }
            if ((mask & NetworkMessageContentMask.MonitoredItemMessage) != 0)
            {
                // If monitored item message, then no network message header
                result = JsonNetworkMessageContentMask.None;
            }
            if ((mask & NetworkMessageContentMask.DataSetMessageHeader) != 0)
            {
                result |= JsonNetworkMessageContentMask.DataSetMessageHeader;
            }
            if ((mask & NetworkMessageContentMask.SingleDataSetMessage) != 0)
            {
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
            DataSetFieldContentMask? fieldMask)
        {
            var result = JsonDataSetMessageContentMask.None;
            if ((mask & DataSetContentMask.Timestamp) != 0)
            {
                result |= JsonDataSetMessageContentMask.Timestamp;
            }
            if ((mask & DataSetContentMask.Status) != 0)
            {
                result |= JsonDataSetMessageContentMask.Status;
            }
            if ((mask & DataSetContentMask.MetaDataVersion) != 0)
            {
                result |= JsonDataSetMessageContentMask.MetaDataVersion;
            }
            if ((mask & DataSetContentMask.SequenceNumber) != 0)
            {
                result |= JsonDataSetMessageContentMask.SequenceNumber;
            }
            if ((mask & DataSetContentMask.DataSetWriterId) != 0)
            {
                result |= JsonDataSetMessageContentMask.DataSetWriterId;
            }
            if ((mask & DataSetContentMask.MessageType) != 0)
            {
                result |= JsonDataSetMessageContentMask.MessageType;
            }
            if ((mask & DataSetContentMask.DataSetWriterName) != 0)
            {
                result |= JsonDataSetMessageContentMask2.DataSetWriterName;
            }
            if ((mask & DataSetContentMask.ReversibleFieldEncoding) != 0)
            {
                result |= JsonDataSetMessageContentMask2.ReversibleFieldEncoding;
            }

            if (fieldMask != null)
            {
                if ((fieldMask & DataSetFieldContentMask.NodeId) != 0)
                {
                    result |= JsonDataSetMessageContentMaskEx.NodeId;
                }
                if ((fieldMask & DataSetFieldContentMask.DisplayName) != 0)
                {
                    result |= JsonDataSetMessageContentMaskEx.DisplayName;
                }
                if ((fieldMask & DataSetFieldContentMask.ExtensionFields) != 0)
                {
                    result |= JsonDataSetMessageContentMaskEx.ExtensionFields;
                }
                if ((fieldMask & DataSetFieldContentMask.EndpointUrl) != 0)
                {
                    result |= JsonDataSetMessageContentMaskEx.EndpointUrl;
                }
                if ((fieldMask & DataSetFieldContentMask.ApplicationUri) != 0)
                {
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
        private static UadpNetworkMessageContentMask ToUadpStackType(this NetworkMessageContentMask mask)
        {
            var result = UadpNetworkMessageContentMask.None;
            if ((mask & NetworkMessageContentMask.PublisherId) != 0)
            {
                result |= UadpNetworkMessageContentMask.PublisherId;
            }
            if ((mask & NetworkMessageContentMask.GroupHeader) != 0)
            {
                result |= UadpNetworkMessageContentMask.GroupHeader;
            }
            if ((mask & NetworkMessageContentMask.WriterGroupId) != 0)
            {
                result |= UadpNetworkMessageContentMask.WriterGroupId;
            }
            if ((mask & NetworkMessageContentMask.GroupVersion) != 0)
            {
                result |= UadpNetworkMessageContentMask.GroupVersion;
            }
            if ((mask & NetworkMessageContentMask.NetworkMessageNumber) != 0)
            {
                result |= UadpNetworkMessageContentMask.NetworkMessageNumber;
            }
            if ((mask & NetworkMessageContentMask.SequenceNumber) != 0)
            {
                result |= UadpNetworkMessageContentMask.SequenceNumber;
            }
            if ((mask & NetworkMessageContentMask.PayloadHeader) != 0)
            {
                result |= UadpNetworkMessageContentMask.PayloadHeader;
            }
            if ((mask & NetworkMessageContentMask.Timestamp) != 0)
            {
                result |= UadpNetworkMessageContentMask.Timestamp;
            }
            if ((mask & NetworkMessageContentMask.Picoseconds) != 0)
            {
                result |= UadpNetworkMessageContentMask.PicoSeconds;
            }
            if ((mask & NetworkMessageContentMask.DataSetClassId) != 0)
            {
                result |= UadpNetworkMessageContentMask.DataSetClassId;
            }
            if ((mask & NetworkMessageContentMask.PromotedFields) != 0)
            {
                result |= UadpNetworkMessageContentMask.PromotedFields;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static UadpDataSetMessageContentMask ToUadpStackType(this DataSetContentMask mask)
        {
            var result = UadpDataSetMessageContentMask.None;
            if ((mask & DataSetContentMask.Timestamp) != 0)
            {
                result |= UadpDataSetMessageContentMask.Timestamp;
            }
            if ((mask & DataSetContentMask.PicoSeconds) != 0)
            {
                result |= UadpDataSetMessageContentMask.PicoSeconds;
            }
            if ((mask & DataSetContentMask.Status) != 0)
            {
                result |= UadpDataSetMessageContentMask.Status;
            }
            if ((mask & DataSetContentMask.SequenceNumber) != 0)
            {
                result |= UadpDataSetMessageContentMask.SequenceNumber;
            }
            if ((mask & DataSetContentMask.MinorVersion) != 0)
            {
                result |= UadpDataSetMessageContentMask.MinorVersion;
            }
            if ((mask & DataSetContentMask.MajorVersion) != 0)
            {
                result |= UadpDataSetMessageContentMask.MajorVersion;
            }
            return result;
        }

        /// <summary>
        /// Get dataset message content mask
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        public static UaDataSetFieldContentMask ToStackType(this DataSetFieldContentMask? mask)
        {
            mask ??=
                    DataSetFieldContentMask.StatusCode |
                    DataSetFieldContentMask.SourceTimestamp |
                    DataSetFieldContentMask.SourcePicoSeconds |
                    DataSetFieldContentMask.ServerPicoSeconds |
                    DataSetFieldContentMask.ServerTimestamp;
            var result = UaDataSetFieldContentMask.None;
            if ((mask & DataSetFieldContentMask.StatusCode) != 0)
            {
                result |= UaDataSetFieldContentMask.StatusCode;
            }
            if ((mask & DataSetFieldContentMask.SourceTimestamp) != 0)
            {
                result |= UaDataSetFieldContentMask.SourceTimestamp;
            }
            if ((mask & DataSetFieldContentMask.ServerTimestamp) != 0)
            {
                result |= UaDataSetFieldContentMask.ServerTimestamp;
            }
            if ((mask & DataSetFieldContentMask.SourcePicoSeconds) != 0)
            {
                result |= UaDataSetFieldContentMask.SourcePicoSeconds;
            }
            if ((mask & DataSetFieldContentMask.ServerPicoSeconds) != 0)
            {
                result |= UaDataSetFieldContentMask.ServerPicoSeconds;
            }
            if ((mask & DataSetFieldContentMask.RawData) != 0)
            {
                result |= UaDataSetFieldContentMask.RawData;
            }
            return result;
        }
    }
}
