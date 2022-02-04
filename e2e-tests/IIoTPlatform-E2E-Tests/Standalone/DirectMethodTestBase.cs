// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.Standalone {

    using IIoTPlatform_E2E_Tests.TestExtensions;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;

    /// <summary>
    /// Base class for test suites that validate functionality of direct method calls.
    /// </summary>
    public class DirectMethodTestBase : IDisposable {

        protected readonly ITestOutputHelper _output;
        protected readonly IIoTMultipleNodesTestContext _context;
        protected readonly ServiceClient _iotHubClient;
        protected readonly IJsonSerializer _serializer;
        protected string _iotHubPublisherDeviceName;
        protected string _iotHubPublisherModuleName;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DirectMethodTestBase(
            ITestOutputHelper output,
            IIoTMultipleNodesTestContext context
        ) {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.OutputHelper = _output;
            _iotHubPublisherDeviceName = _context.DeviceConfig.DeviceId;
            _serializer = new NewtonSoftJsonSerializer();

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
        ) {
            return await TestHelper.CallMethodAsync(
                _iotHubClient,
                _iotHubPublisherDeviceName,
                _iotHubPublisherModuleName,
                parameters,
                _context,
                ct
            ).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Dispose() {
            _iotHubClient?.Dispose();
        }
    }
}
