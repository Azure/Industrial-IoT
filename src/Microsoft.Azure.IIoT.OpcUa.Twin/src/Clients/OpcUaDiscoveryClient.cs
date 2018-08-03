// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements discovery through jobs and twin supervisor.
    /// </summary>
    public sealed class OpcUaDiscoveryClient : IOpcUaDiscoveryServices {

        private static readonly TimeSpan kDiscoveryTimeout = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="logger"></param>
        public OpcUaDiscoveryClient(IIoTHubJobServices jobs, ILogger logger) {
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Discovery application model using the provided discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <returns></returns>
        public async Task<DiscoveryResultModel> DiscoverApplicationsAsync(
            Uri discoveryUrl) {
            if (discoveryUrl == null) {
                throw new ArgumentNullException(nameof(discoveryUrl));
            }
            return await CallServiceOnAllSupervisors<Uri, DiscoveryResultModel>(
                "DiscoverApplication_V1", discoveryUrl, kDiscoveryTimeout);
        }

        /// <summary>
        /// Calls service on all controllers until a successful response is returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="R"></typeparam>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<R> CallServiceOnAllSupervisors<T, R>(
            string service, T request, TimeSpan timeout) {

            // Create job to all supervisors
            var jobId = Guid.NewGuid().ToString();
            var job = await _jobs.CreateAsync(new JobModel {
                JobId = jobId,
                QueryCondition = "FROM devices.modules WHERE " +
                    $"properties.reported.{kTypeProp} = 'supervisor'",
                Type = JobType.ScheduleDeviceMethod,
                MaxExecutionTimeInSeconds = timeout.Seconds + 60,
                MethodParameter = new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(request)
                }
            });
            _logger.Info($"Job {jobId} created...", () => job.Status);
            var notCompleted = true;
            try {
                // Poll to completion
                var cts = new CancellationTokenSource(timeout);
                while (true) {
                    if (!string.IsNullOrEmpty(job.FailureReason)) {
                        throw new MethodCallException(job.FailureReason);
                    }
                    var response = job.Devices
                        .Where(d => d.Status == DeviceJobStatus.Completed)
                        .Where(d => d.Outcome != null)
                        .FirstOrDefault(d => d.Outcome.Status == 200);

                    if (job.Status == JobStatus.Completed ||
                        job.Status == JobStatus.Running ||
                        job.Status == JobStatus.Failed ||
                        job.Status == JobStatus.Cancelled) {
                        notCompleted = false;
                    }
                    if (response != null) {
                        _logger.Info($"Job {jobId} done - response received!",
                            () => job.Status);
                        return JsonConvertEx.DeserializeObject<R>(
                            response.Outcome.JsonPayload);
                    }
                    if (!notCompleted || cts.IsCancellationRequested) {
                        break;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
                    job = await _jobs.RefreshAsync(jobId);
                    _logger.Info($"Job {jobId} polled...", () => job.Status);
                }
                throw new TimeoutException(
                    $"No response received for job {jobId} after {timeout}.");
            }
            finally {
                if (notCompleted) {
                    // Try and cancel job
                    try {
                        await _jobs.CancelAsync(jobId);
                    }
                    catch (Exception ex) {
                        _logger.Warn($"Failed to cancel job {jobId}.", () => ex);
                    }
                }
            }
        }

        public const string kTypeProp = "__type__"; // TODO: Consolidate as common constant

        private readonly IIoTHubJobServices _jobs;
        private readonly ILogger _logger;
    }
}
