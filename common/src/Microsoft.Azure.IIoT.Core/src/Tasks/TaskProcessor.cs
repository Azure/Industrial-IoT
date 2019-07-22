// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks.Default {
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A task (or job) processor build on top of an in memory
    /// BlockingCollection
    /// </summary>
    public sealed class TaskProcessor : ITaskProcessor, IDisposable {

        /// <summary>
        /// The processors task scheduler
        /// </summary>
        public ITaskScheduler Scheduler { get; }

        /// <summary>
        /// Create processor
        /// </summary>
        /// <param name="logger"></param>
        public TaskProcessor(ILogger logger) :
            this(new DefaultConfig(), logger) {
        }

        /// <summary>
        /// Create processor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public TaskProcessor(ITaskProcessorConfig config, ILogger logger) :
            this(config, new DefaultScheduler(), logger) {
        }

        /// <summary>
        /// Create processor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="scheduler"></param>
        /// <param name="logger"></param>
        public TaskProcessor(ITaskProcessorConfig config, ITaskScheduler scheduler,
            ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _processors = new List<ProcessorWorker>();
            for (var i = 0; i < Math.Max(1, config.MaxInstances); i++) {
                _processors.Add(new ProcessorWorker(this));
            }
        }

        /// <inheritdoc/>
        public bool TrySchedule(Func<Task> task, Func<Task> checkpoint) {
            if (task == null) {
                throw new ArgumentNullException(nameof(task));
            }
            if (checkpoint == null) {
                throw new ArgumentNullException(nameof(checkpoint));
            }
            return TrySchedule(new ProcessorWorker.Work {
                Checkpoint = checkpoint,
                Task = task,
                Retries = 2 // TODO
            });
        }

        private bool TrySchedule(ProcessorWorker.Work work) {
            return -1 != BlockingCollection<ProcessorWorker.Work>.TryAddToAny(
                _processors.Select(p => p.Queue).ToArray(), work);
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Op(() => Task.WaitAll(_processors.Select(p => p.CloseAsync()).ToArray()));
        }

        private sealed class ProcessorWorker {

            internal BlockingCollection<Work> Queue { get; }

            /// <summary>
            /// Create worker
            /// </summary>
            /// <param name="processor"></param>
            public ProcessorWorker(TaskProcessor processor) {
                _processor = processor ??
                    throw new ArgumentNullException(nameof(processor));
                Queue = new BlockingCollection<Work>(
                    Math.Max(1, processor._config.MaxQueueSize));
                _worker = processor.Scheduler.Run(WorkAsync);
            }

            /// <summary>
            /// Close worker
            /// </summary>
            /// <returns></returns>
            public Task CloseAsync() {
                Queue.CompleteAdding();
                return _worker;
            }

            /// <summary>
            /// Process items in queue
            /// </summary>
            /// <returns></returns>
            private async Task WorkAsync() {
                while (true) {
                    if (!Queue.TryTake(out var item, TimeSpan.FromSeconds(20))) {
                        if (Queue.IsAddingCompleted) {
                            return;
                        }
                        continue;
                    }
                    try {
                        await item.Task().ConfigureAwait(false);
                    }
                    catch (Exception ex) {
                        if (item.Retries == 0) {
                            // Give up.
                            _processor._logger.Error(ex, "Exception thrown, give up on task!");
                            return;
                        }
                        _processor._logger.Error(ex, "Processing task failed with exception.");
                        item.Retries--;
                        _processor.TrySchedule(item);
                    }
                    await Try.Async(item.Checkpoint);
                }
            }

            internal sealed class Work {

                /// <summary>
                /// Number of retries
                /// </summary>
                public int Retries { get; set; }

                /// <summary>
                /// Request to process
                /// </summary>
                public Func<Task> Task { get; set; }

                /// <summary>
                /// Checkpoint after completion
                /// </summary>
                public Func<Task> Checkpoint { get; set; }
            }

            private readonly TaskProcessor _processor;
            private readonly Task _worker;
        }

        internal sealed class DefaultConfig : ITaskProcessorConfig {
            public int MaxInstances => 1;
            public int MaxQueueSize => 1000;
        }

        private readonly ILogger _logger;
        private readonly ITaskProcessorConfig _config;
        private readonly List<ProcessorWorker> _processors;
    }
}
