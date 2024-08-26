// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// File system services extensions
    /// </summary>
    public static class FileSystemServicesEx
    {
        /// <summary>
        /// Copy from server to provided stream (e.g. file)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="file"></param>
        /// <param name="stream"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ServiceResultModel> CopyToAsync<T>(this IFileSystemServices<T> service,
            T endpoint, FileSystemObjectModel file, Stream stream, CancellationToken ct = default)
        {
            var open = await service.OpenReadAsync(endpoint, file, ct).ConfigureAwait(false);
            if (open.ErrorInfo != null)
            {
                Debug.Assert(open.Result == null);
                return open.ErrorInfo;
            }
            Debug.Assert(open.Result != null);
            await using (var _ = open.Result.ConfigureAwait(false))
            {
                await open.Result.CopyToAsync(stream, ct).ConfigureAwait(false);
            }
            return new ServiceResultModel();
        }

        /// <summary>
        /// Copy from stream (e.g. file) to file on server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="file"></param>
        /// <param name="stream"></param>
        /// <param name="options"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<ServiceResultModel> CopyFromAsync<T>(this IFileSystemServices<T> service,
            T endpoint, FileSystemObjectModel file, Stream stream, FileOpenWriteOptionsModel? options = null,
            CancellationToken ct = default)
        {
            var open = await service.OpenWriteAsync(endpoint, file, options, ct).ConfigureAwait(false);
            if (open.ErrorInfo != null)
            {
                Debug.Assert(open.Result == null);
                return open.ErrorInfo;
            }
            Debug.Assert(open.Result != null);
            await using (var _ = open.Result.ConfigureAwait(false))
            {
                await stream.CopyToAsync(open.Result, ct).ConfigureAwait(false);
            }
            return new ServiceResultModel();
        }
    }
}
