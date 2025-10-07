using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class PokemonManagementServiceTests
{
    private readonly PKHeXService _service;

    public PokemonManagementServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public void GetPokemonData_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetPokemonData(0, 0, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPokemonData_WithValidSession_ReturnsEmptyOrPokemonInfo()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetPokemonData(0, 0, sessionId);
        var json = JObject.Parse(result);
        
        var isEmpty = json["empty"]?.Value<bool>() ?? false;
        var hasSuccess = json["success"]?.Value<bool>() ?? false;
        
        Assert.True(isEmpty || hasSuccess);
    }

    [Fact]
    public void GetAllPokemon_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetAllPokemon("non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAllPokemon_WithValidSession_ReturnsList()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetAllPokemon(sessionId);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["pokemon"]);
    }

    [Fact]
    public void SetPokemonData_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetPokemonData(0, 0, "base64data", "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ModifyPokemon_WithInvalidSessionId_ReturnsError()
    {
        var modifications = new JObject();
        var result = _service.ModifyPokemon(0, 0, modifications, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeletePokemon_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.DeletePokemon(0, 0, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MovePokemon_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.MovePokemon(0, 0, 1, 0, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckLegality_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.CheckLegality(0, 0, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegalizePokemon_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.LegalizePokemon(0, 0, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImportShowdown_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.ImportShowdown(0, 0, "Pikachu", "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportShowdown_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.ExportShowdown(0, 0, "non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRibbons_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetRibbons(sessionId, 0, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("No Pokémon", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["ribbons"]);
            Assert.NotNull(json["ribbonCount"]);
        }
    }

    [Fact]
    public void GetRibbons_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetRibbons("invalid-session", 0, 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetRibbon_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetRibbon(sessionId, 0, 0, "RibbonChampionG3", true);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("No Pokémon", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("not found", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["ribbonName"]);
            Assert.NotNull(json["value"]);
        }
    }

    [Fact]
    public void SetRibbon_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetRibbon("invalid-session", 0, 0, "RibbonChampionG3", true);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRibbonCount_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetRibbonCount(sessionId, 0, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("No Pokémon", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["ribbonCount"]);
        }
    }

    [Fact]
    public void GetRibbonCount_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetRibbonCount("invalid-session", 0, 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetContestStats_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetContestStats(sessionId, 0, 0);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("No Pokémon", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["contestStats"]);
        }
    }

    [Fact]
    public void GetContestStats_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetContestStats("invalid-session", 0, 0);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetContestStat_WithValidSession_Works()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetContestStat(sessionId, 0, 0, "cool", 100);
        var json = JObject.Parse(result);
        
        if (json["error"] != null)
        {
            Assert.True(
                json["error"]?.ToString().Contains("not supported", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("No Pokémon", StringComparison.OrdinalIgnoreCase) == true ||
                json["error"]?.ToString().Contains("Invalid", StringComparison.OrdinalIgnoreCase) == true
            );
        }
        else
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["stat"]);
            Assert.NotNull(json["value"]);
        }
    }

    [Fact]
    public void SetContestStat_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetContestStat("invalid-session", 0, 0, "cool", 100);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckLegality_WithValidPokemon_ReturnsLegalityCheck()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.CheckLegality(0, 0, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] == null)
        {
            Assert.NotNull(json["valid"]);
            Assert.NotNull(json["parsed"]);
            Assert.NotNull(json["report"]);
        }
        else
        {
            Assert.Contains("No Pokemon", json["error"]?.ToString());
        }
    }

    [Fact]
    public async Task CheckLegality_WithInvalidSlot_ReturnsError()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.CheckLegality(0, 999, sessionId);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
    }

    [Fact]
    public async Task LegalizePokemon_WithValidPokemon_NormalizesPokemon()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.LegalizePokemon(0, 0, sessionId);
        var json = JObject.Parse(result);
        
        if (json["error"] == null)
        {
            Assert.True(json["success"]?.Value<bool>());
            Assert.NotNull(json["message"]);
            Assert.NotNull(json["valid"]);
            Assert.NotNull(json["report"]);
        }
        else
        {
            Assert.Contains("No Pokemon", json["error"]?.ToString());
        }
    }
}
