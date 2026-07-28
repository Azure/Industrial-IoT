// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Writer group extensions
    /// </summary>
    internal static class WriterGroupModelEx
    {
        /// <summary>
        /// Clones a writer group, drops the writers that have nothing to
        /// publish, and resolves the messaging profile onto the copy.
        /// </summary>
        /// <remarks>
        /// This is the single place a writer group is prepared for use. Every
        /// consumer must be handed a model that has been through it, because an
        /// unresolved model carries all three content masks as
        /// <see langword="null"/> and the stack type conversions then substitute
        /// one fixed default set for every messaging mode, which silently
        /// collapses the modes onto the same wire format.
        /// </remarks>
        /// <param name="model">Writer group to copy.</param>
        /// <param name="options">Publisher options supplying the defaults.</param>
        /// <returns>The resolved copy.</returns>
        public static WriterGroupModel CopyAndResolve(this WriterGroupModel model,
            PublisherOptions options)
        {
            ArgumentNullException.ThrowIfNull(model);
            return (model with
            {
                DataSetWriters = model.DataSetWriters == null ?
                    Array.Empty<DataSetWriterModel>() :
                    model.DataSetWriters
                        .Where(writer => writer.HasDataToPublish())
                        .Select(writer => writer.Clone())
                        .ToList(),
                LocaleIds = model.LocaleIds?.ToList(),
                MessageSettings = model.MessageSettings == null ? null :
                    model.MessageSettings with { },
                SecurityKeyServices = model.SecurityKeyServices?
                    .Select(service => service.Clone())
                    .ToList()
            }).ResolveMessagingProfile(options);
        }

        /// <summary>
        /// Applies the messaging profile to a writer group, filling in the
        /// network message, data set message and data set field content masks
        /// that a configuration does not state explicitly.
        /// </summary>
        /// <param name="writerGroup">Writer group to resolve in place.</param>
        /// <param name="options">Publisher options supplying the defaults.</param>
        /// <returns>The same writer group, for chaining.</returns>
        public static WriterGroupModel ResolveMessagingProfile(
            this WriterGroupModel writerGroup, PublisherOptions options)
        {
            ArgumentNullException.ThrowIfNull(writerGroup);
            ArgumentNullException.ThrowIfNull(options);

            var defaultMessagingProfile = options.MessagingProfile ??
                MessagingProfile.Get(MessagingMode.PubSub, MessageEncoding.Json);
            if (writerGroup.HeaderLayoutUri != null)
            {
                defaultMessagingProfile = MessagingProfile.Get(
                    Enum.Parse<MessagingMode>(writerGroup.HeaderLayoutUri),
                    writerGroup.MessageType ?? defaultMessagingProfile.MessageEncoding);
            }

            writerGroup.MessageType ??= defaultMessagingProfile.MessageEncoding;

            if (writerGroup.MessageSettings?.NetworkMessageContentMask == null)
            {
                writerGroup.MessageSettings ??= new WriterGroupMessageSettingsModel();
                writerGroup.MessageSettings.NetworkMessageContentMask =
                    defaultMessagingProfile.NetworkMessageContentMask;
            }

            foreach (var dataSetWriter in writerGroup.DataSetWriters ?? [])
            {
                if (dataSetWriter.MessageSettings?.DataSetMessageContentMask == null)
                {
                    dataSetWriter.MessageSettings ??= new DataSetWriterMessageSettingsModel();
                    dataSetWriter.MessageSettings.DataSetMessageContentMask =
                        defaultMessagingProfile.DataSetMessageContentMask;
                }
                dataSetWriter.DataSetFieldContentMask ??=
                    defaultMessagingProfile.DataSetFieldContentMask;

                if (options.WriteValueWhenDataSetHasSingleEntry == true)
                {
                    dataSetWriter.DataSetFieldContentMask
                        |= DataSetFieldContentFlags.SingleFieldDegradeToValue;
                }
            }
            return writerGroup;
        }
    }
}
