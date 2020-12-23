// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System.Linq;

    /// <summary>
    /// Model extensions for twin module
    /// </summary>
    public static class TwinExtensions {

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public static BrowseNextRequestInternalApiModel ToApiModel(
            this BrowseNextRequestModel model) {
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
        public static BrowseNextRequestModel ToServiceModel(
            this BrowseNextRequestInternalApiModel model) {
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
        public static BrowsePathRequestInternalApiModel ToApiModel(
            this BrowsePathRequestModel model) {
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
        public static BrowsePathRequestModel ToServiceModel(
            this BrowsePathRequestInternalApiModel model) {
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
        public static BrowseRequestInternalApiModel ToApiModel(
            this BrowseRequestModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseRequestInternalApiModel {
                NodeIdsOnly = model.NodeIdsOnly,
                NodeId = model.NodeId,
                MaxReferencesToReturn = model.MaxReferencesToReturn,
                Direction = (IIoT.OpcUa.Api.Core.Models.BrowseDirection?)model.Direction,
                View = model.View.ToApiModel(),
                ReferenceTypeId = model.ReferenceTypeId,
                TargetNodesOnly = model.TargetNodesOnly,
                ReadVariableValues = model.ReadVariableValues,
                NodeClassFilter = model.NodeClassFilter?
                    .Select(f => (IIoT.OpcUa.Api.Core.Models.NodeClass)f)
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
            this BrowseRequestInternalApiModel model) {
            if (model == null) {
                return null;
            }
            return new BrowseRequestModel {
                NodeIdsOnly = model.NodeIdsOnly,
                NodeId = model.NodeId,
                MaxReferencesToReturn = model.MaxReferencesToReturn,
                Direction = (BrowseDirection?)model.Direction,
                View = model.View.ToServiceModel(),
                ReferenceTypeId = model.ReferenceTypeId,
                TargetNodesOnly = model.TargetNodesOnly,
                ReadVariableValues = model.ReadVariableValues,
                NodeClassFilter = model.NodeClassFilter?
                    .Select(f => (NodeClass)f)
                    .ToList(),
                NoSubtypes = model.NoSubtypes,
                Header = model.Header.ToServiceModel()
            };
        }
    }
}
