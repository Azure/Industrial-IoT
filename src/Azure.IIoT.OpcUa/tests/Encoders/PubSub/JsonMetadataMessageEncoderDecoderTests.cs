// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using System.Buffers;
    using System.Collections.Generic;
    using Xunit;

    public sealed class JsonMetadataMessageEncoderDecoderTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void EncodeDecodeMetadataMessage(bool standardsCompliant)
        {
            var context = new ServiceMessageContext();
            context.NamespaceUris.GetIndexOrAppend(
                "http://opcfoundation.org/SimpleEvents");
            var metadata = new PublishedDataSetMetaDataModel
            {
                DataSetMetaData = new DataSetMetaDataModel
                {
                    Name = "Events"
                },
                Fields =
                [
                    new PublishedFieldMetaDataModel
                    {
                        Name = "EventId",
                        BuiltInType = (byte)BuiltInType.ByteString,
                        DataType = "i=15",
                        ValueRank = ValueRanks.Scalar
                    },
                    new PublishedFieldMetaDataModel
                    {
                        Name = "CurrentStep",
                        BuiltInType = (byte)BuiltInType.ExtensionObject,
                        DataType = "nsu=http://opcfoundation.org/SimpleEvents;i=183",
                        ValueRank = ValueRanks.Scalar
                    }
                ]
            };
            Assert.True(PubSubMessage.TryCreateMetaDataMessage(
                MessageEncoding.Json,
                "publisher",
                "writerGroup",
                "writer",
                1,
                metadata,
                standardsCompliant ? NamespaceFormat.Expanded : NamespaceFormat.Uri,
                standardsCompliant,
                out var message));
            var buffer = Assert.Single(message.Encode(context, 64 * 1024));

            var reader = new Queue<ReadOnlySequence<byte>>();
            reader.Enqueue(buffer);
            var decoded = new JsonMetaDataMessage();
            Assert.True(decoded.TryDecode(context, reader, null));

            Assert.NotNull(decoded.MetaData);
            Assert.Equal("Events", decoded.MetaData.DataSetMetaData.Name);
            Assert.Collection(decoded.MetaData.Fields,
                field =>
                {
                    Assert.Equal((byte)BuiltInType.ByteString, field.BuiltInType);
                    Assert.Equal("i=15", field.DataType);
                },
                field =>
                {
                    Assert.Equal((byte)BuiltInType.ExtensionObject,
                        field.BuiltInType);
                    Assert.Equal(
                        "nsu=http://opcfoundation.org/SimpleEvents;i=183",
                        field.DataType);
                });
        }
    }
}
