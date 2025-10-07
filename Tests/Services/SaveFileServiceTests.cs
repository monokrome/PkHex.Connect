using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class SaveFileServiceTests
{
    private readonly PKHeXService _service;

    public SaveFileServiceTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public async Task LoadSaveFileAsync_WithInvalidBase64_ReturnsError()
    {
        var invalidBase64 = "not-valid-base64!!!";
        
        var result = await _service.LoadSaveFileAsync(invalidBase64);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Null(json["sessionId"]);
    }

    [Fact]
    public async Task LoadSaveFileAsync_WithEmptyData_ReturnsError()
    {
        var result = await _service.LoadSaveFileAsync("");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Null(json["sessionId"]);
    }

    [Fact]
    public async Task LoadSaveFileAsync_WithInvalidSaveData_ReturnsError()
    {
        var invalidSaveData = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        
        var result = await _service.LoadSaveFileAsync(invalidSaveData);
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Null(json["sessionId"]);
    }

    [Fact]
    public async Task LoadSaveFileAsync_WithValidSave_ReturnsSessionId()
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
        
        var result = await _service.LoadSaveFileAsync(base64Data);
        var json = JObject.Parse(result);
        
        Assert.True(json["success"]?.Value<bool>());
        Assert.NotNull(json["sessionId"]);
        Assert.NotNull(json["game"]);
        Assert.Equal("E", json["game"]?.ToString());
    }

    [Fact]
    public async Task LoadSaveFileAsync_GeneratesUniqueSessionIds()
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
        
        var result1 = await _service.LoadSaveFileAsync(base64Data);
        var result2 = await _service.LoadSaveFileAsync(base64Data);
        
        var json1 = JObject.Parse(result1);
        var json2 = JObject.Parse(result2);
        
        var sessionId1 = json1["sessionId"]?.ToString();
        var sessionId2 = json2["sessionId"]?.ToString();
        
        Assert.NotNull(sessionId1);
        Assert.NotNull(sessionId2);
        Assert.NotEqual(sessionId1, sessionId2);
    }

    [Fact]
    public async Task GetSaveInfo_WithValidSession_ReturnsInfo()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.GetSaveInfo(sessionId);
        var json = JObject.Parse(result);
        
        Assert.Equal("E", json["game"]?.ToString());
        Assert.NotNull(json["trainerName"]);
        Assert.NotNull(json["trainerID"]);
        Assert.NotNull(json["boxCount"]);
    }

    [Fact]
    public void GetSaveInfo_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.GetSaveInfo("non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveSaveFile_WithValidSession_ReturnsBase64Data()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var result = _service.SaveSaveFile(sessionId);
        var json = JObject.Parse(result);
        
        Assert.True(json["success"]?.Value<bool>());
        Assert.NotNull(json["data"]);
        
        var base64Data = json["data"]?.ToString();
        Assert.NotNull(base64Data);
        Assert.NotEmpty(base64Data);
        
        var decodedData = Convert.FromBase64String(base64Data);
        Assert.True(decodedData.Length > 0);
    }

    [Fact]
    public void SaveSaveFile_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.SaveSaveFile("non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
        Assert.Contains("not found", json["error"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
