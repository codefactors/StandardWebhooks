# StandardWebhooks

## Implementation of Standard Webhooks for .NET Core


### Generating HttpContent for a Webhook
```csharp
var testEntity = new TestEntity
{
    Id = 1,
    Name = "Test Entity",
    Description = "This is a test entity"
};

var webhook = new StandardWebhook(WEBHOOK_SIGNING_KEY);

var sendingTime = DateTimeOffset.UtcNow;

var webhookContent = webhook.MakeHttpContent(testEntity, DEFAULT_MSG_ID, sendingTime);

```

## Acknowledgements

This project leverages the work of the **Standard Webhooks** project, published on Github in the [standard-webhooks](https://github.com/standard-webhooks/standard-webhooks) repository.
Specifically it builds upon the [C# reference implementation](https://github.com/standard-webhooks/standard-webhooks/tree/main/libraries/csharp).

## License
This project is licensed under the MIT License.
