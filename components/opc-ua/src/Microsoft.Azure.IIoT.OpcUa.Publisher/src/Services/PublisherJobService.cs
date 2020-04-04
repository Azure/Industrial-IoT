// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        public PublisherJobService(IEndpointRegistry endpoints, IJobScheduler jobs,
            IJobSerializer serializer) {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        }

        /// <inheritdoc/>
        public async Task<PublishStartResultModel> NodePublishStartAsync(
            string endpointId, PublishStartRequestModel request) {
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

            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), job => {
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
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), job => {
                var publishJob = AsJob(job);
                var jobChanged = false;
                var connection = new ConnectionModel {
                    Endpoint = endpoint.Registration.Endpoint,
                    Diagnostics = request.Header?.Diagnostics,
                    User = request.Header?.Elevation
                };

                if (request.NodesToAdd != null) {
                    foreach (var item in request.NodesToAdd) {
                        AddOrUpdateItemInJob(publishJob, item, endpointId, job.Id,
                            connection);
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
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), job => {

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
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultId(endpointId), job => {
                var publishJob = AsJob(job);
                list = publishJob.WriterGroup.DataSetWriters
                    .Select(writer => writer.DataSet.DataSetSource)
                    .SelectMany(source => source.PublishedVariables.PublishedData
                        .Select(variable => new PublishedItemModel {
                            NodeId = variable.PublishedVariableNodeId,
                            DisplayName = variable.PublishedVariableDisplayName,
                            SamplingInterval = variable.SamplingInterval,
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
                    return publishJob;
                }
            }

            return new WriterGroupJobModel {
                MessagingMode = MessagingMode.Samples,
                WriterGroup = new WriterGroupModel {
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
                Engine = null,
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
        private void AddOrUpdateItemInJob(WriterGroupJobModel publishJob,
            PublishedItemModel publishedItem, string endpointId, string publisherId,
            ConnectionModel connection) {

            // Simple - first remove - then add.
            RemoveItemFromJob(publishJob, publishedItem.NodeId, connection);

            // Find existing subscription we can add node to
            List<PublishedDataSetVariableModel> variables = null;
            foreach (var writer in publishJob.WriterGroup.DataSetWriters) {
                if (writer.DataSet.DataSetSource.Connection.IsSameAs(connection) &&
                    writer.DataSet.DataSetSource.SubscriptionSettings?.PublishingInterval ==
                        publishedItem.PublishingInterval) {
                    System.Diagnostics.Debug.Assert(writer.DataSet.DataSetSource.PublishedVariables.PublishedData != null);
                    variables = writer.DataSet.DataSetSource.PublishedVariables.PublishedData;
                    writer.DataSet.DataSetMetaData.ConfigurationVersion.MinorVersion++;
                    break;
                }
            }
            if (variables == null) {
                // No writer found - add new one with a published dataset
                var dataSetWriter = new DataSetWriterModel {
                    DataSetWriterId = GetDefaultId(endpointId),
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
                            ["DataSetWriterId"] = endpointId
                        },
                        DataSetSource = new PublishedDataSetSourceModel {
                            Connection = connection,
                            PublishedEvents = null,
                            PublishedVariables = new PublishedDataItemsModel {
                                PublishedData = new List<PublishedDataSetVariableModel>()
                            },
                            SubscriptionSettings = new PublishedDataSetSettingsModel {
                                PublishingInterval = publishedItem.PublishingInterval
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
                HeartbeatInterval = publishedItem.HeartbeatInterval
            });
        }

        /// <summary>
        /// Add or update subscription
        /// </summary>
        /// <param name="publishJob"></param>
        /// <param name="nodeId"></param>
        /// <param name="connection"></param>
        private bool RemoveItemFromJob(WriterGroupJobModel publishJob,
            string nodeId, ConnectionModel connection) {
            foreach (var writer in publishJob.WriterGroup.DataSetWriters.ToList()) {
                if (!writer.DataSet.DataSetSource.Connection.IsSameAs(connection)) {
                    continue;
                }
                foreach (var item in writer.DataSet.DataSetSource.PublishedVariables.PublishedData.ToList()) {
                    if (item.PublishedVariableNodeId == nodeId) {
                        writer.DataSet.DataSetSource.PublishedVariables.PublishedData.Remove(item);
                        if (writer.DataSet.DataSetSource.PublishedVariables.PublishedData.Count == 0) {
                            // Trim writer also
                            publishJob.WriterGroup.DataSetWriters.Remove(writer);
                        }
                        return true;
                    }
                }
            }
            return false;
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
    }
}
