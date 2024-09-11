// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

// Portions Copyright (c) 2023 Svix (https://www.svix.com) used under MIT licence,
// see https://github.com/standard-webhooks/standard-webhooks/blob/main/libraries/LICENSE.

using Microsoft.AspNetCore.Http;
using StandardWebhooks.Diagnostics;
using System.Net;

namespace StandardWebhooks.Tests;

public class WebhookTests
{
    public const int TOLERANCE_IN_SECONDS = 5 * 60;

    [Fact]
    public void TestMissingIdRasiesException()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);
        testPayload.Headers.Remove(TestPayload.UNBRANDED_ID_HEADER_KEY);

        var wh = new StandardWebhook(testPayload.Secret);

        Assert.Throws<WebhookVerificationException>(() => wh.Verify(testPayload.Payload, testPayload.Headers));
    }

    [Fact]
    public void TestMissingTimestampThrowsException()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);
        testPayload.Headers.Remove(TestPayload.UNBRANDED_TIMESTAMP_HEADER_KEY);

        var wh = new StandardWebhook(testPayload.Secret);

        Assert.Throws<WebhookVerificationException>(() => wh.Verify(testPayload.Payload, testPayload.Headers));
    }

    [Fact]
    public void TestMissingSignatureThrowsException()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);
        testPayload.Headers.Remove(TestPayload.UNBRANDED_SIGNATURE_HEADER_KEY);

        var wh = new StandardWebhook(testPayload.Secret);

        Assert.Throws<WebhookVerificationException>(() => wh.Verify(testPayload.Payload, testPayload.Headers));
    }

    [Fact]
    public void TestInvalidSignatureThrowsException()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);

        testPayload.Headers[TestPayload.UNBRANDED_SIGNATURE_HEADER_KEY] = "v1,g0hM9SsE+OTPJTGt/tmIKtSyZlE3uFJELVlNIOLawdd";

        var wh = new StandardWebhook(testPayload.Secret);

        Assert.Throws<WebhookVerificationException>(() => wh.Verify(testPayload.Payload, testPayload.Headers));
    }

    [Fact]
    public void TestValidSignatureIsValid()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);

        var wh = new StandardWebhook(testPayload.Secret);

        wh.Verify(testPayload.Payload, testPayload.Headers);
    }

    [Fact]
    public void TestUnbrandedSignatureIsValid()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);
        
        HeaderDictionary unbrandedHeaders = new HeaderDictionary();
        
        unbrandedHeaders.Add("webhook-id", testPayload.Headers[TestPayload.UNBRANDED_ID_HEADER_KEY]);
        unbrandedHeaders.Add("webhook-signature", testPayload.Headers[TestPayload.UNBRANDED_SIGNATURE_HEADER_KEY]);
        unbrandedHeaders.Add("webhook-timestamp", testPayload.Headers[TestPayload.UNBRANDED_TIMESTAMP_HEADER_KEY]);
        
        testPayload.Headers = unbrandedHeaders;

        var wh = new StandardWebhook(testPayload.Secret);

        wh.Verify(testPayload.Payload, testPayload.Headers);
    }

    [Fact]
    public void TestOldTimestampThrowsException()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow.AddSeconds(-1 * (TOLERANCE_IN_SECONDS + 1)));

        var wh = new StandardWebhook(testPayload.Secret);

        Assert.Throws<WebhookVerificationException>(() => wh.Verify(testPayload.Payload, testPayload.Headers));
    }

    [Fact]
    public void TestNewTimestampThrowsException()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow.AddSeconds(TOLERANCE_IN_SECONDS + 1));

        var wh = new StandardWebhook(testPayload.Secret);

        Assert.Throws<WebhookVerificationException>(() => wh.Verify(testPayload.Payload, testPayload.Headers));
    }

    [Fact]
    public void TestMultiSigPayloadIsValid()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);

        string[] sigs = new string[] {
            "v1,Ceo5qEr07ixe2NLpvHk3FH9bwy/WavXrAFQ/9tdO6mc=",
            "v2,Ceo5qEr07ixe2NLpvHk3FH9bwy/WavXrAFQ/9tdO6mc=",
            testPayload.Headers[TestPayload.UNBRANDED_SIGNATURE_HEADER_KEY]!, // valid signature
            "v1,Ceo5qEr07ixe2NLpvHk3FH9bwy/WavXrAFQ/9tdO6mc=",
        };

        testPayload.Headers[TestPayload.UNBRANDED_SIGNATURE_HEADER_KEY] = String.Join(" ", sigs);

        var wh = new StandardWebhook(testPayload.Secret);

        wh.Verify(testPayload.Payload, testPayload.Headers);
    }

    [Fact]
    public void TestSignatureVerificationWorksWithoutPrefix()
    {
        var testPayload = new TestPayload(DateTimeOffset.UtcNow);

        var wh = new StandardWebhook(testPayload.Secret);
        wh.Verify(testPayload.Payload, testPayload.Headers);

        wh = new StandardWebhook("whsec_" + testPayload.Secret);
        wh.Verify(testPayload.Payload, testPayload.Headers);
    }

    [Fact]
    public void VerifyWebhookSignWorks()
    {
        var key = "whsec_MfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSw";
        var msgId = "msg_p5jXN8AQM9LWM0D4loKWxJek";
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(1614265330);
        var payload = "{\"test\": 2432232314}";
        var expected = "v1,g0hM9SsE+OTPJTGt/tmIKtSyZlE3uFJELVlNIOLJ1OE=";

        var wh = new StandardWebhook(key);
        var signature = wh.Sign(msgId, timestamp, payload);
        Assert.Equal(signature, expected);
    }
}