// Copyright (c) 2024, Codefactors Ltd.
//
// Codefactors Ltd licenses this file to you under the following license(s):
//
//   * The MIT License, see https://opensource.org/license/mit/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StandardWebhooks.Tests;

public class WebhookContentTests
{
    internal class TestEntity
    {
        public string Name { get; set; }

        public int Value { get; set; }
    }

    [Fact]
    public void EnsureWebhookContentCanBeCreated()
    {
        var testEntity = new TestEntity { Name = "Test", Value = 1234 };

        var webhookContent = WebhookContent<TestEntity>.Create(testEntity);

        Assert.Equal("{\"name\":\"Test\",\"value\":1234}", webhookContent.ToString());
    }
}
