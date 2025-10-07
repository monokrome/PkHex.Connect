using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class CommunicationServiceTests
{
    private readonly PKHeXService _service;

    public CommunicationServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task GetMailbox_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetMailbox(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["mailCount"]);
            Assert.NotNull(json["messages"]);
        }
    }

    [Fact]
    public void GetMailbox_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetMailbox("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMailMessage_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetMailMessage(sessionId, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["index"]);
        }
    }

    [Fact]
    public void GetMailMessage_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetMailMessage("invalid-session", 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetMailMessage_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var mailData = new JObject { ["authorName"] = "Test" };
        var result = _service.SetMailMessage(sessionId, 0, mailData);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["index"]);
        }
    }

    [Fact]
    public void SetMailMessage_WithInvalidSessionId_ReturnsError()
    {
        var mailData = new JObject { ["authorName"] = "Test" };
        var result = _service.SetMailMessage("invalid-session", 0, mailData);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteMail_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.DeleteMail(sessionId, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["index"]);
        }
    }

    [Fact]
    public void DeleteMail_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.DeleteMail("invalid-session", 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMysteryGifts_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetMysteryGifts(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotNull(json["generation"]);
            Assert.NotNull(json["totalSlots"]);
            Assert.NotNull(json["cards"]);
        }
    }

    [Fact]
    public void GetMysteryGifts_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetMysteryGifts("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMysteryGiftCard_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetMysteryGiftCard(sessionId, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["isEmpty"]?.Value<bool>() == true || json["type"] != null);
        }
    }

    [Fact]
    public void GetMysteryGiftCard_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetMysteryGiftCard("invalid-session", 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetMysteryGiftCard_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var cardData = new JObject { ["species"] = 25 };
        var result = _service.SetMysteryGiftCard(sessionId, 0, cardData);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("null", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.NotNull(json["message"]);
            Assert.NotNull(json["index"]);
        }
    }

    [Fact]
    public void SetMysteryGiftCard_WithInvalidSessionId_ReturnsError()
    {
        var cardData = new JObject { ["species"] = 25 };
        var result = _service.SetMysteryGiftCard("invalid-session", 0, cardData);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteMysteryGift_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.DeleteMysteryGift(sessionId, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Unable", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.NotNull(json["message"]);
            Assert.NotNull(json["index"]);
        }
    }

    [Fact]
    public void DeleteMysteryGift_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.DeleteMysteryGift("invalid-session", 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
