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
            _defaultPublishingInterval = ResolvePublishingInterval(
                options?.Value.BatchTriggerInterval);
            _publisherId = options?.Value.PublisherId;
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
                    connection.WriterGroups[0].WriterGroupId, encoding,
                    CreateMessageProfile(writerGroup, connection.WriterGroups[0]));
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

        /// <summary>
        /// Collects the header members and content masks the writer group's
        /// messages must carry, so the Publisher can stamp them on before
        /// encoding. The native writer group builds its messages without them.
        /// </summary>
        /// <param name="source">Public writer group model.</param>
        /// <param name="translated">Translated native writer group.</param>
        private static PubSubShadowMessageProfile CreateMessageProfile(
            WriterGroupModel source, WriterGroupDataType translated)
        {
            var writers = new Dictionary<ushort, PubSubShadowWriterProfile>();
            foreach (var writer in translated.DataSetWriters)
            {
                //
                // The translated settings are JSON or UADP depending on the
                // group's encoding, and the two masks are different
                // enumerations that share the same underlying storage. The
                // translator already computed the right one, so the raw value is
                // carried and the encoder for that encoding interprets it.
                //
                writers[writer.DataSetWriterId] = new PubSubShadowWriterProfile
                {
                    DataSetMessageContentMask = writer.MessageSettings
                        .TryGetValue(out JsonDataSetWriterMessageDataType? json) && json is not null
                            ? json.DataSetMessageContentMask
                            : writer.MessageSettings
                                .TryGetValue(out UadpDataSetWriterMessageDataType? uadp)
                                && uadp is not null ? uadp.DataSetMessageContentMask : 0,
                    DataSetWriterName = writer.Name ?? string.Empty
                };
            }
            var networkMessageContentMask = translated.MessageSettings
                .TryGetValue(out JsonWriterGroupMessageDataType? group) && group is not null
                    ? group.NetworkMessageContentMask
                        | ((source.MessageSettings?.NetworkMessageContentMask
                            & NetworkMessageContentFlags.WriterGroupId) != 0
                            ? (uint)JsonNetworkMessageContentMask.WriterGroupName : 0)
                    : translated.MessageSettings
                        .TryGetValue(out UadpWriterGroupMessageDataType? uadpGroup)
                        && uadpGroup is not null ? uadpGroup.NetworkMessageContentMask : 0;
            return new PubSubShadowMessageProfile
            {
                //
                // The shared JSON mapping does not translate WriterGroupId,
                // because the writer path emits the group under its own member
                // name rather than through the stack's mask. The native encoder
                // needs the WriterGroupName bit to emit it at all, so it is
                // added above rather than in the shared mapping, which the
                // writer path also uses.
                //
                NetworkMessageContentMask = networkMessageContentMask,
                WriterGroupName = source.Name ?? Constants.DefaultWriterGroupName,
                DataSetClassId = new Uuid(source.DataSetWriters?
                    .Select(writer => writer.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty)
                    .FirstOrDefault(id => id != Guid.Empty) ?? Guid.Empty),
                Writers = writers
            };
        }

        public static PubSubConfigurationDataType CreateEmpty()        {
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
                PublishingInterval = (source.PublishingInterval is { } configured
                    ? ResolvePublishingInterval(configured)
                    : _defaultPublishingInterval).TotalMilliseconds,
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
                //
                // The publisher identity carried on the wire must match the one
                // the writer path publishes, which falls back to the configured
                // publisher id and only then to a well-known placeholder. Falling
                // back to the writer group id would emit its hash instead.
                //
                PublisherId = new Variant(source.PublisherId ?? _publisherId
                    ?? Constants.DefaultPublisherId),
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

        private static TimeSpan ResolvePublishingInterval(TimeSpan? interval)
        {
            if (interval is not { } value)
            {
                //
                // Not configured at all, so keep the historical batching cadence.
                //
                return TimeSpan.FromMilliseconds(
                    PublisherConfig.BatchTriggerIntervalLLegacyDefaultMillis);
            }
            //
            // Configured as zero means publish as soon as data is available.
            // Publisher sets it to zero whenever a transport is configured, so
            // substituting the legacy default here would silently batch every
            // message for ten seconds. The native runtime rejects an interval of
            // zero outright, so immediate publishing is expressed as the
            // smallest practical positive interval instead.
            //
            return value > TimeSpan.Zero ? value : ImmediatePublishingInterval;
        }

        /// <summary>
        /// Interval used to express immediate publishing. The native runtime
        /// publishes on a timer and rejects an interval of zero, so immediate
        /// becomes a short interval. It is not made shorter than this because
        /// the timer fires whether or not data is pending, and a very small
        /// value floods the broker with empty network messages.
        /// </summary>
        internal static readonly TimeSpan ImmediatePublishingInterval =
            TimeSpan.FromMilliseconds(100);

        private readonly TimeSpan _defaultPublishingInterval;
        private readonly string? _publisherId;
    }
}
