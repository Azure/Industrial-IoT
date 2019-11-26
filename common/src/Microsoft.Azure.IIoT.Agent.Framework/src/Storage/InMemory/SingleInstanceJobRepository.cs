using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.IIoT.Agent.Framework.Models;
using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.InMemory {
    /// <summary>
    /// 
    /// </summary>
    public class SingleInstanceJobRepository : IJobRepository {
        private readonly JobInfoModel _jobInstance = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobInstance"></param>
        public SingleInstanceJobRepository(JobInfoModel jobInstance) {
            _jobInstance = jobInstance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobInfoModel> AddAsync(JobInfoModel job, CancellationToken ct = default) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobInfoModel> AddOrUpdateAsync(string jobId, Func<JobInfoModel, Task<JobInfoModel>> predicate, CancellationToken ct = default) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobInfoModel> DeleteAsync(string jobId, Func<JobInfoModel, Task<bool>> predicate, CancellationToken ct = default) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobInfoModel> GetAsync(string jobId, CancellationToken ct = default) {
            return Task.FromResult(_jobInstance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="continuationToken"></param>
        /// <param name="maxResults"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobInfoListModel> QueryAsync(JobInfoQueryModel query = null, string continuationToken = null, int? maxResults = null, CancellationToken ct = default) {
            return Task.FromResult(new JobInfoListModel() { Jobs = new List<JobInfoModel>(new[] { _jobInstance }) });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="predicate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<JobInfoModel> UpdateAsync(string jobId, Func<JobInfoModel, Task<bool>> predicate, CancellationToken ct = default) {
            predicate(_jobInstance);
            return Task.FromResult(_jobInstance);
        }
    }
}
