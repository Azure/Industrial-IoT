// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Linq;

    /// <summary>
    /// Credential model
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static CredentialModel ToServiceModel(
            this CredentialApiModel model) {
            if (model == null) {
                return null;
            }
            return new CredentialModel {
                Value = model.Value,
                Type = (IIoT.OpcUa.Core.Models.CredentialType?)model.Type
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        public static DiagnosticsModel ToServiceModel(
            this DiagnosticsApiModel model) {
            if (model == null) {
                return null;
            }
            return new DiagnosticsModel {
                AuditId = model.AuditId,
                Level = (IIoT.OpcUa.Core.Models.DiagnosticsLevel?)model.Level,
                TimeStamp = model.TimeStamp
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
                SamplingInterval = model.SamplingInterval,
                PublishingInterval = model.PublishingInterval
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
        public static PublishStartRequestModel ToServiceModel(this PublishStartRequestApiModel model) {
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
        public static PublishStartResponseApiModel ToApiModel(this PublishStartResultModel model) {
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
        public static PublishStopRequestModel ToServiceModel(this PublishStopRequestApiModel model) {
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
        public static PublishStopResponseApiModel ToApiModel(this PublishStopResultModel model) {
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
        public static PublishBulkRequestModel ToServiceModel(this PublishBulkRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new PublishBulkRequestModel {
                NodesToAdd = model.NodesToAdd?.Select(n => n.ToServiceModel()).ToList(),
                NodesToRemove = model.NodesToRemove?.ToList(),
                Header = model.Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public static PublishBulkResponseApiModel ToApiModel(this PublishBulkResultModel model) {
            if (model == null) {
                return null;
            }
            return new PublishBulkResponseApiModel {
                NodesToAdd = model.NodesToAdd?.Select(n => n.ToApiModel()).ToList(),
                NodesToRemove = model.NodesToRemove?.Select(n => n.ToApiModel()).ToList()
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
                Diagnostics = model.Diagnostics?.ToServiceModel(),
                Elevation = model.Elevation?.ToServiceModel(),
                Locales = model.Locales
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
    }
}
