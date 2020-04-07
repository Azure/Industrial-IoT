// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Auth.Models;
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
            if (model == null) {
                return null;
            }
            return new MonitoredItemMessageApiModel {
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
                Value = model.Value?.Copy(),
                DataType = model.DataType,
                Status = model.Status
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ConfigurationVersionApiModel ToApiModel(
            this ConfigurationVersionModel model) {
            if (model == null) {
                return null;
            }
            return new ConfigurationVersionApiModel {
                MajorVersion = model.MajorVersion,
                MinorVersion = model.MinorVersion
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ConfigurationVersionModel ToServiceModel(
            this ConfigurationVersionApiModel model) {
            if (model == null) {
                return null;
            }
            return new ConfigurationVersionModel {
                MajorVersion = model.MajorVersion,
                MinorVersion = model.MinorVersion
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ConnectionApiModel ToApiModel(
            this ConnectionModel model) {
            if (model == null) {
                return null;
            }
            return new ConnectionApiModel {
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
            if (model == null) {
                return null;
            }
            return new ConnectionModel {
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
            if (model == null) {
                return null;
            }
            return new ContentFilterApiModel {
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
            if (model == null) {
                return null;
            }
            return new ContentFilterModel {
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
            if (model == null) {
                return null;
            }
            return new ContentFilterElementApiModel {
                FilterOperands = model.FilterOperands?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                FilterOperator = (Core.Models.FilterOperatorType)model.FilterOperator
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ContentFilterElementModel ToServiceModel(
            this ContentFilterElementApiModel model) {
            if (model == null) {
                return null;
            }
            return new ContentFilterElementModel {
                FilterOperands = model.FilterOperands?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                FilterOperator = (OpcUa.Core.Models.FilterOperatorType)model.FilterOperator
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static CredentialApiModel ToApiModel(
            this CredentialModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialApiModel {
                Value = model.Value,
                Type = (Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static CredentialModel ToServiceModel(
            this CredentialApiModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialModel {
                Value = model.Value,
                Type = (OpcUa.Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static DataSetMetaDataApiModel ToApiModel(
            this DataSetMetaDataModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetMetaDataApiModel {
                Name = model.Name,
                ConfigurationVersion = model.ConfigurationVersion.ToApiModel(),
                DataSetClassId = model.DataSetClassId,
                Description = model.Description.ToApiModel(),
                Fields = model.Fields?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                EnumDataTypes = model.EnumDataTypes?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                StructureDataTypes = model.StructureDataTypes?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                SimpleDataTypes = model.SimpleDataTypes?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                Namespaces = model.Namespaces?.ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DataSetMetaDataModel ToServiceModel(
            this DataSetMetaDataApiModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetMetaDataModel {
                Name = model.Name,
                ConfigurationVersion = model.ConfigurationVersion.ToServiceModel(),
                DataSetClassId = model.DataSetClassId,
                Description = model.Description.ToServiceModel(),
                Fields = model.Fields?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                EnumDataTypes = model.EnumDataTypes?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                StructureDataTypes = model.StructureDataTypes?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                SimpleDataTypes = model.SimpleDataTypes?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                Namespaces = model.Namespaces?.ToList()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static DataSetWriterApiModel ToApiModel(
            this DataSetWriterModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetWriterApiModel {
                DataSetWriterId = model.DataSetWriterId,
                DataSet = model.DataSet.ToApiModel(),
                DataSetFieldContentMask = (Models.DataSetFieldContentMask?)model.DataSetFieldContentMask,
                DataSetMetaDataSendInterval = model.DataSetMetaDataSendInterval,
                KeyFrameCount = model.KeyFrameCount,
                KeyFrameInterval = model.KeyFrameInterval,
                MessageSettings = model.MessageSettings.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DataSetWriterModel ToServiceModel(
            this DataSetWriterApiModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetWriterModel {
                DataSetWriterId = model.DataSetWriterId,
                DataSet = model.DataSet.ToServiceModel(),
                DataSetFieldContentMask = (OpcUa.Publisher.Models.DataSetFieldContentMask?)model.DataSetFieldContentMask,
                DataSetMetaDataSendInterval = model.DataSetMetaDataSendInterval,
                KeyFrameCount = model.KeyFrameCount,
                KeyFrameInterval = model.KeyFrameInterval,
                MessageSettings = model.MessageSettings.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static DataSetWriterMessageSettingsApiModel ToApiModel(
            this DataSetWriterMessageSettingsModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetWriterMessageSettingsApiModel {
                ConfiguredSize = model.ConfiguredSize,
                DataSetMessageContentMask = (Models.DataSetContentMask?)model.DataSetMessageContentMask,
                DataSetOffset = model.DataSetOffset,
                NetworkMessageNumber = model.NetworkMessageNumber
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DataSetWriterMessageSettingsModel ToServiceModel(
            this DataSetWriterMessageSettingsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DataSetWriterMessageSettingsModel {
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
            if (model == null) {
                return null;
            }
            return new DiagnosticsApiModel {
                AuditId = model.AuditId,
                Level = (Core.Models.DiagnosticsLevel?)model.Level,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DiagnosticsModel ToServiceModel(
            this DiagnosticsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsModel {
                AuditId = model.AuditId,
                Level = (OpcUa.Core.Models.DiagnosticsLevel?)model.Level,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EndpointApiModel ToApiModel(
            this EndpointModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointApiModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                Certificate = model.Certificate,
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EndpointModel ToServiceModel(
            this EndpointApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                Certificate = model.Certificate,
                SecurityMode = (OpcUa.Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EngineConfigurationApiModel ToApiModel(
            this EngineConfigurationModel model) {
            if (model == null) {
                return null;
            }
            return new EngineConfigurationApiModel {
                BatchSize = model.BatchSize,
                DiagnosticsInterval = model.DiagnosticsInterval
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EngineConfigurationModel ToServiceModel(
            this EngineConfigurationApiModel model) {
            if (model == null) {
                return null;
            }
            return new EngineConfigurationModel {
                BatchSize = model.BatchSize,
                DiagnosticsInterval = model.DiagnosticsInterval
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EnumDefinitionApiModel ToApiModel(
            this EnumDefinitionModel model) {
            if (model == null) {
                return null;
            }
            return new EnumDefinitionApiModel {
                Fields = model.Fields?
                    .Select(f => f.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EnumDefinitionModel ToServiceModel(
            this EnumDefinitionApiModel model) {
            if (model == null) {
                return null;
            }
            return new EnumDefinitionModel {
                Fields = model.Fields?
                    .Select(f => f.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EnumDescriptionApiModel ToApiModel(
            this EnumDescriptionModel model) {
            if (model == null) {
                return null;
            }
            return new EnumDescriptionApiModel {
                Name = model.Name,
                BuiltInType = model.BuiltInType,
                DataTypeId = model.DataTypeId,
                EnumDefinition = model.EnumDefinition.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EnumDescriptionModel ToServiceModel(
            this EnumDescriptionApiModel model) {
            if (model == null) {
                return null;
            }
            return new EnumDescriptionModel {
                Name = model.Name,
                BuiltInType = model.BuiltInType,
                DataTypeId = model.DataTypeId,
                EnumDefinition = model.EnumDefinition.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EnumFieldApiModel ToApiModel(
            this EnumFieldModel model) {
            if (model == null) {
                return null;
            }
            return new EnumFieldApiModel {
                Name = model.Name,
                Description = model.Description.ToApiModel(),
                DisplayName = model.DisplayName.ToApiModel(),
                Value = model.Value
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EnumFieldModel ToServiceModel(
            this EnumFieldApiModel model) {
            if (model == null) {
                return null;
            }
            return new EnumFieldModel {
                Name = model.Name,
                Description = model.Description.ToServiceModel(),
                DisplayName = model.DisplayName.ToServiceModel(),
                Value = model.Value
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static FieldMetaDataApiModel ToApiModel(
            this FieldMetaDataModel model) {
            if (model == null) {
                return null;
            }
            return new FieldMetaDataApiModel {
                Description = model.Description.ToApiModel(),
                ArrayDimensions = model.ArrayDimensions?.ToList(),
                BuiltInType = model.BuiltInType,
                DataSetFieldId = model.DataSetFieldId,
                DataTypeId = model.DataTypeId,
                FieldFlags = model.FieldFlags,
                MaxStringLength = model.MaxStringLength,
                Name = model.Name,
                Properties = model.Properties?
                    .ToDictionary(k => k.Key, v => v.Value),
                ValueRank = model.ValueRank
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static FieldMetaDataModel ToServiceModel(
            this FieldMetaDataApiModel model) {
            if (model == null) {
                return null;
            }
            return new FieldMetaDataModel {
                Description = model.Description.ToServiceModel(),
                ArrayDimensions = model.ArrayDimensions?.ToList(),
                BuiltInType = model.BuiltInType,
                DataSetFieldId = model.DataSetFieldId,
                DataTypeId = model.DataTypeId,
                FieldFlags = model.FieldFlags,
                MaxStringLength = model.MaxStringLength,
                Name = model.Name,
                Properties = model.Properties?
                    .ToDictionary(k => k.Key, v => v.Value),
                ValueRank = model.ValueRank
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static FilterOperandApiModel ToApiModel(
            this FilterOperandModel model) {
            if (model == null) {
                return null;
            }
            return new FilterOperandApiModel {
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
        /// Create service model from api model
        /// </summary>
        public static FilterOperandModel ToServiceModel(
            this FilterOperandApiModel model) {
            if (model == null) {
                return null;
            }
            return new FilterOperandModel {
                Index = model.Index,
                Alias = model.Alias,
                Value = model.Value,
                NodeId = model.NodeId,
                AttributeId = (OpcUa.Core.Models.NodeAttribute?)model.AttributeId,
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
            if (model == null) {
                return null;
            }
            return new IdentityTokenApiModel {
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
            if (model == null) {
                return null;
            }
            return new IdentityTokenModel {
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
            if (model == null) {
                return null;
            }
            return new LocalizedTextApiModel {
                Locale = model.Locale,
                Text = model.Text
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static LocalizedTextModel ToServiceModel(
            this LocalizedTextApiModel model) {
            if (model == null) {
                return null;
            }
            return new LocalizedTextModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataItemsApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataItemsModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetModel {
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
        public static PublishedDataSetEventsApiModel ToApiModel(
            this PublishedDataSetEventsModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetEventsApiModel {
                Id = model.Id,
                DiscardNew = model.DiscardNew,
                EventNotifier = model.EventNotifier,
                BrowsePath = model.BrowsePath,
                Filter = model.Filter.ToApiModel(),
                QueueSize = model.QueueSize,
                MonitoringMode = (Models.MonitoringMode?)model.MonitoringMode,
                TriggerId = model.TriggerId,
                SelectedFields = model.SelectedFields?
                    .Select(f => f.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedDataSetEventsModel ToServiceModel(
            this PublishedDataSetEventsApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetEventsModel {
                Id = model.Id,
                DiscardNew = model.DiscardNew,
                EventNotifier = model.EventNotifier,
                BrowsePath = model.BrowsePath,
                Filter = model.Filter.ToServiceModel(),
                QueueSize = model.QueueSize,
                MonitoringMode = (OpcUa.Publisher.Models.MonitoringMode?)model.MonitoringMode,
                TriggerId = model.TriggerId,
                SelectedFields = model.SelectedFields?
                    .Select(f => f.ToServiceModel())
                    .ToList()
            };
        }


        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedDataSetSettingsApiModel ToApiModel(
            this PublishedDataSetSettingsModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSettingsApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSettingsModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSourceApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSourceModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetVariableApiModel {
                Id = model.Id,
                PublishedVariableNodeId = model.PublishedVariableNodeId,
                BrowsePath = model.BrowsePath,
                Attribute = model.Attribute,
                DataChangeFilter = (Models.DataChangeTriggerType?)model.DataChangeFilter,
                DeadbandType = (Models.DeadbandType?)model.DeadbandType,
                DeadbandValue = model.DeadbandValue,
                DiscardNew = model.DiscardNew,
                IndexRange = model.IndexRange,
                MonitoringMode = (Models.MonitoringMode?)model.MonitoringMode,
                MetaDataProperties = model.MetaDataProperties?.ToList(),
                QueueSize = model.QueueSize,
                SamplingInterval = model.SamplingInterval,
                TriggerId = model.TriggerId,
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
            if (model == null) {
                return null;
            }
            return new PublishedDataSetVariableModel {
                Id = model.Id,
                PublishedVariableNodeId = model.PublishedVariableNodeId,
                BrowsePath = model.BrowsePath,
                Attribute = model.Attribute,
                DataChangeFilter = (OpcUa.Publisher.Models.DataChangeTriggerType?)model.DataChangeFilter,
                DeadbandType = (OpcUa.Publisher.Models.DeadbandType?)model.DeadbandType,
                DeadbandValue = model.DeadbandValue,
                DiscardNew = model.DiscardNew,
                IndexRange = model.IndexRange,
                MonitoringMode = (OpcUa.Publisher.Models.MonitoringMode?)model.MonitoringMode,
                MetaDataProperties = model.MetaDataProperties?
                    .ToList(),
                QueueSize = model.QueueSize,
                SamplingInterval = model.SamplingInterval,
                TriggerId = model.TriggerId,
                HeartbeatInterval = model.HeartbeatInterval,
                PublishedVariableDisplayName = model.PublishedVariableDisplayName,
                SubstituteValue = model.SubstituteValue?.Copy()
            };
        }

        /// <summary>
        /// Create config
        /// </summary>
        /// <param name="model"></param>
        public static PublisherConfigApiModel ToApiModel(
            this AgentConfigModel model) {
            if (model == null) {
                return null;
            }
            return new PublisherConfigApiModel {
                Capabilities = model.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static AgentConfigModel ToServiceModel(
            this PublisherConfigApiModel model) {
            if (model == null) {
                return null;
            }
            return new AgentConfigModel {
                Capabilities = model.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value),
                HeartbeatInterval = model.HeartbeatInterval,
                JobCheckInterval = model.JobCheckInterval,
                JobOrchestratorUrl = model.JobOrchestratorUrl,
                MaxWorkers = model.MaxWorkers
            };
        }


        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static SimpleAttributeOperandApiModel ToApiModel(
            this SimpleAttributeOperandModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperandApiModel {
                NodeId = model.NodeId,
                AttributeId = (Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static SimpleAttributeOperandModel ToServiceModel(
            this SimpleAttributeOperandApiModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleAttributeOperandModel {
                NodeId = model.NodeId,
                AttributeId = (OpcUa.Core.Models.NodeAttribute?)model.AttributeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange
            };
        }


        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static SimpleTypeDescriptionApiModel ToApiModel(
            this SimpleTypeDescriptionModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleTypeDescriptionApiModel {
                BaseDataTypeId = model.BaseDataTypeId,
                Name = model.Name,
                DataTypeId = model.DataTypeId,
                BuiltInType = model.BuiltInType
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static SimpleTypeDescriptionModel ToServiceModel(
            this SimpleTypeDescriptionApiModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleTypeDescriptionModel {
                BaseDataTypeId = model.BaseDataTypeId,
                Name = model.Name,
                DataTypeId = model.DataTypeId,
                BuiltInType = model.BuiltInType
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static StructureDefinitionApiModel ToApiModel(
            this StructureDefinitionModel model) {
            if (model == null) {
                return null;
            }
            return new StructureDefinitionApiModel {
                BaseDataTypeId = model.BaseDataTypeId,
                Fields = model.Fields?
                    .Select(f => f.ToApiModel())
                    .ToList(),
                StructureType = (Core.Models.StructureType)model.StructureType
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static StructureDefinitionModel ToServiceModel(
            this StructureDefinitionApiModel model) {
            if (model == null) {
                return null;
            }
            return new StructureDefinitionModel {
                BaseDataTypeId = model.BaseDataTypeId,
                Fields = model.Fields?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                StructureType = (OpcUa.Core.Models.StructureType)model.StructureType
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static StructureDescriptionApiModel ToApiModel(
            this StructureDescriptionModel model) {
            if (model == null) {
                return null;
            }
            return new StructureDescriptionApiModel {
                DataTypeId = model.DataTypeId,
                Name = model.Name,
                StructureDefinition = model.StructureDefinition.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static StructureDescriptionModel ToServiceModel(
            this StructureDescriptionApiModel model) {
            if (model == null) {
                return null;
            }
            return new StructureDescriptionModel {
                DataTypeId = model.DataTypeId,
                Name = model.Name,
                StructureDefinition = model.StructureDefinition.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static StructureFieldApiModel ToApiModel(
            this StructureFieldModel model) {
            if (model == null) {
                return null;
            }
            return new StructureFieldApiModel {
                ArrayDimensions = model.ArrayDimensions?.ToList(),
                DataTypeId = model.DataTypeId,
                Description = model.Description.ToApiModel(),
                IsOptional = model.IsOptional,
                MaxStringLength = model.MaxStringLength,
                Name = model.Name,
                ValueRank = (Core.Models.NodeValueRank?)model.ValueRank
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static StructureFieldModel ToServiceModel(
            this StructureFieldApiModel model) {
            if (model == null) {
                return null;
            }
            return new StructureFieldModel {
                ArrayDimensions = model.ArrayDimensions?.ToList(),
                DataTypeId = model.DataTypeId,
                Description = model.Description.ToServiceModel(),
                IsOptional = model.IsOptional,
                MaxStringLength = model.MaxStringLength,
                Name = model.Name,
                ValueRank = (OpcUa.Core.Models.NodeValueRank?)model.ValueRank
            };
        }
        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriterGroupApiModel ToApiModel(
            this WriterGroupModel model) {
            if (model == null) {
                return null;
            }
            return new WriterGroupApiModel {
                WriterGroupId = model.WriterGroupId,
                HeaderLayoutUri = model.HeaderLayoutUri,
                KeepAliveTime = model.KeepAliveTime,
                LocaleIds = model.LocaleIds?.ToList(),
                MaxNetworkMessageSize = model.MaxNetworkMessageSize,
                MessageSettings = model.MessageSettings.ToApiModel(),
                MessageType = (Models.MessageEncoding?)model.MessageType,
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
                SecurityMode = (Core.Models.SecurityMode?)model.SecurityMode
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static WriterGroupModel ToServiceModel(
            this WriterGroupApiModel model) {
            if (model == null) {
                return null;
            }
            return new WriterGroupModel {
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
                SecurityMode = (OpcUa.Core.Models.SecurityMode?)model.SecurityMode
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriterGroupJobApiModel ToApiModel(
            this WriterGroupJobModel model) {
            if (model?.WriterGroup == null) {
                return null;
            }
            return new WriterGroupJobApiModel {
                WriterGroup = model.WriterGroup.ToApiModel(),
                ConnectionString = model.ConnectionString,
                Engine = model.Engine.ToApiModel(),
                MessagingMode = (Models.MessagingMode?)model.MessagingMode
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static WriterGroupJobModel ToServiceModel(
            this WriterGroupJobApiModel model) {
            if (model == null) {
                return null;
            }
            return new WriterGroupJobModel {
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
            if (model == null) {
                return null;
            }
            return new WriterGroupMessageSettingsApiModel {
                NetworkMessageContentMask = (Models.NetworkMessageContentMask?)model.NetworkMessageContentMask,
                DataSetOrdering = (Models.DataSetOrderingType?)model.DataSetOrdering,
                GroupVersion = model.GroupVersion,
                PublishingOffset = model.PublishingOffset,
                SamplingOffset = model.SamplingOffset
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static WriterGroupMessageSettingsModel ToServiceModel(
            this WriterGroupMessageSettingsApiModel model) {
            if (model == null) {
                return null;
            }
            return new WriterGroupMessageSettingsModel {
                NetworkMessageContentMask = (OpcUa.Publisher.Models.NetworkMessageContentMask?)model.NetworkMessageContentMask,
                DataSetOrdering = (OpcUa.Publisher.Models.DataSetOrderingType?)model.DataSetOrdering,
                GroupVersion = model.GroupVersion,
                PublishingOffset = model.PublishingOffset,
                SamplingOffset = model.SamplingOffset
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedItemApiModel ToApiModel(
            this PublishedItemModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedItemApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedItemModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedItemListRequestApiModel {
                ContinuationToken = model.ContinuationToken
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishedItemListRequestModel ToServiceModel(
            this PublishedItemListRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedItemListRequestModel {
                ContinuationToken = model.ContinuationToken
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishedItemListResponseApiModel ToApiModel(
            this PublishedItemListResultModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedItemListResponseApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishedItemListResultModel {
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
            if (model == null) {
                return null;
            }
            return new PublishStartRequestApiModel {
                Item = model.Item?.ToApiModel(),
                Header = model.Header?.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishStartRequestModel ToServiceModel(
            this PublishStartRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishStartRequestModel {
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
            if (model == null) {
                return null;
            }
            return new PublishStartResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static PublishStartResultModel ToServiceModel(
            this PublishStartResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishStartResultModel {
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static PublishStopRequestApiModel ToApiModel(
            this PublishStopRequestModel model) {
            if (model == null) {
                return null;
            }
            return new PublishStopRequestApiModel {
                NodeId = model.NodeId,
                Header = model.Header?.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static PublishStopRequestModel ToServiceModel(
            this PublishStopRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishStopRequestModel {
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
            if (model == null) {
                return null;
            }
            return new PublishStopResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        /// <param name="model"></param>
        public static PublishStopResultModel ToServiceModel(
            this PublishStopResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishStopResultModel {
                ErrorInfo = model.ErrorInfo.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        public static PublishBulkRequestApiModel ToApiModel(
            this PublishBulkRequestModel model) {
            if (model == null) {
                return null;
            }
            return new PublishBulkRequestApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishBulkRequestModel {
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
            if (model == null) {
                return null;
            }
            return new PublishBulkResponseApiModel {
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
            if (model == null) {
                return null;
            }
            return new PublishBulkResultModel {
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
            if (model == null) {
                return null;
            }
            return new RequestHeaderApiModel {
                Diagnostics = model.Diagnostics?.ToApiModel(),
                Elevation = model.Elevation?.ToApiModel(),
                Locales = model.Locales
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static RequestHeaderModel ToServiceModel(
            this RequestHeaderApiModel model) {
            if (model == null) {
                return null;
            }
            return new RequestHeaderModel {
                Diagnostics = model.Diagnostics?.ToServiceModel(),
                Elevation = model.Elevation?.ToServiceModel(),
                Locales = model.Locales
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultApiModel ToApiModel(
            this ServiceResultModel model) {
            if (model == null) {
                return null;
            }
            return new ServiceResultApiModel {
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
            if (model == null) {
                return null;
            }
            return new ServiceResultModel {
                Diagnostics = model.Diagnostics,
                ErrorMessage = model.ErrorMessage,
                StatusCode = model.StatusCode
            };
        }
    }
}