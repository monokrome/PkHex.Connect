using PKHeXWebSocketServer;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Tests.TestHelpers;

public static class BaseTestHelper
{
    public static async Task<string> LoadTestSaveAsync(PKHeXService service)
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
        
        var result = await service.LoadSaveFileAsync(base64Data);
        var json = JObject.Parse(result);
        
        return json["sessionId"]?.ToString() ?? throw new Exception("Failed to load test save");
    }
}
