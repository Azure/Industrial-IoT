// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Services {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Prometheus;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher client
    /// </summary>
    public sealed class PublisherJobService : IPublishServices<string> {

        /// <summary>
        /// Read default batch trigger interval from environment.
        /// </summary>
        internal static Lazy<TimeSpan> DefaultBatchTriggerInterval => new Lazy<TimeSpan>(() => {
            var env = Environment.GetEnvironmentVariable("PCS_DEFAULT_PUBLISH_JOB_BATCH_INTERVAL");
            if (!string.IsNullOrEmpty(env)) {
                if (int.TryParse(env, out var milliseconds) &&
                    milliseconds >= 100 && milliseconds <= 3600000) {
                    return TimeSpan.FromMilliseconds(milliseconds);
                }
            }
            return TimeSpan.FromMilliseconds(500); // default
        });

        /// <summary>
        /// Read default batch trigger size from environment.
        /// </summary>
        internal static Lazy<int> DefaultBatchSize => new Lazy<int>(() => {
            var env = Environment.GetEnvironmentVariable("PCS_DEFAULT_PUBLISH_JOB_BATCH_SIZE");
            if (!string.IsNullOrEmpty(env) && int.TryParse(env, out var size) &&
                size > 1 && size <= 1000) {
                return size;
            }
            return 50; // default
        });

        /// <summary>
        /// Read default max outgress message buffer size from environment
        /// </summary>
        internal static Lazy<int> DefaultMaxOutgressMessages => new Lazy<int>(() => {
            var env = Environment.GetEnvironmentVariable(PcsVariable.PCS_DEFAULT_PUBLISH_MAX_OUTGRESS_MESSAGES);
            if (!string.IsNullOrEmpty(env) && int.TryParse(env, out var maxOutgressMessages) &&
                maxOutgressMessages > 1 && maxOutgressMessages <= 25000) {
                return maxOutgressMessages;
            }
            return 4096; //default
        });

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="jobs"></param>
        /// <param name="serializer"></param>
        public PublisherJobService(IEndpointRegistry endpoints, IJobScheduler jobs,
            IJobSerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            string endpointId, PublishStartRequestModel request) {
            kNodePublishStart.Inc();
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Item == null) {
                throw new ArgumentNullException(nameof(request.Item));
            }
            if (string.IsNullOrEmpty(request.Item.NodeId)) {
                throw new ArgumentNullException(nameof(request.Item.NodeId));
            }

            var endpoint = await _endpoints.GetEndpointAsync(endpointId);
            if (endpoint == null) {
                throw new ArgumentException("Invalid endpointId");
            }

            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), (job, ct) => {
                ct.ThrowIfCancellationRequested();
                var publishJob = AsJob(job);
                // TODO change to application uri?
                job.Name = endpoint.ApplicationId;
                publishJob.WriterGroup.WriterGroupId = GetDefaultId(endpoint.Registration.EndpointUrl);
                // Add subscription
                AddOrUpdateItemInJob(publishJob, request.Item, endpointId, job.Id,
                    new ConnectionModel {
                        Endpoint = endpoint.Registration.Endpoint,
                        Diagnostics = request.Header?.Diagnostics,
                        User = request.Header?.Elevation
                    });

                job.JobConfiguration = _serializer.SerializeJobConfiguration(
                    publishJob, out var jobType);
                job.JobConfigurationType = jobType;
                if (publishJob.WriterGroup.DataSetWriters.Count != 0 &&
                    job.LifetimeData.Status != JobStatus.Deleted) {
                    job.LifetimeData.Status = JobStatus.Active;
                }
                job.Demands = PublisherDemands(endpoint);
                return Task.FromResult(true);
            });
            return new PublishStartResultModel();
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResultModel> NodePublishBulkAsync(string endpointId,
            PublishBulkRequestModel request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var endpoint = await _endpoints.GetEndpointAsync(endpointId);
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), (job, ct) => {
                ct.ThrowIfCancellationRequested();
                var publishJob = AsJob(job);
                var jobChanged = false;
                var connection = new ConnectionModel {
                    Endpoint = endpoint.Registration.Endpoint,
                    Diagnostics = request.Header?.Diagnostics,
                    User = request.Header?.Elevation
                };

                if (request.NodesToAdd != null) {
                    var dataSetWriterName = Guid.NewGuid().ToString();
                    foreach (var item in request.NodesToAdd) {
                        AddOrUpdateItemInJob(publishJob, item, endpointId, job.Id,
                            connection, dataSetWriterName);
                        jobChanged = true;
                    }
                }
                if (request.NodesToRemove != null) {
                    foreach (var item in request.NodesToRemove) {
                        jobChanged = RemoveItemFromJob(publishJob, item, connection);
                    }
                }

                if (jobChanged) {
                    job.JobConfiguration = _serializer.SerializeJobConfiguration(
                        publishJob, out var jobType);
                    job.JobConfigurationType = jobType;
                    if (publishJob.WriterGroup.DataSetWriters.Count != 0 &&
                        job.LifetimeData.Status != JobStatus.Deleted) {
                        job.LifetimeData.Status = JobStatus.Active;
                    }
                    job.Demands = PublisherDemands(endpoint);
                }
                return Task.FromResult(jobChanged);
            });
            return new PublishBulkResultModel {
                NodesToAdd = request.NodesToAdd?
                    .Select(_ => new ServiceResultModel()).ToList(),
                NodesToRemove = request.NodesToRemove?
                    .Select(_ => new ServiceResultModel()).ToList(),
            };
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            string endpointId, PublishStopRequestModel request) {
            kNodePublishStop.Inc();
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrEmpty(request.NodeId)) {
                throw new ArgumentNullException(nameof(request.NodeId));
            }

            var endpoint = await _endpoints.GetEndpointAsync(endpointId);
            if (endpoint == null) {
                throw new ArgumentException("Invalid endpointId");
            }
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), (job, ct) => {
                ct.ThrowIfCancellationRequested();
                // remove from job
                var publishJob = AsJob(job);
                var jobChanged = RemoveItemFromJob(publishJob, request.NodeId,
                    new ConnectionModel {
                        Endpoint = endpoint.Registration.Endpoint,
                        Diagnostics = request.Header?.Diagnostics,
                        User = request.Header?.Elevation
                    });

                if (jobChanged) {
                    job.JobConfiguration = _serializer.SerializeJobConfiguration(
                        publishJob, out var jobType);
                    job.JobConfigurationType = jobType;
                    if (publishJob.WriterGroup.DataSetWriters.Count == 0 &&
                        job.LifetimeData.Status == JobStatus.Active) {
                        job.LifetimeData.Status = JobStatus.Canceled;
                    }
                    job.Demands = PublisherDemands(endpoint);
                }
                return Task.FromResult(jobChanged);
            });

            return new PublishStopResultModel();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            string endpointId, PublishedItemListRequestModel request) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            List<PublishedItemModel> list = null;
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), (job, ct) => {
                ct.ThrowIfCancellationRequested();
                var publishJob = AsJob(job);
                list = publishJob.WriterGroup.DataSetWriters
                    .Select(writer => writer.DataSet.DataSetSource)
                    .SelectMany(source => source.PublishedVariables.PublishedData
                        .Select(variable => new PublishedItemModel {
                            NodeId = variable.PublishedVariableNodeId,
                            DisplayName = variable.PublishedVariableDisplayName,
                            SamplingInterval = variable.SamplingInterval,
                            HeartbeatInterval = variable.HeartbeatInterval,
                            PublishingInterval = source.SubscriptionSettings.PublishingInterval
                        }))
                    .ToList();
                return Task.FromResult(false);
            });
            return new PublishedItemListResultModel {
                Items = list
            };
        }

        /// <summary>
        /// Create the name for the default job
        /// </summary>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        private static string GetDefaultId(string endpointId) {
            return endpointId;
        }

        /// <summary>
        /// Deserialize the monitored item job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private WriterGroupJobModel AsJob(JobInfoModel job) {
            if (job.JobConfiguration != null) {
                var publishJob = (WriterGroupJobModel)_serializer.DeserializeJobConfiguration(
                    job.JobConfiguration, job.JobConfigurationType);
                if (publishJob != null) {

                    if (publishJob.Engine == null) {
                        publishJob.Engine = new EngineConfigurationModel {
                            BatchSize = DefaultBatchSize.Value,
                            BatchTriggerInterval = DefaultBatchTriggerInterval.Value,
                            DiagnosticsInterval = TimeSpan.FromSeconds(60),
                            MaxMessageSize = 0,
                            MaxOutgressMessages = DefaultMaxOutgressMessages.Value
                        };
                    }
                    else {
                        publishJob.Engine.BatchTriggerInterval = DefaultBatchTriggerInterval.Value;
                        publishJob.Engine.BatchSize = DefaultBatchSize.Value;
                        publishJob.Engine.MaxOutgressMessages = DefaultMaxOutgressMessages.Value;
                    }
                    return publishJob;
                }
            }

            return new WriterGroupJobModel {
                MessagingMode = MessagingMode.Samples,
                WriterGroup = new WriterGroupModel {
                    MessageType = MessageEncoding.Json,
                    WriterGroupId = job.Id,
                    DataSetWriters = new List<DataSetWriterModel>(),
                    MessageSettings = new WriterGroupMessageSettingsModel() {
                        NetworkMessageContentMask =
                                NetworkMessageContentMask.PublisherId |
                                NetworkMessageContentMask.WriterGroupId |
                                NetworkMessageContentMask.SequenceNumber |
                                NetworkMessageContentMask.MonitoredItemMessage |
                                NetworkMessageContentMask.DataSetMessageHeader
                    },
                },
                Engine = new EngineConfigurationModel {
                    BatchSize = DefaultBatchSize.Value,
                    BatchTriggerInterval = DefaultBatchTriggerInterval.Value,
                    DiagnosticsInterval = TimeSpan.FromSeconds(60),
                    MaxMessageSize = 0,
                    MaxOutgressMessages = DefaultMaxOutgressMessages.Value
                },
                ConnectionString = null
            };
        }

        /// <summary>
        /// Add or update item in job
        /// </summary>
        /// <param name="publishJob"></param>
        /// <param name="publishedItem"></param>
        /// <param name="endpointId"></param>
        /// <param name="publisherId"></param>
        /// <param name="connection"></param>
        /// <param name="dataSetWriterName"></param>
        private static void AddOrUpdateItemInJob(WriterGroupJobModel publishJob,
            PublishedItemModel publishedItem, string endpointId, string publisherId,
            ConnectionModel connection, string dataSetWriterName = null) {

            var uniqueDataSetWriterName =
                (string.IsNullOrEmpty(dataSetWriterName) ? GetDefaultId(endpointId) : dataSetWriterName) +
                (publishedItem.PublishingInterval.HasValue ?
                    ('_' + publishedItem.PublishingInterval.Value.TotalMilliseconds.ToString()) : string.Empty);

            // Simple - first remove - then add.
            RemoveItemFromJob(publishJob, publishedItem.NodeId, connection);

            // Find existing subscription we can add node to
            List<PublishedDataSetVariableModel> variables = null;
            foreach (var writer in publishJob.WriterGroup.DataSetWriters) {
                if (writer.DataSet.DataSetSource.Connection.IsSameAs(connection) &&
                    writer.DataSetWriterName == uniqueDataSetWriterName) {
                    System.Diagnostics.Debug.Assert(writer.DataSet.DataSetSource.PublishedVariables.PublishedData != null);
                    variables = writer.DataSet.DataSetSource.PublishedVariables.PublishedData;
                    break;
                }
            }
            if (variables == null) {
                // No writer found - add new one with a published dataset
                var dataSetWriter = new DataSetWriterModel {
                    DataSetWriterName = uniqueDataSetWriterName,
                    DataSet = new PublishedDataSetModel {
                        DataSetMetaData = new DataSetMetaDataModel {
                            DataSetClassId = Guid.NewGuid(),
                            Name = endpointId
                        },
                        ExtensionFields = new Dictionary<string, string> {
                            ["EndpointId"] = endpointId,
                            ["PublisherId"] = publisherId,
                            // todo, probably not needed
                            ["DataSetWriterId"] = uniqueDataSetWriterName
                        },
                        DataSetSource = new PublishedDataSetSourceModel {
                            Connection = connection,
                            PublishedEvents = null,
                            PublishedVariables = new PublishedDataItemsModel {
                                PublishedData = new List<PublishedDataSetVariableModel>()
                            },
                            SubscriptionSettings = new PublishedDataSetSettingsModel {
                                PublishingInterval = publishedItem.PublishingInterval,
                                ResolveDisplayName = true
                                // ...
                            }
                        }
                    },
                    DataSetFieldContentMask =
                        DataSetFieldContentMask.StatusCode |
                        DataSetFieldContentMask.SourceTimestamp |
                        DataSetFieldContentMask.ServerTimestamp |
                        DataSetFieldContentMask.NodeId |
                        DataSetFieldContentMask.DisplayName |
                        DataSetFieldContentMask.ApplicationUri |
                        DataSetFieldContentMask.EndpointUrl |
                        DataSetFieldContentMask.ExtensionFields,
                    MessageSettings = new DataSetWriterMessageSettingsModel() {
                        DataSetMessageContentMask =
                            DataSetContentMask.Timestamp |
                            DataSetContentMask.MetaDataVersion |
                            DataSetContentMask.Status |
                            DataSetContentMask.DataSetWriterId |
                            DataSetContentMask.MajorVersion |
                            DataSetContentMask.MinorVersion |
                            DataSetContentMask.SequenceNumber
                    }
                };
                variables = dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData;
                publishJob.WriterGroup.DataSetWriters.Add(dataSetWriter);
            }

            // Add to published variable list items
            variables.Add(new PublishedDataSetVariableModel {
                SamplingInterval = publishedItem.SamplingInterval,
                PublishedVariableNodeId = publishedItem.NodeId,
                PublishedVariableDisplayName = publishedItem.DisplayName,
                HeartbeatInterval = publishedItem.HeartbeatInterval,
                QueueSize = 1,
            });
        }

        /// <summary>
        /// Add or update subscription
        /// </summary>
        /// <param name="publishJob"></param>
        /// <param name="nodeId"></param>
        /// <param name="connection"></param>
        private static bool RemoveItemFromJob(WriterGroupJobModel publishJob,
            string nodeId, ConnectionModel connection) {
            var found = false;
            foreach (var writer in publishJob.WriterGroup.DataSetWriters.ToList()) {
                if (!writer.DataSet.DataSetSource.Connection.Endpoint.IsSameAs(connection.Endpoint)) {
                    continue;
                }
                foreach (var item in writer.DataSet.DataSetSource.PublishedVariables.PublishedData.ToList()) {
                    if (item.PublishedVariableNodeId == nodeId) {
                        writer.DataSet.DataSetSource.PublishedVariables.PublishedData.Remove(item);
                        if (writer.DataSet.DataSetSource.PublishedVariables.PublishedData.Count == 0) {
                            // Trim writer also
                            publishJob.WriterGroup.DataSetWriters.Remove(writer);
                        }
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }

        /// <summary>
        /// Publisher demands
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static List<DemandModel> PublisherDemands(EndpointInfoModel endpoint) {
            var demands = new List<DemandModel> {
                new DemandModel {
                    Key = "Type",
                    Value = IdentityType.Publisher
                }
            };
            // Add site as demand if available
            if (!string.IsNullOrEmpty(endpoint.Registration.SiteId)) {
                demands.Add(new DemandModel {
                    Key = nameof(endpoint.Registration.SiteId),
                    Value = endpoint.Registration.SiteId
                });
            }
            else if (!string.IsNullOrEmpty(endpoint.Registration.SupervisorId)) {
                var deviceId = SupervisorModelEx.ParseDeviceId(
                    endpoint.Registration.SupervisorId, out _);
                // Otherwise confine to the supervisor's gateway
                demands.Add(new DemandModel {
                    Key = "DeviceId",
                    Value = deviceId
                });
            }
            return demands;
        }

        private readonly IEndpointRegistry _endpoints;
        private readonly IJobScheduler _jobs;
        private readonly IJobSerializer _serializer;
        private static readonly Counter kNodePublishStart = Metrics.CreateCounter("iiot_publisher_node_publish_start", "calls to nodePublishStartAsync");
        private static readonly Counter kNodePublishStop = Metrics.CreateCounter("iiot_publisher_node_publish_stop", "calls to nodePublishStopAsync");

    }
}
