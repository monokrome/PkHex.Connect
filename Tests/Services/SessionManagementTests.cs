using Xunit;
using PKHeXWebSocketServer;
using PKHeXWebSocketServer.Tests.TestHelpers;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.Services;

public class SessionManagementTests
{
    private readonly PKHeXService _service;

    public SessionManagementTests()
    {
        _service = new PKHeXService();
    }

    [Fact]
    public void ListSessions_WithNoSaves_ReturnsEmptyList()
    {
        var freshService = new PKHeXService();
        var result = freshService.ListSessions();
        var json = JObject.Parse(result);
        
        var sessionIds = json["sessionIds"]?.ToObject<List<string>>();
        
        Assert.NotNull(sessionIds);
        Assert.Empty(sessionIds);
        Assert.Equal(0, json["count"]?.Value<int>());
    }

    [Fact]
    public void UnloadSession_WithInvalidSessionId_ReturnsError()
    {
        var result = _service.UnloadSession("non-existent-session-id");
        var json = JObject.Parse(result);
        
        Assert.NotNull(json["error"]);
    }

    [Fact]
    public async Task SessionWorkflow_LoadListUnload_WorksCorrectly()
    {
        var sessionId = await BaseTestHelper.LoadTestSaveAsync(_service);
        
        var listResult = _service.ListSessions();
        var listJson = JObject.Parse(listResult);
        var sessionIds = listJson["sessionIds"]?.ToObject<List<string>>();
        
        Assert.NotNull(sessionIds);
        Assert.Contains(sessionId, sessionIds);
        
        var unloadResult = _service.UnloadSession(sessionId);
        var unloadJson = JObject.Parse(unloadResult);
        
        Assert.True(unloadJson["success"]?.Value<bool>());
        
        var listAfterUnload = _service.ListSessions();
        var listAfterJson = JObject.Parse(listAfterUnload);
        var sessionIdsAfter = listAfterJson["sessionIds"]?.ToObject<List<string>>();
        
        Assert.NotNull(sessionIdsAfter);
        Assert.DoesNotContain(sessionId, sessionIdsAfter);
    }
}
