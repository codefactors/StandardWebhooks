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
    /// <param name="key">Signing key, as string.</param>
    public StandardWebhook(string key)
        : this(key, WebhookConfigurationOptions.StandardWebhooks)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="key">Signing key, as byte array.</param>
    public StandardWebhook(byte[] key)
        : this(key, WebhookConfigurationOptions.StandardWebhooks)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="key">Signing key, as byte array.</param>
    /// <param name="options">Options to set custom header keys.</param>
    public StandardWebhook(string key, WebhookConfigurationOptions options)
        : this(Convert.FromBase64String(key.StartsWith(PREFIX) ? key[PREFIX.Length..] : key), options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhook"/> class.
    /// </summary>
    /// <param name="key">Signing key, as byte array.</param>
    /// <param name="options">Options to set custom header keys.</param>
    public StandardWebhook(byte[] key, WebhookConfigurationOptions options)
    {
        _key = key;

        _idHeaderKey = options.IdHeaderKey;
        _signatureHeaderKey = options.SignatureHeaderKey;
        _timestampHeaderKey = options.TimestampHeaderKey;
    }

    public void Verify(string payload, IHeaderDictionary headers)
    {
        string msgId = headers[_idHeaderKey].ToString();
        string msgSignature = headers[_signatureHeaderKey].ToString();
        string msgTimestamp = headers[_timestampHeaderKey].ToString();

        if (string.IsNullOrEmpty(msgId) || string.IsNullOrEmpty(msgSignature) || string.IsNullOrEmpty(msgTimestamp))
            throw new WebhookVerificationException("Missing Required Headers");

        var timestamp = StandardWebhook.VerifyTimestamp(msgTimestamp);

        var signature = this.Sign(msgId, timestamp, payload);

        var expectedSignature = signature.Split(',')[1];
        var passedSignatures = msgSignature.Split(' ');

        foreach (string versionedSignature in passedSignatures)
        {
            var parts = versionedSignature.Split(',');

            if (parts.Length < 2)
                throw new WebhookVerificationException("Invalid Signature Headers");

            var version = parts[0];
            var passedSignature = parts[1];

            if (version != "v1")
                continue;

            if (WebhookUtils.SecureCompare(expectedSignature, passedSignature))
                return;
        }

        throw new WebhookVerificationException("No matching signature found");
    }

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

    public HttpContent MakeContent<T>(T body, string msgId, DateTimeOffset timestamp)
    {
        var content = StringContent.Create(body);

        var signature = Sign(msgId, timestamp, )

        content.Headers.Add()

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
            throw new WebhookVerificationException("Invalid Signature Headers");
        }

        if (timestamp < now.AddSeconds(-1 * TOLERANCE_IN_SECONDS))
            throw new WebhookVerificationException("Message timestamp too old");

        if (timestamp > now.AddSeconds(TOLERANCE_IN_SECONDS))
            throw new WebhookVerificationException("Message timestamp too new");

        return timestamp;
    }
}