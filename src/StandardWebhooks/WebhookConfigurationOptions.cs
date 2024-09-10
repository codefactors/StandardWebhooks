// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

namespace StandardWebhooks;

/// <summary>
/// Options for initialising <see cref="StandardWebhook"/> instances.
/// </summary>
public class WebhookConfigurationOptions
{
    private static WebhookConfigurationOptions _standard = new WebhookConfigurationOptions
    {
        IdHeaderKey = "webhook-id",
        SignatureHeaderKey = "webhook-signature",
        TimestampHeaderKey = "webhook-timestamp"
    };

    private static WebhookConfigurationOptions _svix = new WebhookConfigurationOptions
    {
        IdHeaderKey = "Svix-Id",
        SignatureHeaderKey = "Svix-Signature",
        TimestampHeaderKey = "Svix-Timestamp"
    };

    /// <summary>
    /// Gets the header key for the ID field.
    /// </summary>
    public string IdHeaderKey { get; init; } = default!;

    /// <summary>
    /// Gets the header key for the signature field.
    /// </summary>
    public string SignatureHeaderKey { get; init; } = default!;

    /// <summary>
    /// Gets the header key for the signature field.
    /// </summary>
    public string TimestampHeaderKey { get; init; } = default!;

    /// <summary>
    /// Gets a static instance of <see cref="WebhookConfigurationOptions"/> for Standard Webhooks as
    /// defined in the standard.
    /// </summary>
    public static WebhookConfigurationOptions StandardWebhooks => _standard;

    /// <summary>
    /// Gets a static instance of <see cref="WebhookConfigurationOptions"/> for Svix webhooks.
    /// </summary>
    public static WebhookConfigurationOptions Svix => _svix;
}