// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Autofac;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher host. Manages updates to the state of the publisher through
    /// a queue model where a processor effects changes from the configuration in
    /// state changes of publishers subscription and session model. In return it
    /// supports aggregation of diagnostics and single sink console output of
    /// the diagnostics data.
    /// </summary>
    public sealed class PublisherHostService : IPublisher, IDisposable, IMetricsContext {

        /// <inheritdoc/>
        public IEnumerable<WriterGroupJobModel> WriterGroups { get; private set; }
            = ImmutableList<WriterGroupJobModel>.Empty;

        /// <inheritdoc/>
        public DateTime LastChange { get; private set; }
            = DateTime.UtcNow;

        /// <inheritdoc/>
        public IEnumerable<PublishDiagnosticInfoModel> DiagnosticInfo
            => _diagnosticInfo.Values;

        /// <inheritdoc/>
        public int Version => _version;

        /// <inheritdoc/>
        public TagList TagList { get; }

        /// <summary>
        /// Create Job host
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PublisherHostService(IWriterGroupScopeFactory factory, ILogger logger) {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentJobs = new Dictionary<string, JobContext>();
            _publishedNodesEntries
                = ImmutableList<PublishedNodesEntryModel>.Empty;
            _diagnosticInfo
                = ImmutableDictionary<string, PublishDiagnosticInfoModel>.Empty;
            TagList = new TagList(new[] {
                new KeyValuePair<string, object>("publisherId", factory.PublisherId),
                new KeyValuePair<string, object>("timestamp_utc",
                    DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
                    CultureInfo.InvariantCulture))
            });
            _completedTask = new TaskCompletionSource();
            _cts = new CancellationTokenSource();
            _changeFeed
                = Channel.CreateUnbounded<(TaskCompletionSource, List<WriterGroupJobModel>)>();
            _processor = Task.Factory.StartNew(() => RunAsync(_cts.Token), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public bool TryUpdate(IEnumerable<WriterGroupJobModel> jobs) {
            return _changeFeed.Writer.TryWrite((_completedTask, jobs.ToList()));
        }

        /// <inheritdoc/>
        public Task UpdateAsync(IEnumerable<WriterGroupJobModel> jobs) {
            var tcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously | TaskCreationOptions.AttachedToParent);
            if (_changeFeed.Writer.TryWrite((tcs, jobs.ToList()))) {
                return tcs.Task;
            }
            return Task.FromException(
                new ResourceExhaustionException("Change feed full"));
        }

        /// <inheritdoc/>
        public void Dispose() {
            try {
                _cts.Cancel();
                _changeFeed.Writer.TryComplete();
                _processor.Wait();
            }
            catch { }
            finally {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Process jobs
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct) {
            await foreach (var (task, changes) in _changeFeed.Reader.ReadAllAsync(default)) {
                if (ct.IsCancellationRequested) {
                    task.SetCanceled(ct);
                    continue;
                }
                try {
                    await ProcessChanges(task, changes, ct);
                }
                catch (ObjectDisposedException) {
                    task.TrySetCanceled(ct);
                }
            }
        }

        /// <summary>
        /// Process the received changes
        /// </summary>
        /// <param name="task"></param>
        /// <param name="changes"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async ValueTask ProcessChanges(TaskCompletionSource task,
            List<WriterGroupJobModel> changes, CancellationToken ct) {
            // Increment change number
            unchecked {
                _version++;
            }
            var exceptions = new List<Exception>();
            foreach (var job in changes) {
                ct.ThrowIfCancellationRequested();
                var jobId = job.GetJobId();
                if (string.IsNullOrEmpty(jobId)) {
                    continue;
                }

                if (job.WriterGroup?.DataSetWriters?.Count > 0) {
                    try {
                        if (_currentJobs.TryGetValue(jobId, out var currentJob)) {
                            await currentJob.UpdateAsync(_version, job, ct);
                        }
                        else {
                            // Create new job
                            currentJob = await JobContext.CreateAsync(this, _version, job, ct);
                            _currentJobs.Add(currentJob.Id, currentJob);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException) {
                        exceptions.Add(ex);
                        _logger.Error(ex, "Failed to process change.");
                    }
                }
            }

            // Anything not having an updated version will be deleted
            foreach (var delete in _currentJobs.Values.Where(j => j.Version < _version).ToList()) {
                try {
                    await delete.DisposeAsync();
                }
                catch (Exception ex) when (ex is not OperationCanceledException) {
                    exceptions.Add(ex);
                    _logger.Error(ex, "Failed to dispose job before removal.");
                }
                _currentJobs.Remove(delete.Id);
            }

            if (exceptions.Count == 0) {
                // Update writer groups
                LastChange = DateTime.UtcNow;
                WriterGroups = _currentJobs.Values
                    .Select(j => j.Job)
                    .ToImmutableList();
                // Complete
                task.TrySetResult();
            }
            else if (exceptions.Count == 1) {
                // Fail
                task.TrySetException(exceptions[0]);
            }
            else {
                // Fail
                task.TrySetException(new AggregateException(
                    "Failed to process changes.", exceptions));
            }
        }

        /// <summary>
        /// Job context
        /// </summary>
        private sealed class JobContext : IAsyncDisposable {

            /// <summary>
            /// Job identifier
            /// </summary>
            public string Id { get; private set; }

            /// <summary>
            /// Current job configuration
            /// </summary>
            public WriterGroupJobModel Job { get; private set; }

            /// <summary>
            /// Message source
            /// </summary>
            public IMessageSource Source { get; }

            /// <summary>
            /// Current job version
            /// </summary>
            public int Version { get; internal set; }

            /// <summary>
            /// Create context
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="version"></param>
            /// <param name="writerGroup"></param>
            private JobContext(PublisherHostService outer, int version,
                WriterGroupJobModel writerGroup) {
                _outer = outer;
                Version = version;
                Job = writerGroup;
                Id = Job.GetJobId();
                _configuration = writerGroup.ToWriterGroupJobConfiguration(
                    _outer._factory.PublisherId);
                _scope = _outer._factory.Create(_configuration);
                Source = _scope.WriterGroup.Source;
            }

            /// <summary>
            /// Create context
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="version"></param>
            /// <param name="writerGroup"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public static async ValueTask<JobContext> CreateAsync(PublisherHostService outer,
                int version, WriterGroupJobModel writerGroup, CancellationToken ct) {
                var job = new JobContext(outer, version, writerGroup);
                try {
                    await job.Source.StartAsync(ct);
                    return job;
                }
                catch (Exception ex) {
                    outer._logger.Error(ex, "Failed to create job {Name}", job.Id);
                    await job.DisposeAsync();
                    throw;
                }
            }

            /// <summary>
            /// Update job
            /// </summary>
            /// <param name="version"></param>
            /// <param name="writerGroup"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async ValueTask UpdateAsync(int version, WriterGroupJobModel writerGroup,
                CancellationToken ct) {
                try {
                    await Source.UpdateAsync(writerGroup, ct);

                    // Update if successful
                    Job = writerGroup;
                    Id = Job.GetJobId();
                }
                catch (Exception ex) {
                    _outer._logger.Error(ex, "Failed to update job {Name}", Id);
                    throw;
                }
                finally {
                    Version = version; // Even if we fail, we want to rev the version
                }
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync() {
                try {
                    await Source.DisposeAsync();
                }
                catch (Exception ex) {
                    _outer._logger.Error(ex, "Failed to dispose job {Name}", Id);
                }
                finally {
                    _scope.Dispose();
                }
            }

            private readonly IWriterGroupScope _scope;
            private readonly PublisherHostService _outer;
            private readonly IWriterGroupConfig _configuration;
        }

        private readonly IWriterGroupScopeFactory _factory;
        private readonly ILogger _logger;
        private readonly Task _processor;
        private readonly Dictionary<string, JobContext> _currentJobs;
        private readonly TaskCompletionSource _completedTask;
        private readonly CancellationTokenSource _cts;
        private readonly Channel<(TaskCompletionSource, List<WriterGroupJobModel>)> _changeFeed;
        private ImmutableList<PublishedNodesEntryModel> _publishedNodesEntries;
        private ImmutableDictionary<string, PublishDiagnosticInfoModel> _diagnosticInfo;
        private int _version;
    }
}
