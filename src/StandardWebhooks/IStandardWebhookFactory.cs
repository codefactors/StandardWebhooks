// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

namespace StandardWebhooks;

/// <summary>
/// Interface for factories that can create <see cref="StandardWebhook"/> instances.
/// </summary>
/// <remarks>Intended particularly for use in ASP.NET dependency injection scenarios.</remarks>
public interface IStandardWebhookFactory
{
    /// <summary>
    /// Creates a new instance of a <see cref="StandardWebhook"/>.
    /// </summary>
    /// <returns>New instance of a <see cref="StandardWebhook"/>.</returns>
    StandardWebhook CreateWebhook();
}
