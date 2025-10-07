using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class ProgressionServiceTests
{
    private readonly PKHeXService _service;

    public ProgressionServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task GetEventFlag_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetEventFlag(0, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotNull(json["flagIndex"]);
            Assert.NotNull(json["value"]);
            Assert.NotNull(json["generation"]);
        }
    }

    [Fact]
    public void GetEventFlag_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetEventFlag(0, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetEventFlag_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetEventFlag(0, true, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.Equal(0, json["flagIndex"]?.Value<int>());
            Assert.True(json["value"]?.Value<bool>());
        }
    }

    [Fact]
    public void SetEventFlag_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetEventFlag(0, true, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEventConst_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetEventConst(0, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotNull(json["constIndex"]);
            Assert.NotNull(json["value"]);
            Assert.NotNull(json["generation"]);
        }
    }

    [Fact]
    public void GetEventConst_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetEventConst(0, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetEventConst_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetEventConst(0, 100, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.Equal(0, json["constIndex"]?.Value<int>());
            Assert.Equal(100, json["value"]?.Value<int>());
        }
    }

    [Fact]
    public void SetEventConst_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetEventConst(0, 100, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHallOfFame_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetHallOfFame(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["entryCount"]);
            Assert.NotNull(json["entries"]);
        }
    }

    [Fact]
    public void GetHallOfFame_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetHallOfFame("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetHallOfFameEntry_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var team = new JArray
        {
            new JObject { ["species"] = 25, ["level"] = 50, ["nickname"] = "Pikachu" }
        };
        
        var result = _service.SetHallOfFameEntry(sessionId, 0, team);
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
            Assert.NotNull(json["teamSize"]);
        }
    }

    [Fact]
    public void SetHallOfFameEntry_WithInvalidSessionId_ReturnsError()
    {
        var team = new JArray
        {
            new JObject { ["species"] = 25, ["level"] = 50 }
        };
        
        var result = _service.SetHallOfFameEntry("invalid-session", 0, team);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetBattleFacilityStats_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetBattleFacilityStats(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.Contains("not supported", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["battleFacilityStats"]);
            Assert.NotNull(json["count"]);
        }
    }

    [Fact]
    public void GetBattleFacilityStats_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetBattleFacilityStats("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Records_APIs_Work_For_Supported_Generations()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);

        var getRecordsResult = _service.GetRecords(sessionId);
        var getRecordsJson = JObject.Parse(getRecordsResult);

        if (getRecordsJson["error"] != null)
        {
            Assert.NotNull(getRecordsJson["error"]);
            Assert.Contains("not supported", getRecordsJson["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.NotNull(getRecordsJson["recordCount"]);
            Assert.True(getRecordsJson["recordCount"]?.Value<int>() > 0);
            Assert.NotNull(getRecordsJson["records"]);
            
            var getRecordValueResult = _service.GetRecordValue(sessionId, 0);
            var getRecordValueJson = JObject.Parse(getRecordValueResult);
            
            Assert.NotNull(getRecordValueJson["index"]);
            Assert.Equal(0, getRecordValueJson["index"]?.Value<int>());
            Assert.NotNull(getRecordValueJson["value"]);
            Assert.NotNull(getRecordValueJson["max"]);

            var setRecordResult = _service.SetRecord(sessionId, 0, 100);
            var setRecordJson = JObject.Parse(setRecordResult);
            
            Assert.True(setRecordJson["success"]?.Value<bool>());
            Assert.Equal(0, setRecordJson["index"]?.Value<int>());
            Assert.Equal(100, setRecordJson["value"]?.Value<int>());
        }
    }
}
