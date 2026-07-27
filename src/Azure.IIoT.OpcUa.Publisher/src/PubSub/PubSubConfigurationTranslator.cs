// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.PubSub
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.PubSub.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class PubSubConfigurationTranslator
    {
        /// <summary>
        /// Whether the translated configuration is activated. The shadow host
        /// is inert by default so it can be hosted without publishing; the
        /// production composition enables it once an egress transport exists.
        /// </summary>
        public bool Activate { get; set; }

        public PubSubConfigurationTranslator(IOptions<PublisherOptions>? options = null)
        {
            _defaultPublishingInterval = NormalizePublishingInterval(
                options?.Value.BatchTriggerInterval);
        }

        public PubSubConfigurationDataType Translate(
            IEnumerable<WriterGroupModel> writerGroups,
            IPubSubIdentityTransaction identities)
        {
            return TranslateWithEncodingRegistry(writerGroups, identities).Configuration;
        }

        internal PubSubShadowConfigurationTranslation TranslateWithEncodingRegistry(
            IEnumerable<WriterGroupModel> writerGroups,
            IPubSubIdentityTransaction identities)
        {
            ArgumentNullException.ThrowIfNull(writerGroups);
            ArgumentNullException.ThrowIfNull(identities);

            var connections = new List<PubSubConnectionDataType>();
            var dataSets = new Dictionary<string, PublishedDataSetDataType>(
                StringComparer.Ordinal);
            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            var encodings = new PubSubShadowEncodingRegistrySnapshot();
            foreach (var writerGroup in writerGroups)
            {
                ArgumentNullException.ThrowIfNull(writerGroup);
                if (!groupIds.Add(writerGroup.Id))
                {
                    throw new ArgumentException(
                        $"Writer group '{writerGroup.Id}' occurs more than once.",
                        nameof(writerGroups));
                }

                var encoding = GetShadowEncoding(writerGroup.MessageType);
                var connection = TranslateWriterGroup(writerGroup, dataSets, identities, encoding);
                connections.Add(connection);
                encodings.Add(connection.Name ?? string.Empty,
                    connection.WriterGroups[0].WriterGroupId, encoding);
            }

            return new PubSubShadowConfigurationTranslation(
                new PubSubConfigurationDataType
                {
                    Enabled = true,
                    Connections = new ArrayOf<PubSubConnectionDataType>(connections.ToArray()),
                    PublishedDataSets = new ArrayOf<PublishedDataSetDataType>(
                        dataSets.Values.ToArray())
                },
                encodings);
        }

        public static PubSubConfigurationDataType CreateEmpty()
        {
            return new PubSubConfigurationDataType
            {
                Enabled = true,
                Connections = [],
                PublishedDataSets = []
            };
        }

        private PubSubConnectionDataType TranslateWriterGroup(
            WriterGroupModel source,
            Dictionary<string, PublishedDataSetDataType> dataSets,
            IPubSubIdentityTransaction identities,
            PubSubShadowEncoding encoding)
        {
            if (string.IsNullOrWhiteSpace(source.Id))
            {
                throw new ArgumentException("A writer group identifier is required.", nameof(source));
            }

            var isUadp = encoding == PubSubShadowEncoding.Uadp;
            var writerGroup = new WriterGroupDataType
            {
                Name = source.Name ?? source.Id,
                WriterGroupId = identities.GetOrAllocate("writer-group", source.Id),
                Enabled = Activate,
                PublishingInterval = NormalizePublishingInterval(source.PublishingInterval
                    ?? _defaultPublishingInterval).TotalMilliseconds,
                KeepAliveTime = source.KeepAliveTime?.TotalMilliseconds ?? 0,
                MaxNetworkMessageSize = source.MaxNetworkMessageSize ?? 1500,
                SecurityMode = MessageSecurityMode.None,
                SecurityGroupId = string.Empty,
                MessageSettings = new ExtensionObject(isUadp
                    ? CreateUadpWriterGroupSettings(source)
                    : CreateJsonWriterGroupSettings(source)),
                DataSetWriters = TranslateWriters(source, dataSets, identities, isUadp, Activate)
            };

            return new PubSubConnectionDataType
            {
                Name = "shadow-" + source.Id,
                Enabled = Activate,
                PublisherId = new Variant(source.PublisherId ?? source.Id),
                TransportProfileUri = GetTransportProfile(encoding),
                Address = new ExtensionObject(new NetworkAddressUrlDataType
                {
                    NetworkInterface = string.Empty,
                    Url = isUadp
                        ? "opc.udp://shadow.invalid:4840"
                        : "mqtt://shadow.invalid"
                }),
                WriterGroups = [writerGroup],
                ReaderGroups = []
            };
        }

        private static ArrayOf<DataSetWriterDataType> TranslateWriters(
            WriterGroupModel group,
            Dictionary<string, PublishedDataSetDataType> dataSets,
            IPubSubIdentityTransaction identities, bool isUadp, bool activate)
        {
            if (group.DataSetWriters is null)
            {
                return [];
            }

            var writers = new List<DataSetWriterDataType>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in group.DataSetWriters)
            {
                ArgumentNullException.ThrowIfNull(source);
                if (string.IsNullOrWhiteSpace(source.Id))
                {
                    throw new ArgumentException(
                        $"Writer group '{group.Id}' contains a writer without an identifier.",
                        nameof(group));
                }
                if (!ids.Add(source.Id))
                {
                    throw new ArgumentException(
                        $"Writer group '{group.Id}' contains duplicate writer '{source.Id}'.",
                        nameof(group));
                }

                var dataSetName = GetDataSetName(group, source);
                dataSets.TryAdd(dataSetName, TranslateDataSet(dataSetName, source.DataSet));
                writers.Add(new DataSetWriterDataType
                {
                    Name = source.DataSetWriterName ?? source.Id,
                    DataSetWriterId = identities.GetOrAllocate("data-set-writer", source.Id),
                    Enabled = activate,
                    DataSetName = dataSetName,
                    KeyFrameCount = source.KeyFrameCount ?? 1,
                    DataSetFieldContentMask = (uint)source.DataSetFieldContentMask.ToStackType(),
                    MessageSettings = new ExtensionObject(isUadp
                        ? CreateUadpDataSetWriterSettings(source)
                        : CreateJsonDataSetWriterSettings(source))
                });
            }
            return new ArrayOf<DataSetWriterDataType>(writers.ToArray());
        }

        private static PublishedDataSetDataType TranslateDataSet(string name,
            PublishedDataSetModel? source)
        {
            var metadata = source?.DataSetMetaData;
            return new PublishedDataSetDataType
            {
                Name = name,
                DataSetMetaData = new DataSetMetaDataType
                {
                    Name = metadata?.Name ?? name,
                    DataSetClassId = new Uuid(metadata?.DataSetClassId ?? Guid.Empty),
                    Fields = [],
                    ConfigurationVersion = new ConfigurationVersionDataType
                    {
                        MajorVersion = metadata?.MajorVersion ?? 1,
                        MinorVersion = 0
                    }
                }
            };
        }

        private static string GetDataSetName(WriterGroupModel group,
            DataSetWriterModel writer)
        {
            return writer.DataSet?.Name
                ?? writer.DataSet?.DataSetMetaData?.Name
                ?? $"{group.Id}:{writer.Id}";
        }

        private static UadpWriterGroupMessageDataType CreateUadpWriterGroupSettings(
            WriterGroupModel source)
        {
            return new UadpWriterGroupMessageDataType
            {
                GroupVersion = source.MessageSettings?.GroupVersion ?? 0,
                NetworkMessageContentMask = (source.MessageSettings is null
                    ? null
                    : source.MessageSettings.NetworkMessageContentMask)
                    .ToStackType(MessageEncoding.Uadp)
            };
        }

        private static JsonWriterGroupMessageDataType CreateJsonWriterGroupSettings(
            WriterGroupModel source)
        {
            return new JsonWriterGroupMessageDataType
            {
                NetworkMessageContentMask = (source.MessageSettings is null
                    ? null
                    : source.MessageSettings.NetworkMessageContentMask)
                    .ToStackType(MessageEncoding.Json)
            };
        }

        private static UadpDataSetWriterMessageDataType CreateUadpDataSetWriterSettings(
            DataSetWriterModel source)
        {
            return new UadpDataSetWriterMessageDataType
            {
                DataSetMessageContentMask = (source.MessageSettings is null
                    ? null
                    : source.MessageSettings.DataSetMessageContentMask)
                    .ToStackType(source.DataSetFieldContentMask, MessageEncoding.Uadp)
            };
        }

        private static JsonDataSetWriterMessageDataType CreateJsonDataSetWriterSettings(
            DataSetWriterModel source)
        {
            return new JsonDataSetWriterMessageDataType
            {
                DataSetMessageContentMask = (source.MessageSettings is null
                    ? null
                    : source.MessageSettings.DataSetMessageContentMask)
                    .ToStackType(source.DataSetFieldContentMask, MessageEncoding.Json)
            };
        }

        internal static PubSubShadowEncoding GetShadowEncoding(MessageEncoding? encoding)
        {
            return encoding switch
            {
                null or MessageEncoding.Json => PubSubShadowEncoding.Json,
                MessageEncoding.JsonReversible => PubSubShadowEncoding.JsonReversible,
                MessageEncoding.JsonGzip => PubSubShadowEncoding.JsonGzip,
                MessageEncoding.JsonReversibleGzip => PubSubShadowEncoding.JsonReversibleGzip,
                MessageEncoding.Uadp => PubSubShadowEncoding.Uadp,
                _ => throw new ArgumentException(
                    $"Message encoding '{encoding}' is not supported by the inert PubSub host.",
                    nameof(encoding))
            };
        }

        internal static string GetTransportProfile(MessageEncoding? encoding)
        {
            return GetTransportProfile(GetShadowEncoding(encoding));
        }

        private static string GetTransportProfile(PubSubShadowEncoding encoding)
        {
            return encoding switch
            {
                PubSubShadowEncoding.Json or PubSubShadowEncoding.JsonReversible
                    or PubSubShadowEncoding.JsonGzip or PubSubShadowEncoding.JsonReversibleGzip
                    => Profiles.PubSubMqttJsonTransport,
                PubSubShadowEncoding.Uadp => Profiles.PubSubUdpUadpTransport,
                _ => throw new ArgumentException(
                    $"Message encoding '{encoding}' is not supported by the inert PubSub host.",
                    nameof(encoding))
            };
        }

        private static TimeSpan NormalizePublishingInterval(TimeSpan? interval)
        {
            if (interval is { } value && value > TimeSpan.Zero)
            {
                return value;
            }
            return TimeSpan.FromMilliseconds(PublisherConfig.BatchTriggerIntervalLLegacyDefaultMillis);
        }

        private readonly TimeSpan _defaultPublishingInterval;
    }
}
