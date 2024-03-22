// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    /// <summary>
    /// Published dataset extensions
    /// </summary>
    public static class PublishedDataSetModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetModel? Clone(this PublishedDataSetModel? model)
        {
            return model == null ? null : (model with
            {
                DataSetSource = model.DataSetSource.Clone(),
                ExtensionFields = model.ExtensionFields?
                    .Select(e => e with { })
                    .ToList()
            });
        }

        /// <summary>
        /// Get minor version of metadata for this data set
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static uint GetMetaDataMinorVersion(this PublishedDataSetModel dataSet)
        {
            return dataSet.EnumerateMetaData().Max(m => m.MetaData?.MinorVersion ?? 0u);
        }

        /// <summary>
        /// Enumerate metadata of all fields in the data set
        /// </summary>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static IEnumerable<
            (string? FieldName, PublishedMetaDataModel? MetaData)> EnumerateMetaData(
            this PublishedDataSetModel dataSet)
        {
            if (dataSet.DataSetSource == null)
            {
                yield break;
            }
            if (dataSet.DataSetSource.PublishedVariables?.PublishedData != null)
            {
                foreach (var item in dataSet.DataSetSource.PublishedVariables.PublishedData)
                {
                    yield return (item.DataSetFieldName, item.MetaData);
                }
            }
            if (dataSet.DataSetSource.PublishedEvents?.PublishedData != null)
            {
                foreach (var evt in dataSet.DataSetSource.PublishedEvents.PublishedData)
                {
                    if (evt.SelectedFields != null)
                    {
                        foreach (var item in evt.SelectedFields)
                        {
                            yield return (item.DataSetFieldName, item.MetaData);
                        }
                    }
                }
            }
           // if (dataSet.DataSetSource.PublishedObjects?.PublishedData != null)
           // {
           //     foreach (var obj in dataSet.DataSetSource.PublishedObjects.PublishedData)
           //     {
           //         if (obj.PublishedVariables != null)
           //         {
           //             foreach (var item in obj.PublishedVariables.PublishedData)
           //             {
           //                 yield return (item.DataSetFieldName, item.MetaData);
           //             }
           //         }
           //     }
           // }
            if (dataSet.ExtensionFields != null)
            {
                foreach (var item in dataSet.ExtensionFields)
                {
                    yield return (item.DataSetFieldName, item.MetaData);
                }
            }
        }
    }
}
