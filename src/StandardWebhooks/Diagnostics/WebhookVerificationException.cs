// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

// Portions Copyright (c) 2023 Svix (https://www.svix.com) used under MIT licence,
// see https://github.com/standard-webhooks/standard-webhooks/blob/main/libraries/LICENSE.

namespace StandardWebhooks.Diagnostics;

/// <summary>
/// Exception that is thrown whenever verification fails.
/// </summary>
public class WebhookVerificationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookVerificationException"/> class.
    /// </summary>
    public WebhookVerificationException()
        : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookVerificationException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public WebhookVerificationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookVerificationException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public WebhookVerificationException(string message, Exception inner)
        : base(message, inner)
    {
    }
}