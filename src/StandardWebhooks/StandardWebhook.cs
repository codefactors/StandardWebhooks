// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

// Portions Copyright (c) 2025 Svix (https://www.svix.com) used under MIT licence,
// see https://github.com/standard-webhooks/standard-webhooks/blob/main/libraries/LICENSE.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using StandardWebhooks.Diagnostics;

namespace StandardWebhooks;

/// <summary>
/// Provides a set of facilities to support the use of Standard Webhooks, as defined at
/// https://github.com/standard-webhooks/standard-webhooks.
/// </summary>
public sealed class StandardWebhook
{
    private const int SIGNATURE_LENGTH_BYTES = HMACSHA256.HashSizeInBytes;
    private const int SIGNATURE_LENGTH_BASE64 = 48;
    private const int SIGNATURE_LENGTH_STRING = 56;
    private const int TOLERANCE_IN_SECONDS = 60 * 5;
    private const int MAX_STACKALLOC = 1024 * 256;
    private const string PREFIX = "whsec_";

    private static readonly UTF8Encoding SafeUTF8Encoding = new UTF8Encoding(false, true);

    private readonly byte[] _key;
    private readonly string _idHeaderKey;
    private readonly string _signatureHeaderKey;
    private readonly string _timestampHeaderKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="signingKey">Signing key, as string.</param>
    public StandardWebhook(string signingKey)
        : this(signingKey, WebhookConfigurationOptions.StandardWebhooks)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="signingKey">Signing key, as byte array.</param>
    public StandardWebhook(byte[] signingKey)
        : this(signingKey, WebhookConfigurationOptions.StandardWebhooks)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="signingKey">Signing key, as byte array.</param>
    /// <param name="options">Options to set custom header keys.</param>
    public StandardWebhook(string signingKey, WebhookConfigurationOptions options)
        : this(Convert.FromBase64String(signingKey.StartsWith(PREFIX) ? signingKey[PREFIX.Length..] : signingKey), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="signingKey">Signing key, as byte array.</param>
    /// <param name="options">Options to set custom header keys.</param>
    public StandardWebhook(byte[] signingKey, WebhookConfigurationOptions options)
    {
        _key = signingKey;

        _idHeaderKey = options.IdHeaderKey;
        _signatureHeaderKey = options.SignatureHeaderKey;
        _timestampHeaderKey = options.TimestampHeaderKey;
    }

    /// <summary>
    /// Verifies the signature of a webhook payload.
    /// </summary>
    /// <param name="payload">Webhook payload in string form.</param>
    /// <param name="headers">HTTP header collection.</param>
    /// <exception cref="WebhookVerificationException">Thrown if any of the message id,
    /// message signature or message timestamp headers are missing or empty.  Also thrown if the signature header
    /// field value is not in the correct format.
    /// </exception>
    public void Verify(string payload, IHeaderDictionary headers)
    {
        ReadOnlySpan<char> msgId = headers[_idHeaderKey].ToString();
        ReadOnlySpan<char> msgSignature = headers[_signatureHeaderKey].ToString();
        ReadOnlySpan<char> msgTimestamp = headers[_timestampHeaderKey].ToString();

        if (msgId.IsEmpty || msgSignature.IsEmpty || msgTimestamp.IsEmpty)
            throw new WebhookVerificationException($"Missing required headers; {_idHeaderKey}, {_signatureHeaderKey} and {_timestampHeaderKey} must be supplied");

        VerifyTimestamp(msgTimestamp);

        Span<char> expectedSignature = stackalloc char[SIGNATURE_LENGTH_STRING];
        CalculateSignature(
            msgId,
            msgTimestamp,
            payload,
            expectedSignature,
            out var charsWritten);
        expectedSignature = expectedSignature.Slice(0, charsWritten);

        var signaturePtr = msgSignature;
        var spaceIndex = signaturePtr.IndexOf(' ');
        do
        {
            var versionedSignature =
                spaceIndex < 0 ? msgSignature : signaturePtr.Slice(0, spaceIndex);

            signaturePtr = signaturePtr.Slice(spaceIndex + 1);
            spaceIndex = signaturePtr.IndexOf(' ');

            var commaIndex = versionedSignature.IndexOf(',');
            if (commaIndex < 0)
            {
                throw new WebhookVerificationException("Invalid Signature Headers");
            }

            var version = versionedSignature.Slice(0, commaIndex);
            if (!version.Equals("v1", StringComparison.InvariantCulture))
            {
                continue;
            }

            var passedSignature = versionedSignature.Slice(commaIndex + 1);
            if (WebhookUtils.SecureCompare(expectedSignature, passedSignature))
            {
                return;
            }
        }
        while (spaceIndex >= 0);

        throw new WebhookVerificationException("No matching signature found");
    }

    /// <summary>
    /// Generates the appropriate signature for the supplied webhook payload.
    /// </summary>
    /// <param name="msgId">Message Id.</param>
    /// <param name="timestamp">Sending timestamp.</param>
    /// <param name="payload">Webhook payload, as a string.</param>
    /// <returns>Standard Webhooks signature in the format 'version,signature'.</returns>
    /// <remarks>Currently only supports 'v1' signatures.</remarks>
    public string Sign(
        ReadOnlySpan<char> msgId,
        DateTimeOffset timestamp,
        ReadOnlySpan<char> payload)
    {
        Span<char> signature = stackalloc char[SIGNATURE_LENGTH_STRING];
        signature[0] = 'v';
        signature[1] = '1';
        signature[2] = ',';
        CalculateSignature(
            msgId,
            timestamp.ToUnixTimeSeconds().ToString(),
            payload,
            signature.Slice(3),
            out var charsWritten);
        return signature.Slice(0, charsWritten + 3).ToString();
    }


    /// <summary>
    /// Generates an <see cref="HttpContent"/> that contains the supplied payload, with the appropriate
    /// Standard Webhooks headers added, including the signature for the payload.
    /// </summary>
    /// <typeparam name="T">Type of payload.</typeparam>
    /// <param name="body">Content for the webhook payload.</param>
    /// <param name="msgId">Message identifier.</param>
    /// <param name="timestamp">Sending timestamp.</param>
    /// <param name="jsonOptions">Optional <see cref="JsonSerializerOptions"/> instance to control how
    /// the payload is serialized.</param>
    /// <returns>An <see cref="HttpContent"/> initialised with the JSON serialized payload and necessary
    /// headers set.</returns>
    [RequiresUnreferencedCode("This code path does not support NativeAOT. Use the JsonSerializationContext overload for NativeAOT Scenarios.")]
    [RequiresDynamicCode("This code path does not support NativeAOT. Use the JsonSerializationContext overload for NativeAOT Scenarios.")]
    public HttpContent MakeHttpContent<T>(T body, string msgId, DateTimeOffset timestamp, JsonSerializerOptions? jsonOptions = null)
    {
        var content = WebhookContent<T>.Create(body, jsonOptions);

        var signature = Sign(msgId, timestamp, content.ToString());

        content.Headers.Add(_idHeaderKey, msgId);
        content.Headers.Add(_timestampHeaderKey, timestamp.ToUnixTimeSeconds().ToString());
        content.Headers.Add(_signatureHeaderKey, signature);

        return content;
    }

    /// <summary>
    /// Generates an <see cref="HttpContent"/> that contains the supplied payload, with the appropriate
    /// Standard Webhooks headers added, including the signature for the payload.
    /// </summary>
    /// <typeparam name="T">Type of payload.</typeparam>
    /// <param name="body">Content for the webhook payload.</param>
    /// <param name="msgId">Message identifier.</param>
    /// <param name="timestamp">Sending timestamp.</param>
    /// <param name="context">The JsonSerializationContext used to serialize this payload.</param>
    /// <returns>An <see cref="HttpContent"/> initialised with the JSON serialized payload and necessary
    /// headers set.</returns>
    public HttpContent MakeHttpContent<T>(T body, string msgId, DateTimeOffset timestamp, JsonSerializerContext context)
    {
        var content = WebhookContent<T>.Create(body, context);

        var signature = Sign(msgId, timestamp, content.ToString());

        content.Headers.Add(_idHeaderKey, msgId);
        content.Headers.Add(_timestampHeaderKey, timestamp.ToUnixTimeSeconds().ToString());
        content.Headers.Add(_signatureHeaderKey, signature);

        return content;
    }

    private static void VerifyTimestamp(ReadOnlySpan<char> timestampHeader)
    {
        DateTimeOffset timestamp;

        var now = DateTimeOffset.UtcNow;

        try
        {
            var timestampInt = long.Parse(timestampHeader);

            timestamp = DateTimeOffset.FromUnixTimeSeconds(timestampInt);
        }
        catch
        {
            throw new WebhookVerificationException("Invalid timestamp header value; must be the number of seconds elapsed since 1970-01-01T00:00:00Z");
        }

        if (timestamp < now.AddSeconds(-1 * TOLERANCE_IN_SECONDS))
            throw new WebhookVerificationException("Message timestamp too old");

        if (timestamp > now.AddSeconds(TOLERANCE_IN_SECONDS))
            throw new WebhookVerificationException("Message timestamp too new");
    }

    private void CalculateSignature(
            ReadOnlySpan<char> msgId,
            ReadOnlySpan<char> timestamp,
            ReadOnlySpan<char> payload,
            Span<char> signature,
            out int charsWritten)
        {
            // Estimate buffer size and use stackalloc for smaller allocations
            int msgIdLength = SafeUTF8Encoding.GetByteCount(msgId);
            int payloadLength = SafeUTF8Encoding.GetByteCount(payload);
            int timestampLength = SafeUTF8Encoding.GetByteCount(timestamp);
            int totalLength = msgIdLength + 1 + timestampLength + 1 + payloadLength;

            Span<byte> toSignBytes =
                totalLength <= MAX_STACKALLOC
                    ? stackalloc byte[totalLength]
                    : new byte[totalLength];

            SafeUTF8Encoding.GetBytes(msgId, toSignBytes.Slice(0, msgIdLength));
            toSignBytes[msgIdLength] = (byte)'.';
            SafeUTF8Encoding.GetBytes(
                timestamp,
                toSignBytes.Slice(msgIdLength + 1, timestampLength));
            toSignBytes[msgIdLength + 1 + timestampLength] = (byte)'.';
            SafeUTF8Encoding.GetBytes(
                payload,
                toSignBytes.Slice(msgIdLength + 1 + timestampLength + 1));

            Span<byte> signatureBin = stackalloc byte[SIGNATURE_LENGTH_BYTES];
            CalculateSignature(toSignBytes, signatureBin);

            Span<byte> signatureB64 = stackalloc byte[SIGNATURE_LENGTH_BASE64];
            var result = Base64.EncodeToUtf8(
                signatureBin,
                signatureB64,
                out _,
                out var bytesWritten);
            if (result != OperationStatus.Done)
                throw new WebhookVerificationException("Failed to encode signature to base64");

            if (
                !SafeUTF8Encoding.TryGetChars(
                    signatureB64.Slice(0, bytesWritten),
                    signature,
                    out charsWritten)
            )
                throw new WebhookVerificationException("Failed to convert signature to utf8");
        }

    private void CalculateSignature(ReadOnlySpan<byte> input, Span<byte> output)
    {
        try
        {
            HMACSHA256.HashData(_key, input, output);
        }
        catch (Exception)
        {
            throw new WebhookVerificationException("Output buffer too small");
        }
    }
}