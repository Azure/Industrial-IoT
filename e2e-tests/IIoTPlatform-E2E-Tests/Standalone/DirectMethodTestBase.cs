// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatformE2ETests.Standalone
{
    using IIoTPlatformE2ETests.TestExtensions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Azure.Devices;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;

    /// <summary>
    /// Base class for test suites that validate functionality of direct method calls.
    /// </summary>
    public class DirectMethodTestBase : IDisposable
    {
        protected readonly IIoTMultipleNodesTestContext _context;
        protected readonly ServiceClient _iotHubClient;
        protected readonly IJsonSerializer _serializer;
        protected string _iotHubPublisherDeviceName;
        protected string _iotHubPublisherModuleName;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="context"></param>
        public DirectMethodTestBase(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        )
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.SetOutputHelper(output);
            _iotHubPublisherDeviceName = _context.DeviceConfig.DeviceId;
            _serializer = new NewtonsoftJsonSerializer();

            // Initialize DeviceServiceClient from IoT Hub connection string.
            _iotHubClient = TestHelper.DeviceServiceClient(
                _context.IoTHubConfig.IoTHubConnectionString,
                TransportType.Amqp_WebSocket_Only
            );
        }

        /// <summary>
        /// Perform direct method call.
        /// </summary>
        /// <param name="parameters"> Direct method parameters. </param>
        /// <param name="ct"> Cancellation token. </param>
        /// <returns></returns>
        public async Task<MethodResultModel> CallMethodAsync(
            MethodParameterModel parameters,
            CancellationToken ct
        )
        {
            return await TestHelper.CallMethodAsync(
                _iotHubClient,
                _iotHubPublisherDeviceName,
                _iotHubPublisherModuleName,
                parameters,
                _context,
                ct
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Restart module with the given name.
        /// </summary>
        /// <param name="moduleName"> Module name. </param>
        /// <param name="ct"> Cancellation token. </param>
        public async Task<MethodResultModel> RestartModuleAsync(
            string moduleName,
            CancellationToken ct)
        {
            var payload = new Dictionary<string, string> {
                {"schemaVersion", "1.0" },
                {"id", moduleName }
            };

            var parameters = new MethodParameterModel
            {
                Name = "RestartModule",
                JsonPayload = _serializer.SerializeToString(payload)
            };

            return await TestHelper.CallMethodAsync(
                _iotHubClient,
                _iotHubPublisherDeviceName,
                "$edgeAgent",
                parameters,
                _context,
                ct
            ).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _iotHubClient?.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
