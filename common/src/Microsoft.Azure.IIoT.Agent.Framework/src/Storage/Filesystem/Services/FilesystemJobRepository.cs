// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public class FileSystemJobRepository : BufferedJobRepository {
        private readonly string _jobsDirectory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filesystemJobRepositoryConfig"></param>
        public FileSystemJobRepository(IFilesystemJobRepositoryConfig filesystemJobRepositoryConfig) : base(filesystemJobRepositoryConfig.UpdateBuffer) {
            _jobsDirectory = filesystemJobRepositoryConfig.RootDirectory.Trim().TrimEnd('/') + "/Jobs";

            if (!Directory.Exists(_jobsDirectory)) {
                Directory.CreateDirectory(_jobsDirectory);
            }
        }

        /// <summary>
        /// Get all jobs from file
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<JobInfoModel> ReadJobs() {
            var files = Directory.GetFiles(_jobsDirectory, "*.json");
            var jobs = new List<JobInfoModel>();
            foreach (var file in files) {
                var json = File.ReadAllText(file);
                var job = JsonConvertEx.DeserializeObject<JobInfoModel>(json);
                jobs.Add(job);
            }
            return jobs.ToArray();
        }

        /// <summary>
        /// Write jobs to file system
        /// </summary>
        /// <returns></returns>
        protected override Task WriteJobs() {
            foreach (var file in Directory.GetFiles(_jobsDirectory)) {
                File.Delete(file);
            }
            foreach (var job in _jobs) {
                var jobFilename = _jobsDirectory.Trim().TrimEnd('/') + "/" + $"{job.Id}.json";
                var json = JsonConvertEx.SerializeObjectPretty(job);
                File.WriteAllText(jobFilename, json);
            }
            return Task.CompletedTask;
        }
    }
}