// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

using Microsoft.AspNetCore.Http;

namespace StandardWebhooks.Tests;

internal class TestPayload
{
    internal const string UNBRANDED_ID_HEADER_KEY = "webhook-id";
    internal const string UNBRANDED_SIGNATURE_HEADER_KEY = "webhook-signature";
    internal const string UNBRANDED_TIMESTAMP_HEADER_KEY = "webhook-timestamp";

    private const string DEFAULT_MSG_ID = "msg_p5jXN8AQM9LWM0D4loKWxJek";
    private const string DEFAULT_PAYLOAD = "{\"test\": 2432232314}";
    private const string DEFAULT_SECRET = "MfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSw";

    public string Id { get; }

    public DateTimeOffset Timestamp { get; }

    public HeaderDictionary Headers { get; set; }

    public string Secret { get; }

    public string Payload { get; }

    public TestPayload(DateTimeOffset timestamp)
    {
        Id = DEFAULT_MSG_ID;
        this.Timestamp = timestamp;

        Payload = DEFAULT_PAYLOAD;
        Secret = DEFAULT_SECRET;

        StandardWebhook wh = new StandardWebhook(Secret);

        var signature = wh.Sign(Id, this.Timestamp, Payload);

        Headers = new HeaderDictionary();

        Headers.Add(UNBRANDED_ID_HEADER_KEY, Id);
        Headers.Add(UNBRANDED_SIGNATURE_HEADER_KEY, signature);
        Headers.Add(UNBRANDED_TIMESTAMP_HEADER_KEY, timestamp.ToUnixTimeSeconds().ToString());
    }
}
