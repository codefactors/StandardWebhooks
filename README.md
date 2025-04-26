# StandardWebhooks

## Implementation of Standard Webhooks for .NET Core

StandardWebhooks is an implementation of the [Standard Webhooks specification](https://github.com/standard-webhooks/standard-webhooks/blob/main/spec/standard-webhooks.md)
initiated by Svix and supported by other industry participants including Zapier, Twilio, ngrok and Kong.

This repo extends the [C# reference implementation](https://github.com/standard-webhooks/standard-webhooks/tree/main/libraries/csharp),
providing a maintained (nuget package) along with some helpful features for ASP.NET Core developers.

It is supported on all current .NET Core platforms (.NET 8.0, NET 9.0).

## Releases

See the [release notes](doc/releases.md) for details of changes in each release.  The latest release should be available on Nuget at https://www.nuget.org/packages/StandardWebhooks.  To install using Package Manager:

```
dotnet add package StandardWebhooks --version 1.0.33
```

## Functionality

### Generating HttpContent for a Webhook
```csharp
var myEntity = new MyEntity
{
    Id = 1,
    Name = "Test Entity",
    Description = "This is a test entity"
};

var webhook = new StandardWebhook(WEBHOOK_SIGNING_KEY);

var sendingTime = DateTimeOffset.UtcNow;

var webhookContent = webhook.MakeHttpContent(myEntity, DEFAULT_MSG_ID, sendingTime);
```

### Verifying a Webhook signature
```csharp
// Assumes messageBody contains string representation of message content
// and request is an HttpRequest with the Standard Webhooks headers set

var webhook = new StandardWebhook(WEBHOOK_SIGNING_KEY);

// Throws WebhookVerificationException if verification fails
webhook.Verify(messageBody, request.Headers);
```

### Factory Pattern
The library provides `IStandardWebhookFactory` and its implementation `StandardWebhookFactory`
which are intended for use in ASP.NET DI scenarios.  Usage:
```csharp
// In Program.cs, or wherever services are configured.  Optionally WebhookConfigurationOptions can be
// provided as a second parameter to configure which header names to use.

services.AddSingleton<IStandardWebhookFactory>(sp =>
    new StandardWebhookFactory(WEBHOOK_SIGNING_KEY));

// In method, add IStandardWebhookFactory as an injected parameter and then:
var webhook = webhookFactory.CreateWebhook();
```

### Standard Configurations
Two standard `WebhookConfigurationOptions` configurations are provided as static instances, `WebhookConfigurationOptions.StandardWebhooks` and
`WebhookConfigurationOptions.Svix`, the former for the HTTP headers as described in the Standard Webhooks specification and
the latter for the headers used by Svix. The default configuration if no options are supplied is `WebhookConfigurationOptions.StandardWebhooks`.

## Questions and Answers
Please raise any issues or questions via the [Issues page on GitHub](https://github.com/codefactors/StandardWebhooks/issues).

## Roadmap
Currently the library doesn't provide any support for sending messages, beyond the ability to create an `HttpContent` instance
from the message payload. The plan is to extent the library to support message transmission using `HttpClient`/`IHttpClientFactory`,
leveraging the retry capabilites of the [Polly](https://github.com/App-vNext/Polly) package.

## Acknowledgements

This project leverages the work of the **Standard Webhooks** project, published on Github in the [standard-webhooks](https://github.com/standard-webhooks/standard-webhooks) repository.
Specifically it builds upon the [C# reference implementation](https://github.com/standard-webhooks/standard-webhooks/tree/main/libraries/csharp).

Signature verification is based on the [svix-webhooks implementation](https://github.com/svix/svix-webhooks).

## License
This project is licensed under the MIT License.
