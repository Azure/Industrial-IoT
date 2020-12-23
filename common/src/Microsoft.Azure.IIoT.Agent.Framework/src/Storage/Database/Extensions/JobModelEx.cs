// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Linq;

    /// <summary>
    /// Job model extensions
    /// </summary>
    public static class JobModelEx {

        /// <summary>
        /// Convert to storage
        /// </summary>
        /// <param name="job"></param>
        /// <param name="etag"></param>
        /// <returns></returns>
        public static JobDocument ToDocumentModel(this JobInfoModel job,
            string etag = null) {
            if (job?.LifetimeData == null) {
                return null;
            }
            return new JobDocument {
                ETag = etag,
                Id = job.Id,
                JobId = job.Id,
                Name = job.Name,
                JobConfiguration = new JobConfigDocument {
                    JobId = job.Id,
                    Job = job.JobConfiguration.Copy()
                },
                Type = job.JobConfigurationType,
                Demands = job.Demands?.Select(d => d.ToDocumentModel(job.Id)).ToList(),
                DesiredActiveAgents = job.RedundancyConfig?.DesiredActiveAgents ?? 1,
                DesiredPassiveAgents = job.RedundancyConfig?.DesiredPassiveAgents ?? 0,
                Created = job.LifetimeData.Created,
                ProcessingStatus = job.LifetimeData.ProcessingStatus?
                    .ToDictionary(k => k.Key, v => v.Value.ToDocumentModel(job.Id)),
                Status = job.LifetimeData.Status,
                Updated = job.LifetimeData.Updated
            };
        }

        /// <summary>
        /// Convert to Service model
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static JobInfoModel ToFrameworkModel(this JobDocument document) {
            if (document == null) {
                return null;
            }
            return new JobInfoModel {
                Id = document.JobId,
                Name = document.Name,
                JobConfiguration = document.JobConfiguration?.Job?.Copy(),
                JobConfigurationType = document.Type,
                Demands = document.Demands?.Select(d => d.ToServiceModel()).ToList(),
                RedundancyConfig = new RedundancyConfigModel {
                    DesiredActiveAgents = document.DesiredActiveAgents,
                    DesiredPassiveAgents = document.DesiredPassiveAgents
                },
                LifetimeData = new JobLifetimeDataModel {
                    Created = document.Created,
                    ProcessingStatus = document.ProcessingStatus?
                        .ToDictionary(k => k.Key, v => v.Value.ToFrameworkModel()),
                    Status = document.Status,
                    Updated = document.Updated
                }
            };
        }
    }
}