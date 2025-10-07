using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class PokemonStorageServiceTests
{
    private readonly PKHeXService _service;

    public PokemonStorageServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task GetBoxNames_WithValidSession_ReturnsBoxNames()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetBoxNames(sessionId);
        var json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["boxNames"]);
        Assert.True(json["boxNames"]?.ToObject<List<string>>()?.Count > 0);
    }

    [Fact]
    public void GetBoxNames_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetBoxNames("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBoxWallpapers_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetBoxWallpapers(sessionId);
        var json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["wallpapers"]);
    }

    [Fact]
    public void GetBoxWallpapers_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetBoxWallpapers("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetBoxWallpaper_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetBoxWallpaper(0, 1, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.Equal(0, json["box"]?.Value<int>());
            Assert.Equal(1, json["wallpaper"]?.Value<int>());
        }
    }

    [Fact]
    public void SetBoxWallpaper_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetBoxWallpaper(0, 1, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetBoxWallpaper_WithInvalidBox_ReturnsError()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetBoxWallpaper(999, 1, sessionId);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
    }

    [Fact]
    public async Task GetBattleBox_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetBattleBox(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotNull(json["battleBoxCount"]);
            Assert.NotNull(json["pokemon"]);
            Assert.NotNull(json["generation"]);
        }
    }

    [Fact]
    public void GetBattleBox_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetBattleBox("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetBattleBoxSlot_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var dummyBase64 = Convert.ToBase64String(new byte[136]);
        var result = _service.SetBattleBoxSlot(sessionId, 0, dummyBase64);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Unable to parse", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["slot"]);
        }
    }

    [Fact]
    public void SetBattleBoxSlot_WithInvalidSessionId_ReturnsError()
    {
        var dummyBase64 = Convert.ToBase64String(new byte[136]);
        var result = _service.SetBattleBoxSlot("invalid-session", 0, dummyBase64);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetBattleBoxSlot_WithInvalidSlot_ReturnsError()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var dummyBase64 = Convert.ToBase64String(new byte[136]);
        var result = _service.SetBattleBoxSlot(sessionId, 999, dummyBase64);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
    }

    [Fact]
    public async Task GetDaycare_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetDaycare(0, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotNull(json["location"]);
            Assert.NotNull(json["generation"]);
            Assert.NotNull(json["slot0Occupied"]);
            Assert.NotNull(json["slot1Occupied"]);
            Assert.NotNull(json["hasEgg"]);
        }
    }

    [Fact]
    public void GetDaycare_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetDaycare(0, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDaycare_WithDifferentLocation_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetDaycare(1, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Equal(1, json["location"]?.Value<int>());
        }
    }
}
