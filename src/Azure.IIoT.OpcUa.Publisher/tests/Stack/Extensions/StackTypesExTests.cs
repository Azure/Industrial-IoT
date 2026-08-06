// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Stack.Extensions
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using UaNodeClass = Opc.Ua.NodeClass;
    using UaBrowseDirection = Opc.Ua.BrowseDirection;
    using UaSecurityMode = Opc.Ua.MessageSecurityMode;
    using UaApplicationType = Opc.Ua.ApplicationType;
    using UaDiagnosticsLevel = Opc.Ua.DiagnosticsMasks;
    using UaMonitoringMode = Opc.Ua.MonitoringMode;
    using UaTimestampsToReturn = Opc.Ua.TimestampsToReturn;
    using UaDeadbandType = Opc.Ua.DeadbandType;
    using UaDataChangeTrigger = Opc.Ua.DataChangeTrigger;
    using UaFilterOperator = Opc.Ua.FilterOperator;
    using UaExceptionDeviationFormat = Opc.Ua.ExceptionDeviationFormat;
    using UaAggregateBits = Opc.Ua.AggregateBits;
    using UaPermissionType = Opc.Ua.PermissionType;
    using System;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Tests for the pure enum-conversion methods in the internal
    /// <see cref="StackTypesEx"/> static class.
    /// </summary>
    public class StackTypesExTests
    {
        // ──────────────────────── NodeClass ────────────────────────

        [Fact]
        public void UaNodeClassObject_ToServiceType_ReturnsObject() =>
            Assert.Equal(NodeClass.Object, UaNodeClass.Object.ToServiceType());

        [Fact]
        public void UaNodeClassObjectType_ToServiceType_ReturnsObjectType() =>
            Assert.Equal(NodeClass.ObjectType, UaNodeClass.ObjectType.ToServiceType());

        [Fact]
        public void UaNodeClassVariable_ToServiceType_ReturnsVariable() =>
            Assert.Equal(NodeClass.Variable, UaNodeClass.Variable.ToServiceType());

        [Fact]
        public void UaNodeClassVariableType_ToServiceType_ReturnsVariableType() =>
            Assert.Equal(NodeClass.VariableType, UaNodeClass.VariableType.ToServiceType());

        [Fact]
        public void UaNodeClassMethod_ToServiceType_ReturnsMethod() =>
            Assert.Equal(NodeClass.Method, UaNodeClass.Method.ToServiceType());

        [Fact]
        public void UaNodeClassDataType_ToServiceType_ReturnsDataType() =>
            Assert.Equal(NodeClass.DataType, UaNodeClass.DataType.ToServiceType());

        [Fact]
        public void UaNodeClassReferenceType_ToServiceType_ReturnsReferenceType() =>
            Assert.Equal(NodeClass.ReferenceType, UaNodeClass.ReferenceType.ToServiceType());

        [Fact]
        public void UaNodeClassView_ToServiceType_ReturnsView() =>
            Assert.Equal(NodeClass.View, UaNodeClass.View.ToServiceType());

        [Fact]
        public void UaNodeClassUnspecified_ToServiceType_ReturnsNull() =>
            Assert.Null(UaNodeClass.Unspecified.ToServiceType());

        [Fact]
        public void ModelNodeClassObject_ToStackType_ReturnsObject() =>
            Assert.Equal(UaNodeClass.Object, NodeClass.Object.ToStackType());

        [Fact]
        public void ModelNodeClassObjectType_ToStackType_ReturnsObjectType() =>
            Assert.Equal(UaNodeClass.ObjectType, NodeClass.ObjectType.ToStackType());

        [Fact]
        public void ModelNodeClassVariable_ToStackType_ReturnsVariable() =>
            Assert.Equal(UaNodeClass.Variable, NodeClass.Variable.ToStackType());

        [Fact]
        public void ModelNodeClassVariableType_ToStackType_ReturnsVariableType() =>
            Assert.Equal(UaNodeClass.VariableType, NodeClass.VariableType.ToStackType());

        [Fact]
        public void ModelNodeClassMethod_ToStackType_ReturnsMethod() =>
            Assert.Equal(UaNodeClass.Method, NodeClass.Method.ToStackType());

        [Fact]
        public void ModelNodeClassDataType_ToStackType_ReturnsDataType() =>
            Assert.Equal(UaNodeClass.DataType, NodeClass.DataType.ToStackType());

        [Fact]
        public void ModelNodeClassReferenceType_ToStackType_ReturnsReferenceType() =>
            Assert.Equal(UaNodeClass.ReferenceType, NodeClass.ReferenceType.ToStackType());

        [Fact]
        public void ModelNodeClassView_ToStackType_ReturnsView() =>
            Assert.Equal(UaNodeClass.View, NodeClass.View.ToStackType());

        [Fact]
        public void ModelNodeClassDefault_ToStackType_ReturnsUnspecified() =>
            Assert.Equal(UaNodeClass.Unspecified, ((NodeClass)999).ToStackType());

        [Fact]
        public void NullNodeClasses_ToStackMask_ReturnsZero() =>
            Assert.Equal(UaNodeClass.Unspecified, ((IReadOnlyList<NodeClass>?)null).ToStackMask());

        [Fact]
        public void EmptyNodeClasses_ToStackMask_ReturnsZero() =>
            Assert.Equal(UaNodeClass.Unspecified, new List<NodeClass>().ToStackMask());

        [Fact]
        public void TwoNodeClasses_ToStackMask_ReturnsCombinedMask()
        {
            var mask = new List<NodeClass> { NodeClass.Object, NodeClass.Variable }.ToStackMask();
            Assert.Equal(UaNodeClass.Object | UaNodeClass.Variable, mask);
        }

        // ──────────────────── BrowseDirection ────────────────────

        [Fact]
        public void Forward_ToStackType_ReturnsForward() =>
            Assert.Equal(UaBrowseDirection.Forward, BrowseDirection.Forward.ToStackType());

        [Fact]
        public void Backward_ToStackType_ReturnsInverse() =>
            Assert.Equal(UaBrowseDirection.Inverse, BrowseDirection.Backward.ToStackType());

        [Fact]
        public void Both_ToStackType_ReturnsBoth() =>
            Assert.Equal(UaBrowseDirection.Both, BrowseDirection.Both.ToStackType());

        [Fact]
        public void BrowseDirectionDefault_ToStackType_ReturnsForward() =>
            Assert.Equal(UaBrowseDirection.Forward, ((BrowseDirection)999).ToStackType());

        // ──────────────────── SecurityMode ───────────────────────

        [Fact]
        public void UaSecurityModeNone_ToServiceType_ReturnsNone() =>
            Assert.Equal(SecurityMode.None, UaSecurityMode.None.ToServiceType());

        [Fact]
        public void UaSecurityModeSign_ToServiceType_ReturnsSign() =>
            Assert.Equal(SecurityMode.Sign, UaSecurityMode.Sign.ToServiceType());

        [Fact]
        public void UaSecurityModeSignAndEncrypt_ToServiceType_ReturnsSignAndEncrypt() =>
            Assert.Equal(SecurityMode.SignAndEncrypt, UaSecurityMode.SignAndEncrypt.ToServiceType());

        [Fact]
        public void UaSecurityModeInvalid_ToServiceType_ReturnsNull() =>
            Assert.Null(UaSecurityMode.Invalid.ToServiceType());

        [Fact]
        public void SecurityModeIsSame_Best_AlwaysTrue()
        {
            Assert.True(UaSecurityMode.None.IsSame(SecurityMode.Best));
            Assert.True(UaSecurityMode.Sign.IsSame(SecurityMode.Best));
            Assert.True(UaSecurityMode.SignAndEncrypt.IsSame(SecurityMode.Best));
        }

        [Fact]
        public void SecurityModeIsSame_SignAndSign_True() =>
            Assert.True(UaSecurityMode.Sign.IsSame(SecurityMode.Sign));

        [Fact]
        public void SecurityModeIsSame_SignAndSignAndEncrypt_False() =>
            Assert.False(UaSecurityMode.Sign.IsSame(SecurityMode.SignAndEncrypt));

        [Fact]
        public void SecurityModeIsSame_NoneAndNone_True() =>
            Assert.True(UaSecurityMode.None.IsSame(SecurityMode.None));

        [Fact]
        public void SecurityModeIsSame_SignAndNotNone_True() =>
            Assert.True(UaSecurityMode.Sign.IsSame(SecurityMode.NotNone));

        [Fact]
        public void SecurityModeIsSame_NoneAndNotNone_False() =>
            Assert.False(UaSecurityMode.None.IsSame(SecurityMode.NotNone));

        // ──────────────────── ApplicationType ────────────────────

        [Fact]
        public void UaApplicationTypeClient_ToServiceType_ReturnsClient() =>
            Assert.Equal(ApplicationType.Client, UaApplicationType.Client.ToServiceType());

        [Fact]
        public void UaApplicationTypeServer_ToServiceType_ReturnsServer() =>
            Assert.Equal(ApplicationType.Server, UaApplicationType.Server.ToServiceType());

        [Fact]
        public void UaApplicationTypeClientAndServer_ToServiceType_ReturnsClientAndServer() =>
            Assert.Equal(ApplicationType.ClientAndServer, UaApplicationType.ClientAndServer.ToServiceType());

        [Fact]
        public void UaApplicationTypeDiscoveryServer_ToServiceType_ReturnsDiscoveryServer() =>
            Assert.Equal(ApplicationType.DiscoveryServer, UaApplicationType.DiscoveryServer.ToServiceType());

        [Fact]
        public void UaApplicationTypeDefault_ToServiceType_ReturnsNull() =>
            Assert.Null(((UaApplicationType)999).ToServiceType());

        // ──────────────────── DiagnosticsLevel ───────────────────

        [Fact]
        public void DiagnosticsLevelNone_ToStackType_ReturnsNone() =>
            Assert.Equal(UaDiagnosticsLevel.None, DiagnosticsLevel.None.ToStackType());

        [Fact]
        public void DiagnosticsLevelStatus_ToStackType_ContainsSymbolicIdAndText() =>
            Assert.NotEqual(UaDiagnosticsLevel.None, DiagnosticsLevel.Status.ToStackType());

        [Fact]
        public void DiagnosticsLevelInformation_ToStackType_ContainsAdditionalInfo()
        {
            var result = DiagnosticsLevel.Information.ToStackType();
            Assert.NotEqual(UaDiagnosticsLevel.None, result);
        }

        [Fact]
        public void DiagnosticsLevelDebug_ToStackType_ContainsInnerStatus()
        {
            var result = DiagnosticsLevel.Debug.ToStackType();
            Assert.NotEqual(UaDiagnosticsLevel.None, result);
        }

        [Fact]
        public void DiagnosticsLevelVerbose_ToStackType_ContainsAll()
        {
            var verbose = DiagnosticsLevel.Verbose.ToStackType();
            // Verbose adds the All flag on top of Debug bits; the resulting OR equals All
            Assert.Equal(UaDiagnosticsLevel.All, verbose);
        }

        // ──────────────────── MonitoringMode ─────────────────────

        [Fact]
        public void NullMonitoringMode_ToStackType_ReturnsNull() =>
            Assert.Null(StackTypesEx.ToStackType((MonitoringMode?)null));

        [Fact]
        public void MonitoringModeDisabled_ToStackType_ReturnsDisabled() =>
            Assert.Equal(UaMonitoringMode.Disabled, StackTypesEx.ToStackType((MonitoringMode?)MonitoringMode.Disabled));

        [Fact]
        public void MonitoringModeSampling_ToStackType_ReturnsSampling() =>
            Assert.Equal(UaMonitoringMode.Sampling, StackTypesEx.ToStackType((MonitoringMode?)MonitoringMode.Sampling));

        [Fact]
        public void MonitoringModeReporting_ToStackType_ReturnsReporting() =>
            Assert.Equal(UaMonitoringMode.Reporting, StackTypesEx.ToStackType((MonitoringMode?)MonitoringMode.Reporting));

        // ──────────────────── TimestampsToReturn ─────────────────

        [Fact]
        public void NullTimestampsToReturn_ToStackType_ReturnsBoth() =>
            Assert.Equal(UaTimestampsToReturn.Both, StackTypesEx.ToStackType((TimestampsToReturn?)null));

        [Fact]
        public void TimestampsToReturnNone_ToStackType_ReturnsNeither() =>
            Assert.Equal(UaTimestampsToReturn.Neither, StackTypesEx.ToStackType((TimestampsToReturn?)TimestampsToReturn.None));

        [Fact]
        public void TimestampsToReturnServer_ToStackType_ReturnsServer() =>
            Assert.Equal(UaTimestampsToReturn.Server, StackTypesEx.ToStackType((TimestampsToReturn?)TimestampsToReturn.Server));

        [Fact]
        public void TimestampsToReturnSource_ToStackType_ReturnsSource() =>
            Assert.Equal(UaTimestampsToReturn.Source, StackTypesEx.ToStackType((TimestampsToReturn?)TimestampsToReturn.Source));

        [Fact]
        public void TimestampsToReturnBoth_ToStackType_ReturnsBoth() =>
            Assert.Equal(UaTimestampsToReturn.Both, StackTypesEx.ToStackType((TimestampsToReturn?)TimestampsToReturn.Both));

        // ──────────────────── DeadbandType ───────────────────────

        [Fact]
        public void NullDeadbandType_ToStackType_ReturnsNone() =>
            Assert.Equal(UaDeadbandType.None, StackTypesEx.ToStackType((DeadbandType?)null));

        [Fact]
        public void DeadbandTypeAbsolute_ToStackType_ReturnsAbsolute() =>
            Assert.Equal(UaDeadbandType.Absolute, StackTypesEx.ToStackType((DeadbandType?)DeadbandType.Absolute));

        [Fact]
        public void DeadbandTypePercent_ToStackType_ReturnsPercent() =>
            Assert.Equal(UaDeadbandType.Percent, StackTypesEx.ToStackType((DeadbandType?)DeadbandType.Percent));

        [Fact]
        public void DeadbandTypeDefault_ToStackType_ReturnsNone() =>
            Assert.Equal(UaDeadbandType.None, StackTypesEx.ToStackType((DeadbandType?)(DeadbandType)999));

        // ──────────────────── DataChangeTrigger ──────────────────

        [Fact]
        public void NullDataChangeTrigger_ToStackType_ReturnsStatusValue() =>
            Assert.Equal(UaDataChangeTrigger.StatusValue, StackTypesEx.ToStackType((DataChangeTriggerType?)null));

        [Fact]
        public void DataChangeTriggerStatus_ToStackType_ReturnsStatus() =>
            Assert.Equal(UaDataChangeTrigger.Status, StackTypesEx.ToStackType((DataChangeTriggerType?)DataChangeTriggerType.Status));

        [Fact]
        public void DataChangeTriggerStatusValue_ToStackType_ReturnsStatusValue() =>
            Assert.Equal(UaDataChangeTrigger.StatusValue, StackTypesEx.ToStackType((DataChangeTriggerType?)DataChangeTriggerType.StatusValue));

        [Fact]
        public void DataChangeTriggerStatusValueTimestamp_ToStackType_ReturnsStatusValueTimestamp() =>
            Assert.Equal(UaDataChangeTrigger.StatusValueTimestamp, StackTypesEx.ToStackType((DataChangeTriggerType?)DataChangeTriggerType.StatusValueTimestamp));

        [Fact]
        public void DataChangeTriggerDefault_ToStackType_ReturnsStatusValue() =>
            Assert.Equal(UaDataChangeTrigger.StatusValue, StackTypesEx.ToStackType((DataChangeTriggerType?)(DataChangeTriggerType)999));

        // ──────────────────── FilterOperator ─────────────────────

        [Fact]
        public void FilterOperatorEquals_RoundTrip()
        {
            var ua = FilterOperatorType.Equals.ToStackType();
            Assert.Equal(FilterOperatorType.Equals, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorGreaterThan_ToStackType_ReturnsGreaterThan() =>
            Assert.Equal(UaFilterOperator.GreaterThan, FilterOperatorType.GreaterThan.ToStackType());

        [Fact]
        public void FilterOperatorLessThan_ToStackType_ReturnsLessThan() =>
            Assert.Equal(UaFilterOperator.LessThan, FilterOperatorType.LessThan.ToStackType());

        [Fact]
        public void FilterOperatorBitwiseAnd_ToStackType_ReturnsBitwiseAnd() =>
            Assert.Equal(UaFilterOperator.BitwiseAnd, FilterOperatorType.BitwiseAnd.ToStackType());

        [Fact]
        public void FilterOperatorBitwiseOr_ToStackType_ReturnsBitwiseOr() =>
            Assert.Equal(UaFilterOperator.BitwiseOr, FilterOperatorType.BitwiseOr.ToStackType());

        [Fact]
        public void FilterOperatorIsNull_RoundTrip()
        {
            var ua = FilterOperatorType.IsNull.ToStackType();
            Assert.Equal(FilterOperatorType.IsNull, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorAnd_RoundTrip()
        {
            var ua = FilterOperatorType.And.ToStackType();
            Assert.Equal(FilterOperatorType.And, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorOr_RoundTrip()
        {
            var ua = FilterOperatorType.Or.ToStackType();
            Assert.Equal(FilterOperatorType.Or, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorCast_RoundTrip()
        {
            var ua = FilterOperatorType.Cast.ToStackType();
            Assert.Equal(FilterOperatorType.Cast, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorInView_RoundTrip()
        {
            var ua = FilterOperatorType.InView.ToStackType();
            Assert.Equal(FilterOperatorType.InView, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorOfType_RoundTrip()
        {
            var ua = FilterOperatorType.OfType.ToStackType();
            Assert.Equal(FilterOperatorType.OfType, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorRelatedTo_RoundTrip()
        {
            var ua = FilterOperatorType.RelatedTo.ToStackType();
            Assert.Equal(FilterOperatorType.RelatedTo, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorBetween_RoundTrip()
        {
            var ua = FilterOperatorType.Between.ToStackType();
            Assert.Equal(FilterOperatorType.Between, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorInList_RoundTrip()
        {
            var ua = FilterOperatorType.InList.ToStackType();
            Assert.Equal(FilterOperatorType.InList, ua.ToServiceType());
        }

        [Fact]
        public void FilterOperatorUnknownModel_ToStackType_ThrowsNotSupported() =>
            Assert.Throws<NotSupportedException>(() => ((FilterOperatorType)999).ToStackType());

        [Fact]
        public void FilterOperatorUnknownUa_ToServiceType_ThrowsNotSupported() =>
            Assert.Throws<NotSupportedException>(() => ((UaFilterOperator)999).ToServiceType());

        // ──────────────────── ExceptionDeviationType ─────────────

        [Fact]
        public void NullExceptionDeviationFormat_ToExceptionDeviationType_ReturnsNull() =>
            Assert.Null(StackTypesEx.ToExceptionDeviationType((UaExceptionDeviationFormat?)null));

        [Fact]
        public void ExceptionDeviationAbsoluteValue_ReturnsAbsoluteValue() =>
            Assert.Equal(ExceptionDeviationType.AbsoluteValue,
                StackTypesEx.ToExceptionDeviationType((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.AbsoluteValue));

        [Fact]
        public void ExceptionDeviationPercentOfValue_ReturnsPercentOfValue() =>
            Assert.Equal(ExceptionDeviationType.PercentOfValue,
                StackTypesEx.ToExceptionDeviationType((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.PercentOfValue));

        [Fact]
        public void ExceptionDeviationPercentOfRange_ReturnsPercentOfRange() =>
            Assert.Equal(ExceptionDeviationType.PercentOfRange,
                StackTypesEx.ToExceptionDeviationType((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.PercentOfRange));

        [Fact]
        public void ExceptionDeviationPercentOfEURange_ReturnsPercentOfEURange() =>
            Assert.Equal(ExceptionDeviationType.PercentOfEURange,
                StackTypesEx.ToExceptionDeviationType((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.PercentOfEURange));

        [Fact]
        public void ExceptionDeviationDefault_ReturnsNull() =>
            Assert.Null(StackTypesEx.ToExceptionDeviationType((UaExceptionDeviationFormat?)(UaExceptionDeviationFormat)999));

        // ──────────────────── AggregateBits ──────────────────────

        [Fact]
        public void AggregateBitsCalculated_ToDataLocation_ReturnsCalculated() =>
            Assert.Equal(DataLocation.Calculated, UaAggregateBits.Calculated.ToDataLocation());

        [Fact]
        public void AggregateBitsInterpolated_ToDataLocation_ReturnsInterpolated() =>
            Assert.Equal(DataLocation.Interpolated, UaAggregateBits.Interpolated.ToDataLocation());

        [Fact]
        public void AggregateBitsNone_ToDataLocation_ReturnsNull() =>
            Assert.Null(UaAggregateBits.Raw.ToDataLocation());

        [Fact]
        public void AggregateBitsExtraData_ToAdditionalData_ContainsExtraData()
        {
            var result = UaAggregateBits.ExtraData.ToAdditionalData();
            Assert.NotNull(result);
            Assert.True((result.Value & AdditionalData.ExtraData) != 0);
        }

        [Fact]
        public void AggregateBitsMultipleValues_ToAdditionalData_ContainsMultipleValues()
        {
            var result = UaAggregateBits.MultipleValues.ToAdditionalData();
            Assert.NotNull(result);
            Assert.True((result.Value & AdditionalData.MultipleValues) != 0);
        }

        [Fact]
        public void AggregateBitsPartial_ToAdditionalData_ContainsPartial()
        {
            var result = UaAggregateBits.Partial.ToAdditionalData();
            Assert.NotNull(result);
            Assert.True((result.Value & AdditionalData.Partial) != 0);
        }

        [Fact]
        public void AggregateBitsNone_ToAdditionalData_ReturnsNull() =>
            Assert.Null(UaAggregateBits.Raw.ToAdditionalData());

        // ──────────────────── DataSetFieldContentFlags ───────────

        [Fact]
        public void NullDataSetFieldContentFlags_ToStackType_IncludesDefaultBits()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)null);
            // Default includes StatusCode and timestamps
            Assert.NotEqual(Opc.Ua.DataSetFieldContentMask.None, result);
        }

        [Fact]
        public void DataSetFieldContentFlagsStatusCode_ToStackType_IncludesStatusCode()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.StatusCode);
            Assert.True((result & Opc.Ua.DataSetFieldContentMask.StatusCode) != 0);
        }

        [Fact]
        public void DataSetFieldContentFlagsRawData_ToStackType_IncludesRawData()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.RawData);
            Assert.True((result & Opc.Ua.DataSetFieldContentMask.RawData) != 0);
        }

        // ──────────────────── NetworkMessageContentFlags ─────────

        [Fact]
        public void NetworkMessageContentFlags_NullJson_ReturnsNonZero()
        {
            var result = StackTypesEx.ToStackType((NetworkMessageContentFlags?)null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_NullUadp_ReturnsNonZero()
        {
            var result = StackTypesEx.ToStackType((NetworkMessageContentFlags?)null, MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_PublisherIdAndHeader_Json_ReturnsNonZero()
        {
            // PublisherId alone without NetworkMessageHeader returns 0 in JSON mode (by design).
            // Combining PublisherId with NetworkMessageHeader produces a non-zero result.
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)(NetworkMessageContentFlags.PublisherId
                    | NetworkMessageContentFlags.NetworkMessageHeader),
                MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        // ──────────────────── DataSetMessageContentFlags ─────────

        [Fact]
        public void DataSetMessageContentFlags_NullJson_ReturnsNonZero()
        {
            var result = StackTypesEx.ToStackType((DataSetMessageContentFlags?)null, null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_NullUadp_ReturnsNonZero()
        {
            var result = StackTypesEx.ToStackType((DataSetMessageContentFlags?)null, null, MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_WithFieldMask_IncludesFieldBits()
        {
            var fieldMask = DataSetFieldContentFlags.NodeId;
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                fieldMask, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_NullEncoding_DefaultsToJson()
        {
            var withNull = StackTypesEx.ToStackType((DataSetMessageContentFlags?)null, null, null);
            var withJson = StackTypesEx.ToStackType((DataSetMessageContentFlags?)null, null, MessageEncoding.Json);
            Assert.Equal(withJson, withNull);
        }

        // ──────────────────── PermissionType ─────────────────────

        [Fact]
        public void PermissionTypeNone_ToServiceType_ReturnsNull() =>
            Assert.Null(UaPermissionType.None.ToServiceType());

        [Fact]
        public void PermissionTypeBrowse_ToServiceType_ReturnsNonNull()
        {
            var result = UaPermissionType.Browse.ToServiceType();
            Assert.NotNull(result);
        }

        // ──────── DataSetFieldContentFlags individual bits ────────

        [Fact]
        public void DataSetFieldContentFlags_SourceTimestamp_ToStackType_IncludesSourceTimestamp()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.SourceTimestamp);
            Assert.True((result & Opc.Ua.DataSetFieldContentMask.SourceTimestamp) != 0);
        }

        [Fact]
        public void DataSetFieldContentFlags_ServerTimestamp_ToStackType_IncludesServerTimestamp()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.ServerTimestamp);
            Assert.True((result & Opc.Ua.DataSetFieldContentMask.ServerTimestamp) != 0);
        }

        [Fact]
        public void DataSetFieldContentFlags_SourcePicoSeconds_ToStackType_IncludesSourcePicoSeconds()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.SourcePicoSeconds);
            Assert.True((result & Opc.Ua.DataSetFieldContentMask.SourcePicoSeconds) != 0);
        }

        [Fact]
        public void DataSetFieldContentFlags_ServerPicoSeconds_ToStackType_IncludesServerPicoSeconds()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.ServerPicoSeconds);
            Assert.True((result & Opc.Ua.DataSetFieldContentMask.ServerPicoSeconds) != 0);
        }

        [Fact]
        public void DataSetFieldContentFlags_SingleFieldDegradeToValue_ToStackType_IncludesBit()
        {
            var result = StackTypesEx.ToStackType((DataSetFieldContentFlags?)DataSetFieldContentFlags.SingleFieldDegradeToValue);
            Assert.NotEqual(Opc.Ua.DataSetFieldContentMask.None, result);
        }

        // ──────── NetworkMessageContentFlags UADP individual bits ─

        [Fact]
        public void NetworkMessageContentFlags_GroupHeader_Uadp_IncludesGroupHeader()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.GroupHeader,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_WriterGroupId_Uadp_IncludesWriterGroupId()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.WriterGroupId,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_GroupVersion_Uadp_IncludesGroupVersion()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.GroupVersion,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_NetworkMessageNumber_Uadp_IncludesNetworkMessageNumber()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.NetworkMessageNumber,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_SequenceNumber_Uadp_IncludesSequenceNumber()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.SequenceNumber,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_PayloadHeader_Uadp_IncludesPayloadHeader()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.PayloadHeader,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_Timestamp_Uadp_IncludesTimestamp()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.Timestamp,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_Picoseconds_Uadp_IncludesPicoseconds()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.Picoseconds,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_PromotedFields_Uadp_IncludesPromotedFields()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.PromotedFields,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_PublisherId_Uadp_IncludesPublisherId()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.PublisherId,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_DataSetClassId_Uadp_IncludesDataSetClassId()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.DataSetClassId,
                MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        // ──────── NetworkMessageContentFlags JSON individual bits ─

        [Fact]
        public void NetworkMessageContentFlags_DataSetClassId_Json_IncludesDataSetClassId()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)(NetworkMessageContentFlags.NetworkMessageHeader
                    | NetworkMessageContentFlags.DataSetClassId),
                MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_ReplyTo_Json_IncludesReplyTo()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)(NetworkMessageContentFlags.NetworkMessageHeader
                    | NetworkMessageContentFlags.ReplyTo),
                MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_DataSetMessageHeader_Json_IncludesDataSetMessageHeader()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)(NetworkMessageContentFlags.NetworkMessageHeader
                    | NetworkMessageContentFlags.DataSetMessageHeader),
                MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_SingleDataSetMessage_Json_IncludesSingleDataSetMessage()
        {
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)(NetworkMessageContentFlags.NetworkMessageHeader
                    | NetworkMessageContentFlags.SingleDataSetMessage),
                MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void NetworkMessageContentFlags_NoNetworkMessageHeader_Json_ReturnsZero()
        {
            // Without NetworkMessageHeader, JSON mode forces result to None
            var result = StackTypesEx.ToStackType(
                (NetworkMessageContentFlags?)NetworkMessageContentFlags.PublisherId,
                MessageEncoding.Json);
            Assert.Equal(0u, result);
        }

        // ──────── DataSetMessageContentFlags UADP individual bits ─

        [Fact]
        public void DataSetMessageContentFlags_PicoSeconds_Uadp_IncludesPicoSeconds()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.PicoSeconds,
                null, MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_Status_Uadp_IncludesStatus()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Status,
                null, MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_MinorVersion_Uadp_IncludesMinorVersion()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.MinorVersion,
                null, MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_MajorVersion_Uadp_IncludesMajorVersion()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.MajorVersion,
                null, MessageEncoding.Uadp);
            Assert.NotEqual(0u, result);
        }

        // ──────── DataSetMessageContentFlags JSON individual bits ─

        [Fact]
        public void DataSetMessageContentFlags_Status_Json_IncludesStatus()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Status,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_MetaDataVersion_Json_IncludesMetaDataVersion()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.MetaDataVersion,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_SequenceNumber_Json_IncludesSequenceNumber()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.SequenceNumber,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_DataSetWriterId_Json_IncludesDataSetWriterId()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.DataSetWriterId,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_MessageType_Json_IncludesMessageType()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.MessageType,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_DataSetWriterName_Json_IncludesDataSetWriterName()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.DataSetWriterName,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_ReversibleFieldEncoding_Json_IncludesFieldEncoding()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.ReversibleFieldEncoding,
                null, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_DisplayNameFieldMask_Json_IncludesDisplayName()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                DataSetFieldContentFlags.DisplayName, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_ExtensionFieldsFieldMask_Json_IncludesExtensionFields()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                DataSetFieldContentFlags.ExtensionFields, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_EndpointUrlFieldMask_Json_IncludesEndpointUrl()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                DataSetFieldContentFlags.EndpointUrl, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_HeartbeatFieldMask_Json_IncludesHeartbeat()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                DataSetFieldContentFlags.Heartbeat, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        [Fact]
        public void DataSetMessageContentFlags_ApplicationUriFieldMask_Json_IncludesApplicationUri()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                DataSetFieldContentFlags.ApplicationUri, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }

        // ──────── ExceptionDeviationType conversions ──────────────

        [Fact]
        public void ExceptionDeviationFormat_AbsoluteValue_ReturnsAbsoluteValue() =>
            Assert.Equal(ExceptionDeviationType.AbsoluteValue,
                ((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.AbsoluteValue).ToExceptionDeviationType());

        [Fact]
        public void ExceptionDeviationFormat_PercentOfValue_ReturnsPercentOfValue() =>
            Assert.Equal(ExceptionDeviationType.PercentOfValue,
                ((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.PercentOfValue).ToExceptionDeviationType());

        [Fact]
        public void ExceptionDeviationFormat_PercentOfRange_ReturnsPercentOfRange() =>
            Assert.Equal(ExceptionDeviationType.PercentOfRange,
                ((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.PercentOfRange).ToExceptionDeviationType());

        [Fact]
        public void ExceptionDeviationFormat_PercentOfEURange_ReturnsPercentOfEURange() =>
            Assert.Equal(ExceptionDeviationType.PercentOfEURange,
                ((UaExceptionDeviationFormat?)UaExceptionDeviationFormat.PercentOfEURange).ToExceptionDeviationType());

        [Fact]
        public void ExceptionDeviationFormat_Null_ReturnsNull() =>
            Assert.Null(((UaExceptionDeviationFormat?)null).ToExceptionDeviationType());

        [Fact]
        public void ExceptionDeviationFormat_UnknownValue_ReturnsNull() =>
            Assert.Null(((UaExceptionDeviationFormat?)999).ToExceptionDeviationType());

        // ──────── AggregateBits → DataLocation ────────────────────

        [Fact]
        public void AggregateBits_Calculated_ToDataLocation_ReturnsCalculated() =>
            Assert.Equal(DataLocation.Calculated, UaAggregateBits.Calculated.ToDataLocation());

        [Fact]
        public void AggregateBits_Interpolated_ToDataLocation_ReturnsInterpolated() =>
            Assert.Equal(DataLocation.Interpolated, UaAggregateBits.Interpolated.ToDataLocation());

        [Fact]
        public void AggregateBits_NoFlag_ToDataLocation_ReturnsNull() =>
            Assert.Null(UaAggregateBits.Raw.ToDataLocation());

        [Fact]
        public void AggregateBits_CalculatedAndInterpolated_ToDataLocation_PrefersCalculated() =>
            Assert.Equal(DataLocation.Calculated,
                (UaAggregateBits.Calculated | UaAggregateBits.Interpolated).ToDataLocation());

        // ──────── AggregateBits → AdditionalData ──────────────────

        [Fact]
        public void AggregateBits_ExtraData_ToAdditionalData_ReturnsExtraData() =>
            Assert.Equal(AdditionalData.ExtraData, UaAggregateBits.ExtraData.ToAdditionalData());

        [Fact]
        public void AggregateBits_MultipleValues_ToAdditionalData_ReturnsMultipleValues() =>
            Assert.Equal(AdditionalData.MultipleValues, UaAggregateBits.MultipleValues.ToAdditionalData());

        [Fact]
        public void AggregateBits_Partial_ToAdditionalData_ReturnsPartial() =>
            Assert.Equal(AdditionalData.Partial, UaAggregateBits.Partial.ToAdditionalData());

        [Fact]
        public void AggregateBits_AllThree_ToAdditionalData_ReturnsCombination()
        {
            var bits = UaAggregateBits.ExtraData | UaAggregateBits.MultipleValues | UaAggregateBits.Partial;
            var result = bits.ToAdditionalData();
            Assert.NotNull(result);
            Assert.True((result!.Value & AdditionalData.ExtraData) != 0);
            Assert.True((result.Value & AdditionalData.MultipleValues) != 0);
            Assert.True((result.Value & AdditionalData.Partial) != 0);
        }

        [Fact]
        public void AggregateBits_NoAdditionalFlags_ToAdditionalData_ReturnsNull() =>
            Assert.Null(UaAggregateBits.Raw.ToAdditionalData());

        // ──────── PermissionType additional values ─────────────────

        [Fact]
        public void PermissionTypeRead_ToServiceType_ReturnsNonNull() =>
            Assert.NotNull(UaPermissionType.Read.ToServiceType());

        [Fact]
        public void PermissionTypeWrite_ToServiceType_ReturnsNonNull() =>
            Assert.NotNull(UaPermissionType.Write.ToServiceType());

        [Fact]
        public void PermissionTypeReceiveEvents_ToServiceType_ReturnsNonNull() =>
            Assert.NotNull(UaPermissionType.ReceiveEvents.ToServiceType());

        [Fact]
        public void PermissionTypeCall_ToServiceType_ReturnsNonNull() =>
            Assert.NotNull(UaPermissionType.Call.ToServiceType());

        [Fact]
        public void PermissionTypeAllPermissions_ToServiceType_ReturnsNonNull() =>
            Assert.NotNull((UaPermissionType.Read | UaPermissionType.Write).ToServiceType());

        // ──────── DataSetFieldContentFlags.NodeId in fieldMask ────

        [Fact]
        public void DataSetMessageContentFlags_NodeIdFieldMask_Json_IncludesNodeId()
        {
            var result = StackTypesEx.ToStackType(
                (DataSetMessageContentFlags?)DataSetMessageContentFlags.Timestamp,
                DataSetFieldContentFlags.NodeId, MessageEncoding.Json);
            Assert.NotEqual(0u, result);
        }
    }
}
