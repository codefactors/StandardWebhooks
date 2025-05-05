// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

using System.Text;
using System.Text.Json;

namespace StandardWebhooks
{
    /// <summary>
    /// Internal defaults for <see cref="WebhookContent{T}"/>.
    /// </summary>
    internal static class WebhookContentDefaults
    {
        /// <summary>
        /// Default Uft8Encoding instance.
        /// </summary>
        public static readonly Encoding Utf8Encoding = new UTF8Encoding(false, true);

        /// <summary>
        /// Default JsonSerializerOptions instance.
        /// </summary>
        public static readonly JsonSerializerOptions JsonSerializerOptions =
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false };
    }
}