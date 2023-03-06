// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Hosting
{
    using Microsoft.Azure.IIoT.Module.Default;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.Devices.Client;
    using Autofac;
    using Furly.Exceptions;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class MethodRouterTests
    {
        private readonly IJsonSerializer _serializer = new NewtonsoftJsonSerializer();

        [Fact]
        public async Task TestTest1Invocation()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                FillRandom(buffer);
                var expected = new TestModel { Test = buffer };

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "Test1_V1",
                    JsonPayload = _serializer.SerializeToString(expected)
                }).ConfigureAwait(false);

                var returned = _serializer.Deserialize<TestModel>(response.JsonPayload);
                Assert.Equal(expected.Test, returned.Test);
                Assert.Equal(200, response.Status);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest2Invocation()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                FillRandom(buffer);

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "Test2_V1",
                    JsonPayload = _serializer.SerializeToString(buffer)
                }).ConfigureAwait(false);

                var returned = _serializer.Deserialize<byte[]>(response.JsonPayload);
                Assert.True(buffer.SequenceEqual(returned));
                Assert.Equal(200, response.Status);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest3Invocation()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                FillRandom(buffer);

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "Test3_V1",
                    JsonPayload = _serializer.SerializeToString(new
                    {
                        request = buffer,
                        value = 55
                    })
                }).ConfigureAwait(false);

                var returned = _serializer.Deserialize<byte[]>(response.JsonPayload);
                Assert.True(buffer.SequenceEqual(returned));
                Assert.Equal(200, response.Status);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest3InvocationV2()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var buffer = new byte[1049];
                FillRandom(buffer);
                const int expected = 3254;

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "Test3_v2",
                    JsonPayload = _serializer.SerializeToString(new
                    {
                        request = buffer,
                        value = expected
                    })
                }).ConfigureAwait(false);

                var returned = _serializer.Deserialize<int>(response.JsonPayload);
                Assert.Equal(expected, returned);
                Assert.Equal(200, response.Status);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNoParam()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "TestNoParameters_V1",
                    JsonPayload = _serializer.SerializeObjectToString(null)
                }).ConfigureAwait(false);

                var returned = _serializer.Deserialize<string>(response.JsonPayload);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
                Assert.Equal(200, response.Status);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoReturnInvocationNoReturn()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "TestNoReturn_V1",
                    JsonPayload = _serializer.SerializeToString(nameof(TestControllerV1.TestNoReturnAsync))
                }).ConfigureAwait(false);

                Assert.Null(response.JsonPayload);
                Assert.Equal(200, response.Status);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoParametersAndNoReturnInvocationNoParamAndNoReturn()
        {
            var controller = new TestControllerV1();
            await ModuleHostHarness.RunTestAsync(controller.YieldReturn(), (Func<string, string, IContainer, Task>)(async (device, module, services) =>
            {
                var hub = services.Resolve<IIoTHubTwinServices>();

                var response = await hub.CallMethodAsync(device, module, new MethodParameterModel
                {
                    Name = "TestNoParametersAndNoReturn_V1",
                    JsonPayload = _serializer.SerializeObjectToString((object)null)
                }).ConfigureAwait(false);

                Assert.Null((object)response.JsonPayload);
                Assert.Equal(200, (int)response.Status);
                Assert.True(controller._noparamcalled);
            })).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest1InvocationWithSmallBufferUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[1049];
                FillRandom(buffer);
                var expected = new TestModel { Test = buffer };
                var response = await hub.CallMethodAsync(device, module, "Test1_V1",
                    _serializer.SerializeToString(expected)).ConfigureAwait(false);
                var returned = _serializer.Deserialize<TestModel>(response);
                Assert.Equal(expected.Test, returned.Test);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest1InvocationWithLargeBufferUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                FillRandom(buffer);
                var expected = new TestModel { Test = buffer };
                var response = await hub.CallMethodAsync(device, module, "Test1_V1",
                    _serializer.SerializeToString(expected)).ConfigureAwait(false);
                var returned = _serializer.Deserialize<TestModel>(response);
                Assert.Equal(expected.Test, returned.Test);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest2InvocationWithLargeBufferUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                FillRandom(buffer);

                var response = await hub.CallMethodAsync(device, module, "Test2_V1",
                    _serializer.SerializeToString(buffer)).ConfigureAwait(false);

                var returned = _serializer.Deserialize<byte[]>(response);
                Assert.True(buffer.SequenceEqual(returned));
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest3InvocationWithLargeBufferUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                FillRandom(buffer);

                var response = await hub.CallMethodAsync(device, module, "Test3_V1",
                    _serializer.SerializeToString(new
                    {
                        request = buffer,
                        value = 55
                    })
                ).ConfigureAwait(false);

                var returned = _serializer.Deserialize<byte[]>(response);
                Assert.True(buffer.SequenceEqual(returned));
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTest3InvocationV2WithLargeBufferUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var buffer = new byte[300809];
                FillRandom(buffer);
                const int expected = 3254;

                var response = await hub.CallMethodAsync(device, module, "Test3_V2",
                    _serializer.SerializeToString(new
                    {
                        request = buffer,
                        value = expected
                    })
                ).ConfigureAwait(false);

                var returned = _serializer.Deserialize<int>(response);
                Assert.Equal(expected, returned);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNoParamUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParameters_V1",
                    _serializer.SerializeObjectToString(null)).ConfigureAwait(false);

                var returned = _serializer.Deserialize<string>(response);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoParametersInvocationNullParamUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParameters_V1",
                    null).ConfigureAwait(false);

                var returned = _serializer.Deserialize<string>(response);
                Assert.Equal(nameof(TestControllerV1.TestNoParametersAsync), returned);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoReturnInvocationNoReturnUsingMethodClient()
        {
            await ModuleHostHarness.RunTestAsync(GetControllers(), async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoReturn_V1",
                    _serializer.SerializeToString(nameof(TestControllerV1.TestNoReturnAsync))).ConfigureAwait(false);

                Assert.Null(response);
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestTestNoParametersAndNoReturnInvocationNoParamAndNoReturnUsingMethodClientAsync()
        {
            var controller = new TestControllerV1();
            await ModuleHostHarness.RunTestAsync(controller.YieldReturn(), (Func<string, string, IContainer, Task>)(async (device, module, services) =>
            {
                var hub = services.Resolve<IMethodClient>();

                var response = await hub.CallMethodAsync(device, module, "TestNoParametersAndNoReturn_V1",
                    _serializer.SerializeObjectToString((object)null)).ConfigureAwait(false);

                Assert.Null((object)response);
                Assert.True(controller._noparamcalled);
            })).ConfigureAwait(false);
        }

        [Fact]
        public void TestTest1InvocationNonChunked()
        {
            var router = GetRouter();

            var buffer = new byte[1049];
            FillRandom(buffer);
            var expected = new TestModel { Test = buffer };
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test1_V1",
                    _serializer.SerializeToMemory((object)expected).ToArray())).Result;

            var returned = _serializer.Deserialize<TestModel>(
                response.ResultAsJson);
            Assert.Equal(expected.Test, returned.Test);
        }

        [Fact]
        public void TestTest1InvocationChunked()
        {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Console<ChunkMethodClient>());

            var buffer = new byte[300809];
            FillRandom(buffer);
            var expected = new TestModel { Test = buffer };
            var response = client.CallMethodAsync("test", "test", "Test1_V1",
                _serializer.SerializeToMemory((object)expected).ToArray(),
                    null, null, default).Result;

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
        public void TestTest2InvocationNonChunked(int size)
        {
            var router = GetRouter();
            var expected = new byte[size];
            FillRandom(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_V1", _serializer.SerializeToMemory((object)expected).ToArray())).Result;
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
        public void TestTest8InvocationV1NonChunked(int size)
        {
            var router = GetRouter();
            var expected = new byte[size];
            FillRandom(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test8_V1", _serializer.SerializeToMemory((object)expected).ToArray())).Result;
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
        public void TestTest8InvocationV2NonChunked(int size)
        {
            var router = GetRouter();
            var expected = new byte[size];
            FillRandom(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test8_V2", _serializer.SerializeToMemory((object)expected).ToArray())).Result;
            var returned = _serializer.Deserialize<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest2InvocationNonChunkedFailsWithLargeBuffer()
        {
            var router = GetRouter();
            var expected = new byte[96 * 1024];
            FillRandom(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_V1", _serializer.SerializeToMemory((object)expected).ToArray())).Result;
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
        public void TestTest2InvocationChunked(int size)
        {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Console<ChunkMethodClient>());
            var expected = new byte[size];
            FillRandom(expected);
            var response = client.CallMethodAsync("test", "test", "Test2_V1",
                _serializer.SerializeToMemory((object)expected).ToArray(),
                    null, null, default).Result;

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
        public void TestTest8InvocationV1Chunked(int size)
        {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Console<ChunkMethodClient>());
            var expected = new byte[size];
            FillRandom(expected);
            var response = client.CallMethodAsync("test", "test", "Test8_V1",
                _serializer.SerializeToMemory((object)expected).ToArray(),
                    null, null, default).Result;

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
        public void TestTest8InvocationV2Chunked(int size)
        {
            var router = GetRouter();
            var client = new ChunkMethodClient(new TestMethodClient(router),
                _serializer, Log.Console<ChunkMethodClient>());
            var expected = new byte[size];
            FillRandom(expected);
            var response = client.CallMethodAsync("test", "test", "Test8_V2",
                _serializer.SerializeToMemory((object)expected).ToArray(),
                    null, null, default).Result;

            var returned = _serializer.Deserialize<byte[]>(
                Encoding.UTF8.GetString(response));
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest3InvocationNonChunked()
        {
            var router = GetRouter();
            var expected = new byte[1049];
            FillRandom(expected);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test3_V1",
                    _serializer.SerializeToMemory((object)(new
                    {
                        request = expected,
                        Value = 3254
                    })).ToArray())).Result;

            var returned = _serializer.Deserialize<byte[]>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        [Fact]
        public void TestTest2InvocationV2NonChunked()
        {
            var router = GetRouter();
            var buffer = new byte[1049];
            FillRandom(buffer);
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test2_v2",
                    _serializer.SerializeToMemory((object)buffer).ToArray())).Result;

            Assert.Equal(400, response.Status);
            var deserializedResponse = _serializer.Deserialize<MethodCallStatusExceptionModel>(
                response.ResultAsJson);
            var ex = deserializedResponse.Details.ConvertTo<ArgumentNullException>();
            Assert.Equal("request", ex.ParamName);
        }

        [Fact]
        public void TestTest3InvocationV2NonChunked()
        {
            var router = GetRouter();
            var buffer = new byte[1049];
            FillRandom(buffer);
            const int expected = 3254;
            var response = router.InvokeMethodAsync(new MethodRequest(
                "Test3_v2",
                    _serializer.SerializeToMemory((object)(new
                    {
                        request = buffer,
                        Value = expected
                    })).ToArray())).Result;

            var returned = _serializer.Deserialize<int>(
                response.ResultAsJson);
            Assert.Equal(expected, returned);
        }

        private static void FillRandom(byte[] expected)
        {
#pragma warning disable CA5394 // Do not use insecure randomness
            Random.Shared.NextBytes(expected);
#pragma warning restore CA5394 // Do not use insecure randomness
        }

        private MethodRouter GetRouter()
        {
            return new MethodRouter(_serializer, Log.Console<ChunkMethodClient>())
            {
                Controllers = GetControllers()
            };
        }

        private static List<IMethodController> GetControllers()
        {
            return new List<IMethodController> {
                new TestControllerV1(),
                new TestControllerV2(),
                new TestControllerV1And2()
            };
        }

        /// <summary>
        /// Method call exception model.
        /// </summary>
        [DataContract]
        public class MethodCallStatusExceptionModel
        {
            /// <summary>
            /// Exception message.
            /// </summary>
            [DataMember(Name = "Message", Order = 0,
                EmitDefaultValue = true)]
            public string Message { get; set; }

            /// <summary>
            /// Details of the exception.
            /// </summary>
            [DataMember(Name = "Details", Order = 1,
                EmitDefaultValue = true)]
            public VariantValue Details { get; set; }
        }

        public class TestMethodClient : IJsonMethodClient
        {
            public int MaxMethodPayloadSizeInBytes => 120 * 1024;

            public TestMethodClient(MethodRouter router)
            {
                _router = router;
            }

            public async Task<string> CallMethodAsync(string deviceId,
                string moduleId, string method, string payload,
                TimeSpan? timeout, CancellationToken ct)
            {
                var result = await _router.InvokeMethodAsync(
                    new MethodRequest(method, Encoding.UTF8.GetBytes(payload),
                        timeout, timeout)).ConfigureAwait(false);
                if (result.Status != 200)
                {
                    throw new MethodCallStatusException(result.ResultAsJson,
                        result.Status);
                }
                return result.ResultAsJson;
            }

            private readonly MethodRouter _router;
        }

        public class TestModel
        {
            public byte[] Test { get; set; }
        }

        [Version("_V1")]
        public class TestControllerV1 : IMethodController
        {
            public static Task<TestModel> Test1Async(TestModel request)
            {
                return Task.FromResult(request);
            }
            public static Task<byte[]> Test2Async(byte[] request)
            {
                return Task.FromResult(request);
            }
            public static Task<byte[]> Test3Async(byte[] request, int value)
            {
                if (value == 0)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                return Task.FromResult(request);
            }
            public Task<string> TestNoParametersAsync()
            {
                return Task.FromResult(nameof(TestNoParametersAsync));
            }
            public Task TestNoReturnAsync(string input)
            {
                if (input != nameof(TestNoReturnAsync))
                {
                    throw new ArgumentNullException(nameof(input));
                }
                return Task.CompletedTask;
            }
            public Task TestNoParametersAndNoReturnAsync()
            {
                _noparamcalled = true;
                return Task.CompletedTask;
            }
            public bool _noparamcalled;
        }

        [Version("_V2")]
        public class TestControllerV2 : IMethodController
        {
            public static Task<byte[]> Test2Async(byte[] request)
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }
                return Task.FromException<byte[]>(
                    new ArgumentNullException(nameof(request)));
            }

            public static Task<int> Test3Async(byte[] request, int value)
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }
                return Task.FromResult(value);
            }
        }

        [Version("_V1")]
        [Version("_V2")]
        public class TestControllerV1And2 : IMethodController
        {
            public static Task<byte[]> Test8Async(byte[] request)
            {
                return Task.FromResult(request);
            }
        }
    }
}
