// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Linq;

    /// <summary>
    /// Model extensions for twin module
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeReadRequestApiModel ToApiModel(this AttributeReadRequestModel model) {
            if (model == null) {
                return null;
            }
            return new AttributeReadRequestApiModel {
                NodeId = model.NodeId,
                Attribute = (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.NodeAttribute)model.Attribute
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static AttributeReadRequestModel ToServiceModel(this AttributeReadRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new AttributeReadRequestModel {
                NodeId = model.NodeId,
                Attribute = (Microsoft.Azure.IIoT.OpcUa.Core.Models.NodeAttribute)model.Attribute
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeReadResponseApiModel ToApiModel(this AttributeReadResultModel model) {
            if (model == null) {
                return null;
            }
            return new AttributeReadResponseApiModel {
                Value = model.Value,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }


        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeWriteRequestApiModel ToApiModel(this AttributeWriteRequestModel model) {
            if (model == null) {
                return null;
            }
            return new AttributeWriteRequestApiModel {
                NodeId = model.NodeId,
                Value = model.Value,
                Attribute = (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.NodeAttribute)model.Attribute
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static AttributeWriteRequestModel ToServiceModel(this AttributeWriteRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new AttributeWriteRequestModel {
                NodeId = model.NodeId,
                Value = model.Value,
                Attribute = (Microsoft.Azure.IIoT.OpcUa.Core.Models.NodeAttribute)model.Attribute
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static AttributeWriteResponseApiModel ToApiModel(this AttributeWriteResultModel model) {
            if (model == null) {
                return null;
            }
            return new AttributeWriteResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static AuthenticationMethodApiModel ToApiModel(this AuthenticationMethodModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodApiModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (IIoT.OpcUa.Api.Core.Models.CredentialType?)model.CredentialType ??
                    IIoT.OpcUa.Api.Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static AuthenticationMethodModel ToServiceModel(this AuthenticationMethodApiModel model) {
            if (model == null) {
                return null;
            }
            return new AuthenticationMethodModel {
                Id = model.Id,
                SecurityPolicy = model.SecurityPolicy,
                Configuration = model.Configuration,
                CredentialType = (Microsoft.Azure.IIoT.OpcUa.Core.Models.CredentialType?)model.CredentialType ??
                    IIoT.OpcUa.Core.Models.CredentialType.None
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseNextRequestInternalApiModel ToApiModel(this BrowseNextRequestModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseNextRequestInternalApiModel {
                NodeIdsOnly = model.NodeIdsOnly,
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
        public static BrowseNextRequestModel ToServiceModel(this BrowseNextRequestInternalApiModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseNextRequestModel {
                NodeIdsOnly = model.NodeIdsOnly,
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
        public static BrowseNextResponseApiModel ToApiModel(this BrowseNextResultModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseNextResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel(),
                ContinuationToken = model.ContinuationToken,
                References = model.References?
                    .Select(r => r.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowsePathRequestInternalApiModel ToApiModel(this BrowsePathRequestModel model) {
            if (model == null) {
                return null;
            }
            return new BrowsePathRequestInternalApiModel {
                NodeIdsOnly = model.NodeIdsOnly,
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
        public static BrowsePathRequestModel ToServiceModel(this BrowsePathRequestInternalApiModel model) {
            if (model == null) {
                return null;
            }
            return new BrowsePathRequestModel {
                NodeIdsOnly = model.NodeIdsOnly,
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
        public static BrowsePathResponseApiModel ToApiModel(this BrowsePathResultModel model) {
            if (model == null) {
                return null;
            }
            return new BrowsePathResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel(),
                Targets = model.Targets?
                    .Select(r => r.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseRequestInternalApiModel ToApiModel(this BrowseRequestModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseRequestInternalApiModel {
                NodeIdsOnly = model.NodeIdsOnly,
                NodeId = model.NodeId,
                MaxReferencesToReturn = model.MaxReferencesToReturn,
                Direction = (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.BrowseDirection?)model.Direction,
                ReferenceTypeId = model.ReferenceTypeId,
                NoSubtypes = model.NoSubtypes,
                TargetNodesOnly = model.TargetNodesOnly,
                ReadVariableValues = model.ReadVariableValues,
                NodeClassFilter = model.NodeClassFilter?
                    .Select(f => (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.NodeClass)f)
                    .ToList(),
                View = model.View.ToApiModel(),
                Header = model.Header.ToApiModel(),
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static BrowseRequestModel ToServiceModel(this BrowseRequestInternalApiModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseRequestModel {
                NodeIdsOnly = model.NodeIdsOnly,
                NodeId = model.NodeId,
                MaxReferencesToReturn = model.MaxReferencesToReturn,
                Direction = (Microsoft.Azure.IIoT.OpcUa.Core.Models.BrowseDirection?)model.Direction,
                View = model.View.ToServiceModel(),
                ReferenceTypeId = model.ReferenceTypeId,
                TargetNodesOnly = model.TargetNodesOnly,
                ReadVariableValues = model.ReadVariableValues,
                NodeClassFilter = model.NodeClassFilter?
                    .Select(f => (Microsoft.Azure.IIoT.OpcUa.Core.Models.NodeClass)f)
                    .ToList(),
                NoSubtypes = model.NoSubtypes,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseResponseApiModel ToApiModel(this BrowseResultModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseResponseApiModel {
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
        /// <returns></returns>
        public static BrowseViewModel ToServiceModel(this BrowseViewApiModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseViewModel {
                ViewId = model.ViewId,
                Version = model.Version,
                Timestamp = model.Timestamp
            };
        }

        /// <summary>
        /// Convert back to api model
        /// </summary>
        /// <returns></returns>
        public static BrowseViewApiModel ToApiModel(this BrowseViewModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseViewApiModel {
                ViewId = model.ViewId,
                Version = model.Version,
                Timestamp = model.Timestamp
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static CredentialApiModel ToApiModel(this CredentialModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialApiModel {
                Value = model.Value,
                Type = (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static CredentialModel ToServiceModel(this CredentialApiModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialModel {
                Value = model.Value,
                Type = (Microsoft.Azure.IIoT.OpcUa.Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static DiagnosticsApiModel ToApiModel(this DiagnosticsModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsApiModel {
                AuditId = model.AuditId,
                Level = (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.DiagnosticsLevel?)model.Level,
                TimeStamp = model.TimeStamp,
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static DiagnosticsModel ToServiceModel(this DiagnosticsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsModel {
                AuditId = model.AuditId,
                Level = (Microsoft.Azure.IIoT.OpcUa.Core.Models.DiagnosticsLevel?)model.Level,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static EndpointActivationStatusApiModel ToApiModel(this EndpointActivationStatusModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationStatusApiModel {
                Id = model.Id,
                ActivationState = (Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models.EndpointActivationState?)model.ActivationState
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static EndpointActivationStatusModel ToServiceModel(this EndpointActivationStatusApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationStatusModel {
                Id = model.Id,
                ActivationState = (Microsoft.Azure.IIoT.OpcUa.Registry.Models.EndpointActivationState?)model.ActivationState
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static EndpointApiModel ToApiModel(this EndpointModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointApiModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                Certificate = model.Certificate,
                SecurityMode = (Microsoft.Azure.IIoT.OpcUa.Api.Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static EndpointModel ToServiceModel(this EndpointApiModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointModel {
                Url = model.Url,
                AlternativeUrls = model.AlternativeUrls,
                Certificate = model.Certificate,
                SecurityMode = (Microsoft.Azure.IIoT.OpcUa.Core.Models.SecurityMode?)model.SecurityMode,
                SecurityPolicy = model.SecurityPolicy,
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextRequestApiModel ToApiModel(this HistoryReadNextRequestModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextRequestApiModel {
                ContinuationToken = model.ContinuationToken,
                Abort = model.Abort,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static HistoryReadNextRequestModel ToServiceModel(this HistoryReadNextRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextRequestModel {
                ContinuationToken = model.ContinuationToken,
                Abort = model.Abort,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadNextResponseApiModel<VariantValue> ToApiModel(this HistoryReadNextResultModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadNextResponseApiModel<VariantValue> {
                History = model.History,
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryReadRequestApiModel<VariantValue> ToApiModel(this HistoryReadRequestModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadRequestApiModel<VariantValue> {
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                IndexRange = model.IndexRange,
                Details = model.Details,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static HistoryReadRequestModel<VariantValue> ToServiceModel(this HistoryReadRequestApiModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadRequestModel<VariantValue> {
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
        public static HistoryReadResponseApiModel<VariantValue> ToApiModel(this HistoryReadResultModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryReadResponseApiModel<VariantValue> {
                History = model.History,
                ContinuationToken = model.ContinuationToken,
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateRequestApiModel<VariantValue> ToApiModel(this HistoryUpdateRequestModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateRequestApiModel<VariantValue> {
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
        public static HistoryUpdateRequestModel<VariantValue> ToServiceModel(this HistoryUpdateRequestApiModel<VariantValue> model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateRequestModel<VariantValue> {
                Details = model.Details,
                NodeId = model.NodeId,
                BrowsePath = model.BrowsePath,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static HistoryUpdateResponseApiModel ToApiModel(this HistoryUpdateResultModel model) {
            if (model == null) {
                return null;
            }
            return new HistoryUpdateResponseApiModel {
                Results = model.Results?
                .Select(r => r.ToApiModel())
                .ToList(),
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }


        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodCallArgumentApiModel ToApiModel(this MethodCallArgumentModel model) {
            if (model == null) {
                return null;
            }
            return new MethodCallArgumentApiModel {
                Value = model.Value,
                DataType = model.DataType
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodCallArgumentModel ToServiceModel(this MethodCallArgumentApiModel model) {
            if (model == null) {
                return null;
            }
            return new MethodCallArgumentModel {
                Value = model.Value,
                DataType = model.DataType
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodCallRequestApiModel ToApiModel(this MethodCallRequestModel model) {
            if (model == null) {
                return null;
            }
            return new MethodCallRequestApiModel {
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
        public static MethodCallRequestModel ToServiceModel(this MethodCallRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new MethodCallRequestModel {
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
        public static MethodCallResponseApiModel ToApiModel(this MethodCallResultModel model) {
            if (model == null) {
                return null;
            }
            return new MethodCallResponseApiModel {
                Results = model.Results?
                    .Select(a => a.ToApiModel())
                    .ToList(),
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataArgumentApiModel ToApiModel(this MethodMetadataArgumentModel model) {
            if (model == null) {
                return null;
            }
            return new MethodMetadataArgumentApiModel {
                DefaultValue = model.DefaultValue,
                Type = model.Type.ToApiModel(),
                ValueRank = (IIoT.OpcUa.Api.Core.Models.NodeValueRank?)model.ValueRank,
                ArrayDimensions = model.ArrayDimensions,
                Description = model.Description,
                Name = model.Name
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodMetadataArgumentModel ToServiceModel(this MethodMetadataArgumentApiModel model) {
            if (model == null) {
                return null;
            }
            return new MethodMetadataArgumentModel {
                DefaultValue = model.DefaultValue,
                Type = model.Type.ToServiceModel(),
                ValueRank = (IIoT.OpcUa.Core.Models.NodeValueRank?)model.ValueRank,
                ArrayDimensions = model.ArrayDimensions,
                Description = model.Description,
                Name = model.Name
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataRequestApiModel ToApiModel(this MethodMetadataRequestModel model) {
            if (model == null) {
                return null;
            }
            return new MethodMetadataRequestApiModel {
                MethodId = model.MethodId,
                MethodBrowsePath = model.MethodBrowsePath,
                Header = model.Header.ToApiModel()
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static MethodMetadataRequestModel ToServiceModel(this MethodMetadataRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new MethodMetadataRequestModel {
                MethodId = model.MethodId,
                MethodBrowsePath = model.MethodBrowsePath,
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static MethodMetadataResponseApiModel ToApiModel(this MethodMetadataResultModel model) {
            if (model == null) {
                return null;
            }
            return new MethodMetadataResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel(),
                ObjectId = model.ObjectId,
                InputArguments = model.InputArguments?
                    .Select(a => a.ToApiModel())
                    .ToList(),
                OutputArguments = model.OutputArguments?
                    .Select(a => a.ToApiModel())
                    .ToList(),
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ModelUploadStartRequestApiModel ToApiModel(this ModelUploadStartRequestModel model) {
            if (model == null) {
                return null;
            }
            return new ModelUploadStartRequestApiModel {
                ContentEncoding = model.ContentEncoding,
                Diagnostics = model.Diagnostics.ToApiModel()
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ModelUploadStartRequestModel ToServiceModel(this ModelUploadStartRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ModelUploadStartRequestModel {
                ContentEncoding = model.ContentEncoding,
                Diagnostics = model.Diagnostics.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ModelUploadStartResponseApiModel ToApiModel(this ModelUploadStartResultModel model) {
            if (model == null) {
                return null;
            }
            return new ModelUploadStartResponseApiModel {
                BlobName = model.BlobName,
                ContentEncoding = model.ContentEncoding,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public static ModelUploadStartResultModel ToServiceModel(this ModelUploadStartResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new ModelUploadStartResultModel {
                BlobName = model.BlobName,
                ContentEncoding = model.ContentEncoding,
                TimeStamp = model.TimeStamp
            };
        }

        /// <summary>
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static NodeApiModel ToApiModel(this NodeModel model) {
            if (model == null) {
                return null;
            }
            return new NodeApiModel {
                NodeId = model.NodeId,
                Children = model.Children,
                BrowseName = model.BrowseName,
                DisplayName = model.DisplayName,
                Description = model.Description,
                NodeClass = (IIoT.OpcUa.Api.Core.Models.NodeClass?)model.NodeClass,
                IsAbstract = model.IsAbstract,
                AccessLevel = (IIoT.OpcUa.Api.Core.Models.NodeAccessLevel?)model.AccessLevel,
                EventNotifier = (IIoT.OpcUa.Api.Core.Models.NodeEventNotifier?)model.EventNotifier,
                Executable = model.Executable,
                DataType = model.DataType,
                ValueRank = (IIoT.OpcUa.Api.Core.Models.NodeValueRank?)model.ValueRank,
                AccessRestrictions = (IIoT.OpcUa.Api.Core.Models.NodeAccessRestrictions?)model.AccessRestrictions,
                ArrayDimensions = model.ArrayDimensions,
                ContainsNoLoops = model.ContainsNoLoops,
                DataTypeDefinition = model.DataTypeDefinition,
                Value = model.Value,
                Historizing = model.Historizing,
                InverseName = model.InverseName,
                ErrorInfo = model.ErrorInfo.ToApiModel(),
                ServerPicoseconds = model.ServerPicoseconds,
                SourcePicoseconds = model.SourcePicoseconds,
                SourceTimestamp = model.SourceTimestamp,
                ServerTimestamp = model.ServerTimestamp,
                MinimumSamplingInterval = model.MinimumSamplingInterval,
                Symmetric = model.Symmetric,
                UserAccessLevel = (IIoT.OpcUa.Api.Core.Models.NodeAccessLevel?)model.UserAccessLevel,
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
        public static NodeModel ToServiceModel(this NodeApiModel model) {
            if (model == null) {
                return null;
            }
            return new NodeModel {
                NodeId = model.NodeId,
                Children = model.Children,
                BrowseName = model.BrowseName,
                DisplayName = model.DisplayName,
                Description = model.Description,
                NodeClass = (IIoT.OpcUa.Core.Models.NodeClass?)model.NodeClass,
                IsAbstract = model.IsAbstract,
                AccessLevel = (IIoT.OpcUa.Core.Models.NodeAccessLevel?)model.AccessLevel,
                EventNotifier = (IIoT.OpcUa.Core.Models.NodeEventNotifier?)model.EventNotifier,
                Executable = model.Executable,
                DataType = model.DataType,
                ValueRank = (IIoT.OpcUa.Core.Models.NodeValueRank?)model.ValueRank,
                AccessRestrictions = (IIoT.OpcUa.Core.Models.NodeAccessRestrictions?)model.AccessRestrictions,
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
                UserAccessLevel = (IIoT.OpcUa.Core.Models.NodeAccessLevel?)model.UserAccessLevel,
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
        public static NodePathTargetApiModel ToApiModel(this NodePathTargetModel model) {
            if (model == null) {
                return null;
            }
            return new NodePathTargetApiModel {
                RemainingPathIndex = model.RemainingPathIndex,
                BrowsePath = model.BrowsePath,
                Target = model.Target.ToApiModel()
            };
        }

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public static NodeReferenceApiModel ToApiModel(this NodeReferenceModel model) {
            if (model == null) {
                return null;
            }
            return new NodeReferenceApiModel {
                ReferenceTypeId = model.ReferenceTypeId,
                Direction = (IIoT.OpcUa.Api.Core.Models.BrowseDirection?)model.Direction,
                Target = model.Target.ToApiModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ReadRequestApiModel ToApiModel(this ReadRequestModel model) {
            if (model == null) {
                return null;
            }
            return new ReadRequestApiModel {
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
        public static ReadRequestModel ToServiceModel(this ReadRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ReadRequestModel {
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
        public static ReadResponseApiModel ToApiModel(this ReadResultModel model) {
            if (model == null) {
                return null;
            }
            return new ReadResponseApiModel {
                Results = model.Results?
                    .Select(a => a.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static RequestHeaderApiModel ToApiModel(this RequestHeaderModel model) {
            if (model == null) {
                return null;
            }
            return new RequestHeaderApiModel {
                Elevation = model.Elevation.ToApiModel(),
                Diagnostics = model.Diagnostics.ToApiModel(),
                Locales = model.Locales
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static RequestHeaderModel ToServiceModel(this RequestHeaderApiModel model) {
            if (model == null) {
                return null;
            }
            return new RequestHeaderModel {
                Diagnostics = model.Diagnostics.ToServiceModel(),
                Elevation = model.Elevation.ToServiceModel(),
                Locales = model.Locales
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static RolePermissionApiModel ToApiModel(this RolePermissionModel model) {
            if (model == null) {
                return null;
            }
            return new RolePermissionApiModel {
                RoleId = model.RoleId,
                Permissions = (IIoT.OpcUa.Api.Core.Models.RolePermissions?)model.Permissions
            };
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public static RolePermissionModel ToServiceModel(this RolePermissionApiModel model) {
            if (model == null) {
                return null;
            }
            return new RolePermissionModel {
                RoleId = model.RoleId,
                Permissions = (IIoT.OpcUa.Core.Models.RolePermissions?)model.Permissions
            };
        }

        /// <summary>
        /// Create node api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static ServiceResultApiModel ToApiModel(this ServiceResultModel model) {
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
        /// <returns></returns>
        public static ServiceResultModel ToServiceModel(this ServiceResultApiModel model) {
            if (model == null) {
                return null;
            }
            return new ServiceResultModel {
                Diagnostics = model.Diagnostics,
                StatusCode = model.StatusCode,
                ErrorMessage = model.ErrorMessage
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static SupervisorStatusApiModel ToApiModel(this SupervisorStatusModel model) {
            if (model == null) {
                return null;
            }
            return new SupervisorStatusApiModel {
                DeviceId = model.DeviceId,
                ModuleId = model.ModuleId,
                SiteId = model.SiteId,
                Endpoints = model.Endpoints?
                    .Select(e => e.ToApiModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static ValueReadRequestApiModel ToApiModel(this ValueReadRequestModel model) {
            if (model == null) {
                return null;
            }
            return new ValueReadRequestApiModel {
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
        public static ValueReadRequestModel ToServiceModel(this ValueReadRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ValueReadRequestModel {
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
        public static ValueReadResponseApiModel ToApiModel(this ValueReadResultModel model) {
            if (model == null) {
                return null;
            }
            return new ValueReadResponseApiModel {
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
        public static ValueWriteRequestApiModel ToApiModel(this ValueWriteRequestModel model) {
            if (model == null) {
                return null;
            }
            return new ValueWriteRequestApiModel {
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
        public static ValueWriteRequestModel ToServiceModel(this ValueWriteRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new ValueWriteRequestModel {
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
        public static ValueWriteResponseApiModel ToApiModel(this ValueWriteResultModel model) {
            if (model == null) {
                return null;
            }
            return new ValueWriteResponseApiModel {
                ErrorInfo = model.ErrorInfo.ToApiModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriteRequestApiModel ToApiModel(this WriteRequestModel model) {
            if (model == null) {
                return null;
            }
            return new WriteRequestApiModel {
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
        public static WriteRequestModel ToServiceModel(this WriteRequestApiModel model) {
            if (model == null) {
                throw null;
            }
            return new WriteRequestModel {
                Attributes = model.Attributes?.Select(a => a.ToServiceModel()).ToList(),
                Header = model.Header.ToServiceModel()
            };
        }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static WriteResponseApiModel ToApiModel(this WriteResultModel model) {
            if (model == null) {
                throw null;
            }
            return new WriteResponseApiModel {
                Results = model.Results?
                    .Select(a => a.ToApiModel())
                    .ToList()
            };
        }
    }
}
