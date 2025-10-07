using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class GameKnowledgeServiceTests
{
    private readonly PKHeXService _service;

    public GameKnowledgeServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task ItemsAPI_FullWorkflow_WorksCorrectly()
    {
        var projectRoot = Directory.GetCurrentDirectory();
        while (projectRoot != null && !File.Exists(Path.Combine(projectRoot, "PKHeXWebSocketServer.sln")))
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName;
        }
        if (projectRoot == null) throw new Exception("Could not find project root");
        
        var saveFilePath = Path.Combine(projectRoot, "share", "saves", "emerald.sav");
        var saveData = await File.ReadAllBytesAsync(saveFilePath);
        var base64Data = Convert.ToBase64String(saveData);
        
        var loadResult = await _service.LoadSaveFileAsync(base64Data);
        var loadJson = JObject.Parse(loadResult);
        var sessionId = loadJson["sessionId"]?.ToString();
        Assert.NotNull(sessionId);

        var giveMasterBallResult = _service.AddItemToPouch(1, 5, 2, sessionId);
        var giveMasterBallJson = JObject.Parse(giveMasterBallResult);
        Assert.True(giveMasterBallJson["success"]?.Value<bool>());

        var getItemsResult = _service.GetPouchItems(sessionId);
        var getItemsJson = JObject.Parse(getItemsResult);
        
        var pouches = getItemsJson["pouches"]?.ToObject<List<JObject>>();
        Assert.NotNull(pouches);
        
        var ballsPouchResult = pouches.FirstOrDefault(p => p["pouchIndex"]?.Value<int>() == 2);
        Assert.NotNull(ballsPouchResult);
        
        var items = ballsPouchResult["items"]?.ToObject<List<JObject>>();
        Assert.NotNull(items);
        var masterBall = items.FirstOrDefault(i => i["itemId"]?.Value<int>() == 1);
        Assert.NotNull(masterBall);
        Assert.Equal(5, masterBall["count"]?.Value<int>());

        var giveItemResult = _service.AddItemToPouch(13, 10, null, sessionId);
        var giveItemJson = JObject.Parse(giveItemResult);
        
        Assert.True(giveItemJson["success"]?.Value<bool>());
        Assert.Equal(13, giveItemJson["itemId"]?.Value<int>());
        Assert.Equal(10, giveItemJson["count"]?.Value<int>());

        getItemsResult = _service.GetPouchItems(sessionId);
        getItemsJson = JObject.Parse(getItemsResult);
        pouches = getItemsJson["pouches"]?.ToObject<List<JObject>>();
        
        var itemsPouch = pouches.FirstOrDefault(p => p["items"]?.ToObject<List<JObject>>()?.Any(i => i["itemId"]?.Value<int>() == 13) == true);
        Assert.NotNull(itemsPouch);
        var potionItem = itemsPouch["items"]?.ToObject<List<JObject>>()?.FirstOrDefault(i => i["itemId"]?.Value<int>() == 13);
        Assert.NotNull(potionItem);
        Assert.Equal(10, potionItem["count"]?.Value<int>());

        var removeItemResult = _service.RemoveItemFromPouch(1, 3, sessionId);
        var removeItemJson = JObject.Parse(removeItemResult);
        
        Assert.True(removeItemJson["success"]?.Value<bool>());
        Assert.Equal(1, removeItemJson["itemId"]?.Value<int>());
        Assert.Equal(2, removeItemJson["count"]?.Value<int>());

        getItemsResult = _service.GetPouchItems(sessionId);
        getItemsJson = JObject.Parse(getItemsResult);
        pouches = getItemsJson["pouches"]?.ToObject<List<JObject>>();
        ballsPouchResult = pouches.FirstOrDefault(p => p["pouchIndex"]?.Value<int>() == 2);
        items = ballsPouchResult["items"]?.ToObject<List<JObject>>();
        masterBall = items.FirstOrDefault(i => i["itemId"]?.Value<int>() == 1);
        Assert.NotNull(masterBall);
        Assert.Equal(2, masterBall["count"]?.Value<int>());

        removeItemResult = _service.RemoveItemFromPouch(1, 10, sessionId);
        removeItemJson = JObject.Parse(removeItemResult);
        
        Assert.True(removeItemJson["success"]?.Value<bool>());
        Assert.Contains("Removed item", removeItemJson["message"]?.ToString());

        getItemsResult = _service.GetPouchItems(sessionId);
        getItemsJson = JObject.Parse(getItemsResult);
        pouches = getItemsJson["pouches"]?.ToObject<List<JObject>>();
        ballsPouchResult = pouches.FirstOrDefault(p => p["pouchIndex"]?.Value<int>() == 2);
        items = ballsPouchResult["items"]?.ToObject<List<JObject>>();
        masterBall = items.FirstOrDefault(i => i["itemId"]?.Value<int>() == 1);
        Assert.Null(masterBall);
    }

    [Fact]
    public async Task GetPokedex_WithValidSession_ReturnsPokedexData()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetPokedex(sessionId);
        var json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["pokedex"]);
        var pokedex = json["pokedex"]?.ToObject<List<JObject>>();
        Assert.NotNull(pokedex);
        Assert.True(pokedex.Count > 0);
        
        var firstEntry = pokedex[0];
        Assert.NotNull(firstEntry["species"]);
        Assert.NotNull(firstEntry["caught"]);
        Assert.NotNull(firstEntry["seen"]);
    }

    [Fact]
    public void GetPokedex_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetPokedex("invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetPokedexCaught_WithValidSpecies_SetsStatus()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetPokedexCaught(25, true, 0, sessionId);
        var json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.True(json["success"]?.Value<bool>());
        Assert.Equal(25, json["species"]?.Value<int>());
        Assert.True(json["caught"]?.Value<bool>());
    }

    [Fact]
    public void SetPokedexCaught_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetPokedexCaught(25, true, 0, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetPokedexCaught_WithInvalidSpecies_ReturnsError()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetPokedexCaught(99999, true, 0, sessionId);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("Invalid species", json["error"]?.ToString());
    }

    [Fact]
    public async Task SetPokedexSeen_WithValidSpecies_SetsStatus()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetPokedexSeen(1, true, 0, sessionId);
        var json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.True(json["success"]?.Value<bool>());
        Assert.Equal(1, json["species"]?.Value<int>());
        Assert.True(json["seen"]?.Value<bool>());
    }

    [Fact]
    public void SetPokedexSeen_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SetPokedexSeen(1, true, 0, "invalid-session");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetPokedexSeen_WithInvalidSpecies_ReturnsError()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SetPokedexSeen(-1, true, 0, sessionId);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("Invalid species", json["error"]?.ToString());
    }

    [Fact]
    public void GameDataLookup_APIs_WorkCorrectly()
    {
        var result = _service.GetSpeciesName(25);
        var json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["name"]);
        Assert.NotEmpty(json["name"]?.ToString());
        Assert.Equal(25, json["id"]?.Value<int>());
        
        result = _service.GetMoveName(85);
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["name"]);
        Assert.NotEmpty(json["name"]?.ToString());
        Assert.Equal(85, json["id"]?.Value<int>());
        
        result = _service.GetAbilityName(26);
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["name"]);
        Assert.NotEmpty(json["name"]?.ToString());
        Assert.Equal(26, json["id"]?.Value<int>());
        
        result = _service.GetItemName(1);
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["name"]);
        Assert.NotEmpty(json["name"]?.ToString());
        Assert.Equal(1, json["id"]?.Value<int>());
        
        result = _service.GetNatureName(0);
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["name"]);
        Assert.NotEmpty(json["name"]?.ToString());
        Assert.Equal(0, json["id"]?.Value<int>());
        
        result = _service.GetTypeName(4);
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["name"]);
        Assert.NotEmpty(json["name"]?.ToString());
        Assert.Equal(4, json["id"]?.Value<int>());
        
        result = _service.GetAllSpecies();
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["species"]);
        Assert.True(json["count"]?.Value<int>() > 0);
        
        result = _service.GetAllMoves();
        json = JObject.Parse(result);
        
        Assert.Null(json["error"]);
        Assert.NotNull(json["moves"]);
        Assert.True(json["count"]?.Value<int>() > 0);
        
        result = _service.GetSpeciesName(99999);
        json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Null(json["id"]);
        Assert.Null(json["name"]);
    }
}
