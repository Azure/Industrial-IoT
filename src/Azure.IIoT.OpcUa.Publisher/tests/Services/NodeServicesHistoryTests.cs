// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Tests.Services
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Parser;
    using Azure.IIoT.OpcUa.Publisher.Services;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Newtonsoft;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using Opc.Ua;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public sealed class NodeServicesHistoryTests
    {
        [Fact]
        public async Task HistoryReadNextPassesOriginalParametersAsync()
        {
            var details = new ExtensionObject(new ReadRawModifiedDetails());
            var encodedDetails = new NewtonsoftJsonSerializer().Parse("{}");
            var codec = new Mock<IVariantEncoder>();
            codec.Setup(c => c.Decode(encodedDetails, BuiltInType.ExtensionObject))
                .Returns(new Variant(details));

            ExtensionObject passedDetails = null;
            var passedTimestamps = Opc.Ua.TimestampsToReturn.Both;
            var services = new Mock<ISessionServices>();
            services.Setup(s => s.HistoryReadAsync(
                    It.IsAny<RequestHeader>(), It.IsAny<ExtensionObject>(),
                    It.IsAny<Opc.Ua.TimestampsToReturn>(), false,
                    It.IsAny<HistoryReadValueIdCollection>(), It.IsAny<CancellationToken>()))
                .Callback<RequestHeader, ExtensionObject, Opc.Ua.TimestampsToReturn, bool,
                    HistoryReadValueIdCollection, CancellationToken>(
                    (_, value, timestamps, _, _, _) =>
                    {
                        passedDetails = value;
                        passedTimestamps = timestamps;
                    })
                .ReturnsAsync(new HistoryReadResponse
                {
                    ResponseHeader = new ResponseHeader(),
                    Results = new HistoryReadResultCollection
                    {
                        new HistoryReadResult
                        {
                            StatusCode = StatusCodes.Good,
                            HistoryData = new ExtensionObject(new HistoryData())
                        }
                    },
                    DiagnosticInfos = []
                });

            var session = new Mock<IOpcUaSession>();
            session.SetupGet(s => s.Codec).Returns(codec.Object);
            session.SetupGet(s => s.Services).Returns(services.Object);
            var client = new Mock<IOpcUaClientManager<string>>();
            client.Setup(c => c.ExecuteAsync(It.IsAny<string>(),
                    It.IsAny<Func<ServiceCallContext,
                        Task<HistoryReadNextResponseModel<object>>>>(),
                    It.IsAny<RequestHeaderModel>(), It.IsAny<CancellationToken>()))
                .Returns(async (string _, Func<ServiceCallContext,
                        Task<HistoryReadNextResponseModel<object>>> operation,
                    RequestHeaderModel _, CancellationToken _) =>
                {
                    using var context = new ServiceCallContext(session.Object,
                        TimeSpan.FromSeconds(10));
                    return await operation(context);
                });

            using var sut = new NodeServices<string>(client.Object,
                Mock.Of<IFilterParser>(), NullLogger<NodeServices<string>>.Instance,
                Options.Create(new PublisherOptions()));
            await sut.HistoryReadNextAsync("endpoint", new HistoryReadNextRequestModel
            {
                ContinuationToken = Convert.ToBase64String([1, 2, 3]),
                Details = encodedDetails,
                TimestampsToReturn = Models.TimestampsToReturn.Source
            }, (_, _) => new object(), default);

            Assert.Same(details, passedDetails);
            Assert.Equal(Opc.Ua.TimestampsToReturn.Source, passedTimestamps);
        }
    }
}
