// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Auth.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Linq;

    /// <summary>
    /// Api model extensions
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static MonitoredItemMessageApiModel ToApiModel(
            this MonitoredItemMessageModel model) {
            return model == null
                ? null
                : new MonitoredItemMessageApiModel {
                    PublisherId = model.PublisherId,
                    DataSetWriterId = model.DataSetWriterId,
                    EndpointId = model.EndpointId,
                    NodeId = model.NodeId,
                    DisplayName = model.DisplayName,
                    ServerTimestamp = model.ServerTimestamp,
                    ServerPicoseconds = model.ServerPicoseconds,
                    SourceTimestamp = model.SourceTimestamp,
                    SourcePicoseconds = model.SourcePicoseconds,
                    Timestamp = model.Timestamp,
                    SequenceNumber = model.SequenceNumber,
                    Value = model.Value?.Copy(),
                    DataType = model.DataType,
                    Status = model.Status
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ConnectionApiModel ToApiModel(
            this ConnectionModel model) {
            return model == null
                ? null
                : new ConnectionApiModel {
                    Endpoint = model.Endpoint.ToApiModel(),
                    User = model.User.ToApiModel(),
                    Diagnostics = model.Diagnostics.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ConnectionModel ToServiceModel(
            this ConnectionApiModel model) {
            return model == null
                ? null
                : new ConnectionModel {
                    Endpoint = model.Endpoint.ToServiceModel(),
                    User = model.User.ToServiceModel(),
                    Diagnostics = model.Diagnostics.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ContentFilterApiModel ToApiModel(
            this ContentFilterModel model) {
            return model == null
                ? null
                : new ContentFilterApiModel {
                    Elements = model.Elements?
                    .Select(e => e.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ContentFilterModel ToServiceModel(
            this ContentFilterApiModel model) {
            return model == null
                ? null
                : new ContentFilterModel {
                    Elements = model.Elements?
                    .Select(e => e.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ContentFilterElementApiModel ToApiModel(
            this ContentFilterElementModel model) {
            return model == null
                ? null
                : new ContentFilterElementApiModel {
                    FilterOperands = model.FilterOperands?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                    FilterOperator = (FilterOperatorType)model.FilterOperator
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ContentFilterElementModel ToServiceModel(
            this ContentFilterElementApiModel model) {
            return model == null
                ? null
                : new ContentFilterElementModel {
                    FilterOperands = model.FilterOperands?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                    FilterOperator = (Core.Models.FilterOperatorType)model.FilterOperator
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static CredentialApiModel ToApiModel(
            this CredentialModel model) {
            return model == null
                ? null
                : new CredentialApiModel {
                    Value = model.Value,
                    Type = (CredentialType?)model.Type
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static CredentialModel ToServiceModel(
            this CredentialApiModel model) {
            return model == null
                ? null
                : new CredentialModel {
                    Value = model.Value,
                    Type = (Core.Models.CredentialType?)model.Type
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static DataSetMetaDataApiModel ToApiModel(
            this DataSetMetaDataModel model) {
            return model == null
                ? null
                : new DataSetMetaDataApiModel {
                    Name = model.Name,
                    DataSetClassId = model.DataSetClassId,
                    Description = model.Description
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DataSetMetaDataModel ToServiceModel(
            this DataSetMetaDataApiModel model) {
            return model == null
                ? null
                : new DataSetMetaDataModel {
                    Name = model.Name,
                    DataSetClassId = model.DataSetClassId,
                    Description = model.Description
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static DataSetWriterApiModel ToApiModel(
            this DataSetWriterModel model) {
            return model == null
                ? null
                : new DataSetWriterApiModel {
                    DataSetWriterName = model.DataSetWriterName,
                    DataSet = model.DataSet.ToApiModel(),
                    DataSetFieldContentMask = (IIoT.Api.Publisher.Models.DataSetFieldContentMask?)model.DataSetFieldContentMask,
                    MetaDataUpdateTime = model.MetaDataUpdateTime,
                    MetaDataQueueName = model.MetaDataQueueName,
                    KeyFrameCount = model.KeyFrameCount,
                    MessageSettings = model.MessageSettings.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DataSetWriterModel ToServiceModel(
            this DataSetWriterApiModel model) {
            return model == null
                ? null
                : new DataSetWriterModel {
                    DataSetWriterName = model.DataSetWriterName,
                    DataSet = model.DataSet.ToServiceModel(),
                    DataSetFieldContentMask = (OpcUa.Publisher.Models.DataSetFieldContentMask?)model.DataSetFieldContentMask,
                    MetaDataUpdateTime = model.MetaDataUpdateTime,
                    MetaDataQueueName = model.MetaDataQueueName,
                    KeyFrameCount = model.KeyFrameCount,
                    MessageSettings = model.MessageSettings.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static DataSetWriterMessageSettingsApiModel ToApiModel(
            this DataSetWriterMessageSettingsModel model) {
            return model == null
                ? null
                : new DataSetWriterMessageSettingsApiModel {
                    ConfiguredSize = model.ConfiguredSize,
                    DataSetMessageContentMask = (IIoT.Api.Publisher.Models.DataSetContentMask?)model.DataSetMessageContentMask,
                    DataSetOffset = model.DataSetOffset,
                    NetworkMessageNumber = model.NetworkMessageNumber
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DataSetWriterMessageSettingsModel ToServiceModel(
            this DataSetWriterMessageSettingsApiModel model) {
            return model == null
                ? null
                : new DataSetWriterMessageSettingsModel {
                    ConfiguredSize = model.ConfiguredSize,
                    DataSetMessageContentMask = (OpcUa.Publisher.Models.DataSetContentMask?)model.DataSetMessageContentMask,
                    DataSetOffset = model.DataSetOffset,
                    NetworkMessageNumber = model.NetworkMessageNumber
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static DiagnosticsApiModel ToApiModel(
            this DiagnosticsModel model) {
            return model == null
                ? null
                : new DiagnosticsApiModel {
                    AuditId = model.AuditId,
                    Level = (DiagnosticsLevel?)model.Level,
                    TimeStamp = model.TimeStamp
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DiagnosticsModel ToServiceModel(
            this DiagnosticsApiModel model) {
            return model == null
                ? null
                : new DiagnosticsModel {
                    AuditId = model.AuditId,
                    Level = (Core.Models.DiagnosticsLevel?)model.Level,
                    TimeStamp = model.TimeStamp
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EndpointApiModel ToApiModel(
            this EndpointModel model) {
            return model == null
                ? null
                : new EndpointApiModel {
                    Url = model.Url,
                    AlternativeUrls = model.AlternativeUrls,
                    Certificate = model.Certificate,
                    SecurityMode = (SecurityMode?)model.SecurityMode,
                    SecurityPolicy = model.SecurityPolicy
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EndpointModel ToServiceModel(
            this EndpointApiModel model) {
            return model == null
                ? null
                : new EndpointModel {
                    Url = model.Url,
                    AlternativeUrls = model.AlternativeUrls,
                    Certificate = model.Certificate,
                    SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode,
                    SecurityPolicy = model.SecurityPolicy
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EngineConfigurationApiModel ToApiModel(
            this EngineConfigurationModel model) {
            return model == null
                ? null
                : new EngineConfigurationApiModel {
                    BatchSize = model.BatchSize,
                    BatchTriggerInterval = model.BatchTriggerInterval,
                    MaxMessageSize = model.MaxMessageSize,
                    MaxOutgressMessages = model.MaxOutgressMessages,
                    UseStandardsCompliantEncoding = model.UseStandardsCompliantEncoding,
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EngineConfigurationModel ToServiceModel(
            this EngineConfigurationApiModel model) {
            return model == null
                ? null
                : new EngineConfigurationModel {
                    BatchSize = model.BatchSize,
                    BatchTriggerInterval = model.BatchTriggerInterval,
                    MaxMessageSize = model.MaxMessageSize,
                    MaxOutgressMessages = model.MaxOutgressMessages,
                    UseStandardsCompliantEncoding = model.UseStandardsCompliantEncoding,
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static FilterOperandApiModel ToApiModel(
            this FilterOperandModel model) {
            return model == null
                ? null
                : new FilterOperandApiModel {
                    Index = model.Index,
                    Alias = model.Alias,
                    Value = model.Value,
                    NodeId = model.NodeId,
                    AttributeId = (NodeAttribute?)model.AttributeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static FilterOperandModel ToServiceModel(
            this FilterOperandApiModel model) {
            return model == null
                ? null
                : new FilterOperandModel {
                    Index = model.Index,
                    Alias = model.Alias,
                    Value = model.Value,
                    NodeId = model.NodeId,
                    AttributeId = (Core.Models.NodeAttribute?)model.AttributeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange
                };
        }

        /// <summary>
        /// Create twin model
        /// </summary>
        /// <param name="model"></param>
        public static IdentityTokenApiModel ToApiModel(
            this IdentityTokenModel model) {
            return model == null
                ? null
                : new IdentityTokenApiModel {
                    Identity = model.Identity,
                    Key = model.Key,
                    Expires = model.Expires
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static IdentityTokenModel ToServiceModel(
            this IdentityTokenApiModel model) {
            return model == null
                ? null
                : new IdentityTokenModel {
                    Identity = model.Identity,
                    Key = model.Key,
                    Expires = model.Expires
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static LocalizedTextApiModel ToApiModel(
            this LocalizedTextModel model) {
            return model == null
                ? null
                : new LocalizedTextApiModel {
                    Locale = model.Locale,
                    Text = model.Text
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static LocalizedTextModel ToServiceModel(
            this LocalizedTextApiModel model) {
            return model == null
                ? null
                : new LocalizedTextModel {
                    Locale = model.Locale,
                    Text = model.Text
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataItemsApiModel ToApiModel(
            this PublishedDataItemsModel model) {
            return model == null
                ? null
                : new PublishedDataItemsApiModel {
                    PublishedData = model.PublishedData?
                    .Select(d => d.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedEventItemsApiModel ToApiModel(
            this PublishedEventItemsModel model) {
            return model == null
                ? null
                : new PublishedEventItemsApiModel {
                    PublishedData = model.PublishedData?
                    .Select(d => d.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataItemsModel ToServiceModel(
            this PublishedDataItemsApiModel model) {
            return model == null
                ? null
                : new PublishedDataItemsModel {
                    PublishedData = model.PublishedData?
                    .Select(d => d.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedEventItemsModel ToServiceModel(
            this PublishedEventItemsApiModel model) {
            return model == null
                ? null
                : new PublishedEventItemsModel {
                    PublishedData = model.PublishedData?
                    .Select(d => d.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataSetApiModel ToApiModel(
            this PublishedDataSetModel model) {
            return model == null
                ? null
                : new PublishedDataSetApiModel {
                    Name = model.Name,
                    DataSetSource = model.DataSetSource.ToApiModel(),
                    DataSetMetaData = model.DataSetMetaData.ToApiModel(),
                    ExtensionFields = model.ExtensionFields?
                    .ToDictionary(k => k.Key, v => v.Value)
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataSetModel ToServiceModel(
            this PublishedDataSetApiModel model) {
            return model == null
                ? null
                : new PublishedDataSetModel {
                    Name = model.Name,
                    DataSetSource = model.DataSetSource.ToServiceModel(),
                    DataSetMetaData = model.DataSetMetaData.ToServiceModel(),
                    ExtensionFields = model.ExtensionFields?
                    .ToDictionary(k => k.Key, v => v.Value)
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataSetEventApiModel ToApiModel(
            this PublishedDataSetEventModel model) {
            return model == null
                ? null
                : new PublishedDataSetEventApiModel {
                    Id = model.Id,
                    DiscardNew = model.DiscardNew,
                    EventNotifier = model.EventNotifier,
                    PublishedEventName = model.PublishedEventName,
                    BrowsePath = model.BrowsePath,
                    WhereClause = model.WhereClause.ToApiModel(),
                    QueueSize = model.QueueSize,
                    MonitoringMode = (IIoT.Api.Publisher.Models.MonitoringMode?)model.MonitoringMode,
                    SelectClauses = model.SelectClauses?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                    ConditionHandling = model.ConditionHandling.ToApiModel(),
                    TypeDefinitionId = model.TypeDefinitionId,
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static EventFilterApiModel ToApiModel(
            this EventFilterModel model) {
            return model == null
                ? null
                : new EventFilterApiModel {
                    SelectClauses = model.SelectClauses?
                    .Select(e => e.ToApiModel())
                    .ToList(),
                    TypeDefinitionId = model.TypeDefinitionId,
                    WhereClause = model.WhereClause.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EventFilterModel ToServiceModel(
            this EventFilterApiModel model) {
            return model == null
                ? null
                : new EventFilterModel {
                    SelectClauses = model.SelectClauses?
                    .Select(e => e.ToServiceModel())
                    .ToList(),
                    TypeDefinitionId = model.TypeDefinitionId,
                    WhereClause = model.WhereClause.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ConditionHandlingOptionsApiModel ToApiModel(
            this ConditionHandlingOptionsModel model) {
            return model == null
                ? null
                : new ConditionHandlingOptionsApiModel {
                    UpdateInterval = model.UpdateInterval,
                    SnapshotInterval = model.SnapshotInterval,
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ConditionHandlingOptionsModel ToServiceModel(
            this ConditionHandlingOptionsApiModel model) {
            return model == null
                ? null
                : new ConditionHandlingOptionsModel {
                    UpdateInterval = model.UpdateInterval,
                    SnapshotInterval = model.SnapshotInterval,
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataSetEventModel ToServiceModel(
            this PublishedDataSetEventApiModel model) {
            return model == null
                ? null
                : new PublishedDataSetEventModel {
                    Id = model.Id,
                    DiscardNew = model.DiscardNew,
                    EventNotifier = model.EventNotifier,
                    PublishedEventName = model.PublishedEventName,
                    BrowsePath = model.BrowsePath,
                    WhereClause = model.WhereClause.ToServiceModel(),
                    QueueSize = model.QueueSize,
                    MonitoringMode = (OpcUa.Publisher.Models.MonitoringMode?)model.MonitoringMode,
                    SelectClauses = model.SelectClauses?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                    ConditionHandling = model.ConditionHandling.ToServiceModel(),
                    TypeDefinitionId = model.TypeDefinitionId,
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataSetSettingsApiModel ToApiModel(
            this PublishedDataSetSettingsModel model) {
            return model == null
                ? null
                : new PublishedDataSetSettingsApiModel {
                    LifeTimeCount = model.LifeTimeCount,
                    MaxKeepAliveCount = model.MaxKeepAliveCount,
                    MaxNotificationsPerPublish = model.MaxNotificationsPerPublish,
                    Priority = model.Priority,
                    ResolveDisplayName = model.ResolveDisplayName,
                    PublishingInterval = model.PublishingInterval
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataSetSettingsModel ToServiceModel(
            this PublishedDataSetSettingsApiModel model) {
            return model == null
                ? null
                : new PublishedDataSetSettingsModel {
                    LifeTimeCount = model.LifeTimeCount,
                    MaxKeepAliveCount = model.MaxKeepAliveCount,
                    MaxNotificationsPerPublish = model.MaxNotificationsPerPublish,
                    Priority = model.Priority,
                    ResolveDisplayName = model.ResolveDisplayName,
                    PublishingInterval = model.PublishingInterval
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataSetSourceApiModel ToApiModel(
            this PublishedDataSetSourceModel model) {
            return model == null
                ? null
                : new PublishedDataSetSourceApiModel {
                    Connection = model.Connection.ToApiModel(),
                    PublishedEvents = model.PublishedEvents.ToApiModel(),
                    PublishedVariables = model.PublishedVariables.ToApiModel(),
                    SubscriptionSettings = model.SubscriptionSettings.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataSetSourceModel ToServiceModel(
            this PublishedDataSetSourceApiModel model) {
            return model == null
                ? null
                : new PublishedDataSetSourceModel {
                    Connection = model.Connection.ToServiceModel(),
                    PublishedEvents = model.PublishedEvents.ToServiceModel(),
                    PublishedVariables = model.PublishedVariables.ToServiceModel(),
                    SubscriptionSettings = model.SubscriptionSettings.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataSetVariableApiModel ToApiModel(
            this PublishedDataSetVariableModel model) {
            return model == null
                ? null
                : new PublishedDataSetVariableApiModel {
                    Id = model.Id,
                    DataSetClassFieldId = model.DataSetClassFieldId,
                    PublishedVariableNodeId = model.PublishedVariableNodeId,
                    BrowsePath = model.BrowsePath,
                    Attribute = (NodeAttribute?)model.Attribute,
                    DataChangeTrigger = (DataChangeTriggerType?)model.DataChangeTrigger,
                    DeadbandType = (DeadbandType?)model.DeadbandType,
                    DeadbandValue = model.DeadbandValue,
                    DiscardNew = model.DiscardNew,
                    IndexRange = model.IndexRange,
                    MonitoringMode = (IIoT.Api.Publisher.Models.MonitoringMode?)model.MonitoringMode,
                    MetaDataProperties = model.MetaDataProperties?.ToList(),
                    QueueSize = model.QueueSize,
                    SamplingInterval = model.SamplingInterval,
                    SkipFirst = model.SkipFirst,
                    HeartbeatInterval = model.HeartbeatInterval,
                    PublishedVariableDisplayName = model.PublishedVariableDisplayName,
                    SubstituteValue = model.SubstituteValue?.Copy()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataSetVariableModel ToServiceModel(
            this PublishedDataSetVariableApiModel model) {
            return model == null
                ? null
                : new PublishedDataSetVariableModel {
                    Id = model.Id,
                    PublishedVariableNodeId = model.PublishedVariableNodeId,
                    BrowsePath = model.BrowsePath,
                    DataSetClassFieldId = model.DataSetClassFieldId,
                    Attribute = (Core.Models.NodeAttribute?)model.Attribute,
                    DataChangeTrigger = (OpcUa.Publisher.Models.DataChangeTriggerType?)model.DataChangeTrigger,
                    DeadbandType = (OpcUa.Publisher.Models.DeadbandType?)model.DeadbandType,
                    DeadbandValue = model.DeadbandValue,
                    DiscardNew = model.DiscardNew,
                    IndexRange = model.IndexRange,
                    MonitoringMode = (OpcUa.Publisher.Models.MonitoringMode?)model.MonitoringMode,
                    MetaDataProperties = model.MetaDataProperties?
                    .ToList(),
                    QueueSize = model.QueueSize,
                    SkipFirst = model.SkipFirst,
                    SamplingInterval = model.SamplingInterval,
                    HeartbeatInterval = model.HeartbeatInterval,
                    PublishedVariableDisplayName = model.PublishedVariableDisplayName,
                    SubstituteValue = model.SubstituteValue?.Copy()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static SimpleAttributeOperandApiModel ToApiModel(
            this SimpleAttributeOperandModel model) {
            return model == null
                ? null
                : new SimpleAttributeOperandApiModel {
                    TypeDefinitionId = model.TypeDefinitionId,
                    AttributeId = (NodeAttribute?)model.AttributeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange,
                    DataSetClassFieldId = model.DataSetClassFieldId,
                    DisplayName = model.DisplayName
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static SimpleAttributeOperandModel ToServiceModel(
            this SimpleAttributeOperandApiModel model) {
            return model == null
                ? null
                : new SimpleAttributeOperandModel {
                    TypeDefinitionId = model.TypeDefinitionId,
                    AttributeId = (Core.Models.NodeAttribute?)model.AttributeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange,
                    DataSetClassFieldId = model.DataSetClassFieldId,
                    DisplayName = model.DisplayName
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriterGroupApiModel ToApiModel(
            this WriterGroupModel model) {
            return model == null
                ? null
                : new WriterGroupApiModel {
                    WriterGroupId = model.WriterGroupId,
                    HeaderLayoutUri = model.HeaderLayoutUri,
                    KeepAliveTime = model.KeepAliveTime,
                    LocaleIds = model.LocaleIds?.ToList(),
                    MaxNetworkMessageSize = model.MaxNetworkMessageSize,
                    MessageSettings = model.MessageSettings.ToApiModel(),
                    MessageType = (IIoT.Api.Publisher.Models.MessageEncoding?)model.MessageType,
                    Name = model.Name,
                    Priority = model.Priority,
                    SecurityGroupId = model.SecurityGroupId,
                    SecurityKeyServices = model.SecurityKeyServices?
                    .Select(s => s.ToApiModel())
                    .ToList(),
                    DataSetWriters = model.DataSetWriters?
                    .Select(s => s.ToApiModel())
                    .ToList(),
                    PublishingInterval = model.PublishingInterval,
                    SecurityMode = (SecurityMode?)model.SecurityMode
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static WriterGroupModel ToServiceModel(
            this WriterGroupApiModel model) {
            return model == null
                ? null
                : new WriterGroupModel {
                    WriterGroupId = model.WriterGroupId,
                    HeaderLayoutUri = model.HeaderLayoutUri,
                    KeepAliveTime = model.KeepAliveTime,
                    LocaleIds = model.LocaleIds?.ToList(),
                    MaxNetworkMessageSize = model.MaxNetworkMessageSize,
                    MessageSettings = model.MessageSettings.ToServiceModel(),
                    MessageType = (OpcUa.Publisher.Models.MessageEncoding?)model.MessageType,
                    Name = model.Name,
                    Priority = model.Priority,
                    SecurityGroupId = model.SecurityGroupId,
                    SecurityKeyServices = model.SecurityKeyServices?
                    .Select(s => s.ToServiceModel())
                    .ToList(),
                    DataSetWriters = model.DataSetWriters?
                    .Select(s => s.ToServiceModel())
                    .ToList(),
                    PublishingInterval = model.PublishingInterval,
                    SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriterGroupJobApiModel ToApiModel(
            this WriterGroupJobModel model) {
            return model?.WriterGroup == null
                ? null
                : new WriterGroupJobApiModel {
                    WriterGroup = model.WriterGroup.ToApiModel(),
                    ConnectionString = model.ConnectionString,
                    Engine = model.Engine.ToApiModel(),
                    MessagingMode = (IIoT.Api.Publisher.Models.MessagingMode?)model.MessagingMode
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static WriterGroupJobModel ToServiceModel(
            this WriterGroupJobApiModel model) {
            return model == null
                ? null
                : new WriterGroupJobModel {
                    WriterGroup = model.WriterGroup.ToServiceModel(),
                    ConnectionString = model.ConnectionString,
                    Engine = model.Engine.ToServiceModel(),
                    MessagingMode = (OpcUa.Publisher.Models.MessagingMode?)model.MessagingMode
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriterGroupMessageSettingsApiModel ToApiModel(
            this WriterGroupMessageSettingsModel model) {
            return model == null
                ? null
                : new WriterGroupMessageSettingsApiModel {
                    NetworkMessageContentMask = (IIoT.Api.Publisher.Models.NetworkMessageContentMask?)model.NetworkMessageContentMask,
                    DataSetOrdering = (IIoT.Api.Publisher.Models.DataSetOrderingType?)model.DataSetOrdering,
                    GroupVersion = model.GroupVersion,
                    PublishingOffset = model.PublishingOffset,
                    MaxMessagesPerPublish = model.MaxMessagesPerPublish,
                    SamplingOffset = model.SamplingOffset
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static WriterGroupMessageSettingsModel ToServiceModel(
            this WriterGroupMessageSettingsApiModel model) {
            return model == null
                ? null
                : new WriterGroupMessageSettingsModel {
                    NetworkMessageContentMask = (OpcUa.Publisher.Models.NetworkMessageContentMask?)model.NetworkMessageContentMask,
                    DataSetOrdering = (OpcUa.Publisher.Models.DataSetOrderingType?)model.DataSetOrdering,
                    GroupVersion = model.GroupVersion,
                    PublishingOffset = model.PublishingOffset,
                    MaxMessagesPerPublish = model.MaxMessagesPerPublish,
                    SamplingOffset = model.SamplingOffset
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedItemApiModel ToApiModel(
            this PublishedItemModel model) {
            return model == null
                ? null
                : new PublishedItemApiModel {
                    NodeId = model.NodeId,
                    DisplayName = model.DisplayName,
                    HeartbeatInterval = model.HeartbeatInterval,
                    SamplingInterval = model.SamplingInterval,
                    PublishingInterval = model.PublishingInterval
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedItemModel ToServiceModel(
            this PublishedItemApiModel model) {
            return model == null
                ? null
                : new PublishedItemModel {
                    NodeId = model.NodeId,
                    DisplayName = model.DisplayName,
                    HeartbeatInterval = model.HeartbeatInterval,
                    SamplingInterval = model.SamplingInterval,
                    PublishingInterval = model.PublishingInterval
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static PublishedItemListRequestApiModel ToApiModel(
            this PublishedItemListRequestModel model) {
            return model == null
                ? null
                : new PublishedItemListRequestApiModel {
                    ContinuationToken = model.ContinuationToken
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedItemListRequestModel ToServiceModel(
            this PublishedItemListRequestApiModel model) {
            return model == null
                ? null
                : new PublishedItemListRequestModel {
                    ContinuationToken = model.ContinuationToken
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedItemListResponseApiModel ToApiModel(
            this PublishedItemListResultModel model) {
            return model == null
                ? null
                : new PublishedItemListResponseApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(n => n.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedItemListResultModel ToServiceModel(
            this PublishedItemListResponseApiModel model) {
            return model == null
                ? null
                : new PublishedItemListResultModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(n => n.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static PublishStartRequestApiModel ToApiModel(
            this PublishStartRequestModel model) {
            return model == null
                ? null
                : new PublishStartRequestApiModel {
                    Item = model.Item?.ToApiModel(),
                    Header = model.Header?.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishStartRequestModel ToServiceModel(
            this PublishStartRequestApiModel model) {
            return model == null
                ? null
                : new PublishStartRequestModel {
                    Item = model.Item?.ToServiceModel(),
                    Header = model.Header?.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishStartResponseApiModel ToApiModel(
            this PublishStartResultModel model) {
            return model == null
                ? null
                : new PublishStartResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static PublishStartResultModel ToServiceModel(
            this PublishStartResponseApiModel model) {
            return model == null
                ? null
                : new PublishStartResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static PublishStopRequestApiModel ToApiModel(
            this PublishStopRequestModel model) {
            return model == null
                ? null
                : new PublishStopRequestApiModel {
                    NodeId = model.NodeId,
                    Header = model.Header?.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishStopRequestModel ToServiceModel(
            this PublishStopRequestApiModel model) {
            return model == null
                ? null
                : new PublishStopRequestModel {
                    NodeId = model.NodeId,
                    Header = model.Header?.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishStopResponseApiModel ToApiModel(
            this PublishStopResultModel model) {
            return model == null
                ? null
                : new PublishStopResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static PublishStopResultModel ToServiceModel(
            this PublishStopResponseApiModel model) {
            return model == null
                ? null
                : new PublishStopResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static PublishBulkRequestApiModel ToApiModel(
            this PublishBulkRequestModel model) {
            return model == null
                ? null
                : new PublishBulkRequestApiModel {
                    NodesToAdd = model.NodesToAdd?
                    .Select(n => n.ToApiModel())
                    .ToList(),
                    NodesToRemove = model.NodesToRemove?
                    .ToList(),
                    Header = model.Header?.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishBulkRequestModel ToServiceModel(
            this PublishBulkRequestApiModel model) {
            return model == null
                ? null
                : new PublishBulkRequestModel {
                    NodesToAdd = model.NodesToAdd?
                    .Select(n => n.ToServiceModel())
                    .ToList(),
                    NodesToRemove = model.NodesToRemove?
                    .ToList(),
                    Header = model.Header?.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishBulkResponseApiModel ToApiModel(
            this PublishBulkResultModel model) {
            return model == null
                ? null
                : new PublishBulkResponseApiModel {
                    NodesToAdd = model.NodesToAdd?
                    .Select(n => n.ToApiModel())
                    .ToList(),
                    NodesToRemove = model.NodesToRemove?
                    .Select(n => n.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static PublishBulkResultModel ToServiceModel(
            this PublishBulkResponseApiModel model) {
            return model == null
                ? null
                : new PublishBulkResultModel {
                    NodesToAdd = model.NodesToAdd?
                    .Select(n => n.ToServiceModel())
                    .ToList(),
                    NodesToRemove = model.NodesToRemove?
                    .Select(n => n.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <returns></returns>
        public static RequestHeaderApiModel ToApiModel(
            this RequestHeaderModel model) {
            return model == null
                ? null
                : new RequestHeaderApiModel {
                    Diagnostics = model.Diagnostics?.ToApiModel(),
                    Locales = model.Locales
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static RequestHeaderModel ToServiceModel(
            this RequestHeaderApiModel model) {
            return model == null
                ? null
                : new RequestHeaderModel {
                    Diagnostics = model.Diagnostics?.ToServiceModel(),
                    Locales = model.Locales
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultApiModel ToApiModel(
            this ServiceResultModel model) {
            return model == null
                ? null
                : new ServiceResultApiModel {
                    Diagnostics = model.Diagnostics,
                    ErrorMessage = model.ErrorMessage,
                    StatusCode = model.StatusCode
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultModel ToServiceModel(
            this ServiceResultApiModel model) {
            return model == null
                ? null
                : new ServiceResultModel {
                    Diagnostics = model.Diagnostics,
                    ErrorMessage = model.ErrorMessage,
                    StatusCode = model.StatusCode
                };
        }


        /// <summary>
        /// Create api model
        /// </summary>
        public static AggregateConfigurationApiModel ToApiModel(
            this AggregateConfigurationModel model) {
            return model == null
                ? null
                : new AggregateConfigurationApiModel {
                    UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults,
                    TreatUncertainAsBad = model.TreatUncertainAsBad,
                    PercentDataBad = model.PercentDataBad,
                    PercentDataGood = model.PercentDataGood,
                    UseSlopedExtrapolation = model.UseSlopedExtrapolation
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static AggregateConfigurationModel ToServiceModel(
            this AggregateConfigurationApiModel model) {
            return model == null
                ? null
                : new AggregateConfigurationModel {
                    UseServerCapabilitiesDefaults = model.UseServerCapabilitiesDefaults,
                    TreatUncertainAsBad = model.TreatUncertainAsBad,
                    PercentDataBad = model.PercentDataBad,
                    PercentDataGood = model.PercentDataGood,
                    UseSlopedExtrapolation = model.UseSlopedExtrapolation
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteEventsDetailsApiModel ToApiModel(
            this DeleteEventsDetailsModel model) {
            return model == null
                ? null
                : new DeleteEventsDetailsApiModel {
                    EventIds = model.EventIds
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteEventsDetailsModel ToServiceModel(
            this DeleteEventsDetailsApiModel model) {
            return model == null
                ? null
                : new DeleteEventsDetailsModel {
                    EventIds = model.EventIds
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteModifiedValuesDetailsApiModel ToApiModel(
            this DeleteModifiedValuesDetailsModel model) {
            return model == null
                ? null
                : new DeleteModifiedValuesDetailsApiModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteModifiedValuesDetailsModel ToServiceModel(
            this DeleteModifiedValuesDetailsApiModel model) {
            return model == null
                ? null
                : new DeleteModifiedValuesDetailsModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteValuesAtTimesDetailsApiModel ToApiModel(
            this DeleteValuesAtTimesDetailsModel model) {
            return model == null
                ? null
                : new DeleteValuesAtTimesDetailsApiModel {
                    ReqTimes = model.ReqTimes
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteValuesAtTimesDetailsModel ToServiceModel(
            this DeleteValuesAtTimesDetailsApiModel model) {
            return model == null
                ? null
                : new DeleteValuesAtTimesDetailsModel {
                    ReqTimes = model.ReqTimes
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static DeleteValuesDetailsApiModel ToApiModel(
            this DeleteValuesDetailsModel model) {
            return model == null
                ? null
                : new DeleteValuesDetailsApiModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DeleteValuesDetailsModel ToServiceModel(
            this DeleteValuesDetailsApiModel model) {
            return model == null
                ? null
                : new DeleteValuesDetailsModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoricEventApiModel ToApiModel(
            this HistoricEventModel model) {
            return model == null
                ? null
                : new HistoricEventApiModel {
                    EventFields = model.EventFields
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static HistoricEventModel ToServiceModel(
            this HistoricEventApiModel model) {
            return model == null
                ? null
                : new HistoricEventModel {
                    EventFields = model.EventFields
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoricValueApiModel ToApiModel(
            this HistoricValueModel model) {
            return model == null
                ? null
                : new HistoricValueApiModel {
                    Value = model.Value,
                    StatusCode = model.StatusCode,
                    SourceTimestamp = model.SourceTimestamp,
                    SourcePicoseconds = model.SourcePicoseconds,
                    ServerTimestamp = model.ServerTimestamp,
                    ServerPicoseconds = model.ServerPicoseconds,
                    ModificationInfo = model.ModificationInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static HistoricValueModel ToServiceModel(
            this HistoricValueApiModel model) {
            return model == null
                ? null
                : new HistoricValueModel {
                    Value = model.Value,
                    StatusCode = model.StatusCode,
                    SourceTimestamp = model.SourceTimestamp,
                    SourcePicoseconds = model.SourcePicoseconds,
                    ServerTimestamp = model.ServerTimestamp,
                    ServerPicoseconds = model.ServerPicoseconds,
                    ModificationInfo = model.ModificationInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextRequestApiModel ToApiModel(
            this HistoryReadNextRequestModel model) {
            return model == null
                ? null
                : new HistoryReadNextRequestApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Abort = model.Abort,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static HistoryReadNextRequestModel ToServiceModel(
            this HistoryReadNextRequestApiModel model) {
            return model == null
                ? null
                : new HistoryReadNextRequestModel {
                    ContinuationToken = model.ContinuationToken,
                    Abort = model.Abort,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadNextResponseApiModel<T> ToApiModel<S, T>(
            this HistoryReadNextResultModel<S> model, Func<S, T> convert) {
            return model == null
                ? null
                : new HistoryReadNextResponseApiModel<T> {
                    History = convert(model.History),
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create from api model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadNextResultModel<T> ToServiceModel<S, T>(
            this HistoryReadNextResponseApiModel<S> model, Func<S, T> convert) {
            return model == null
                ? null
                : new HistoryReadNextResultModel<T> {
                    History = convert(model.History),
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadRequestApiModel<VariantValue> ToApiModel(
            this HistoryReadRequestModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryReadRequestApiModel<VariantValue> {
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange,
                    Details = model.Details,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadRequestModel<VariantValue> ToServiceModel(
            this HistoryReadRequestApiModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryReadRequestModel<VariantValue> {
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange,
                    Details = model.Details,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextResponseApiModel<VariantValue> ToApiModel(
            this HistoryReadNextResultModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryReadNextResponseApiModel<VariantValue> {
                    History = model.History,
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextResultModel<VariantValue> ToServiceModel(
            this HistoryReadNextResponseApiModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryReadNextResultModel<VariantValue> {
                    History = model.History,
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadResponseApiModel<VariantValue> ToApiModel(
            this HistoryReadResultModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryReadResponseApiModel<VariantValue> {
                    History = model.History,
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadResultModel<VariantValue> ToServiceModel(
            this HistoryReadResponseApiModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryReadResultModel<VariantValue> {
                    History = model.History,
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateRequestApiModel<VariantValue> ToApiModel(
            this HistoryUpdateRequestModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryUpdateRequestApiModel<VariantValue> {
                    Details = model.Details,
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static HistoryUpdateRequestModel<VariantValue> ToServiceModel(
            this HistoryUpdateRequestApiModel<VariantValue> model) {
            return model == null
                ? null
                : new HistoryUpdateRequestModel<VariantValue> {
                    Details = model.Details,
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryReadRequestApiModel<S> ToApiModel<S, T>(
            this HistoryReadRequestModel<T> model, Func<T, S> convert) {
            return model == null
                ? null
                : new HistoryReadRequestApiModel<S> {
                    Details = convert(model.Details),
                    BrowsePath = model.BrowsePath,
                    NodeId = model.NodeId,
                    IndexRange = model.IndexRange,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryReadRequestModel<S> ToServiceModel<S, T>(
            this HistoryReadRequestApiModel<T> model, Func<T, S> convert) {
            return model == null
                ? null
                : new HistoryReadRequestModel<S> {
                    Details = convert(model.Details),
                    BrowsePath = model.BrowsePath,
                    NodeId = model.NodeId,
                    IndexRange = model.IndexRange,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadResponseApiModel<T> ToApiModel<S, T>(
            this HistoryReadResultModel<S> model, Func<S, T> convert) {
            return model == null
                ? null
                : new HistoryReadResponseApiModel<T> {
                    History = convert(model.History),
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        public static HistoryReadResultModel<T> ToServiceModel<S, T>(
            this HistoryReadResponseApiModel<S> model, Func<S, T> convert) {
            return model == null
                ? null
                : new HistoryReadResultModel<T> {
                    History = convert(model.History),
                    ContinuationToken = model.ContinuationToken,
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryUpdateRequestApiModel<S> ToApiModel<S, T>(
            this HistoryUpdateRequestModel<T> model, Func<T, S> convert) {
            return model == null
                ? null
                : new HistoryUpdateRequestApiModel<S> {
                    Details = convert(model.Details),
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        public static HistoryUpdateRequestModel<S> ToServiceModel<S, T>(
            this HistoryUpdateRequestApiModel<T> model, Func<T, S> convert) {
            return model == null
                ? null
                : new HistoryUpdateRequestModel<S> {
                    Details = convert(model.Details),
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateResponseApiModel ToApiModel(
            this HistoryUpdateResultModel model) {
            return model == null
                ? null
                : new HistoryUpdateResponseApiModel {
                    Results = model.Results?
                    .Select(r => r.ToApiModel())
                    .ToList(),
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateResultModel ToServiceModel(
            this HistoryUpdateResponseApiModel model) {
            return model == null
                ? null
                : new HistoryUpdateResultModel {
                    Results = model.Results?
                    .Select(r => r.ToServiceModel())
                    .ToList(),
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static InsertEventsDetailsApiModel ToApiModel(
            this InsertEventsDetailsModel model) {
            return model == null
                ? null
                : new InsertEventsDetailsApiModel {
                    Filter = model.Filter.ToApiModel(),
                    Events = model.Events?
                    .Select(v => v.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static InsertEventsDetailsModel ToServiceModel(
            this InsertEventsDetailsApiModel model) {
            return model == null
                ? null
                : new InsertEventsDetailsModel {
                    Filter = model.Filter.ToServiceModel(),
                    Events = model.Events?
                    .Select(v => v.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static InsertValuesDetailsApiModel ToApiModel(
            this InsertValuesDetailsModel model) {
            return model == null
                ? null
                : new InsertValuesDetailsApiModel {
                    Values = model.Values?
                    .Select(v => v.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static InsertValuesDetailsModel ToServiceModel(
            this InsertValuesDetailsApiModel model) {
            return model == null
                ? null
                : new InsertValuesDetailsModel {
                    Values = model.Values?
                    .Select(v => v.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ModificationInfoApiModel ToApiModel(
            this ModificationInfoModel model) {
            return model == null
                ? null
                : new ModificationInfoApiModel {
                    ModificationTime = model.ModificationTime,
                    UpdateType = (HistoryUpdateOperation?)model.UpdateType,
                    UserName = model.UserName
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ModificationInfoModel ToServiceModel(
            this ModificationInfoApiModel model) {
            return model == null
                ? null
                : new ModificationInfoModel {
                    ModificationTime = model.ModificationTime,
                    UpdateType = (History.Models.HistoryUpdateOperation?)model.UpdateType,
                    UserName = model.UserName
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadEventsDetailsApiModel ToApiModel(
            this ReadEventsDetailsModel model) {
            return model == null
                ? null
                : new ReadEventsDetailsApiModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    NumEvents = model.NumEvents,
                    Filter = model.Filter.ToApiModel()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadEventsDetailsModel ToServiceModel(
            this ReadEventsDetailsApiModel model) {
            return model == null
                ? null
                : new ReadEventsDetailsModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    NumEvents = model.NumEvents,
                    Filter = model.Filter.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadModifiedValuesDetailsApiModel ToApiModel(
            this ReadModifiedValuesDetailsModel model) {
            return model == null
                ? null
                : new ReadModifiedValuesDetailsApiModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    NumValues = model.NumValues
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadModifiedValuesDetailsModel ToServiceModel(
            this ReadModifiedValuesDetailsApiModel model) {
            return model == null
                ? null
                : new ReadModifiedValuesDetailsModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    NumValues = model.NumValues
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadProcessedValuesDetailsApiModel ToApiModel(
            this ReadProcessedValuesDetailsModel model) {
            return model == null
                ? null
                : new ReadProcessedValuesDetailsApiModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    ProcessingInterval = model.ProcessingInterval,
                    AggregateConfiguration = model.AggregateConfiguration.ToApiModel(),
                    AggregateTypeId = model.AggregateTypeId
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadProcessedValuesDetailsModel ToServiceModel(
            this ReadProcessedValuesDetailsApiModel model) {
            return model == null
                ? null
                : new ReadProcessedValuesDetailsModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    ProcessingInterval = model.ProcessingInterval,
                    AggregateConfiguration = model.AggregateConfiguration.ToServiceModel(),
                    AggregateTypeId = model.AggregateTypeId
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadValuesAtTimesDetailsApiModel ToApiModel(
            this ReadValuesAtTimesDetailsModel model) {
            return model == null
                ? null
                : new ReadValuesAtTimesDetailsApiModel {
                    ReqTimes = model.ReqTimes,
                    UseSimpleBounds = model.UseSimpleBounds
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadValuesAtTimesDetailsModel ToServiceModel(
            this ReadValuesAtTimesDetailsApiModel model) {
            return model == null
                ? null
                : new ReadValuesAtTimesDetailsModel {
                    ReqTimes = model.ReqTimes,
                    UseSimpleBounds = model.UseSimpleBounds
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReadValuesDetailsApiModel ToApiModel(
            this ReadValuesDetailsModel model) {
            return model == null
                ? null
                : new ReadValuesDetailsApiModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    NumValues = model.NumValues,
                    ReturnBounds = model.ReturnBounds
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReadValuesDetailsModel ToServiceModel(
            this ReadValuesDetailsApiModel model) {
            return model == null
                ? null
                : new ReadValuesDetailsModel {
                    EndTime = model.EndTime,
                    StartTime = model.StartTime,
                    NumValues = model.NumValues,
                    ReturnBounds = model.ReturnBounds
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        public static ReplaceEventsDetailsApiModel ToApiModel(
            this ReplaceEventsDetailsModel model) {
            return model == null
                ? null
                : new ReplaceEventsDetailsApiModel {
                    Filter = model.Filter.ToApiModel(),
                    Events = model.Events?
                    .Select(v => v.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReplaceEventsDetailsModel ToServiceModel(
            this ReplaceEventsDetailsApiModel model) {
            return model == null
                ? null
                : new ReplaceEventsDetailsModel {
                    Filter = model.Filter.ToServiceModel(),
                    Events = model.Events?
                    .Select(v => v.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReplaceValuesDetailsApiModel ToApiModel(
            this ReplaceValuesDetailsModel model) {
            return model == null
                ? null
                : new ReplaceValuesDetailsApiModel {
                    Values = model.Values?
                    .Select(v => v.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ReplaceValuesDetailsModel ToServiceModel(
            this ReplaceValuesDetailsApiModel model) {
            return model == null
                ? null
                : new ReplaceValuesDetailsModel {
                    Values = model.Values?
                    .Select(v => v.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoApiModel ToApiModel(
            this ApplicationInfoModel model) {
            return model == null
                ? null
                : new ApplicationInfoApiModel {
                    ApplicationId = model.ApplicationId,
                    ApplicationType = (ApplicationType)model.ApplicationType,
                    ApplicationUri = model.ApplicationUri,
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    LocalizedNames = model.LocalizedNames,
                    ProductUri = model.ProductUri,
                    SiteId = model.SiteId,
                    HostAddresses = model.HostAddresses,
                    DiscovererId = model.DiscovererId,
                    DiscoveryProfileUri = model.DiscoveryProfileUri,
                    DiscoveryUrls = model.DiscoveryUrls,
                    Capabilities = model.Capabilities,
                    NotSeenSince = model.NotSeenSince,
                    GatewayServerUri = model.GatewayServerUri,
                    Created = model.Created.ToApiModel(),
                    Updated = model.Updated.ToApiModel(),
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoModel ToServiceModel(
            this ApplicationInfoApiModel model) {
            return model == null
                ? null
                : new ApplicationInfoModel {
                    ApplicationId = model.ApplicationId,
                    ApplicationType = (Core.Models.ApplicationType)model.ApplicationType,
                    ApplicationUri = model.ApplicationUri,
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    LocalizedNames = model.LocalizedNames,
                    ProductUri = model.ProductUri,
                    SiteId = model.SiteId,
                    HostAddresses = model.HostAddresses,
                    DiscovererId = model.DiscovererId,
                    DiscoveryProfileUri = model.DiscoveryProfileUri,
                    DiscoveryUrls = model.DiscoveryUrls,
                    Capabilities = model.Capabilities,
                    NotSeenSince = model.NotSeenSince,
                    GatewayServerUri = model.GatewayServerUri,
                    Created = model.Created.ToServiceModel(),
                    Updated = model.Updated.ToServiceModel(),
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoListApiModel ToApiModel(
            this ApplicationInfoListModel model) {
            return model == null
                ? null
                : new ApplicationInfoListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationInfoListModel ToServiceModel(
            this ApplicationInfoListApiModel model) {
            return model == null
                ? null
                : new ApplicationInfoListModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationApiModel ToApiModel(
            this ApplicationRegistrationModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationApiModel {
                    Application = model.Application.ToApiModel(),
                    Endpoints = model.Endpoints?
                    .Select(e => e.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationModel ToServiceModel(
            this ApplicationRegistrationApiModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationModel {
                    Application = model.Application.ToServiceModel(),
                    Endpoints = model.Endpoints?
                    .Select(e => e.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationQueryApiModel ToApiModel(
            this ApplicationRegistrationQueryModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationQueryApiModel {
                    ApplicationType = (ApplicationType?)model.ApplicationType,
                    ApplicationUri = model.ApplicationUri,
                    ProductUri = model.ProductUri,
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    Capability = model.Capability,
                    SiteOrGatewayId = model.SiteOrGatewayId,
                    IncludeNotSeenSince = model.IncludeNotSeenSince,
                    GatewayServerUri = model.GatewayServerUri,
                    DiscovererId = model.DiscovererId,
                    DiscoveryProfileUri = model.DiscoveryProfileUri
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationQueryModel ToServiceModel(
            this ApplicationRegistrationQueryApiModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationQueryModel {
                    ApplicationType = (Core.Models.ApplicationType?)model.ApplicationType,
                    ApplicationUri = model.ApplicationUri,
                    ProductUri = model.ProductUri,
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    Capability = model.Capability,
                    SiteOrGatewayId = model.SiteOrGatewayId,
                    IncludeNotSeenSince = model.IncludeNotSeenSince,
                    GatewayServerUri = model.GatewayServerUri,
                    DiscovererId = model.DiscovererId,
                    DiscoveryProfileUri = model.DiscoveryProfileUri
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationRequestApiModel ToApiModel(
            this ApplicationRegistrationRequestModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationRequestApiModel {
                    ApplicationType = (ApplicationType?)model.ApplicationType,
                    ApplicationUri = model.ApplicationUri,
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                    ProductUri = model.ProductUri,
                    DiscoveryProfileUri = model.DiscoveryProfileUri,
                    DiscoveryUrls = model.DiscoveryUrls,
                    SiteId = model.SiteId,
                    GatewayServerUri = model.GatewayServerUri,
                    Capabilities = model.Capabilities
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationRequestModel ToServiceModel(
            this ApplicationRegistrationRequestApiModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationRequestModel {
                    ApplicationType = (Core.Models.ApplicationType?)model.ApplicationType,
                    ApplicationUri = model.ApplicationUri,
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                    ProductUri = model.ProductUri,
                    DiscoveryProfileUri = model.DiscoveryProfileUri,
                    DiscoveryUrls = model.DiscoveryUrls,
                    SiteId = model.SiteId,
                    GatewayServerUri = model.GatewayServerUri,
                    Capabilities = model.Capabilities
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationResponseApiModel ToApiModel(
            this ApplicationRegistrationResultModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationResponseApiModel {
                    Id = model.Id
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationResultModel ToServiceModel(
            this ApplicationRegistrationResponseApiModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationResultModel {
                    Id = model.Id
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationUpdateApiModel ToApiModel(
            this ApplicationRegistrationUpdateModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationUpdateApiModel {
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                    ProductUri = model.ProductUri,
                    Capabilities = model.Capabilities,
                    DiscoveryUrls = model.DiscoveryUrls,
                    GatewayServerUri = model.GatewayServerUri,
                    DiscoveryProfileUri = model.DiscoveryProfileUri
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationRegistrationUpdateModel ToServiceModel(
            this ApplicationRegistrationUpdateApiModel model) {
            return model == null
                ? null
                : new ApplicationRegistrationUpdateModel {
                    ApplicationName = model.ApplicationName,
                    Locale = model.Locale,
                    LocalizedNames = model.LocalizedNames?
                    .ToDictionary(k => k.Key, v => v.Value),
                    ProductUri = model.ProductUri,
                    Capabilities = model.Capabilities,
                    DiscoveryUrls = model.DiscoveryUrls,
                    GatewayServerUri = model.GatewayServerUri,
                    DiscoveryProfileUri = model.DiscoveryProfileUri
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationSiteListApiModel ToApiModel(
            this ApplicationSiteListModel model) {
            return model == null
                ? null
                : new ApplicationSiteListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Sites = model.Sites?.ToList()
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationSiteListModel ToServiceModel(
            this ApplicationSiteListApiModel model) {
            return model == null
                ? null
                : new ApplicationSiteListModel {
                    ContinuationToken = model.ContinuationToken,
                    Sites = model.Sites?.ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AuthenticationMethodApiModel ToApiModel(
            this AuthenticationMethodModel model) {
            return model == null
                ? null
                : new AuthenticationMethodApiModel {
                    Id = model.Id,
                    SecurityPolicy = model.SecurityPolicy,
                    Configuration = model.Configuration,
                    CredentialType = (CredentialType?)model.CredentialType ??
                    Publisher.Models.CredentialType.None
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static AuthenticationMethodModel ToServiceModel(
            this AuthenticationMethodApiModel model) {
            return model == null
                ? null
                : new AuthenticationMethodModel {
                    Id = model.Id,
                    SecurityPolicy = model.SecurityPolicy,
                    Configuration = model.Configuration,
                    CredentialType = (Core.Models.CredentialType?)model.CredentialType ??
                   OpcUa.Core.Models.CredentialType.None
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererApiModel ToApiModel(
            this DiscovererModel model) {
            return model == null
                ? null
                : new DiscovererApiModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    LogLevel = (TraceLogLevel?)model.LogLevel,
                    RequestedMode = (DiscoveryMode?)model.RequestedMode,
                    RequestedConfig = model.RequestedConfig.ToApiModel(),
                    Discovery = (DiscoveryMode?)model.Discovery,
                    DiscoveryConfig = model.DiscoveryConfig.ToApiModel(),
                    OutOfSync = model.OutOfSync,
                    Version = model.Version,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererModel ToServiceModel(
            this DiscovererApiModel model) {
            return model == null
                ? null
                : new DiscovererModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    LogLevel = (Registry.Models.TraceLogLevel?)model.LogLevel,
                    RequestedMode = (Registry.Models.DiscoveryMode?)model.RequestedMode,
                    RequestedConfig = model.RequestedConfig.ToServiceModel(),
                    Discovery = (Registry.Models.DiscoveryMode?)model.Discovery,
                    DiscoveryConfig = model.DiscoveryConfig.ToServiceModel(),
                    OutOfSync = model.OutOfSync,
                    Version = model.Version,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscovererListApiModel ToApiModel(
            this DiscovererListModel model) {
            return model == null
                ? null
                : new DiscovererListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscovererQueryModel ToServiceModel(
            this DiscovererQueryApiModel model) {
            return model == null
                ? null
                : new DiscovererQueryModel {
                    SiteId = model.SiteId,
                    Connected = model.Connected,
                    Discovery = (Registry.Models.DiscoveryMode?)model.Discovery
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscovererUpdateModel ToServiceModel(
            this DiscovererUpdateApiModel model) {
            return model == null
                ? null
                : new DiscovererUpdateModel {
                    SiteId = model.SiteId,
                    LogLevel = (Registry.Models.TraceLogLevel?)model.LogLevel,
                    Discovery = (Registry.Models.DiscoveryMode?)model.Discovery,
                    DiscoveryConfig = model.DiscoveryConfig.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscoveryConfigApiModel ToApiModel(
            this DiscoveryConfigModel model) {
            return model == null
                ? null
                : new DiscoveryConfigApiModel {
                    AddressRangesToScan = model.AddressRangesToScan,
                    NetworkProbeTimeout = model.NetworkProbeTimeout,
                    MaxNetworkProbes = model.MaxNetworkProbes,
                    PortRangesToScan = model.PortRangesToScan,
                    PortProbeTimeout = model.PortProbeTimeout,
                    MaxPortProbes = model.MaxPortProbes,
                    MinPortProbesPercent = model.MinPortProbesPercent,
                    IdleTimeBetweenScans = model.IdleTimeBetweenScans,
                    ActivationFilter = model.ActivationFilter.ToApiModel(),
                    Locales = model.Locales,
                    DiscoveryUrls = model.DiscoveryUrls
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryConfigModel ToServiceModel(
            this DiscoveryConfigApiModel model) {
            return model == null
                ? null
                : new DiscoveryConfigModel {
                    AddressRangesToScan = model.AddressRangesToScan,
                    NetworkProbeTimeout = model.NetworkProbeTimeout,
                    MaxNetworkProbes = model.MaxNetworkProbes,
                    PortRangesToScan = model.PortRangesToScan,
                    PortProbeTimeout = model.PortProbeTimeout,
                    MaxPortProbes = model.MaxPortProbes,
                    MinPortProbesPercent = model.MinPortProbesPercent,
                    IdleTimeBetweenScans = model.IdleTimeBetweenScans,
                    ActivationFilter = model.ActivationFilter.ToServiceModel(),
                    Locales = model.Locales,
                    DiscoveryUrls = model.DiscoveryUrls
                };
        }

        /// <summary>
        /// Convert to Api model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryCancelApiModel ToApiModel(
            this DiscoveryCancelModel model) {
            return model == null
                ? null
                : new DiscoveryCancelApiModel {
                    Id = model.Id
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryCancelModel ToServiceModel(
            this DiscoveryCancelApiModel model) {
            return model == null
                ? null
                : new DiscoveryCancelModel {
                    Id = model.Id
                };
        }

        /// <summary>
        /// Convert to Api model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryRequestApiModel ToApiModel(
            this DiscoveryRequestModel model) {
            return model == null
                ? null
                : new DiscoveryRequestApiModel {
                    Id = model.Id,
                    Configuration = model.Configuration.ToApiModel(),
                    Discovery = (DiscoveryMode?)model.Discovery
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static DiscoveryRequestModel ToServiceModel(
            this DiscoveryRequestApiModel model) {
            return model == null
                ? null
                : new DiscoveryRequestModel {
                    Id = model.Id,
                    Configuration = model.Configuration.ToServiceModel(),
                    Discovery = (Registry.Models.DiscoveryMode?)model.Discovery
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationFilterApiModel ToApiModel(
            this EndpointActivationFilterModel model) {
            return model == null
                ? null
                : new EndpointActivationFilterApiModel {
                    TrustLists = model.TrustLists,
                    SecurityPolicies = model.SecurityPolicies,
                    SecurityMode = (SecurityMode?)model.SecurityMode
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static EndpointActivationFilterModel ToServiceModel(
            this EndpointActivationFilterApiModel model) {
            return model == null
                ? null
                : new EndpointActivationFilterModel {
                    TrustLists = model.TrustLists,
                    SecurityPolicies = model.SecurityPolicies,
                    SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationStatusApiModel ToApiModel(
            this EndpointActivationStatusModel model) {
            return model == null
                ? null
                : new EndpointActivationStatusApiModel {
                    Id = model.Id,
                    ActivationState = (EndpointActivationState?)model.ActivationState
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationStatusModel ToServiceModel(
            this EndpointActivationStatusApiModel model) {
            return model == null
                ? null
                : new EndpointActivationStatusModel {
                    Id = model.Id,
                    ActivationState = (Registry.Models.EndpointActivationState?)model.ActivationState
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoApiModel ToApiModel(
            this EndpointInfoModel model) {
            return model == null
                ? null
                : new EndpointInfoApiModel {
                    ApplicationId = model.ApplicationId,
                    NotSeenSince = model.NotSeenSince,
                    Registration = model.Registration.ToApiModel(),
                    ActivationState = (EndpointActivationState?)model.ActivationState,
                    EndpointState = (EndpointConnectivityState?)model.EndpointState,
                    OutOfSync = model.OutOfSync
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoModel ToServiceModel(
            this EndpointInfoApiModel model) {
            return model == null
                ? null
                : new EndpointInfoModel {
                    ApplicationId = model.ApplicationId,
                    NotSeenSince = model.NotSeenSince,
                    Registration = model.Registration.ToServiceModel(),
                    ActivationState = (Registry.Models.EndpointActivationState?)model.ActivationState,
                    EndpointState = (Registry.Models.EndpointConnectivityState?)model.EndpointState,
                    OutOfSync = model.OutOfSync
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoListApiModel ToApiModel(
            this EndpointInfoListModel model) {
            return model == null
                ? null
                : new EndpointInfoListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointInfoListModel ToServiceModel(
            this EndpointInfoListApiModel model) {
            return model == null
                ? null
                : new EndpointInfoListModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationApiModel ToApiModel(
            this EndpointRegistrationModel model) {
            return model == null
                ? null
                : new EndpointRegistrationApiModel {
                    Id = model.Id,
                    Endpoint = model.Endpoint.ToApiModel(),
                    EndpointUrl = model.EndpointUrl,
                    AuthenticationMethods = model.AuthenticationMethods?
                    .Select(p => p.ToApiModel())
                    .ToList(),
                    SecurityLevel = model.SecurityLevel,
                    SiteId = model.SiteId,
                    DiscovererId = model.DiscovererId,
                    SupervisorId = model.SupervisorId
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationModel ToServiceModel(
            this EndpointRegistrationApiModel model) {
            return model == null
                ? null
                : new EndpointRegistrationModel {
                    Id = model.Id,
                    Endpoint = model.Endpoint.ToServiceModel(),
                    EndpointUrl = model.EndpointUrl,
                    AuthenticationMethods = model.AuthenticationMethods?
                    .Select(p => p.ToServiceModel())
                    .ToList(),
                    SecurityLevel = model.SecurityLevel,
                    SiteId = model.SiteId,
                    DiscovererId = model.DiscovererId,
                    SupervisorId = model.SupervisorId
                };
        }

        /// <summary>
        /// Convert to Api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationQueryApiModel ToApiModel(
            this EndpointRegistrationQueryModel model) {
            return model == null
                ? null
                : new EndpointRegistrationQueryApiModel {
                    Url = model.Url,
                    Connected = model.Connected,
                    Activated = model.Activated,
                    EndpointState = (EndpointConnectivityState?)model.EndpointState,
                    Certificate = model.Certificate,
                    SecurityPolicy = model.SecurityPolicy,
                    SecurityMode = (SecurityMode?)model.SecurityMode,
                    ApplicationId = model.ApplicationId,
                    DiscovererId = model.DiscovererId,
                    SiteOrGatewayId = model.SiteOrGatewayId,
                    SupervisorId = model.SupervisorId,
                    IncludeNotSeenSince = model.IncludeNotSeenSince
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointRegistrationQueryModel ToServiceModel(
            this EndpointRegistrationQueryApiModel model) {
            return model == null
                ? null
                : new EndpointRegistrationQueryModel {
                    Url = model.Url,
                    Connected = model.Connected,
                    Activated = model.Activated,
                    EndpointState = (Registry.Models.EndpointConnectivityState?)model.EndpointState,
                    Certificate = model.Certificate,
                    SecurityPolicy = model.SecurityPolicy,
                    SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode,
                    ApplicationId = model.ApplicationId,
                    DiscovererId = model.DiscovererId,
                    SiteOrGatewayId = model.SiteOrGatewayId,
                    SupervisorId = model.SupervisorId,
                    IncludeNotSeenSince = model.IncludeNotSeenSince
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayApiModel ToApiModel(
            this GatewayModel model) {
            return model == null
                ? null
                : new GatewayApiModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayInfoApiModel ToApiModel(
            this GatewayInfoModel model) {
            return model == null
                ? null
                : new GatewayInfoApiModel {
                    Gateway = model.Gateway.ToApiModel(),
                    Modules = model.Modules.ToApiModel()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayModulesApiModel ToApiModel(
            this GatewayModulesModel model) {
            return model == null
                ? null
                : new GatewayModulesApiModel {
                    Publisher = model.Publisher.ToApiModel(),
                    Supervisor = model.Supervisor.ToApiModel(),
                    Discoverer = model.Discoverer.ToApiModel()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayListApiModel ToApiModel(
            this GatewayListModel model) {
            return model == null
                ? null
                : new GatewayListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayQueryModel ToServiceModel(
            this GatewayQueryApiModel model) {
            return model == null
                ? null
                : new GatewayQueryModel {
                    SiteId = model.SiteId,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static GatewayUpdateModel ToServiceModel(
            this GatewayUpdateApiModel model) {
            return model == null
                ? null
                : new GatewayUpdateModel {
                    SiteId = model.SiteId,
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherApiModel ToApiModel(
            this PublisherModel model) {
            return model == null
                ? null
                : new PublisherApiModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    LogLevel = (TraceLogLevel?)model.LogLevel,
                    OutOfSync = model.OutOfSync,
                    Version = model.Version,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherModel ToServiceModel(
            this PublisherApiModel model) {
            return model == null
                ? null
                : new PublisherModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    LogLevel = (Registry.Models.TraceLogLevel?)model.LogLevel,
                    OutOfSync = model.OutOfSync,
                    Version = model.Version,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherConfigApiModel ToApiModel(
            this PublisherConfigModel model) {
            return model == null
                ? null
                : new PublisherConfigApiModel {
                    Capabilities = model.Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                    HeartbeatInterval = model.HeartbeatInterval,
                    JobCheckInterval = model.JobCheckInterval,
                    JobOrchestratorUrl = model.JobOrchestratorUrl,
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherConfigModel ToServiceModel(
            this PublisherConfigApiModel model) {
            return model == null
                ? null
                : new PublisherConfigModel {
                    Capabilities = model.Capabilities?.ToDictionary(k => k.Key, v => v.Value),
                    HeartbeatInterval = model.HeartbeatInterval,
                    JobCheckInterval = model.JobCheckInterval,
                    JobOrchestratorUrl = model.JobOrchestratorUrl,
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherListApiModel ToApiModel(
            this PublisherListModel model) {
            return model == null
                ? null
                : new PublisherListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create services model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherListModel ToServiceModel(
            this PublisherListApiModel model) {
            return model == null
                ? null
                : new PublisherListModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherQueryApiModel ToApiModel(
            this PublisherQueryModel model) {
            return model == null
                ? null
                : new PublisherQueryApiModel {
                    SiteId = model.SiteId,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherQueryModel ToServiceModel(
            this PublisherQueryApiModel model) {
            return model == null
                ? null
                : new PublisherQueryModel {
                    SiteId = model.SiteId,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherUpdateApiModel ToApiModel(
            this PublisherUpdateModel model) {
            return model == null
                ? null
                : new PublisherUpdateApiModel {
                    SiteId = model.SiteId,
                    LogLevel = (TraceLogLevel?)model.LogLevel,
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublisherUpdateModel ToServiceModel(
            this PublisherUpdateApiModel model) {
            return model == null
                ? null
                : new PublisherUpdateModel {
                    SiteId = model.SiteId,
                    LogLevel = (Registry.Models.TraceLogLevel?)model.LogLevel,
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RegistryOperationApiModel ToApiModel(
            this RegistryOperationContextModel model) {
            return model == null
                ? null
                : new RegistryOperationApiModel {
                    Time = model.Time,
                    AuthorityId = model.AuthorityId
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RegistryOperationContextModel ToServiceModel(
            this RegistryOperationApiModel model) {
            return model == null
                ? null
                : new RegistryOperationContextModel {
                    Time = model.Time,
                    AuthorityId = model.AuthorityId
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ServerRegistrationRequestModel ToServiceModel(
            this ServerRegistrationRequestApiModel model) {
            return model == null
                ? null
                : new ServerRegistrationRequestModel {
                    DiscoveryUrl = model.DiscoveryUrl,
                    Id = model.Id,
                    ActivationFilter = model.ActivationFilter.ToServiceModel()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorApiModel ToApiModel(
            this SupervisorModel model) {
            return model == null
                ? null
                : new SupervisorApiModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    LogLevel = (TraceLogLevel?)model.LogLevel,
                    OutOfSync = model.OutOfSync,
                    Version = model.Version,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorModel ToServiceModel(
            this SupervisorApiModel model) {
            return model == null
                ? null
                : new SupervisorModel {
                    Id = model.Id,
                    SiteId = model.SiteId,
                    LogLevel = (Registry.Models.TraceLogLevel?)model.LogLevel,
                    OutOfSync = model.OutOfSync,
                    Version = model.Version,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorListApiModel ToApiModel(
            this SupervisorListModel model) {
            return model == null
                ? null
                : new SupervisorListApiModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorListModel ToServiceModel(
            this SupervisorListApiModel model) {
            return model == null
                ? null
                : new SupervisorListModel {
                    ContinuationToken = model.ContinuationToken,
                    Items = model.Items?
                    .Select(s => s.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorQueryApiModel ToApiModel(
            this SupervisorQueryModel model) {
            return model == null
                ? null
                : new SupervisorQueryApiModel {
                    SiteId = model.SiteId,
                    EndpointId = model.EndpointId,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorQueryModel ToServiceModel(
            this SupervisorQueryApiModel model) {
            return model == null
                ? null
                : new SupervisorQueryModel {
                    SiteId = model.SiteId,
                    EndpointId = model.EndpointId,
                    Connected = model.Connected
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorStatusApiModel ToApiModel(
            this SupervisorStatusModel model) {
            return model == null
                ? null
                : new SupervisorStatusApiModel {
                    DeviceId = model.DeviceId,
                    ModuleId = model.ModuleId,
                    SiteId = model.SiteId,
                    Endpoints = model.Endpoints?
                    .Select(e => e.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorStatusModel ToServiceModel(
            this SupervisorStatusApiModel model) {
            return model == null
                ? null
                : new SupervisorStatusModel {
                    DeviceId = model.DeviceId,
                    ModuleId = model.ModuleId,
                    SiteId = model.SiteId,
                    Endpoints = model.Endpoints?
                    .Select(e => e.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorUpdateApiModel ToApiModel(
            this SupervisorUpdateModel model) {
            return model == null
                ? null
                : new SupervisorUpdateApiModel {
                    SiteId = model.SiteId,
                    LogLevel = (TraceLogLevel?)model.LogLevel
                };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorUpdateModel ToServiceModel(
            this SupervisorUpdateApiModel model) {
            return model == null
                ? null
                : new SupervisorUpdateModel {
                    SiteId = model.SiteId,
                    LogLevel = (Registry.Models.TraceLogLevel?)model.LogLevel
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateApiModel ToApiModel(
            this X509CertificateModel model) {
            return model == null
                ? null
                : new X509CertificateApiModel {
                    Certificate = model.Certificate,
                    NotAfterUtc = model.NotAfterUtc,
                    NotBeforeUtc = model.NotBeforeUtc,
                    SerialNumber = model.SerialNumber,
                    Subject = model.Subject,
                    SelfSigned = model.SelfSigned,
                    Thumbprint = model.Thumbprint
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateModel ToServiceModel(
            this X509CertificateApiModel model) {
            return model == null
                ? null
                : new X509CertificateModel {
                    Certificate = model.Certificate,
                    NotAfterUtc = model.NotAfterUtc,
                    NotBeforeUtc = model.NotBeforeUtc,
                    SerialNumber = model.SerialNumber,
                    Subject = model.Subject,
                    SelfSigned = model.SelfSigned,
                    Thumbprint = model.Thumbprint
                };
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateChainApiModel ToApiModel(
            this X509CertificateChainModel model) {
            return model == null
                ? null
                : new X509CertificateChainApiModel {
                    Status = model.Status?
                    .Select(s => (X509ChainStatus)s)
                    .ToList(),
                    Chain = model.Chain?
                    .Select(c => c.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        public static X509CertificateChainModel ToServiceModel(
            this X509CertificateChainApiModel model) {
            return model == null
                ? null
                : new X509CertificateChainModel {
                    Status = model.Status?
                    .Select(s => (Core.Models.X509ChainStatus)s)
                    .ToList(),
                    Chain = model.Chain?
                    .Select(c => c.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeReadRequestApiModel ToApiModel(
            this AttributeReadRequestModel model) {
            return model == null
                ? null
                : new AttributeReadRequestApiModel {
                    NodeId = model.NodeId,
                    Attribute = (NodeAttribute)model.Attribute
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static AttributeReadRequestModel ToServiceModel(
            this AttributeReadRequestApiModel model) {
            return model == null
                ? null
                : new AttributeReadRequestModel {
                    NodeId = model.NodeId,
                    Attribute = (Core.Models.NodeAttribute)model.Attribute
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeReadResponseApiModel ToApiModel(
            this AttributeReadResultModel model) {
            return model == null
                ? null
                : new AttributeReadResponseApiModel {
                    Value = model.Value,
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeReadResultModel ToServiceModel(
            this AttributeReadResponseApiModel model) {
            return model == null
                ? null
                : new AttributeReadResultModel {
                    Value = model.Value,
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeWriteRequestApiModel ToApiModel(
            this AttributeWriteRequestModel model) {
            return model == null
                ? null
                : new AttributeWriteRequestApiModel {
                    NodeId = model.NodeId,
                    Value = model.Value,
                    Attribute = (NodeAttribute)model.Attribute
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static AttributeWriteRequestModel ToServiceModel(
            this AttributeWriteRequestApiModel model) {
            return model == null
                ? null
                : new AttributeWriteRequestModel {
                    NodeId = model.NodeId,
                    Value = model.Value,
                    Attribute = (Core.Models.NodeAttribute)model.Attribute
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeWriteResponseApiModel ToApiModel(
            this AttributeWriteResultModel model) {
            return model == null
                ? null
                : new AttributeWriteResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeWriteResultModel ToServiceModel(
            this AttributeWriteResponseApiModel model) {
            return model == null
                ? null
                : new AttributeWriteResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseNextRequestApiModel ToApiModel(
            this BrowseNextRequestModel model) {
            return model == null
                ? null
                : new BrowseNextRequestApiModel {
                    Abort = model.Abort,
                    TargetNodesOnly = model.TargetNodesOnly,
                    ReadVariableValues = model.ReadVariableValues,
                    ContinuationToken = model.ContinuationToken,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static BrowseNextRequestModel ToServiceModel(
            this BrowseNextRequestApiModel model) {
            return model == null
                ? null
                : new BrowseNextRequestModel {
                    Abort = model.Abort,
                    TargetNodesOnly = model.TargetNodesOnly,
                    ReadVariableValues = model.ReadVariableValues,
                    ContinuationToken = model.ContinuationToken,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseNextResponseApiModel ToApiModel(
            this BrowseNextResultModel model) {
            return model == null
                ? null
                : new BrowseNextResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel(),
                    ContinuationToken = model.ContinuationToken,
                    References = model.References?
                    .Select(r => r.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseNextResultModel ToServiceModel(
            this BrowseNextResponseApiModel model) {
            return model == null
                ? null
                : new BrowseNextResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel(),
                    ContinuationToken = model.ContinuationToken,
                    References = model.References?
                    .Select(r => r.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowsePathRequestApiModel ToApiModel(
            this BrowsePathRequestModel model) {
            return model == null
                ? null
                : new BrowsePathRequestApiModel {
                    NodeId = model.NodeId,
                    BrowsePaths = model.BrowsePaths,
                    ReadVariableValues = model.ReadVariableValues,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static BrowsePathRequestModel ToServiceModel(
            this BrowsePathRequestApiModel model) {
            return model == null
                ? null
                : new BrowsePathRequestModel {
                    NodeId = model.NodeId,
                    BrowsePaths = model.BrowsePaths,
                    ReadVariableValues = model.ReadVariableValues,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowsePathResponseApiModel ToApiModel(
            this BrowsePathResultModel model) {
            return model == null
                ? null
                : new BrowsePathResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel(),
                    Targets = model.Targets?
                    .Select(r => r.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowsePathResultModel ToServiceModel(
            this BrowsePathResponseApiModel model) {
            return model == null
                ? null
                : new BrowsePathResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel(),
                    Targets = model.Targets?
                    .Select(r => r.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseRequestApiModel ToApiModel(
            this BrowseRequestModel model) {
            return model == null
                ? null
                : new BrowseRequestApiModel {
                    NodeId = model.NodeId,
                    MaxReferencesToReturn = model.MaxReferencesToReturn,
                    Direction = (BrowseDirection?)model.Direction,
                    View = model.View.ToApiModel(),
                    ReferenceTypeId = model.ReferenceTypeId,
                    TargetNodesOnly = model.TargetNodesOnly,
                    ReadVariableValues = model.ReadVariableValues,
                    NodeClassFilter = model.NodeClassFilter?
                    .Select(f => (NodeClass)f)
                    .ToList(),
                    NoSubtypes = model.NoSubtypes,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static BrowseRequestModel ToServiceModel(
            this BrowseRequestApiModel model) {
            return model == null
                ? null
                : new BrowseRequestModel {
                    NodeId = model.NodeId,
                    MaxReferencesToReturn = model.MaxReferencesToReturn,
                    Direction = (Core.Models.BrowseDirection?)model.Direction,
                    View = model.View.ToServiceModel(),
                    ReferenceTypeId = model.ReferenceTypeId,
                    TargetNodesOnly = model.TargetNodesOnly,
                    ReadVariableValues = model.ReadVariableValues,
                    NodeClassFilter = model.NodeClassFilter?
                    .Select(f => (Core.Models.NodeClass)f)
                    .ToList(),
                    NoSubtypes = model.NoSubtypes,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseResponseApiModel ToApiModel(
            this BrowseResultModel model) {
            return model == null
                ? null
                : new BrowseResponseApiModel {
                    Node = model.Node.ToApiModel(),
                    ErrorInfo = model.ErrorInfo.ToApiModel(),
                    ContinuationToken = model.ContinuationToken,
                    References = model.References?
                    .Select(r => r.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseResultModel ToServiceModel(
            this BrowseResponseApiModel model) {
            return model == null
                ? null
                : new BrowseResultModel {
                    Node = model.Node.ToServiceModel(),
                    ErrorInfo = model.ErrorInfo.ToServiceModel(),
                    ContinuationToken = model.ContinuationToken,
                    References = model.References?
                    .Select(r => r.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static BrowseViewModel ToServiceModel(
            this BrowseViewApiModel model) {
            return model == null
                ? null
                : new BrowseViewModel {
                    ViewId = model.ViewId,
                    Version = model.Version,
                    Timestamp = model.Timestamp
                };
        }

        /// <summary>
        /// Convert back to api model
        /// </summary>
        /// <returns></returns>
        public static BrowseViewApiModel ToApiModel(
            this BrowseViewModel model) {
            return model == null
                ? null
                : new BrowseViewApiModel {
                    ViewId = model.ViewId,
                    Version = model.Version,
                    Timestamp = model.Timestamp
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodCallArgumentApiModel ToApiModel(
            this MethodCallArgumentModel model) {
            return model == null
                ? null
                : new MethodCallArgumentApiModel {
                    Value = model.Value,
                    DataType = model.DataType
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodCallArgumentModel ToServiceModel(
            this MethodCallArgumentApiModel model) {
            return model == null
                ? null
                : new MethodCallArgumentModel {
                    Value = model.Value,
                    DataType = model.DataType
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodCallRequestApiModel ToApiModel(
            this MethodCallRequestModel model) {
            return model == null
                ? null
                : new MethodCallRequestApiModel {
                    MethodId = model.MethodId,
                    ObjectId = model.ObjectId,
                    MethodBrowsePath = model.MethodBrowsePath,
                    ObjectBrowsePath = model.ObjectBrowsePath,
                    Arguments = model.Arguments?
                    .Select(s => s.ToApiModel()).ToList(),
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodCallRequestModel ToServiceModel(
            this MethodCallRequestApiModel model) {
            return model == null
                ? null
                : new MethodCallRequestModel {
                    MethodId = model.MethodId,
                    ObjectId = model.ObjectId,
                    MethodBrowsePath = model.MethodBrowsePath,
                    ObjectBrowsePath = model.ObjectBrowsePath,
                    Arguments = model.Arguments?
                    .Select(s => s.ToServiceModel()).ToList(),
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodCallResponseApiModel ToApiModel(
            this MethodCallResultModel model) {
            return model == null
                ? null
                : new MethodCallResponseApiModel {
                    Results = model.Results?
                    .Select(a => a.ToApiModel())
                    .ToList(),
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodCallResultModel ToServiceModel(
            this MethodCallResponseApiModel model) {
            return model == null
                ? null
                : new MethodCallResultModel {
                    Results = model.Results?
                    .Select(a => a.ToServiceModel())
                    .ToList(),
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataArgumentApiModel ToApiModel(
            this MethodMetadataArgumentModel model) {
            return model == null
                ? null
                : new MethodMetadataArgumentApiModel {
                    DefaultValue = model.DefaultValue,
                    Type = model.Type.ToApiModel(),
                    ValueRank = (NodeValueRank?)model.ValueRank,
                    ArrayDimensions = model.ArrayDimensions,
                    Description = model.Description,
                    Name = model.Name
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodMetadataArgumentModel ToServiceModel(
            this MethodMetadataArgumentApiModel model) {
            return model == null
                ? null
                : new MethodMetadataArgumentModel {
                    DefaultValue = model.DefaultValue,
                    Type = model.Type.ToServiceModel(),
                    ValueRank = (Core.Models.NodeValueRank?)model.ValueRank,
                    ArrayDimensions = model.ArrayDimensions,
                    Description = model.Description,
                    Name = model.Name
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataRequestApiModel ToApiModel(
            this MethodMetadataRequestModel model) {
            return model == null
                ? null
                : new MethodMetadataRequestApiModel {
                    MethodId = model.MethodId,
                    MethodBrowsePath = model.MethodBrowsePath,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodMetadataRequestModel ToServiceModel(
            this MethodMetadataRequestApiModel model) {
            return model == null
                ? null
                : new MethodMetadataRequestModel {
                    MethodId = model.MethodId,
                    MethodBrowsePath = model.MethodBrowsePath,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataResponseApiModel ToApiModel(
            this MethodMetadataResultModel model) {
            return model == null
                ? null
                : new MethodMetadataResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel(),
                    ObjectId = model.ObjectId,
                    InputArguments = model.InputArguments?
                    .Select(a => a.ToApiModel())
                    .ToList(),
                    OutputArguments = model.OutputArguments?
                    .Select(a => a.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataResultModel ToServiceModel(
            this MethodMetadataResponseApiModel model) {
            return model == null
                ? null
                : new MethodMetadataResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel(),
                    ObjectId = model.ObjectId,
                    InputArguments = model.InputArguments?
                    .Select(a => a.ToServiceModel())
                    .ToList(),
                    OutputArguments = model.OutputArguments?
                    .Select(a => a.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static NodeApiModel ToApiModel(
            this NodeModel model) {
            return model == null
                ? null
                : new NodeApiModel {
                    NodeId = model.NodeId,
                    Children = model.Children,
                    BrowseName = model.BrowseName,
                    DisplayName = model.DisplayName,
                    Description = model.Description,
                    NodeClass = (NodeClass?)model.NodeClass,
                    IsAbstract = model.IsAbstract,
                    AccessLevel = (NodeAccessLevel?)model.AccessLevel,
                    EventNotifier = (NodeEventNotifier?)model.EventNotifier,
                    Executable = model.Executable,
                    DataType = model.DataType,
                    ValueRank = (NodeValueRank?)model.ValueRank,
                    AccessRestrictions = (NodeAccessRestrictions?)model.AccessRestrictions,
                    ArrayDimensions = model.ArrayDimensions,
                    ContainsNoLoops = model.ContainsNoLoops,
                    DataTypeDefinition = model.DataTypeDefinition,
                    Value = model.Value,
                    Historizing = model.Historizing,
                    ErrorInfo = model.ErrorInfo.ToApiModel(),
                    ServerPicoseconds = model.ServerPicoseconds,
                    SourcePicoseconds = model.SourcePicoseconds,
                    SourceTimestamp = model.SourceTimestamp,
                    ServerTimestamp = model.ServerTimestamp,
                    InverseName = model.InverseName,
                    MinimumSamplingInterval = model.MinimumSamplingInterval,
                    Symmetric = model.Symmetric,
                    UserAccessLevel = (NodeAccessLevel?)model.UserAccessLevel,
                    UserExecutable = model.UserExecutable,
                    UserWriteMask = model.UserWriteMask,
                    WriteMask = model.WriteMask,
                    RolePermissions = model.RolePermissions?
                    .Select(p => p.ToApiModel())
                    .ToList(),
                    UserRolePermissions = model.UserRolePermissions?
                    .Select(p => p.ToApiModel())
                    .ToList(),
                    TypeDefinitionId = model.TypeDefinitionId
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static NodeModel ToServiceModel(
            this NodeApiModel model) {
            return model == null
                ? null
                : new NodeModel {
                    NodeId = model.NodeId,
                    Children = model.Children,
                    BrowseName = model.BrowseName,
                    DisplayName = model.DisplayName,
                    Description = model.Description,
                    NodeClass = (Core.Models.NodeClass?)model.NodeClass,
                    IsAbstract = model.IsAbstract,
                    AccessLevel = (Core.Models.NodeAccessLevel?)model.AccessLevel,
                    EventNotifier = (Core.Models.NodeEventNotifier?)model.EventNotifier,
                    Executable = model.Executable,
                    DataType = model.DataType,
                    ValueRank = (Core.Models.NodeValueRank?)model.ValueRank,
                    AccessRestrictions = (Core.Models.NodeAccessRestrictions?)model.AccessRestrictions,
                    ArrayDimensions = model.ArrayDimensions,
                    ContainsNoLoops = model.ContainsNoLoops,
                    DataTypeDefinition = model.DataTypeDefinition,
                    Value = model.Value,
                    Historizing = model.Historizing,
                    InverseName = model.InverseName,
                    ErrorInfo = model.ErrorInfo.ToServiceModel(),
                    ServerPicoseconds = model.ServerPicoseconds,
                    SourcePicoseconds = model.SourcePicoseconds,
                    SourceTimestamp = model.SourceTimestamp,
                    ServerTimestamp = model.ServerTimestamp,
                    MinimumSamplingInterval = model.MinimumSamplingInterval,
                    Symmetric = model.Symmetric,
                    UserAccessLevel = (Core.Models.NodeAccessLevel?)model.UserAccessLevel,
                    UserExecutable = model.UserExecutable,
                    UserWriteMask = model.UserWriteMask,
                    WriteMask = model.WriteMask,
                    RolePermissions = model.RolePermissions?
                    .Select(p => p.ToServiceModel())
                    .ToList(),
                    UserRolePermissions = model.UserRolePermissions?
                    .Select(p => p.ToServiceModel())
                    .ToList(),
                    TypeDefinitionId = model.TypeDefinitionId
                };
        }

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public static NodePathTargetApiModel ToApiModel(
            this NodePathTargetModel model) {
            return model == null
                ? null
                : new NodePathTargetApiModel {
                    BrowsePath = model.BrowsePath,
                    RemainingPathIndex = model.RemainingPathIndex,
                    Target = model.Target.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static NodePathTargetModel ToServiceModel(
            this NodePathTargetApiModel model) {
            return model == null
                ? null
                : new NodePathTargetModel {
                    BrowsePath = model.BrowsePath,
                    RemainingPathIndex = model.RemainingPathIndex,
                    Target = model.Target.ToServiceModel()
                };
        }

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public static NodeReferenceApiModel ToApiModel(
            this NodeReferenceModel model) {
            return model == null
                ? null
                : new NodeReferenceApiModel {
                    ReferenceTypeId = model.ReferenceTypeId,
                    Direction = (BrowseDirection?)model.Direction,
                    Target = model.Target.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static NodeReferenceModel ToServiceModel(
            this NodeReferenceApiModel model) {
            return model == null
                ? null
                : new NodeReferenceModel {
                    ReferenceTypeId = model.ReferenceTypeId,
                    Direction = (Core.Models.BrowseDirection?)model.Direction,
                    Target = model.Target.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ReadRequestApiModel ToApiModel(
            this ReadRequestModel model) {
            return model == null
                ? null
                : new ReadRequestApiModel {
                    Attributes = model.Attributes?
                    .Select(a => a.ToApiModel())
                    .ToList(),
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static ReadRequestModel ToServiceModel(
            this ReadRequestApiModel model) {
            return model == null
                ? null
                : new ReadRequestModel {
                    Attributes = model.Attributes?
                    .Select(a => a.ToServiceModel())
                    .ToList(),
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ReadResponseApiModel ToApiModel(
            this ReadResultModel model) {
            return model == null
                ? null
                : new ReadResponseApiModel {
                    Results = model.Results?
                    .Select(a => a.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <param name="model"></param>
        public static ReadResultModel ToServiceModel(
            this ReadResponseApiModel model) {
            return model == null
                ? null
                : new ReadResultModel {
                    Results = model.Results?
                    .Select(a => a.ToServiceModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static RolePermissionApiModel ToApiModel(
            this RolePermissionModel model) {
            return model == null
                ? null
                : new RolePermissionApiModel {
                    RoleId = model.RoleId,
                    Permissions = (RolePermissions?)model.Permissions
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static RolePermissionModel ToServiceModel(
            this RolePermissionApiModel model) {
            return model == null
                ? null
                : new RolePermissionModel {
                    RoleId = model.RoleId,
                    Permissions = (Core.Models.RolePermissions?)model.Permissions
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueReadRequestApiModel ToApiModel(
            this ValueReadRequestModel model) {
            return model == null
                ? null
                : new ValueReadRequestApiModel {
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static ValueReadRequestModel ToServiceModel(
            this ValueReadRequestApiModel model) {
            return model == null
                ? null
                : new ValueReadRequestModel {
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    IndexRange = model.IndexRange,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueReadResponseApiModel ToApiModel(
            this ValueReadResultModel model) {
            return model == null
                ? null
                : new ValueReadResponseApiModel {
                    Value = model.Value,
                    DataType = model.DataType,
                    SourcePicoseconds = model.SourcePicoseconds,
                    SourceTimestamp = model.SourceTimestamp,
                    ServerPicoseconds = model.ServerPicoseconds,
                    ServerTimestamp = model.ServerTimestamp,
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueReadResultModel ToServiceModel(
            this ValueReadResponseApiModel model) {
            return model == null
                ? null
                : new ValueReadResultModel {
                    Value = model.Value,
                    DataType = model.DataType,
                    SourcePicoseconds = model.SourcePicoseconds,
                    SourceTimestamp = model.SourceTimestamp,
                    ServerPicoseconds = model.ServerPicoseconds,
                    ServerTimestamp = model.ServerTimestamp,
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueWriteRequestApiModel ToApiModel(
            this ValueWriteRequestModel model) {
            return model == null
                ? null
                : new ValueWriteRequestApiModel {
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    DataType = model.DataType,
                    IndexRange = model.IndexRange,
                    Value = model.Value,
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static ValueWriteRequestModel ToServiceModel(
            this ValueWriteRequestApiModel model) {
            return model == null
                ? null
                : new ValueWriteRequestModel {
                    NodeId = model.NodeId,
                    BrowsePath = model.BrowsePath,
                    DataType = model.DataType,
                    IndexRange = model.IndexRange,
                    Value = model.Value,
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueWriteResponseApiModel ToApiModel(
            this ValueWriteResultModel model) {
            return model == null
                ? null
                : new ValueWriteResponseApiModel {
                    ErrorInfo = model.ErrorInfo.ToApiModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueWriteResultModel ToServiceModel(
            this ValueWriteResponseApiModel model) {
            return model == null
                ? null
                : new ValueWriteResultModel {
                    ErrorInfo = model.ErrorInfo.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriteRequestApiModel ToApiModel(
            this WriteRequestModel model) {
            return model == null
                ? null
                : new WriteRequestApiModel {
                    Attributes = model.Attributes?
                    .Select(a => a.ToApiModel())
                    .ToList(),
                    Header = model.Header.ToApiModel()
                };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static WriteRequestModel ToServiceModel(
            this WriteRequestApiModel model) {
            return model == null
                ? null
                : new WriteRequestModel {
                    Attributes = model.Attributes?
                    .Select(a => a.ToServiceModel())
                    .ToList(),
                    Header = model.Header.ToServiceModel()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriteResponseApiModel ToApiModel(
            this WriteResultModel model) {
            return model == null
                ? null
                : new WriteResponseApiModel {
                    Results = model.Results?
                    .Select(a => a.ToApiModel())
                    .ToList()
                };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriteResultModel ToServiceModel(
            this WriteResponseApiModel model) {
            return model == null
                ? null
                : new WriteResultModel {
                    Results = model.Results?
                    .Select(a => a.ToServiceModel())
                    .ToList()
                };
        }

    }
}