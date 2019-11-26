// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.InMemory {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Filesystem;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public class InMemoryJobRepository : BufferedJobRepository {
        /// <summary>
        /// 
        /// </summary>
        public InMemoryJobRepository() : base(0) {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<JobInfoModel> ReadJobs() {
            return new JobInfoModel[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Task WriteJobs() {
            return Task.CompletedTask;
        }
    }
}