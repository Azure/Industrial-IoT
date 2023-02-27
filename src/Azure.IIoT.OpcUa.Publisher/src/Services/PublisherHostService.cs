// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Models;
    using Autofac;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Logging;
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
    public sealed class PublisherHostService : IPublisherHost, IDisposable,
        IMetricsContext
    {
        /// <inheritdoc/>
        public string PublisherId { get; }

        /// <inheritdoc/>
        public IEnumerable<WriterGroupJobModel> WriterGroups { get; private set; }
            = Enumerable.Empty<WriterGroupJobModel>();

        /// <inheritdoc/>
        public DateTime LastChange { get; private set; }
            = DateTime.UtcNow;

        /// <inheritdoc/>
        public int Version { get; private set; }

        /// <inheritdoc/>
        public TagList TagList { get; }

        /// <summary>
        /// Create Job host
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PublisherHostService(IWriterGroupScopeFactory factory,
            IProcessInfo identity, ILogger logger)
        {
            PublisherId = identity.ToIdentityString();
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentJobs = new Dictionary<string, JobContext>();
            TagList = new TagList(new[] {
                new KeyValuePair<string, object>("publisherId", PublisherId),
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
        public bool TryUpdate(IEnumerable<WriterGroupJobModel> jobs)
        {
            return _changeFeed.Writer.TryWrite((_completedTask, jobs.ToList()));
        }

        /// <inheritdoc/>
        public Task UpdateAsync(IEnumerable<WriterGroupJobModel> jobs)
        {
            var tcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            if (_changeFeed.Writer.TryWrite((tcs, jobs.ToList())))
            {
                return tcs.Task;
            }
            return Task.FromException(
                new ResourceExhaustionException("Change feed full"));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _changeFeed.Writer.TryComplete();
                _processor.Wait();
            }
            catch { }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <summary>
        /// Process jobs
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct)
        {
            await foreach (var (task, changes) in _changeFeed.Reader.ReadAllAsync(default))
            {
                if (ct.IsCancellationRequested)
                {
                    task.SetCanceled(ct);
                    continue;
                }
                try
                {
                    await ProcessChanges(task, changes, ct).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
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
            List<WriterGroupJobModel> changes, CancellationToken ct)
        {
            // Increment change number
            unchecked
            {
                Version++;
            }
            var exceptions = new List<Exception>();
            foreach (var job in changes)
            {
                ct.ThrowIfCancellationRequested();
                var jobId = job.GetJobId();
                if (string.IsNullOrEmpty(jobId))
                {
                    continue;
                }

                if (job.WriterGroup?.DataSetWriters?.Count > 0)
                {
                    try
                    {
                        if (_currentJobs.TryGetValue(jobId, out var currentJob))
                        {
                            await currentJob.UpdateAsync(Version, job, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            // Create new job
                            currentJob = await JobContext.CreateAsync(this, Version, job, ct).ConfigureAwait(false);
                            _currentJobs.Add(currentJob.Id, currentJob);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        exceptions.Add(ex);
                        _logger.LogError(ex, "Failed to process change.");
                    }
                }
            }

            // Anything not having an updated version will be deleted
            foreach (var delete in _currentJobs.Values.Where(j => j.Version < Version).ToList())
            {
                try
                {
                    await delete.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    exceptions.Add(ex);
                    _logger.LogError(ex, "Failed to dispose job before removal.");
                }
                _currentJobs.Remove(delete.Id);
            }

            if (exceptions.Count == 0)
            {
                // Update writer groups
                LastChange = DateTime.UtcNow;
                WriterGroups = _currentJobs.Values
                    .Select(j => j.Job)
                    .ToImmutableList();
                // Complete
                task.TrySetResult();
            }
            else if (exceptions.Count == 1)
            {
                // Fail
                task.TrySetException(exceptions[0]);
            }
            else
            {
                // Fail
                task.TrySetException(new AggregateException(
                    "Failed to process changes.", exceptions));
            }
        }

        /// <summary>
        /// Job context
        /// </summary>
        private sealed class JobContext : IAsyncDisposable
        {
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
                WriterGroupJobModel writerGroup)
            {
                _outer = outer;
                Version = version;
                Job = writerGroup;
                Id = Job.GetJobId();
                _configuration = writerGroup.ToWriterGroupJobConfiguration(
                    _outer.PublisherId);
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
                int version, WriterGroupJobModel writerGroup, CancellationToken ct)
            {
                var job = new JobContext(outer, version, writerGroup);
                try
                {
                    await job.Source.StartAsync(ct).ConfigureAwait(false);
                    return job;
                }
                catch (Exception ex)
                {
                    outer._logger.LogError(ex, "Failed to create job {Name}", job.Id);
                    await job.DisposeAsync().ConfigureAwait(false);
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
                CancellationToken ct)
            {
                try
                {
                    await Source.UpdateAsync(writerGroup, ct).ConfigureAwait(false);

                    // Update if successful
                    Job = writerGroup;
                    Id = Job.GetJobId();
                }
                catch (Exception ex)
                {
                    _outer._logger.LogError(ex, "Failed to update job {Name}", Id);
                    throw;
                }
                finally
                {
                    Version = version; // Even if we fail, we want to rev the version
                }
            }

            /// <inheritdoc/>
            public async ValueTask DisposeAsync()
            {
                try
                {
                    await Source.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _outer._logger.LogError(ex, "Failed to dispose job {Name}", Id);
                }
                finally
                {
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
    }
}
