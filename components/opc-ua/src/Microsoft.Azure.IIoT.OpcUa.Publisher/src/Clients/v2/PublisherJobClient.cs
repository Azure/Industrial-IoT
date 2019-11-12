// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2 {
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher client
    /// </summary>
    public sealed class PublisherJobClient : IPublishServices<string> {

        /// <summary>
        /// Create client
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="jobs"></param>
        /// <param name="serializer"></param>
        /// <param name="logger"></param>
        public PublisherJobClient(IEndpointRegistry endpoints, IJobScheduler jobs,
            IJobSerializer serializer, ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultJobId(endpointId), job => {
                var publishJob = AsMonitoredItemJob(job);

                // Add subscription
                AddOrUpdateItemInJob(publishJob, request.Item, endpointId,
                    new ConnectionModel {
                        Endpoint = endpoint.Registration.Endpoint,
                        Diagnostics = request.Header?.Diagnostics,
                        User = request.Header?.Elevation
                    });

                job.JobConfiguration = _serializer.SerializeJobConfiguration(
                    publishJob, out var jobType);
                job.JobConfigurationType = jobType;
                if (publishJob.Job.Subscriptions.Count != 0 &&
                    job.LifetimeData.Status != JobStatus.Deleted) {
                    job.LifetimeData.Status = JobStatus.Active;
                }
                job.Demands = PublisherDemands(endpoint);
                return Task.FromResult(true);
            });
            return new PublishStartResultModel();
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
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultJobId(endpointId), job => {

                // remove from job
                var publishJob = AsMonitoredItemJob(job);
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
                    if (publishJob.Job.Subscriptions.Count == 0 &&
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
            var result = await _jobs.NewOrUpdateJobAsync(GetDefaultJobId(endpointId), job => {
                var publishJob = AsMonitoredItemJob(job);
                list = publishJob.Job.Subscriptions
                    .SelectMany(s => s.Subscription.MonitoredItems
                        .Select(i => new PublishedItemModel {
                            NodeId = i.NodeId,
                            SamplingInterval = i.SamplingInterval,
                            PublishingInterval = s.Subscription.PublishingInterval
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
        private static string GetDefaultJobId(string endpointId) {
            return $"{endpointId}_default";
        }

        /// <summary>
        /// Deserialize the monitored item job
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private MonitoredItemDeviceJobModel AsMonitoredItemJob(JobInfoModel job) {
            if (job.JobConfiguration != null) {
                var publishJob = (MonitoredItemDeviceJobModel)_serializer.DeserializeJobConfiguration(
                    job.JobConfiguration, job.JobConfigurationType);
                if (publishJob != null) {
                    return publishJob;
                }
            }
            return new MonitoredItemDeviceJobModel {
                Job = new MonitoredItemJobModel {
                    Subscriptions = new List<SubscriptionInfoModel>()
                }
            };
        }

        /// <summary>
        /// Add or update item in job
        /// </summary>
        /// <param name="publishJob"></param>
        /// <param name="publishedItem"></param>
        /// <param name="endpointId"></param>
        /// <param name="connection"></param>
        private void AddOrUpdateItemInJob(MonitoredItemDeviceJobModel publishJob,
            PublishedItemModel publishedItem, string endpointId, ConnectionModel connection) {

            // Simple - first remove - then add.
            RemoveItemFromJob(publishJob, publishedItem.NodeId, connection);

            // Find existing subscription we can add node to
            List<MonitoredItemModel> monitoredItems = null;
            foreach (var s in publishJob.Job.Subscriptions) {
                if (s.Connection.IsSameAs(connection) &&
                    s.Subscription.PublishingInterval == publishedItem.PublishingInterval) {
                    System.Diagnostics.Debug.Assert(s.Subscription.MonitoredItems != null);
                    monitoredItems = s.Subscription.MonitoredItems;
                    break;
                }
            }
            if (monitoredItems == null) {
                // No subscription found - add new subscription
                var subscription = new SubscriptionInfoModel {
                    Connection = connection,
                    MessageMode = MessageModes.MonitoredItem,
                    Subscription = new SubscriptionModel {
                        Id = GetDefaultJobId(endpointId),
                        PublishingInterval = publishedItem.PublishingInterval,
                        MonitoredItems = new List<MonitoredItemModel>()
                    },
                    ExtraFields = new Dictionary<string, string> {
                        [nameof(MonitoredItemMessageModel.EndpointId)] = endpointId
                    }
                };
                monitoredItems = subscription.Subscription.MonitoredItems;
                publishJob.Job.Subscriptions.Add(subscription);
            }

            // Add to monitored items
            monitoredItems.Add(new MonitoredItemModel {
                SamplingInterval = publishedItem.SamplingInterval,
                NodeId = publishedItem.NodeId
            });
        }

        /// <summary>
        /// Add or update subscription
        /// </summary>
        /// <param name="publishJob"></param>
        /// <param name="nodeId"></param>
        /// <param name="connection"></param>
        private bool RemoveItemFromJob(MonitoredItemDeviceJobModel publishJob,
            string nodeId, ConnectionModel connection) {
            foreach (var s in publishJob.Job.Subscriptions.ToList()) {
                if (!s.Connection.IsSameAs(connection)) {
                    continue;
                }
                foreach (var item in s.Subscription.MonitoredItems.ToList()) {
                    if (item.NodeId == nodeId) {
                        s.Subscription.MonitoredItems.Remove(item);
                        if (s.Subscription.MonitoredItems.Count == 0) {
                            // Trim subscription also
                            publishJob.Job.Subscriptions.Remove(s);
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
                    Value = "Publisher"
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
        private readonly ILogger _logger;
        private readonly IJobSerializer _serializer;
    }
}
