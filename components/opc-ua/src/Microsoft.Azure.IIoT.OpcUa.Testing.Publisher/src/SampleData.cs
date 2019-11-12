// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Sample data
    /// </summary>
    public static class SampleData {

        /// <summary>
        /// Get pub sub job
        /// </summary>
        /// <param name="opcServerEndpointUrl"></param>
        /// <param name="messageSinkConnection"></param>
        /// <returns></returns>
        public static PubSubJobModel GetDataSetWriterDeviceJobModel(
            string opcServerEndpointUrl, string messageSinkConnection) {
            return new PubSubJobModel {
                ConnectionString = messageSinkConnection,
                Job = GetDataSetWriterJobModel(opcServerEndpointUrl),
            };
        }

        /// <summary>
        /// Get monitored item job
        /// </summary>
        /// <param name="opcServerEndpointUrl"></param>
        /// <param name="messageSinkConnection"></param>
        /// <returns></returns>
        public static MonitoredItemDeviceJobModel GetMonitoredItemDeviceJobModel(
            string opcServerEndpointUrl, string messageSinkConnection) {
            var opcPublisherJob = new MonitoredItemDeviceJobModel {
                ConnectionString = messageSinkConnection,
                Job = GetMonitoredItemJobModel(opcServerEndpointUrl)
            };
            return opcPublisherJob;
        }

        /// <summary>
        /// Get subscription models
        /// </summary>
        /// <param name="opcServerEndpointUrl"></param>
        /// <returns></returns>
        public static List<SubscriptionInfoModel> GetSubscriptionInfoModels(string opcServerEndpointUrl) {
            var subscriptionModel = new SubscriptionInfoModel {
                Connection = new ConnectionModel {
                    Endpoint = GetEndpointModel(opcServerEndpointUrl)
                },
                Subscription = new SubscriptionModel {
                    Id = "sub1",
                    PublishingInterval = 100
                }
            };
            var ni1 = new MonitoredItemModel {
                NodeId = "ns=0;i=2256",
                SamplingInterval = 10,
                HeartbeatInterval = 3600,
                SkipFirst = false,
                QueueSize = 100
            };
            subscriptionModel.Subscription.MonitoredItems = new List<MonitoredItemModel> {ni1};
            var subscriptionModel2 = subscriptionModel.Clone();
            subscriptionModel2.Subscription.Id = "sub2";
            subscriptionModel2.Subscription.MonitoredItems.First().NodeId = "ns=2;s=RandomSignedInt32";

            var subscriptions = new List<SubscriptionInfoModel> {
                subscriptionModel,
                subscriptionModel2
            };
            return subscriptions;
        }

        /// <summary>
        /// Get data set writer jobs
        /// </summary>
        /// <param name="opcServerEndpointUrl"></param>
        /// <returns></returns>
        public static DataSetWriterGroupModel GetDataSetWriterJobModel(string opcServerEndpointUrl) {
            var p = new DataSetWriterGroupModel {
                Connection = new ConnectionModel {
                    Endpoint = GetEndpointModel(opcServerEndpointUrl),
                },
                DataSetWriter = new DataSetWriterModel {
                    DataSets = new List<DataSetModel> { GetDataSetModel() },
                    MetadataMessageInterval = TimeSpan.Zero,
                    KeyframeMessageInterval = TimeSpan.FromSeconds(30),
                },
                PublishingInterval = TimeSpan.FromSeconds(10),
                SendChangeMessages = true,
                Engine = GetConfiguration()
            };
            return p;
        }

        /// <summary>
        /// Get monitored item job
        /// </summary>
        /// <param name="opcServerEndpointUrl"></param>
        /// <returns></returns>
        public static MonitoredItemJobModel GetMonitoredItemJobModel(
            string opcServerEndpointUrl) {
            return new MonitoredItemJobModel {
                Subscriptions = GetSubscriptionInfoModels(opcServerEndpointUrl),
                Engine = GetConfiguration(),
            };
        }

        /// <summary>
        /// Get job configuration
        /// </summary>
        /// <returns></returns>
        public static EngineConfigurationModel GetConfiguration() {
            return new EngineConfigurationModel {
                BatchSize = 1,
                DiagnosticsInterval = TimeSpan.FromSeconds(30)
            };
        }
        /// <summary>
        /// Get data set
        /// </summary>
        /// <returns></returns>

        public static DataSetModel GetDataSetModel() {
            var f1 = new DataSetFieldModel {NodeId = "ns=0;i=2256"};
            var f2 = new DataSetFieldModel {NodeId = "ns=2;s=RandomSignedInt32"};
            var dataSet = new DataSetModel {Name = "MyFirstDataSet", Fields = new List<DataSetFieldModel> {f1, f2}};
            return dataSet;
        }

        /// <summary>
        /// Get endpoint model
        /// </summary>
        /// <param name="opcServerEndpointUrl"></param>
        /// <returns></returns>
        public static EndpointModel GetEndpointModel(string opcServerEndpointUrl) {
            return new EndpointModel {
                Url = opcServerEndpointUrl,
                SecurityMode = SecurityMode.SignAndEncrypt
            };
        }
    }
}