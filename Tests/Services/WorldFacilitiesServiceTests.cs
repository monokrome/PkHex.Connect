using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class WorldFacilitiesServiceTests
{
    private readonly PKHeXService _service;

    public WorldFacilitiesServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task GetSecretBase_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetSecretBase(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("only supported in Generation", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["generation"]);
        }
    }

    [Fact]
    public void GetSecretBase_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetSecretBase("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEntralinkData_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetEntralinkData(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("only supported in Generation 5", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["generation"]);
            Assert.NotNull(json["entralink"]);
        }
    }

    [Fact]
    public void GetEntralinkData_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetEntralinkData("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetFestivalPlaza_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetFestivalPlaza(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("only supported in Generation 7", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["generation"]);
            Assert.NotNull(json["festivalPlaza"]);
        }
    }

    [Fact]
    public void GetFestivalPlaza_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetFestivalPlaza("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPokePelago_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetPokePelago(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("only supported in Generation 7", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["generation"]);
            Assert.NotNull(json["pokePelago"]);
        }
    }

    [Fact]
    public void GetPokePelago_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetPokePelago("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPokeJobs_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetPokeJobs(sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("only supported in Generation 8", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not available", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["generation"]);
            Assert.NotNull(json["pokeJobs"]);
        }
    }

    [Fact]
    public void GetPokeJobs_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetPokeJobs("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
