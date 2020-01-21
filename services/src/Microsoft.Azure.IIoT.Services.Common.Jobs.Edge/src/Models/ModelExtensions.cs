// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Models {
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
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HeartbeatModel ToServiceModel(
            this HeartbeatApiModel model) {
            if (model == null) {
                return null;
            }
            return new HeartbeatModel {
                Worker = model.Worker?.ToServiceModel(),
                Job = model.Job?.ToServiceModel()
            };
        }

        /// <summary>
        /// Create response
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HeartbeatResponseApiModel ToApiModel(
            this HeartbeatResultModel model) {
            if (model == null) {
                return null;
            }
            return new HeartbeatResponseApiModel {
                HeartbeatInstruction = (Api.Jobs.Models.HeartbeatInstruction)model.HeartbeatInstruction,
                LastActiveHeartbeat = model.LastActiveHeartbeat,
                UpdatedJob = model.UpdatedJob.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static JobHeartbeatModel ToServiceModel(
            this JobHeartbeatApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobHeartbeatModel {
                State = model.State?.DeepClone(),
                ProcessMode = (Agent.Framework.Models.ProcessMode)model.ProcessMode,
                JobHash = model.JobHash,
                JobId = model.JobId,
                Status = (Agent.Framework.Models.JobStatus)model.Status
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
                .Select(d => d.ToApiModel()).ToList(),
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
        public static JobProcessingInstructionApiModel ToApiModel(
            this JobProcessingInstructionModel model) {
            if (model == null) {
                return null;
            }
            return new JobProcessingInstructionApiModel {
                ProcessMode = (Api.Jobs.Models.ProcessMode?)model.ProcessMode,
                Job = model.Job.ToApiModel()
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static JobRequestModel ToServiceModel(
            this JobRequestApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobRequestModel {
                Capabilities = model.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value)
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
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static WorkerHeartbeatModel ToServiceModel(
            this WorkerHeartbeatApiModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerHeartbeatModel {
                WorkerId = model.WorkerId,
                AgentId = model.AgentId,
                Status = (Agent.Framework.Models.WorkerStatus)model.Status
            };
        }
    }
}