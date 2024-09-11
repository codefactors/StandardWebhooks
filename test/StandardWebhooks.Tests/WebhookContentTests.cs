// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

using System.Text.Json;
using System.Text.Json.Serialization;

namespace StandardWebhooks.Tests;

public class WebhookContentTests
{
    private const string TEST_SECRET = "NfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSx";
    private const string DEFAULT_MSG_ID = "msg_p5jXN8AQM9LWM0D4loKWxJdf";

    internal class TestEntity
    {
        public string Name { get; set; } = default!;

        public int Value { get; set; }

        public string? OptionalValue { get; set; }
    }

    [Fact]
    public void EnsureWebhookContentCanBeCreated()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        var testEntity = new TestEntity { Name = "Test", Value = 1234 };

        var webhookContent = WebhookContent<TestEntity>.Create(testEntity, options);

        Assert.Equal("{\"name\":\"Test\",\"value\":1234}", webhookContent.ToString());
    }

    [Fact]
    public void EnsureCorrectHeadersAdded()
    {

        var testEntity = new TestEntity { Name = "Test", Value = 123456, OptionalValue = "Mary had a little lamb" };
        var jsonContent = "{\"name\":\"Test\",\"value\":123456,\"optionalValue\":\"Mary had a little lamb\"}";

        var webhook = new StandardWebhook(TEST_SECRET);

        var sendingTime = DateTimeOffset.UtcNow;

        var webhookContent = webhook.MakeHttpContent(testEntity, DEFAULT_MSG_ID, sendingTime);

        var headers = (webhookContent as WebhookContent<TestEntity>)?.Headers!;

        Assert.Equal(DEFAULT_MSG_ID, headers.GetValues(TestPayload.UNBRANDED_ID_HEADER_KEY).First());
        Assert.Equal(sendingTime.ToUnixTimeSeconds().ToString(), headers.GetValues(TestPayload.UNBRANDED_TIMESTAMP_HEADER_KEY).First());
        
        var actualSignature = headers.GetValues(TestPayload.UNBRANDED_SIGNATURE_HEADER_KEY).First();
        var expectedSignature = webhook.Sign(DEFAULT_MSG_ID, sendingTime, jsonContent);

        Assert.Equal(expectedSignature, actualSignature);
    }
}
