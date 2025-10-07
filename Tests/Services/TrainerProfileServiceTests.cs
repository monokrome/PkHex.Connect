using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class TrainerProfileServiceTests
{
    private readonly PKHeXService _service;

    public TrainerProfileServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task HallOfFame_SecondsToStart_And_SecondsToFame_APIs_Work()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);

        var getSecondsToStartResult = _service.GetSecondsToStart(sessionId);
        var getSecondsToStartJson = JObject.Parse(getSecondsToStartResult);
        
        Assert.NotNull(getSecondsToStartJson["secondsToStart"]);
        Assert.NotNull(getSecondsToStartJson["timeDisplay"]);

        var setSecondsToStartResult = _service.SetSecondsToStart(sessionId, 7200);
        var setSecondsToStartJson = JObject.Parse(setSecondsToStartResult);
        
        Assert.True(setSecondsToStartJson["success"]?.Value<bool>());
        Assert.Equal(7200u, setSecondsToStartJson["secondsToStart"]?.Value<uint>());
        Assert.NotNull(setSecondsToStartJson["timeDisplay"]);
        var timeDisplay = setSecondsToStartJson["timeDisplay"]?.ToString();
        Assert.NotNull(timeDisplay);
        Assert.Contains("2h", timeDisplay);

        var getSecondsToFameResult = _service.GetSecondsToFame(sessionId);
        var getSecondsToFameJson = JObject.Parse(getSecondsToFameResult);
        
        Assert.NotNull(getSecondsToFameJson["secondsToFame"]);
        Assert.NotNull(getSecondsToFameJson["timeDisplay"]);

        var setSecondsToFameResult = _service.SetSecondsToFame(sessionId, 14400);
        var setSecondsToFameJson = JObject.Parse(setSecondsToFameResult);
        
        Assert.True(setSecondsToFameJson["success"]?.Value<bool>());
        Assert.Equal(14400u, setSecondsToFameJson["secondsToFame"]?.Value<uint>());
        Assert.NotNull(setSecondsToFameJson["timeDisplay"]);
        var timeDisplayFame = setSecondsToFameJson["timeDisplay"]?.ToString();
        Assert.NotNull(timeDisplayFame);
        Assert.Contains("4h", timeDisplayFame);
    }

    [Fact]
    public void SetTrainerInfo_WithInvalidSessionId_ReturnsError()
    {
        var trainerData = new JObject();
        var result = _service.SetTrainerInfo(trainerData, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBoxNames_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetBoxNames("non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTrainerAppearance_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetTrainerAppearance(sessionId);
        var json = JObject.Parse(result);
        
        Assert.True(json["success"]?.Value<bool>());
        Assert.NotNull(json["appearance"]);
        
        var appearance = json["appearance"] as JObject;
        Assert.NotNull(appearance);
        Assert.NotNull(appearance["generation"]);
        Assert.NotNull(appearance["supported"]);
    }

    [Fact]
    public void GetTrainerAppearance_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetTrainerAppearance("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetTrainerAppearance_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var appearanceData = new JObject { ["skin"] = 1 };
        var result = _service.SetTrainerAppearance(sessionId, appearanceData);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["generation"]);
        }
    }

    [Fact]
    public void SetTrainerAppearance_WithInvalidSessionId_ReturnsError()
    {
        var appearanceData = new JObject { ["skin"] = 1 };
        var result = _service.SetTrainerAppearance("invalid-session", appearanceData);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTrainerCard_WithValidSession_ReturnsCardInfo()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetTrainerCard(sessionId);
        var json = JObject.Parse(result);
        
        Assert.True(json["success"]?.Value<bool>());
        Assert.NotNull(json["trainerCard"]);
        
        var card = json["trainerCard"] as JObject;
        Assert.NotNull(card);
        Assert.NotNull(card["trainerName"]);
        Assert.NotNull(card["trainerId"]);
        Assert.NotNull(card["generation"]);
        Assert.NotNull(card["playTime"]);
    }

    [Fact]
    public void GetTrainerCard_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetTrainerCard("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
