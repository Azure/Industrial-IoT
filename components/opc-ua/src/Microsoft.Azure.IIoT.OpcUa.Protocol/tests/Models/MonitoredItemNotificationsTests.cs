namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Tests.Models {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;
    using Newtonsoft.Json;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Protocol.Models;
    using Xunit;

    public class MonitoredItemNotificationsTests {
        
        [Fact(Skip="Only for performance evaluation, should not run daily")]
        public void ComparePerformanceOfConversion() {

            BenchmarkRunner.Run<MonitoredItemNotificationConversion>(new DebugBuildConfig());
        }

        [Fact]
        public void Test_ComplianceToOldImplementation_Expect_EqualResults() {
            var notification = JsonConvert.DeserializeObject<DataChangeNotification>(File.ReadAllText(GetTestFilePath("DataChangeNotification.json")));
            var monitoredItems = JsonConvert.DeserializeObject<IEnumerable<MonitoredItem>>(File.ReadAllText(GetTestFilePath("SubscriptionMonitoredItems.json")));

            var oldResult = MonitoredItemNotificationConversion.ToMonitoredItemNotifications(notification, monitoredItems).ToList();
            var newResult = MonitoredItemNotificationConversion.ToMonitoredItemNotificationsNew(notification, monitoredItems).ToList();

            for (int i = 0; i < oldResult.Count; i++) {
                var itemOld = oldResult[i];
                var itemNew = newResult[i];

                Assert.Equal(itemOld.AttributeId, itemNew.AttributeId);
                Assert.Equal(itemOld.ClientHandle, itemNew.ClientHandle);
                Assert.Equal(itemOld.DiagnosticInfo, itemNew.DiagnosticInfo);
                Assert.Equal(itemOld.DisplayName, itemNew.DisplayName);
                Assert.Equal(itemOld.Id, itemNew.Id);
                Assert.Equal(itemOld.IsHeartbeat, itemNew.IsHeartbeat);
                Assert.Equal(itemOld.NodeId, itemNew.NodeId);
                Assert.Equal(itemOld.NotificationData, itemNew.NotificationData);
                Assert.Equal(itemOld.Overflow, itemNew.Overflow);
                Assert.Equal(itemOld.PublishTime, itemNew.PublishTime);
                Assert.Equal(itemOld.SequenceNumber, itemNew.SequenceNumber);
                Assert.Equal(itemOld.StringTable, itemNew.StringTable);
                Assert.Equal(itemOld.Value, itemNew.Value);
            }
        }

        private string GetTestFilePath(string filename) {
            return Path.Combine(Environment.CurrentDirectory, "TestData", filename);
        }
    }

    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MarkdownExporter]
    [HtmlExporter]
    public class MonitoredItemNotificationConversion {
        [Params(1000, 1000)]
        public static int _numberOfElements;

        private DataChangeNotification _notification;
        private IList<MonitoredItem> _monitoredItems;
        
        [GlobalSetup]
        public void Setup() {
            _notification = new DataChangeNotification();

            for (int i = 0; i < _numberOfElements; i++) {

                _notification.MonitoredItems.Add(new MonitoredItemNotification() {
                    Message = new NotificationMessage(),
                    Value = new DataValue(new Variant(i)),
                    ClientHandle = (uint)i
                });
            }

            _monitoredItems = new List<MonitoredItem>(_numberOfElements);
            for (uint i = 0; i < _numberOfElements; i++) {
                _monitoredItems.Add(new MonitoredItem(i));

            }
        }

        [Benchmark]
        public IEnumerable<MonitoredItemNotificationModel> Original() => ToMonitoredItemNotifications(_notification, _monitoredItems).ToList();

        [Benchmark]
        public IEnumerable<MonitoredItemNotificationModel> New() => ToMonitoredItemNotificationsNew(_notification, _monitoredItems).ToList();

        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotifications(
            DataChangeNotification notification, IEnumerable<MonitoredItem> monitoredItems) {
            for (var i = 0; i < notification.MonitoredItems.Count; i++) {
                var monitoredItem = monitoredItems.SingleOrDefault(
                    m => m.ClientHandle == notification?.MonitoredItems[i]?.ClientHandle);
                if (monitoredItem == null) {
                    continue;
                }
                var message = notification?.MonitoredItems[i]?
                    .ToMonitoredItemNotification(monitoredItem);
                if (message == null) {
                    continue;
                }
                yield return message;
            }
        }

        public static IEnumerable<MonitoredItemNotificationModel> ToMonitoredItemNotificationsNew(
            DataChangeNotification notification, IEnumerable<MonitoredItem> monitoredItems) {
            var handles = new Dictionary<uint, MonitoredItem>(_numberOfElements);

            foreach (var monitoredItem in monitoredItems) {
                handles.Add(monitoredItem.ClientHandle, monitoredItem);
            }

            foreach (var monitoredItemWithNotification in notification.MonitoredItems) {
                if (handles.TryGetValue(monitoredItemWithNotification.ClientHandle, out var monitoredItem)) {
                    var message = monitoredItemWithNotification.ToMonitoredItemNotification(monitoredItem);
                    if (message == null) {
                        continue;
                    }
                    yield return message;
                }
            }
        }
    }
}
