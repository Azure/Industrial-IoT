// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.Devices.Client;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;
    using Autofac;
    using System.Linq;
    using System.Threading;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Tests.Engine;

    public class MethodRouterTests {
        private readonly IJsonSerializer _serializer = new NewtonSoftJsonSerializer();

        [Fact]
        public async Task TestTest1Invocation() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                kRand.NextBytes(buffer);
                var expected = new TestModel { Test = buffer };

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test1_V1",
                    JsonPayload = _serializer.SerializeToString(expected)
                });

                var returned = _serializer.Deserialize<TestModel>(response.JsonPayload);
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
                kRand.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test2_V1",
                    JsonPayload = _serializer.SerializeToString(buffer)
                });

                var returned = _serializer.Deserialize<byte[]>(response.JsonPayload);
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
                kRand.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test3_V1",
                    JsonPayload = _serializer.SerializeToString(new {
                        request = buffer,
                        value = 55
                    })
                });

                var returned = _serializer.Deserialize<byte[]>(response.JsonPayload);
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
                kRand.NextBytes(buffer);
                var expected = 3254;

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel {
                    Name = "Test3_v2",
                    JsonPayload = _serializer.SerializeToString(new {
                        request = buffer,
                        value = expected
                    })
                });

                var returned = _serializer.Deserialize<int>(response.JsonPayload);
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
                    JsonPayload = _serializer.SerializeToString(null)
                });

                var returned = _serializer.Deserialize<string>(response.JsonPayload);
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
                    JsonPayload = _serializer.SerializeToString(nameof(TestControllerV1.TestNoReturnAsync))
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
                    JsonPayload = _serializer.SerializeToString(null)
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
                kRand.NextBytes(buffer);
                var expected = new TestModel { Test = buffer };
                var response = await hub.CallMethodAsync(device, module, "Test1_V1",
                    _serializer.SerializeToString(expected));
                var returned = _serializer.Deserialize<TestModel>(response);
                Assert.Equal(expected.Test, returned.Test);
            });
        }

        [Fact]
        public async Task TestTest1InvocationWithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                kRand.NextBytes(buffer);
                var expected = new TestModel { Test = buffer };
                var response = await hub.CallMethodAsync(device, module, "Test1_V1",
                    _serializer.SerializeToString(expected));
                var returned = _serializer.Deserialize<TestModel>(response);
                Assert.Equal(expected.Test, returned.Test);
            });
        }

        [Fact]
        public async Task TestTest2InvocationWithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                kRand.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, "Test2_V1",
                    _serializer.SerializeToString(buffer));

                var returned = _serializer.Deserialize<byte[]>(response);
                Assert.True(buffer.SequenceEqual(returned));
            });
        }

        [Fact]
        public async Task TestTest3InvocationWithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                kRand.NextBytes(buffer);

                var response = await hub.CallMethodAsync(device, module, "Test3_V1",
                    _serializer.SerializeToString(new {
                        request = buffer,
                        value = 55
                    })
                );

                var returned = _serializer.Deserialize<byte[]>(response);
                Assert.True(buffer.SequenceEqual(returned));
            });
        }

        [Fact]
        public async Task TestTest3InvocationV2WithLargeBufferUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                kRand.NextBytes(buffer);
                var expected = 3254;

                var response = await hub.CallMethodAsync(device, module, "Test3_V2",
                    _serializer.SerializeToString(new {
                        request = buffer,
                        value = expected
                    })
                );

                var returned = _serializer.Deserialize<int>(response);
                Assert.Equal(expected, returned);
            });
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNoParamUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParameters_V1",
                    _serializer.SerializeToString(null));

                var returned = _serializer.Deserialize<string>(response);
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

                var returned = _serializer.Deserialize<string>(response);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
            });
        }

        [Fact]
        public async Task TestTestNoReturnInvocationNoReturnUsingMethodClient() {
            var harness = new ModuleHostHarness();
            await harness.RunTestAsync(GetControllers(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoReturn_V1",
                    _serializer.SerializeToString(nameof(TestControllerV1.TestNoReturnAsync)));

                Assert.Null(response);
            });
        }

        [Fact]
        public async Task TestTestNoParametersAndNoReturnInvocationNoParamAndNoReturnUsingMethodClientAsync() {
            var harness = new ModuleHostHarness();
            var controller = new TestControllerV1();
            await harness.RunTestAsync(controller.YieldReturn(), async (device, module, services) => {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParametersAndNoReturn_V1",
                    _serializer.SerializeToString(null));

                Assert.Null(response);
                Assert.True(controller._noparamcalled);
            });
        }

        [Fact]
        public void TestTest1InvocationNonChunked() {
            var router = GetRouter();

            var buffer = new byte[1049];
            kRand.NextBytes(buffer);
            var expected = new TestModel { Test = buffer };
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test1_V1",
                    _serializer.SerializeToBytes(expected).ToArray())).Result;

            var returned = _serializer.Deserialize<TestModel>(
                response.ResultAsJson);
            Assert.Equal(expected.Test, returned.Test);
        }

        [Fact]
        public void TestTest1InvocationChunked() {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Logger);

            var buffer = new byte[300809];
            kRand.NextBytes(buffer);
            var expected = new TestModel { Test = buffer };
            var response = client.CallMethodAsync("test", "test", "Test1_V1",
                _serializer.SerializeToBytes(expected).ToArray(),
                    null, null, CancellationToken.None).Result;

            var returned = _serializer.Deserialize<TestModel>(
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
            kRand.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_V1", _serializer.SerializeToBytes(expected).ToArray())).Result;
            var returned = _serializer.Deserialize<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(19)]
        [InlineData(1049)]
        [InlineData(64 * 1024)]
        [InlineData(95 * 1024)]
        public void TestTest8InvocationV1NonChunked(int size) {
            var router = GetRouter();
            var expected = new byte[size];
            kRand.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test8_V1", _serializer.SerializeToBytes(expected).ToArray())).Result;
            var returned = _serializer.Deserialize<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(19)]
        [InlineData(1049)]
        [InlineData(64 * 1024)]
        [InlineData(95 * 1024)]
        public void TestTest8InvocationV2NonChunked(int size) {
            var router = GetRouter();
            var expected = new byte[size];
            kRand.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test8_V2", _serializer.SerializeToBytes(expected).ToArray())).Result;
            var returned = _serializer.Deserialize<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest2InvocationNonChunkedFailsWithLargeBuffer() {
            var router = GetRouter();
            var expected = new byte[96 * 1024];
            kRand.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_V1", _serializer.SerializeToBytes(expected).ToArray())).Result;
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
                _serializer, Log.Logger);
            var expected = new byte[size];
            kRand.NextBytes(expected);
            var response = client.CallMethodAsync("test", "test", "Test2_V1",
                _serializer.SerializeToBytes(expected).ToArray(),
                    null, null, CancellationToken.None).Result;

            var returned = _serializer.Deserialize<byte[]>(
                Encoding.UTF8.GetString(response));
            Assert.Equal(expected, returned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(19)]
        [InlineData(1049)]
        [InlineData(128 * 1024)]
        [InlineData(450000)]
        [InlineData(129 * 1024)]
        public void TestTest8InvocationV1Chunked(int size) {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Logger);
            var expected = new byte[size];
            kRand.NextBytes(expected);
            var response = client.CallMethodAsync("test", "test", "Test8_V1",
                _serializer.SerializeToBytes(expected).ToArray(),
                    null, null, CancellationToken.None).Result;

            var returned = _serializer.Deserialize<byte[]>(
                Encoding.UTF8.GetString(response));
            Assert.Equal(expected, returned);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(19)]
        [InlineData(1049)]
        [InlineData(128 * 1024)]
        [InlineData(450000)]
        [InlineData(129 * 1024)]
        public void TestTest8InvocationV2Chunked(int size) {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Logger);
            var expected = new byte[size];
            kRand.NextBytes(expected);
            var response = client.CallMethodAsync("test", "test", "Test8_V2",
                _serializer.SerializeToBytes(expected).ToArray(),
                    null, null, CancellationToken.None).Result;

            var returned = _serializer.Deserialize<byte[]>(
                Encoding.UTF8.GetString(response));
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest3InvocationNonChunked() {
            var router = GetRouter();
            var expected = new byte[1049];
            kRand.NextBytes(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test3_V1",
                    _serializer.SerializeToBytes(new {
                        request = expected,
                        Value = 3254
                    }).ToArray())).Result;

            var returned = _serializer.Deserialize<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest2InvocationV2NonChunked() {
            var router = GetRouter();
            var buffer = new byte[1049];
            kRand.NextBytes(buffer);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_v2",
                    _serializer.SerializeToBytes(buffer).ToArray())).Result;

            Assert.Equal(400, response.Status);
            var deserializedResponse = _serializer.Deserialize<MethodCallStatusExceptionModel>(
                response.ResultAsJson);
            var ex = _serializer.Deserialize<ArgumentNullException>(
                deserializedResponse.Details);
            Assert.Equal("request", ex.ParamName);
        }

        [Fact]
        public void TestTest3InvocationV2NonChunked() {
            var router = GetRouter();
            var buffer = new byte[1049];
            kRand.NextBytes(buffer);
            var expected = 3254;
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test3_v2",
                    _serializer.SerializeToBytes(new {
                        request = buffer,
                        Value = expected
                    }).ToArray())).Result;

            var returned = _serializer.Deserialize<int>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        private static readonly Random kRand = new Random();

        private MethodRouter GetRouter() {
            return new MethodRouter(_serializer, Log.Logger) {
                Controllers = GetControllers()
            };
        }

        private static List<IMethodController> GetControllers() {
            return new List<IMethodController> {
                new TestControllerV1(),
                new TestControllerV2(),
                new TestControllerV1And2()
            };
        }

        public class TestMethodClient : IJsonMethodClient {

            public int MaxMethodPayloadCharacterCount => 120 * 1024;

            public TestMethodClient(MethodRouter router) {
                _router = router;
            }

            public async Task<string> CallMethodAsync(string deviceId,
                string moduleId, string method, string payload,
                TimeSpan? timeout, CancellationToken ct) {
                var result = await _router.InvokeMethodAsync(
                    new MethodRequest(method, Encoding.UTF8.GetBytes(payload),
                        timeout, timeout));
                if (result.Status != 200) {
                    throw new MethodCallStatusException(result.ResultAsJson,
                        result.Status);
                }
                return result.ResultAsJson;
            }

            private readonly MethodRouter _router;
        }

        public class TestModel {
            public byte[] Test { get; set; }
        }

        [Version("_V1")]
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

        [Version("_V2")]
        public class TestControllerV2 : IMethodController {

            public Task<byte[]> Test2Async(byte[] request) {
                if (request == null) {
                    throw new ArgumentNullException(nameof(request));
                }
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

        [Version("_V1")]
        [Version("_V2")]
        public class TestControllerV1And2 : IMethodController {

            public Task<byte[]> Test8Async(byte[] request) {
                return Task.FromResult(request);
            }
        }
    }
}
