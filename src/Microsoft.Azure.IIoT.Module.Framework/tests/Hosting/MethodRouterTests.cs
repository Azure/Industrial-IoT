// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module.Default;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    public class MethodRouterTests {

        [Fact]
        public async Task TestTest1Invocation() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                r.NextBytes(buffer);
                var expected = new TestModel { Test = buffer };

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test1_V1",
                    JsonPayload = JsonConvertEx.SerializeObject(expected)
                });

                var returned = JsonConvertEx.DeserializeObject<TestModel>(response.JsonPayload);
                Assert.Equal(expected.Test, returned.Test);
                Assert.Equal(200, response.Status);
            });
        }

        [Fact]
        public async Task TestTest2Invocation() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                r.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test2_V1",
                    JsonPayload = JsonConvertEx.SerializeObject(buffer)
                });

                var returned = JsonConvertEx.DeserializeObject<byte[]>(response.JsonPayload);
                Assert.True(buffer.SequenceEqual(returned));
                Assert.Equal(200, response.Status);
            });
        }

        [Fact]
        public async Task TestTest3Invocation() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                r.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test3_V1",
                    JsonPayload = JsonConvertEx.SerializeObject(new {
                        request = buffer,
                        value = 55
                    })
                });

                var returned = JsonConvertEx.DeserializeObject<byte[]>(response.JsonPayload);
                Assert.True(buffer.SequenceEqual(returned));
                Assert.Equal(200, response.Status);
            });
        }

        [Fact]
        public async Task TestTest3InvocationV2() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                r.NextBytes(buffer);
                var expected = 3254;

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test3_v2",
                    JsonPayload = JsonConvertEx.SerializeObject(new {
                        request = buffer,
                        value = expected
                    })
                });

                var returned = JsonConvertEx.DeserializeObject<int>(response.JsonPayload);
                Assert.Equal(expected, returned);
                Assert.Equal(200, response.Status);
            });
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNoParam() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "TestNoParameters_V1",
                    JsonPayload = JsonConvertEx.SerializeObject(null)
                });

                var returned = JsonConvertEx.DeserializeObject<string>(response.JsonPayload);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
                Assert.Equal(200, response.Status);
            });
        }

        [Fact]
        public async Task TestTestNoReturnInvocationNoReturn() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "TestNoReturn_V1",
                    JsonPayload = JsonConvertEx.SerializeObject(nameof(TestControllerV1.TestNoReturnAsync))
                });

                Assert.Null(response.JsonPayload);
                Assert.Equal(200, response.Status);
            });
        }

        [Fact]
        public async Task TestTestNoParametersAndNoReturnInvocationNoParamAndNoReturn() {
            var harness = new ModuleHostHarness();
            var controller = new TestControllerV1();
            await harness.RunTestAsync(controller.YieldReturn(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "TestNoParametersAndNoReturn_V1",
                    JsonPayload = JsonConvertEx.SerializeObject(null)
                });

                Assert.Null(response.JsonPayload);
                Assert.Equal(200, response.Status);
                Assert.True(controller._noparamcalled);
            });
        }

        [Fact]
        public async Task TestTest1InvocationWithSmallBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[1049];
                r.NextBytes(buffer);
                var expected = new TestModel { Test = buffer };
                var response = await hub.CallMethodAsync(device, module, "Test1_V1",
                    JsonConvertEx.SerializeObject(expected));
                var returned = JsonConvertEx.DeserializeObject<TestModel>(response);
                Assert.Equal(expected.Test, returned.Test);
            });
        }

        [Fact]
        public async Task TestTest1InvocationWithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                r.NextBytes(buffer);
                var expected = new TestModel { Test = buffer };
                var response = await hub.CallMethodAsync(device, module, "Test1_V1",
                    JsonConvertEx.SerializeObject(expected));
                var returned = JsonConvertEx.DeserializeObject<TestModel>(response);
                Assert.Equal(expected.Test, returned.Test);
            });
        }

        [Fact]
        public async Task TestTest2InvocationWithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                r.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, "Test2_V1",
                    JsonConvertEx.SerializeObject(buffer));

                var returned = JsonConvertEx.DeserializeObject<byte[]>(response);
                Assert.True(buffer.SequenceEqual(returned));
            });
        }

        [Fact]
        public async Task TestTest3InvocationWithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                r.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, "Test3_V1",
                    JsonConvertEx.SerializeObject(new {
                        request = buffer,
                        value = 55
                    })
                );

                var returned = JsonConvertEx.DeserializeObject<byte[]>(response);
                Assert.True(buffer.SequenceEqual(returned));
            });
        }

        [Fact]
        public async Task TestTest3InvocationV2WithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                r.NextBytes(buffer);
                var expected = 3254;

                var response = await hub.CallMethodAsync(device, module, "Test3_V2",
                    JsonConvertEx.SerializeObject(new {
                        request = buffer,
                        value = expected
                    })
                );

                var returned = JsonConvertEx.DeserializeObject<int>(response);
                Assert.Equal(expected, returned);
            });
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNoParamUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParameters_V1",
                    JsonConvertEx.SerializeObject(null));

                var returned = JsonConvertEx.DeserializeObject<string>(response);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
            });
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNullParamUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParameters_V1",
                    null);

                var returned = JsonConvertEx.DeserializeObject<string>(response);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
            });
        }

        [Fact]
        public async Task TestTestNoReturnInvocationNoReturnUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoReturn_V1",
                    JsonConvertEx.SerializeObject(nameof(TestControllerV1.TestNoReturnAsync)));

                Assert.Null(response);
            });
        }

        [Fact]
        public async Task TestTestNoParametersAndNoReturnInvocationNoParamAndNoReturnUsingMethodClient() {
            var harness = new ModuleHostHarness();
            var controller = new TestControllerV1();
            await harness.RunTestAsync(controller.YieldReturn(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParametersAndNoReturn_V1",
                    JsonConvertEx.SerializeObject(null));

                Assert.Null(response);
                Assert.True(controller._noparamcalled);
            });
        }

        [Fact]
        public void TestTest1InvocationNonChunked() {
            var router = GetRouter();

            var buffer = new byte[1049];
            r.NextBytes(buffer);
            var expected = new TestModel { Test = buffer };
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test1_V1", Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(expected)))).Result;

            var returned = JsonConvertEx.DeserializeObject<TestModel>(
                response.ResultAsJson);
            Assert.Equal(expected.Test, returned.Test);
        }

        [Fact]
        public void TestTest1InvocationChunked() {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                Log.Logger);

            var buffer = new byte[300809];
            r.NextBytes(buffer);
            var expected = new TestModel { Test = buffer };
            var response = client.CallMethodAsync("test", "test", "Test1_V1",
                Encoding.UTF8.GetBytes(JsonConvertEx.SerializeObject(expected)),
                    null, null).Result;

            var returned = JsonConvertEx.DeserializeObject<TestModel>(
                Encoding.UTF8.GetString(response));
            Assert.Equal(expected.Test, returned.Test);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(19)]
        [InlineData(1049)]
        [InlineData(64 * 1024)]
        [InlineData(95 * 1024)]
        public void TestTest2InvocationNonChunked(int size) {
            var router = GetRouter();
            var expected = new byte[size];
            r.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_V1", Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(expected)))).Result;
            var returned = JsonConvertEx.DeserializeObject<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest2InvocationNonChunkedFailsWithLargeBuffer() {
            var router = GetRouter();
            var expected = new byte[96 * 1024];
            r.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_V1", Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(expected)))).Result;
            Assert.Equal((int)HttpStatusCode.RequestEntityTooLarge,
                response.Status);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(19)]
        [InlineData(1049)]
        [InlineData(128 * 1024)]
        [InlineData(450000)]
        [InlineData(129 * 1024)]
        public void TestTest2InvocationChunked(int size) {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                Log.Logger);
            var expected = new byte[size];
            r.NextBytes(expected);
            var response = client.CallMethodAsync("test", "test", "Test2_V1",
                Encoding.UTF8.GetBytes(JsonConvertEx.SerializeObject(expected)),
                    null, null).Result;

            var returned = JsonConvertEx.DeserializeObject<byte[]>(
                Encoding.UTF8.GetString(response));
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest3InvocationNonChunked() {
            var router = GetRouter();
            var expected = new byte[1049];
            r.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test3_V1", Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(new {
                        request = expected,
                        Value = 3254
                    })))).Result;

            var returned = JsonConvertEx.DeserializeObject<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest2InvocationV2NonChunked() {
            var router = GetRouter();
            var buffer = new byte[1049];
            r.NextBytes(buffer);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_v2", Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(buffer)))).Result;

            Assert.Equal(400, response.Status);
            var ex = JsonConvertEx.DeserializeObject<ArgumentNullException>(
                response.ResultAsJson);
            Assert.Equal("request", ex.ParamName);
        }

        [Fact]
        public void TestTest3InvocationV2NonChunked() {
            var router = GetRouter();
            var buffer = new byte[1049];
            r.NextBytes(buffer);
            var expected = 3254;
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test3_v2", Encoding.UTF8.GetBytes(
                    JsonConvertEx.SerializeObject(new {
                        request = buffer,
                        Value = expected
                    })))).Result;

            var returned = JsonConvertEx.DeserializeObject<int>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        private static readonly Random r = new Random();

        private static MethodRouter GetRouter() {
            return new MethodRouter(Log.Logger) {
                Controllers = GetControllers()
            };
        }

        private static List<IMethodController> GetControllers() {
            return new List<IMethodController> {
                new TestControllerV1(),
                new TestControllerV2()
            };
        }

        public class TestMethodClient : IJsonMethodClient {

            public int MaxMethodPayloadCharacterCount => 120 * 1024;

            public TestMethodClient(MethodRouter router) {
                _router = router;
            }

            public async Task<string> CallMethodAsync(string deviceId,
                string moduleId, string method, string payload,
                TimeSpan? timeout) {
                var result = await _router.InvokeMethodAsync(
                    new MethodRequest(method, Encoding.UTF8.GetBytes(payload),
                        timeout, timeout));
                if (result.Status != 200) {
                    throw new MethodCallStatusException(
                        Encoding.UTF8.GetBytes(result.ResultAsJson),
                        result.Status);
                }
                return result.ResultAsJson;
            }

            private readonly MethodRouter _router;
        }

        public class TestModel {
            public byte[] Test { get; set; }
        }

        [Version(1)]
        public class TestControllerV1 : IMethodController {

            public Task<TestModel> Test1Async(TestModel request) {
                return Task.FromResult(request);
            }
            public Task<byte[]> Test2Async(byte[] request) {
                return Task.FromResult(request);
            }
            public Task<byte[]> Test3Async(byte[] request, int value) {
                if (value == 0) {
                    throw new ArgumentNullException(nameof(value));
                }
                return Task.FromResult(request);
            }
            public Task<string> TestNoParametersAsync() {
                return Task.FromResult(nameof(TestNoParametersAsync));
            }
            public Task TestNoReturnAsync(string input) {
                if (input != nameof(TestNoReturnAsync)) {
                    throw new ArgumentNullException(nameof(input));
                }
                return Task.CompletedTask;
            }
            public Task TestNoParametersAndNoReturnAsync() {
                _noparamcalled = true;
                return Task.CompletedTask;
            }
            public bool _noparamcalled;
        }

        [Version(2)]
        public class TestControllerV2 : IMethodController {

            public Task<byte[]> Test2Async(byte[] request) {
                return Task.FromException<byte[]>(
                    new ArgumentNullException(nameof(request)));
            }

            public Task<int> Test3Async(byte[] request, int value) {
                if (request == null) {
                    throw new ArgumentNullException(nameof(request));
                }
                return Task.FromResult(value);
            }
        }
    }
}
