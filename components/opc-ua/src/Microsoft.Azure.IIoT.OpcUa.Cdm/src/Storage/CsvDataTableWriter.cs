// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Cdm.Storage {
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Models;
    using Microsoft.Azure.IIoT.Storage;
    using Microsoft.Azure.IIoT.Cdm;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Writes data tables into files on file storage
    /// </summary>
    public class CsvDataTableWriter : IDataTableWriter {

        /// <inheritdoc/>
        public IFileStorage Storage { get; }

        /// <summary>
        /// CDM Azure Data lake storage handler
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="storage"></param>
        public CsvDataTableWriter(ILogger logger, IFileStorage storage) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <inheritdoc/>
        public async Task<bool> WriteAsync<T>(string drive, string folder, string partition,
            List<T> data, string separator) {
            try {
                var storage = await Storage.CreateOrOpenDriveAsync(drive);
                var directory = await storage.CreateOrOpenSubFolderAsync(folder);
                var file = await directory.CreateOrOpenFileAsync(partition);
                var size = await file.GetSizeAsync();
                var content = Encoding.UTF8.GetBytes(BuildCsvData(data, separator, size == 0));
                await file.WriteAsync(content, content.Length, size);
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to write data in the csv partition");
                throw ex;
            }
            return true;
        }

        /// <summary>
        /// prepare a csv formated block
        /// </summary>
        /// <param name="data"></param>
        /// <param name="separator"></param>
        /// <param name="addHeader"></param>
        /// <returns></returns>
        private string BuildCsvData<T>(List<T> data, string separator, bool addHeader = false) {
            var sb = new StringBuilder();
            var info = typeof(T).GetProperties();
            if (addHeader) {
                foreach (var prop in info) {
                    if (prop.Name != nameof(DataSetMessageModel.Payload)) {
                        AddValueToCsvStringBuilder(prop.Name, separator, sb);
                    }
                    else {
                        var payload = prop.GetValue(data[0]) as Dictionary<string, DataValueModel>;
                        foreach (var node in payload.OrderBy(i => i.Key)) {
                            AddValueToCsvStringBuilder($"{node.Key}_value", separator, sb);
                            AddValueToCsvStringBuilder($"{node.Key}_status", separator, sb);
                            AddValueToCsvStringBuilder($"{node.Key}_sourceTimestamp", separator, sb);
                            AddValueToCsvStringBuilder($"{node.Key}_serverTimestamp", separator, sb);
                        }
                    }
                }
                sb.Remove(sb.Length - 1, 1);
            }
            foreach (var obj in data) {
                sb.AppendLine();
                foreach (var prop in info) {
                    if (prop.Name != nameof(DataSetMessageModel.Payload)) {
                        AddValueToCsvStringBuilder(prop.GetValue(obj), separator, sb);
                    }
                    else {
                        var payload = prop.GetValue(obj) as Dictionary<string, DataValueModel>;
                        foreach (var node in payload.OrderBy(i => i.Key)) {
                            var nodeProperties = node.Value.GetType().GetProperties();
                            foreach (var nodeProp in nodeProperties) {
                                AddValueToCsvStringBuilder(nodeProp.GetValue(node.Value), separator, sb);
                            }
                        }
                    }
                }
                sb.Remove(sb.Length - 1, 1);
            }
            return sb.ToString();
        }

        private void AddValueToCsvStringBuilder(object value,
            string separator, StringBuilder sb) {

            if (value != null) {
                var str = value?.ToString();
                if (str != null &&
                    (str.Contains(separator) ||
                    str.Contains("\"") || str.Contains("\r") ||
                    str.Contains("\n"))) {
                    sb.Append('\"');
                    foreach (var nextChar in str) {
                        sb.Append(nextChar);
                        if (nextChar == '"') {
                            sb.Append('\"');
                        }
                    }
                    sb.Append('\"');
                }
                else {
                    sb.Append(str);
                }
            }
            sb.Append(separator);
        }

        private readonly ILogger _logger;
    }
}
