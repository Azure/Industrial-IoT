// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using System.Linq;

    /// <summary>
    /// Demand model
    /// </summary>
    public static class ModelExtensions {

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DemandApiModel ToApiModel(
            this DemandModel model) {
            if (model == null) {
                return null;
            }
            return new DemandApiModel {
                Key = model.Key,
                Operator = (Api.Jobs.Models.DemandOperators?)model.Operator,
                Value = model.Value
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static JobInfoApiModel ToApiModel(
            this JobInfoModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoApiModel {
                LifetimeData = model.LifetimeData.ToApiModel(),
                RedundancyConfig = model.RedundancyConfig.ToApiModel(),
                Demands = model.Demands?
                    .Select(d => d.ToApiModel())
                    .ToList(),
                JobConfiguration = model.JobConfiguration?.DeepClone(),
                JobConfigurationType = model.JobConfigurationType,
                Name = model.Name,
                Id = model.Id
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static JobLifetimeDataApiModel ToApiModel(
            this JobLifetimeDataModel model) {
            if (model == null) {
                return null;
            }
            return new JobLifetimeDataApiModel {
                Status = (Api.Jobs.Models.JobStatus)model.Status,
                Updated = model.Updated,
                Created = model.Created,
                ProcessingStatus = model.ProcessingStatus?
                    .ToDictionary(k => k.Key, v => v.Value.ToApiModel())
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static ProcessingStatusApiModel ToApiModel(
            this ProcessingStatusModel model) {
            if (model == null) {
                return null;
            }
            return new ProcessingStatusApiModel {
                LastKnownHeartbeat = model.LastKnownHeartbeat,
                LastKnownState = model.LastKnownState?.DeepClone(),
                ProcessMode = (Api.Jobs.Models.ProcessMode?)model.ProcessMode
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static RedundancyConfigApiModel ToApiModel(
            this RedundancyConfigModel model) {
            if (model == null) {
                return null;
            }
            return new RedundancyConfigApiModel {
                DesiredActiveAgents = model.DesiredActiveAgents,
                DesiredPassiveAgents = model.DesiredPassiveAgents
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static JobInfoListApiModel ToApiModel(
            this JobInfoListModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoListApiModel {
                Jobs = model.Jobs?
                    .Select(d => d.ToApiModel())
                    .ToList(),
                ContinuationToken = model.ContinuationToken
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static JobInfoQueryModel ToServiceModel(
            this JobInfoQueryApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoQueryModel {
                Name = model.Name,
                JobConfigurationType = model.JobConfigurationType,
                Status = (Agent.Framework.Models.JobStatus?)model.Status
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static WorkerInfoApiModel ToApiModel(
            this WorkerInfoModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerInfoApiModel {
                WorkerId = model.WorkerId,
                AgentId = model.AgentId,
                Status = (Api.Jobs.Models.WorkerStatus)model.Status,
                LastSeen = model.LastSeen
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static WorkerInfoListApiModel ToApiModel(
            this WorkerInfoListModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerInfoListApiModel {
                Workers = model.Workers?
                    .Select(d => d.ToApiModel())
                    .ToList(),
                ContinuationToken = model.ContinuationToken
            };
        }
    }
}