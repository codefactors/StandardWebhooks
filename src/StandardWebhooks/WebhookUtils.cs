// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

// Portions Copyright (c) 2023 Svix (https://www.svix.com) used under MIT licence,
// see https://github.com/standard-webhooks/standard-webhooks/blob/main/libraries/LICENSE.
//
// Portions Copyright (c) Stripe Inc used under Apache License v2.0, see
// https://github.com/stripe/stripe-dotnet/blob/master/LICENSE

using System.Runtime.CompilerServices;

namespace StandardWebhooks;

/// <summary>
/// Utility facilities class.
/// </summary>
internal static class WebhookUtils
{
    /// <summary>
    /// Compares the supplied strings in a secure fashion.
    /// </summary>
    /// <param name="a">First string to compare.</param>
    /// <param name="b">Second string to compare.</param>
    /// <returns>true if the strings are identical; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either of the input parameters are null.</exception>
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static bool SecureCompare(string a, string b)
    {
        if (a == null)
            throw new ArgumentNullException(nameof(a));

        if (b == null)
            throw new ArgumentNullException(nameof(b));

        if (a.Length != b.Length)
            return false;

        var result = 0;

        for (var i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }
}