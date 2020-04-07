// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;

    public class SettingsRouterTests {

        [Fact]
        public async Task TestSettingDesiredPropertyAndCheckReported1() {

            var harness = new ModuleHostHarness();
            var controller = new TestController1();
            await harness.RunTestAsync(controller.YieldReturn(),
                async (deviceId, moduleId, services) => {
                    var test = _serializer.FromObject("test4");
                    // Act
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting1), test);
                    var twin = await hub.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.True(controller._applyCalled);
                    Assert.Equal("test4", controller.TestSetting1);
                    Assert.Equal(test, twin.Properties.Reported[nameof(TestController1.TestSetting1)]);
                    Assert.True((bool)twin.Properties.Reported[TwinProperty.Connected]);
                });
        }

        [Fact]
        public async Task TestSetting2DesiredPropertyAndCheckReported1() {

            var harness = new ModuleHostHarness();
            var controller = new TestController1();
            await harness.RunTestAsync(controller.YieldReturn(),
                async (deviceId, moduleId, services) => {
                    var test = _serializer.FromObject("test");
                    var test2 = _serializer.FromObject("test2");
                    // Act
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting1), test);
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting2), test2);
                    var twin = await hub.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.True(controller._applyCalled);
                    Assert.Equal("test", controller.TestSetting1);
                    Assert.Equal("test2", controller.TestSetting2);
                    Assert.Equal(test, twin.Properties.Reported[nameof(TestController1.TestSetting1)]);
                    Assert.Equal(test2, twin.Properties.Reported[nameof(TestController1.TestSetting2)]);
                    Assert.True((bool)twin.Properties.Reported[TwinProperty.Connected]);
                });
        }

        [Fact]
        public async Task TestSetting3DesiredPropertyAndCheckReported1() {

            var harness = new ModuleHostHarness();
            var controller = new TestController1();
            await harness.RunTestAsync(controller.YieldReturn(),
                async (deviceId, moduleId, services) => {
                    var test = _serializer.FromObject("test");
                    var test2 = _serializer.FromObject("test2");
                    var test3 = _serializer.FromObject("test3");

                    // Act
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting1), test);
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting2), test2);
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting3), test3);
                    var twin = await hub.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.True(controller._applyCalled);
                    Assert.Equal("test", controller.TestSetting1);
                    Assert.Equal("test2", controller.TestSetting2);
                    Assert.Equal("test3", controller.TestSetting3);
                    Assert.Equal(test, twin.Properties.Reported[nameof(TestController1.TestSetting1)]);
                    Assert.Equal(test2, twin.Properties.Reported[nameof(TestController1.TestSetting2)]);
                    Assert.Equal(test3, twin.Properties.Reported[nameof(TestController1.TestSetting3)]);
                    Assert.True((bool)twin.Properties.Reported[TwinProperty.Connected]);
                });
        }

        [Fact]
        public async Task TestSettingDesiredPropertyAndCheckReported2Async() {

            var harness = new ModuleHostHarness();
            var controller = new TestController2();
            await harness.RunTestAsync(controller.YieldReturn(),
                async (deviceId, moduleId, services) => {
                    var expected = new Test {
                        Item1 = "test",
                        Item2 = 5454,
                        Item3 = DateTime.UtcNow
                    };
                    var test = _serializer.FromObject(expected);

                    var hub = services.Resolve<IIoTHubTwinServices>();

                    // Act
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController2.TestSetting), test);
                    var twin = await hub.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.True(controller._applyCalled);
                    Assert.Equal(expected, controller.TestSetting);
                    Assert.Equal(test, twin.Properties.Reported[nameof(TestController2.TestSetting)]);
                    Assert.True((bool)twin.Properties.Reported[TwinProperty.Connected]);
                });
        }

        [Fact]
        public async Task TestPresetSettingIsReportedOnModuleStart() {

            var harness = new ModuleHostHarness();
            var controller = new TestController1();
            await harness.RunTestAsync(controller.YieldReturn(),
                async (deviceId, moduleId, services) => {
                    // Act
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    var twin = await hub.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.Equal("yearn", controller.TestSetting3);

                    // TODO : Should report initial state of setting if not null
                    // Assert.Equal("yearn", twin.Properties.Reported[nameof(TestController1.TestSetting3)]);
                });
        }

        [Fact]
        public async Task TestSettingDesiredPropertyToNullAndCheckReported1() {

            var harness = new ModuleHostHarness();
            var controller = new TestController1();
            await harness.RunTestAsync(controller.YieldReturn(),
                async (deviceId, moduleId, services) => {
                    var hub = services.Resolve<IIoTHubTwinServices>();
                    var twin = await hub.GetAsync(deviceId, moduleId);

                    // TODO : Assert precondition
                    // Assert.True(twin.Properties.Reported.TryGetValue(nameof(TestController1.TestSetting3), out var pre));

                    // Act
                    await hub.UpdatePropertyAsync(deviceId, moduleId,
                        nameof(TestController1.TestSetting3), null);
                    twin = await hub.GetAsync(deviceId, moduleId);

                    // Assert
                    Assert.True(controller._applyCalled);
                    Assert.Null(controller.TestSetting3);
                    Assert.False(twin.Properties.Reported.TryGetValue(nameof(TestController1.TestSetting3), out var post));
                    Assert.Null(post);
                    Assert.True((bool)twin.Properties.Reported[TwinProperty.Connected]);
                });
        }

        public class TestController1 : ISettingsController {
            public bool _applyCalled;

            public string TestSetting1 { get; set; }

            public string TestSetting2 { get; set; }

            public string TestSetting3 { get; set; } = "yearn";

            public Task ThrowAsync() {
                return Task.FromException(new Exception());
            }

            public Task ApplyAsync() {
                _applyCalled = true;
                return Task.CompletedTask;
            }
        }

        public class Test {
            public string Item1 { get; set; }
            public int Item2 { get; set; }
            public DateTime Item3 { get; set; }

            public override bool Equals(object obj) {
                return obj is Test test &&
                       Item1 == test.Item1 &&
                       Item2 == test.Item2 &&
                       Item3 == test.Item3;
            }
            public override int GetHashCode() {
                return HashCode.Combine(Item1, Item2, Item3);
            }
        }

        public class TestController2 : ISettingsController {
            public bool _applyCalled;

            public Test TestSetting { get; set; }

            public Task ApplyAsync() {
                _applyCalled = true;
                return Task.CompletedTask;
            }
        }

        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();
    }
}
