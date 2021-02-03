// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Services {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Prometheus;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher client
    /// </summary>
    public sealed class PublisherJobService : IPublishServices<string> {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="jobs"></param>
        /// <param name="serializer"></param>
        /// <param name="config"></param>
        public PublisherJobService(
            IEndpointRegistry endpoints,
            IJobScheduler jobs,
            IJobSerializer serializer,
            IPublishServicesConfig config
        ) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            string endpointId,
            PublishStartRequestModel request,
            CancellationToken ct = default
        ) {
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

            var endpoint = await _endpoints.GetEndpointAsync(endpointId, ct: ct);
            if (endpoint == null) {
                throw new ArgumentException("Invalid endpointId");
            }

            var jobId = GetDefaultId(endpointId);
            var result = await _jobs.NewOrUpdateJobAsync(jobId, job => {
                var publishJob = AsJob(job);
                // TODO change to application uri?
                job.Name = endpoint.ApplicationId;
                publishJob.WriterGroup.WriterGroupId = GetDefaultId(endpoint.Registration.EndpointUrl);
                // Add subscription
                var connection = new ConnectionModel {
                    Endpoint = endpoint.Registration.Endpoint,
                    Diagnostics = request.Header?.Diagnostics,
                    User = request.Header?.Elevation
                };
                AddOrUpdateItemInJob(publishJob, request.Item, endpointId, job.Id, connection);

                job.JobConfiguration = _serializer.SerializeJobConfiguration(
                    publishJob, out var jobType);
                job.JobConfigurationType = jobType;
                if (publishJob.WriterGroup.DataSetWriters.Count != 0 &&
                    job.LifetimeData.Status != JobStatus.Deleted) {
                    job.LifetimeData.Status = JobStatus.Active;
                }
                job.Demands = PublisherDemands(endpoint);
                return Task.FromResult(true);
            }, ct);
            return new PublishStartResultModel();
        }

        /// <inheritdoc/>
        public async Task<PublishBulkResultModel> NodePublishBulkAsync(
            string endpointId,
            PublishBulkRequestModel request,
            CancellationToken ct = default
        ) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var endpoint = await _endpoints.GetEndpointAsync(endpointId, ct: ct);
            if (endpoint == null) {
                throw new ArgumentException("Invalid endpointId");
            }

            PublishBulkResultModel bulkResult = null;

            var jobId = GetDefaultId(endpointId);
            var result = await _jobs.NewOrUpdateJobAsync(jobId, job => {
                var publishJob = AsJob(job);
                var jobChanged = false;
                var connection = new ConnectionModel {
                    Endpoint = endpoint.Registration.Endpoint,
                    Diagnostics = request.Header?.Diagnostics,
                    User = request.Header?.Elevation
                };

                bulkResult = new PublishBulkResultModel {
                    NodesToAdd = new Dictionary<string, ServiceResultModel>(),
                    NodesToRemove = new Dictionary<string, ServiceResultModel>()
                };

                // Add nodes.
                if (request.NodesToAdd != null && request.NodesToAdd.Count() > 0) {
                    var dataSetWriterName = Guid.NewGuid().ToString();
                    foreach (var item in request.NodesToAdd) {
                        AddOrUpdateItemInJob(publishJob, item, endpointId, job.Id,
                            connection, dataSetWriterName);
                        jobChanged = true;

                        bulkResult.NodesToAdd.Add(item.NodeId, new ServiceResultModel());
                    }
                }

                // Remove nodes.
                if (request.NodesToRemove != null && request.NodesToRemove.Count() > 0) {
                    foreach (var item in request.NodesToRemove) {
                        var currentJobChanged = RemoveItemFromJob(publishJob, item, connection);
                        jobChanged |= currentJobChanged;

                        var serviceResultModel = currentJobChanged
                            ? new ServiceResultModel()
                            : new ServiceResultModel() {
                                StatusCode = (uint) HttpStatusCode.NotFound,
                                ErrorMessage = "Job not found"
                            };
                        bulkResult.NodesToRemove.Add(item, serviceResultModel);
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
            }, ct);

            return bulkResult;
        }

        /// <inheritdoc/>
        public async Task<PublishStopResultModel> NodePublishStopAsync(
            string endpointId,
            PublishStopRequestModel request,
            CancellationToken ct = default
        ) {
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

            var endpoint = await _endpoints.GetEndpointAsync(endpointId, ct: ct);
            if (endpoint == null) {
                throw new ArgumentException("Invalid endpointId");
            }

            var jobChanged = false;
            var jobId = GetDefaultId(endpointId);
            await _jobs.NewOrUpdateJobAsync(jobId, job => {
                // remove from job
                var publishJob = AsJob(job);
                var connection = new ConnectionModel {
                    Endpoint = endpoint.Registration.Endpoint,
                    Diagnostics = request.Header?.Diagnostics,
                    User = request.Header?.Elevation
                };
                jobChanged = RemoveItemFromJob(publishJob, request.NodeId, connection);

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

                // Return whether the provided job has been changed or not.
                return Task.FromResult(jobChanged);
            }, ct);

            if (!jobChanged) {
                throw new ResourceNotFoundException($"Job does not contain node id: {request.NodeId}");
            }

            return new PublishStopResultModel();
        }

        /// <inheritdoc/>
        public async Task<PublishedItemListResultModel> NodePublishListAsync(
            string endpointId,
            PublishedItemListRequestModel request,
            CancellationToken ct = default
        ) {
            if (string.IsNullOrEmpty(endpointId)) {
                throw new ArgumentNullException(nameof(endpointId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            List<PublishedItemModel> list = null;
            var jobId = GetDefaultId(endpointId);
            var result = await _jobs.NewOrUpdateJobAsync(jobId, job => {
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
            }, ct);
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
                            BatchSize = _config.DefaultBatchSize,
                            BatchTriggerInterval = _config.DefaultBatchTriggerInterval,
                            DiagnosticsInterval = TimeSpan.FromSeconds(60),
                            MaxMessageSize = 0,
                            MaxEgressMessageQueue = _config.DefaultMaxEgressMessageQueue
                        };
                    }
                    else {
                        publishJob.Engine.BatchTriggerInterval = _config.DefaultBatchTriggerInterval;
                        publishJob.Engine.BatchSize = _config.DefaultBatchSize;
                        publishJob.Engine.MaxEgressMessageQueue = _config.DefaultMaxEgressMessageQueue;
                    }
                    return publishJob;
                }
            }

            return new WriterGroupJobModel {
                MessagingMode = _config.DefaultMessagingMode,
                WriterGroup = new WriterGroupModel {
                    MessageType = _config.DefaultMessageEncoding,
                    WriterGroupId = job.Id,
                    DataSetWriters = new List<DataSetWriterModel>(),
                    MessageSettings = new WriterGroupMessageSettingsModel() {
                        NetworkMessageContentMask =
                                NetworkMessageContentMask.PublisherId |
                                NetworkMessageContentMask.WriterGroupId |
                                NetworkMessageContentMask.NetworkMessageNumber |
                                NetworkMessageContentMask.SequenceNumber |
                                NetworkMessageContentMask.PayloadHeader |
                                NetworkMessageContentMask.Timestamp |
                                NetworkMessageContentMask.DataSetClassId |
                                NetworkMessageContentMask.NetworkMessageHeader |
                                NetworkMessageContentMask.DataSetMessageHeader
                    },
                },
                Engine = new EngineConfigurationModel {
                    BatchSize = _config.DefaultBatchSize,
                    BatchTriggerInterval = _config.DefaultBatchTriggerInterval,
                    DiagnosticsInterval = TimeSpan.FromSeconds(60),
                    MaxMessageSize = 0,
                    MaxEgressMessageQueue = _config.DefaultMaxEgressMessageQueue
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
        private void AddOrUpdateItemInJob(
            WriterGroupJobModel publishJob,
            PublishedItemModel publishedItem,
            string endpointId,
            string publisherId,
            ConnectionModel connection,
            string dataSetWriterName = null
        ) {
            var dataSetWriterId =
                (string.IsNullOrEmpty(dataSetWriterName) ? GetDefaultId(endpointId) : dataSetWriterName) +
                (publishedItem.PublishingInterval.HasValue ?
                    ('_' + publishedItem.PublishingInterval.Value.TotalMilliseconds.ToString()) : String.Empty);

            // Simple - first remove - then add.
            RemoveItemFromJob(publishJob, publishedItem.NodeId, connection);

            // Find existing subscription we can add node to
            List<PublishedDataSetVariableModel> variables = null;
            foreach (var writer in publishJob.WriterGroup.DataSetWriters) {
                if (writer.DataSet.DataSetSource.Connection.IsSameAs(connection) &&
                    writer.DataSetWriterId == dataSetWriterId ) {
                    System.Diagnostics.Debug.Assert(writer.DataSet.DataSetSource.PublishedVariables.PublishedData != null);
                    variables = writer.DataSet.DataSetSource.PublishedVariables.PublishedData;
                    writer.DataSet.DataSetMetaData.ConfigurationVersion.MinorVersion++;
                    break;
                }
            }
            if (variables == null) {
                // No writer found - add new one with a published dataset
                var dataSetWriter = new DataSetWriterModel {
                    DataSetWriterId = dataSetWriterId,
                    DataSet = new PublishedDataSetModel {
                        Name = null,
                        DataSetMetaData = new DataSetMetaDataModel {
                            ConfigurationVersion = new ConfigurationVersionModel {
                                MajorVersion = 1,
                                MinorVersion = 0
                            },
                            DataSetClassId = Guid.NewGuid(),
                            Name = endpointId
                        },
                        ExtensionFields = new Dictionary<string, string> {
                            ["EndpointId"] = endpointId,
                            ["PublisherId"] = publisherId,
                            // todo, probably not needed
                            ["DataSetWriterId"] = dataSetWriterId
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
                    },
                    //  TODO provide default settings
                    KeyFrameCount = null,
                    DataSetMetaDataSendInterval = null,
                    KeyFrameInterval = null
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
        /// Remove a node from job.
        /// </summary>
        /// <param name="publishJob"></param>
        /// <param name="nodeId"></param>
        /// <param name="connection"></param>
        /// <returns> Returns true if an item has been removed from the job, false otherwise. </returns>
        private bool RemoveItemFromJob(
            WriterGroupJobModel publishJob,
            string nodeId,
            ConnectionModel connection
        ) {
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
        private readonly IPublishServicesConfig _config;
        private static readonly Counter kNodePublishStart = Metrics.CreateCounter("iiot_publisher_node_publish_start", "calls to nodePublishStartAsync");
        private static readonly Counter kNodePublishStop = Metrics.CreateCounter("iiot_publisher_node_publish_stop", "calls to nodePublishStopAsync");
    }
}
