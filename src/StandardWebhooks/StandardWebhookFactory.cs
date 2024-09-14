// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

namespace StandardWebhooks;

/// <summary>
/// Factory for creating <see cref="StandardWebhook"/> instances using the supplied
/// factory constructor parameters.
/// </summary>
/// <remarks>Intended particularly for use in ASP.NET dependency injection scenarios.</remarks>
public class StandardWebhookFactory : IStandardWebhookFactory
{
    private readonly string _signingKey;
    private readonly WebhookConfigurationOptions? _webhookConfigurationOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardWebhookFactory"/> class.
    /// </summary>
    /// <param name="signingKey">Signing key, as byte array.</param>
    /// <param name="webhookConfigurationOptions">Options to set custom header keys.</param>
    public StandardWebhookFactory(
        string signingKey,
        WebhookConfigurationOptions? webhookConfigurationOptions)
    {
        _signingKey = signingKey;
        _webhookConfigurationOptions = webhookConfigurationOptions;
    }

    /// <summary>
    /// Creates a new instance of a <see cref="StandardWebhook"/>.
    /// </summary>
    /// <returns>New instance of a <see cref="StandardWebhook"/>.</returns>
    public StandardWebhook CreateWebhook() =>
        _webhookConfigurationOptions != null ?
        new StandardWebhook(_signingKey, _webhookConfigurationOptions) :
        new StandardWebhook(_signingKey);
}
