// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StandardWebhooks;

/// <summary>
/// Provides HTTP content for Standard Webhooks.
/// </summary>
/// <typeparam name="T">The type of the content.</typeparam>
public class WebhookContent<T> : ByteArrayContent
{
    // Maintain copy of the content so we can return it as a string.
    private readonly byte[] _content;

    private WebhookContent(byte[] content)
        : base(content)
    {
        _content = content;

        Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = WebhookContentDefaults.Utf8Encoding.WebName };
    }

    /// <summary>Creates a new instance of the <see cref="WebhookContent{T}"/> class.</summary>
    /// <param name="content">The content to be used to initialize the <see cref="WebhookContent{T}"/>.</param>
    /// <param name="jsonOptions">The JSON serialization options to be used to serialize the content. Optional,
    /// defaults to <see cref="JsonSerializerDefaults.Web"/>  with WriteIndented set to false.</param>
    /// <returns>New instance of a <see cref="WebhookContent{T}"/>.</returns>
    public static WebhookContent<T> Create(T content, JsonSerializerOptions? jsonOptions = null)
    {
#pragma warning disable IL3050 This overload can still be used in NativeAOT as long as the serializer context has been provided to the options object.
#pragma warning disable IL2026
        var utf8bytes = JsonSerializer.SerializeToUtf8Bytes(content, jsonOptions ?? WebhookContentDefaults.JsonSerializerOptions);
#pragma warning restore IL2026
#pragma warning restore IL3050

        return new WebhookContent<T>(utf8bytes);
    }

    /// <summary>Creates a new instance of the <see cref="WebhookContent{T}"/> class.</summary>
    /// <param name="content">The content to be used to initialize the <see cref="WebhookContent{T}"/>.</param>
    /// <param name="context">The JsonSerializationContext used to serialize this payload.</param>
    /// <returns>New instance of a <see cref="WebhookContent{T}"/>.</returns>
    public static WebhookContent<T> Create(T content, JsonSerializerContext context)
    {
        var utf8bytes = JsonSerializer.SerializeToUtf8Bytes(content, typeof(T), context);

        return new WebhookContent<T>(utf8bytes);
    }

    /// <summary>
    /// Gets the content as a string.
    /// </summary>
    /// <returns>String representation of the content.</returns>
    public override string ToString() => WebhookContentDefaults.Utf8Encoding.GetString(_content);
}