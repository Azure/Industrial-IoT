// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks.Default {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A task (or job) processor build on top of an in memory
    /// <see cref="BlockingCollection{T}>"/>
    /// </summary>
    public class TaskProcessor : ITaskProcessor, IDisposable {

        /// <summary>
        /// Create processor
        /// </summary>
        /// <param name="logger"></param>
        public TaskProcessor(ITaskProcessorConfig config, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _processors = new List<ProcessorWorker>();
            for (var i = 0; i < Math.Max(1, config.MaxInstances); i++) {
                _processors.Add(new ProcessorWorker(this));
            }
        }

        /// <inheritdoc/>
        public bool TrySchedule(Func<Task> job, Func<Task> checkpoint) {
            return -1 != BlockingCollection<ProcessorWorker.Work>.TryAddToAny(
                _processors.Select(p => p.Queue).ToArray(),
                new ProcessorWorker.Work {
                    Checkpoint = checkpoint,
                    Process = job
                });
        }

        /// <inheritdoc/>
        public void Dispose() => Try.Op(() =>
            Task.WaitAll(_processors.Select(p => p.CloseAsync()).ToArray()));

        private class ProcessorWorker {

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
                _worker = Task.Run(WorkAsync);
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
                        await item.Process();
                    }
                    catch (AggregateException ex) {
                        _processor._logger.Error($"Processing job failed with exception.",
                            () => ex);
                    }
                    await Try.Async(item.Checkpoint);
                }
            }

            internal class Work {

                /// <summary>
                /// Request to process
                /// </summary>
                public Func<Task> Process { get; set; }

                /// <summary>
                /// Checkpoint after completion
                /// </summary>
                public Func<Task> Checkpoint { get; set; }
            }

            private readonly TaskProcessor _processor;
            private readonly Task _worker;
        }

        private readonly ILogger _logger;
        private readonly ITaskProcessorConfig _config;
        private readonly List<ProcessorWorker> _processors;
    }
}
