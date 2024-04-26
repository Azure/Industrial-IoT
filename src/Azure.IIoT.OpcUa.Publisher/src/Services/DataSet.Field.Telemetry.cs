// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    internal partial class DataSetResolver
    {
        internal abstract partial class Field
        {
            /// <summary>
            /// Telemetry variable item
            /// </summary>
            public sealed class Telemetry : Variable
            {
                public Telemetry(DataSetResolver resolver,
                    DataSetWriterModel writer, PublishedDataSetVariableModel variable)
                    : base(resolver, writer, variable)
                {
                }

                /// <inheritdoc/>
                public static IEnumerable<DataSetWriterModel> Split(DataSetWriterModel writer,
                    IEnumerable<Field> items, int maxItemsPerWriter)
                {
                    foreach (var variables in items
                        .OfType<Telemetry>()
                        .Batch(maxItemsPerWriter))
                    {
                        var copy = Copy(writer);
                        Debug.Assert(copy.DataSet?.DataSetSource != null);
                        copy.DataSet.DataSetSource.PublishedVariables = new PublishedDataItemsModel
                        {
                            PublishedData = variables
                                .Select((f, i) => f._variable with { FieldIndex = i })
                                .ToList()
                        };
                        var offset = copy.DataSet.DataSetSource.PublishedVariables.PublishedData.Count;
                        copy.DataSet.ExtensionFields = copy.DataSet.ExtensionFields?
                            // No need to clone more members of the field
                            .Select((f, i) => f with { FieldIndex = i + offset })
                            .ToList();
                        yield return copy;
                    }
                }
            }
        }
    }
}
