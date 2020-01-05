// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements discovery through jobs and twin supervisor.
    /// </summary>
    public sealed class DiscoveryJobClient : IDiscoveryServices {

        private static readonly TimeSpan kDiscoveryTimeout = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="logger"></param>
        public DiscoveryJobClient(IIoTHubJobServices jobs, ILogger logger) {
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task DiscoverAsync(DiscoveryRequestModel request,
            CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await CallServiceOnAllSupervisorsAsync("Discover_V2",
                request, kDiscoveryTimeout, ct);
        }

        /// <inheritdoc/>
        public async Task CancelAsync(DiscoveryCancelModel request,
            CancellationToken ct) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await CallServiceOnAllSupervisorsAsync("Cancel_V2",
                request, kDiscoveryTimeout, ct);
        }

        /// <summary>
        /// Calls service on all controllers until a successful response
        /// is returned
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="request"></param>
        /// <param name="timeout"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task CallServiceOnAllSupervisorsAsync<T>(
            string service, T request, TimeSpan timeout, CancellationToken ct) {

            // Create job to all supervisors
            var jobId = Guid.NewGuid().ToString();
            var job = await _jobs.CreateAsync(new JobModel {
                JobId = jobId,
                QueryCondition = "FROM devices.modules WHERE " +
                    $"properties.reported.{TwinProperty.Type} = 'supervisor'",
                Type = JobType.ScheduleDeviceMethod,
                MaxExecutionTimeInSeconds = timeout.Seconds,
                MethodParameter = new MethodParameterModel {
                    Name = service,
                    JsonPayload = JsonConvertEx.SerializeObject(request)
                }
            }, ct);
            _logger.Information("Job {jobId} created ({status})...", jobId,
                job.Status);
            using (var cts = new CancellationTokenSource(timeout + timeout)) {
                ct.Register(cts.Cancel);
                while (true) {
                    if (!string.IsNullOrEmpty(job.FailureReason)) {
                        throw new MethodCallException(job.FailureReason);
                    }
                    if (job.Status == JobStatus.Completed ||
                        job.Status == JobStatus.Running ||
                        job.Status == JobStatus.Failed ||
                        job.Status == JobStatus.Cancelled) {
                        return;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
                    job = await _jobs.RefreshAsync(jobId, ct);
                    _logger.Information("Job {jobId} polled ({status} - {msg})...",
                        jobId, job.Status, job.StatusMessage);
                    // Poll to completion
                }
                throw new TimeoutException(
                    $"No response received for job {jobId}.  Failing.");
            }
        }

        private readonly IIoTHubJobServices _jobs;
        private readonly ILogger _logger;
    }
}
