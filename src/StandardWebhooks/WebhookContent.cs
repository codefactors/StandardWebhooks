// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace StandardWebhooks;

/// <summary>
/// Provides HTTP content for Standard Webhooks.
/// </summary>
public class WebhookContent<T> : ByteArrayContent
{
    private const string DefaultMediaType = "application/json";

    private static readonly Encoding DefaultEncoding = Encoding.UTF8;

    /// <summary>Creates a new instance of the <see cref="WebhookContent"/> class.</summary>
    /// <param name="content">The content used to initialize the <see cref="WebhookContent"/>.</param>
    /// <remarks>The media type for the <see cref="WebhookContent"/> created defaults to text/plain.</remarks>
    public WebhookContent(T content)
        : this(content, DefaultEncoding, DefaultMediaType)
    {
    }

    /// <summary>Creates a new instance of the <see cref="WebhookContent"/> class.</summary>
    /// <param name="content">The content used to initialize the <see cref="WebhookContent"/>.</param>
    /// <param name="mediaType">The media type to use for the content.</param>
    public WebhookContent(T content, MediaTypeHeaderValue mediaType)
        : this(content, DefaultEncoding, mediaType)
    {
    }

    /// <summary>Creates a new instance of the <see cref="WebhookContent"/> class.</summary>
    /// <param name="content">The content used to initialize the <see cref="WebhookContent"/>.</param>
    /// <param name="encoding">The encoding to use for the content.</param>
    /// <remarks>The media type for the <see cref="WebhookContent"/> created defaults to text/plain.</remarks>
    public WebhookContent(T content, Encoding? encoding)
        : this(content, encoding, DefaultMediaType)
    {
    }

    /// <summary>Creates a new instance of the <see cref="WebhookContent"/> class.</summary>
    /// <param name="content">The content used to initialize the <see cref="WebhookContent"/>.</param>
    /// <param name="encoding">The encoding to use for the content.</param>
    /// <param name="mediaType">The media type to use for the content.</param>
    public WebhookContent(T content, Encoding? encoding, string mediaType)
        : this(content, encoding, new MediaTypeHeaderValue(mediaType ?? DefaultMediaType, (encoding ?? DefaultEncoding).WebName))
    {
    }

    /// <summary>Creates a new instance of the <see cref="WebhookContent"/> class.</summary>
    /// <param name="content">The content used to initialize the <see cref="WebhookContent"/>.</param>
    /// <param name="encoding">The encoding to use for the content.</param>
    /// <param name="mediaType">The media type to use for the content.</param>
    public WebhookContent(string content, Encoding? encoding, MediaTypeHeaderValue mediaType)
        : base(GetContentByteArray(content, encoding))
    {
        Headers.ContentType = mediaType;
    }

    /// <summary>Serialize and write the byte array provided in the constructor to an HTTP content stream as an asynchronous operation.</summary>
    /// <param name="stream">The target stream.</param>
    /// <param name="context">Information about the transport, like channel binding token. This parameter may be <see langword="null" />.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
        // Only skip the original protected virtual SerializeToStreamAsync if this
        // isn't a derived type that may have overridden the behavior.
        GetType() == typeof(WebhookContent) ? SerializeToStreamAsyncCore(stream, cancellationToken) :
        base.SerializeToStreamAsync(stream, context, cancellationToken);

    protected override bool TryComputeLength(out long length)
    {
        throw new NotImplementedException();
    }

    private static byte[] GetContentByteArray(string content, Encoding? encoding)
    {
        ArgumentNullException.ThrowIfNull(content);

        // In this case we treat 'null' strings differently from string.Empty in order to be consistent with our
        // other *Content constructors: 'null' throws, empty values are allowed.

        return (encoding ?? DefaultEncoding).GetBytes(content);
    }
}
