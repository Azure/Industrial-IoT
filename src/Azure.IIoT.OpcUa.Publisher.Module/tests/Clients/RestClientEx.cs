// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Clients
{
    using Azure.IIoT.OpcUa.Core.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class RestClientEx
    {
        public static async Task<T> PostAsync<T>(this IHttpClientFactory factory,
            Uri uri, object? body, CancellationToken ct = default)
        {
            using var httpClient = factory.CreateClient();
            using var request = CreatePostRequest(uri, body);
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await Json.DeserializeAsync<T>(stream, ct).ConfigureAwait(false) ??
                throw new HttpRequestException("Bad response");
        }

        public static async IAsyncEnumerable<T> PostStreamAsync<T>(this IHttpClientFactory factory,
            Uri uri, object? body, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using var httpClient = factory.CreateClient();
            using var request = CreatePostRequest(uri, body);
            using var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var values = await Json.DeserializeAsync<IReadOnlyList<T>>(stream, ct).ConfigureAwait(false) ?? [];
            foreach (var value in values)
            {
                ct.ThrowIfCancellationRequested();
                yield return value;
            }
        }

        private static HttpRequestMessage CreatePostRequest(Uri uri, object? body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, uri);
            var content = new ByteArrayContent(Json.SerializeObjectToMemory(body).ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue(Json.MimeType);
            request.Content = content;
            return request;
        }
    }
}
