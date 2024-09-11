// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

// Portions Copyright (c) 2023 Svix (https://www.svix.com) used under MIT licence,
// see https://github.com/standard-webhooks/standard-webhooks/blob/main/libraries/LICENSE.

using StandardWebhooks.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StandardWebhooks;

/// <summary>
/// Provides a set of facilities to support the use of Standard Webhooks, as defined at
/// https://github.com/standard-webhooks/standard-webhooks.
/// </summary>
public sealed class StandardWebhook
{
    private const int TOLERANCE_IN_SECONDS = 60 * 5;
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
        string msgId = headers[_idHeaderKey].ToString();
        string msgSignature = headers[_signatureHeaderKey].ToString();
        string msgTimestamp = headers[_timestampHeaderKey].ToString();

        if (string.IsNullOrEmpty(msgId) || string.IsNullOrEmpty(msgSignature) || string.IsNullOrEmpty(msgTimestamp))
            throw new WebhookVerificationException($"Missing required headers; {_idHeaderKey}, {_signatureHeaderKey} and {_timestampHeaderKey} must be supplied");

        var timestamp = VerifyTimestamp(msgTimestamp);

        var expectedSignature = Sign(msgId, timestamp, payload)
            .Split(',')[1];

        var passedSignatures = msgSignature.Split(' ');

        foreach (string versionedSignature in passedSignatures)
        {
            var parts = versionedSignature.Split(',');

            if (parts.Length < 2)
                throw new WebhookVerificationException("Invalid signature header; must be in the form 'version,signature'");

            var version = parts[0];
            var passedSignature = parts[1];

            if (version != "v1")
                continue;

            if (WebhookUtils.SecureCompare(expectedSignature, passedSignature))
                return;
        }

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
    public string Sign(string msgId, DateTimeOffset timestamp, string payload)
    {
        var toSign = $"{msgId}.{timestamp.ToUnixTimeSeconds()}.{payload}";
        var toSignBytes = SafeUTF8Encoding.GetBytes(toSign);

        using (var hmac = new HMACSHA256(this._key))
        {
            var hash = hmac.ComputeHash(toSignBytes);

            var signature = Convert.ToBase64String(hash);

            return $"v1,{signature}";
        }
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
    public HttpContent MakeHttpContent<T>(T body, string msgId, DateTimeOffset timestamp, JsonSerializerOptions? jsonOptions = null)
    {
        var content = WebhookContent<T>.Create(body, jsonOptions);

        var signature = Sign(msgId, timestamp, content.ToString());

        content.Headers.Add(_idHeaderKey, msgId);
        content.Headers.Add(_timestampHeaderKey, timestamp.ToUnixTimeSeconds().ToString());
        content.Headers.Add(_signatureHeaderKey, signature);

        return content;
    }

    private static DateTimeOffset VerifyTimestamp(string timestampHeader)
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

        return timestamp;
    }
}