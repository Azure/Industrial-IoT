// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Autofac;
    using Furly.Exceptions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
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
    public sealed class PublisherService : IPublisher, IAsyncDisposable, IDisposable,
        IMetricsContext
    {
        /// <inheritdoc/>
        public string PublisherId { get; }

        /// <inheritdoc/>
        public IEnumerable<WriterGroupModel> WriterGroups { get; private set; }
            = Enumerable.Empty<WriterGroupModel>();

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
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public PublisherService(IWriterGroupScopeFactory factory,
            IOptions<PublisherOptions> options, ILogger<PublisherService> logger)
        {
            PublisherId = options?.Value.PublisherId ??
                throw new ArgumentNullException(nameof(options));
            _factory = factory ??
                throw new ArgumentNullException(nameof(factory));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));

            _currentJobs = new Dictionary<string, WriterGroupJob>();

            TagList = new TagList(new[]
            {
                new KeyValuePair<string, object?>(Constants.PublisherIdTag, PublisherId)
            });
            _completedTask = new TaskCompletionSource();
            _cts = new CancellationTokenSource();
            _changeFeed
                = Channel.CreateUnbounded<(TaskCompletionSource, List<WriterGroupModel>)>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false
                    });
            _processor = Task.Factory.StartNew(() => RunAsync(_cts.Token), _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public bool TryUpdate(IEnumerable<WriterGroupModel> writerGroups)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return _changeFeed.Writer.TryWrite((_completedTask, writerGroups.ToList()));
        }

        /// <inheritdoc/>
        public Task UpdateAsync(IEnumerable<WriterGroupModel> writerGroups)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            var tcs = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            if (_changeFeed.Writer.TryWrite((tcs, writerGroups.ToList())))
            {
                return tcs.Task;
            }
            return Task.FromException(new ResourceExhaustionException("Change feed full"));
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                _logger.LogDebug("Closing publisher service...");
                _cts.Cancel();
                _changeFeed.Writer.TryComplete();
                try
                {
                    await _processor.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to close publisher service processor.");
                }
                _logger.LogInformation("Publisher service closed succesfully.");
            }
            finally
            {
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Process writer group changes
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken ct)
        {
            try
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
                        await ProcessChangesAsync(task, changes, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        task.TrySetCanceled(ct);
                    }
                    catch (Exception ex)
                    {
                        Debug.Fail("We have not processed all exceptions.");
                        task.TrySetException(ex);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Disposing - stop all groups before exiting
                foreach (var group in _currentJobs)
                {
                    try
                    {
                        await group.Value.DisposeAsync().ConfigureAwait(false);
                        _logger.LogInformation("Writer group job {Job} stopped.",
                            group.Key);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to stop writer group job {Job}.",
                            group.Key);
                    }
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
        private async ValueTask ProcessChangesAsync(TaskCompletionSource task,
            List<WriterGroupModel> changes, CancellationToken ct)
        {
            // Increment change number
            unchecked
            {
                Version++;
            }
            var exceptions = new List<Exception>();
            foreach (var writerGroup in changes)
            {
                ct.ThrowIfCancellationRequested();
                var jobId = writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId;
                if (writerGroup.DataSetWriters?.Count > 0)
                {
                    try
                    {
                        if (_currentJobs.TryGetValue(jobId, out var currentJob))
                        {
                            await currentJob.UpdateAsync(Version, writerGroup, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            // Create new writer group job
                            currentJob = await WriterGroupJob.CreateAsync(this, jobId, Version,
                                writerGroup, ct).ConfigureAwait(false);
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
                    _logger.LogError(ex, "Failed to dispose writer group job before removal.");
                }
                _currentJobs.Remove(delete.Id);
            }

            if (exceptions.Count == 0)
            {
                // Update writer groups
                LastChange = DateTime.UtcNow;
                WriterGroups = _currentJobs.Values
                    .Select(j => j.WriterGroup)
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
        private sealed class WriterGroupJob : IAsyncDisposable
        {
            /// <summary>
            /// Immutable writer group identifier
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// Current writer group configuration
            /// </summary>
            public WriterGroupModel WriterGroup { get; private set; }

            /// <summary>
            /// Message source
            /// </summary>
            public IMessageSource Source { get; }

            /// <summary>
            /// Current writer group job version
            /// </summary>
            public int Version { get; internal set; }

            /// <summary>
            /// Create context
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="version"></param>
            /// <param name="id"></param>
            /// <param name="writerGroup"></param>
            private WriterGroupJob(PublisherService outer, int version, string id,
                WriterGroupModel writerGroup)
            {
                _outer = outer;
                Version = version;
                WriterGroup = writerGroup with { WriterGroupId = id };
                Id = id;
                _scope = _outer._factory.Create(WriterGroup);
                Source = _scope.WriterGroup.Source;
            }

            /// <summary>
            /// Create context
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="id"></param>
            /// <param name="version"></param>
            /// <param name="writerGroup"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public static async ValueTask<WriterGroupJob> CreateAsync(PublisherService outer,
                string id, int version, WriterGroupModel writerGroup, CancellationToken ct)
            {
                var context = new WriterGroupJob(outer, version, id, writerGroup);
                try
                {
                    await context.Source.StartAsync(ct).ConfigureAwait(false);
                    return context;
                }
                catch (Exception ex)
                {
                    outer._logger.LogError(ex, "Failed to create writer group job {Name}", context.Id);
                    await context.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
            }

            /// <summary>
            /// Update writer group job
            /// </summary>
            /// <param name="version"></param>
            /// <param name="writerGroup"></param>
            /// <param name="ct"></param>
            /// <returns></returns>
            public async ValueTask UpdateAsync(int version, WriterGroupModel writerGroup,
                CancellationToken ct)
            {
                try
                {
                    var newWriterGroup = writerGroup with { WriterGroupId = Id };

                    await Source.UpdateAsync(newWriterGroup, ct).ConfigureAwait(false);

                    // Update inner state if successful
                    WriterGroup = newWriterGroup;
                }
                catch (Exception ex)
                {
                    _outer._logger.LogError(ex, "Failed to update writer group job {Name}", Id);
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
                    _outer._logger.LogError(ex, "Failed to dispose writer group job {Name}", Id);
                }
                finally
                {
                    _scope.Dispose();
                }
            }

            private readonly IWriterGroupScope _scope;
            private readonly PublisherService _outer;
        }

        private bool _isDisposed;
        private readonly IWriterGroupScopeFactory _factory;
        private readonly ILogger _logger;
        private readonly Task _processor;
        private readonly Dictionary<string, WriterGroupJob> _currentJobs;
        private readonly TaskCompletionSource _completedTask;
        private readonly CancellationTokenSource _cts;
        private readonly Channel<(TaskCompletionSource, List<WriterGroupModel>)> _changeFeed;
    }
}
