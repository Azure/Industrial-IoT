// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
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
                Operator = (DemandOperators?)model.Operator,
                Value = model.Value
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DemandModel ToServiceModel(
            this DemandApiModel model) {
            if (model == null) {
                return null;
            }
            return new DemandModel {
                Key = model.Key,
                Operator = (Agent.Framework.Models.DemandOperators?)model.Operator,
                Value = model.Value
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HeartbeatApiModel ToApiModel(
            this HeartbeatModel model) {
            if (model == null) {
                return null;
            }
            return new HeartbeatApiModel {
                Worker = model.Worker?.ToApiModel(),
                Job = model.Job?.ToApiModel()
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
                HeartbeatInstruction = (HeartbeatInstruction)model.HeartbeatInstruction,
                LastActiveHeartbeat = model.LastActiveHeartbeat,
                UpdatedJob = model.UpdatedJob.ToApiModel()
            };
        }

        /// <summary>
        /// Create response
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static HeartbeatResultModel ToServiceModel(
            this HeartbeatResponseApiModel model) {
            if (model == null) {
                return null;
            }
            return new HeartbeatResultModel {
                HeartbeatInstruction = (Agent.Framework.Models.HeartbeatInstruction)model.HeartbeatInstruction,
                LastActiveHeartbeat = model.LastActiveHeartbeat,
                UpdatedJob = model.UpdatedJob.ToServiceModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static JobHeartbeatApiModel ToApiModel(
            this JobHeartbeatModel model) {
            if (model == null) {
                return null;
            }
            return new JobHeartbeatApiModel {
                State = model.State?.Copy(),
                ProcessMode = (ProcessMode)model.ProcessMode,
                JobHash = model.JobHash,
                JobId = model.JobId,
                Status = (JobStatus)model.Status
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
                State = model.State?.Copy(),
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
                    .Select(d => d.ToApiModel())
                    .ToList(),
                JobConfiguration = model.JobConfiguration?.Copy(),
                JobConfigurationType = model.JobConfigurationType,
                Name = model.Name,
                Id = model.Id
            };
        }

        /// <summary>
        /// Create service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static JobInfoModel ToServiceModel(
            this JobInfoApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoModel {
                LifetimeData = model.LifetimeData.ToServiceModel(),
                RedundancyConfig = model.RedundancyConfig.ToServiceModel(),
                Demands = model.Demands?
                    .Select(d => d.ToServiceModel())
                    .ToList(),
                JobConfiguration = model.JobConfiguration?.Copy(),
                JobConfigurationType = model.JobConfigurationType,
                Name = model.Name,
                Id = model.Id
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
                ProcessMode = (ProcessMode?)model.ProcessMode,
                Job = model.Job.ToApiModel()
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static JobProcessingInstructionModel ToServiceModel(
            this JobProcessingInstructionApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobProcessingInstructionModel {
                ProcessMode = (Agent.Framework.Models.ProcessMode?)model.ProcessMode,
                Job = model.Job.ToServiceModel()
            };
        }

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <returns></returns>
        public static JobRequestApiModel ToApiModel(
            this JobRequestModel model) {
            if (model == null) {
                return null;
            }
            return new JobRequestApiModel {
                Capabilities = model.Capabilities?
                    .ToDictionary(k => k.Key, v => v.Value)
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
        public static JobLifetimeDataApiModel ToApiModel(
            this JobLifetimeDataModel model) {
            if (model == null) {
                return null;
            }
            return new JobLifetimeDataApiModel {
                Status = (JobStatus)model.Status,
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
        public static JobLifetimeDataModel ToServiceModel(
            this JobLifetimeDataApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobLifetimeDataModel {
                Status = (Agent.Framework.Models.JobStatus)model.Status,
                Updated = model.Updated,
                Created = model.Created,
                ProcessingStatus = model.ProcessingStatus?
                    .ToDictionary(k => k.Key, v => v.Value.ToServiceModel())
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
                LastKnownState = model.LastKnownState?.Copy(),
                ProcessMode = (ProcessMode?)model.ProcessMode
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static ProcessingStatusModel ToServiceModel(
            this ProcessingStatusApiModel model) {
            if (model == null) {
                return null;
            }
            return new ProcessingStatusModel {
                LastKnownHeartbeat = model.LastKnownHeartbeat,
                LastKnownState = model.LastKnownState?.Copy(),
                ProcessMode = (Agent.Framework.Models.ProcessMode?)model.ProcessMode
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
        public static RedundancyConfigModel ToServiceModel(
            this RedundancyConfigApiModel model) {
            if (model == null) {
                return null;
            }
            return new RedundancyConfigModel {
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
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static JobInfoListModel ToServiceModel(
            this JobInfoListApiModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoListModel {
                Jobs = model.Jobs?
                    .Select(d => d.ToServiceModel())
                    .ToList(),
                ContinuationToken = model.ContinuationToken
            };
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public static WorkerHeartbeatApiModel ToApiModel(
            this WorkerHeartbeatModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerHeartbeatApiModel {
                WorkerId = model.WorkerId,
                AgentId = model.AgentId,
                Status = (WorkerStatus)model.Status
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

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <returns></returns>
        public static JobInfoQueryApiModel ToApiModel(
            this JobInfoQueryModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoQueryApiModel {
                Name = model.Name,
                JobConfigurationType = model.JobConfigurationType,
                Status = (JobStatus?)model.Status
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
                Status = (WorkerStatus)model.Status,
                LastSeen = model.LastSeen
            };
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static WorkerInfoModel ToServiceModel(
            this WorkerInfoApiModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerInfoModel {
                WorkerId = model.WorkerId,
                AgentId = model.AgentId,
                Status = (Agent.Framework.Models.WorkerStatus)model.Status,
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

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public static WorkerInfoListModel ToServiceModel(
            this WorkerInfoListApiModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerInfoListModel {
                Workers = model.Workers?
                    .Select(d => d.ToServiceModel())
                    .ToList(),
                ContinuationToken = model.ContinuationToken
            };
        }
    }
}