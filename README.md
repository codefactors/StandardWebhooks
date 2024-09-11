# StandardWebhooks

## Implementation of Standard Webhooks for .NET Core


## Subhead
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

### Acknowledgements


### License
This project is licensed under the MIT License.
