using System.Reflection;
using Newtonsoft.Json.Linq;

namespace PKHeXWebSocketServer.Metadata;

public static class HtmlDocumentationGenerator
{
    private static bool CheckIfActionRequiresSession(string actionName)
    {
        if (string.IsNullOrEmpty(actionName))
            return true;

        if (actionName == "listSessions")
            return false;

        var assembly = typeof(PKHeXService).Assembly;
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "PKHeXWebSocketServer.Services" && !t.IsAbstract);

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in methods)
            {
                var apiActionAttr = method.GetCustomAttribute<ApiActionAttribute>();
                if (apiActionAttr != null && apiActionAttr.Name == actionName)
                {
                    return apiActionAttr.RequiresSession;
                }
            }
        }

        return true;
    }

    public static string GenerateHtml()
    {
        var schemaJson = SchemaBuilder.GenerateJsonSchema();
        var schema = JObject.Parse(schemaJson);
        
        var components = schema["components"]?["messages"] as JObject;
        if (components == null) return GenerateErrorHtml("No messages found in schema");

        var actions = new List<ActionDoc>();
        
        foreach (var message in components.Properties())
        {
            var messageObj = message.Value as JObject;
            var payload = messageObj?["payload"] as JObject;
            var properties = payload?["properties"] as JObject;
            
            if (properties == null) continue;

            var actionName = message.Name;
            var description = messageObj?["summary"]?.ToString() ?? "";
            var category = messageObj?["tags"]?[0]?["name"]?.ToString() ?? "General";
            
            var parameters = new List<ParamDoc>();
            var requiredFields = payload?["required"]?.ToObject<List<string>>() ?? new List<string>();
            
            foreach (var prop in properties.Properties())
            {
                if (prop.Name == "action") continue;
                
                var propObj = prop.Value as JObject;
                parameters.Add(new ParamDoc
                {
                    Name = prop.Name,
                    Type = propObj?["type"]?.ToString() ?? "any",
                    Description = propObj?["description"]?.ToString() ?? "",
                    Required = requiredFields.Contains(prop.Name)
                });
            }

            var requiresSession = CheckIfActionRequiresSession(actionName);

            actions.Add(new ActionDoc
            {
                Name = actionName,
                Category = category,
                Description = description,
                Parameters = parameters,
                RequiresSession = requiresSession
            });
        }

        return GenerateHtmlFromActions(actions);
    }

    private static string GenerateHtmlFromActions(List<ActionDoc> actions)
    {
        var categorized = actions.GroupBy(a => a.Category).OrderBy(g => g.Key);
        
        var html = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>PKHeX WebSocket API Documentation</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            line-height: 1.6; 
            color: #333; 
            background: #f5f5f5;
            padding: 20px;
        }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; margin-bottom: 10px; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        .subtitle { color: #7f8c8d; margin-bottom: 30px; }
        h2 { color: #2980b9; margin-top: 40px; margin-bottom: 20px; padding-bottom: 10px; border-bottom: 2px solid #ecf0f1; }
        .action { 
            background: #f8f9fa; 
            border-left: 4px solid #3498db; 
            padding: 20px; 
            margin-bottom: 20px; 
            border-radius: 4px;
        }
        .action-name { 
            font-size: 1.3em; 
            font-weight: bold; 
            color: #2c3e50; 
            margin-bottom: 8px;
            font-family: 'Courier New', monospace;
        }
        .action-desc { color: #555; margin-bottom: 15px; }
        .session-badge { 
            display: inline-block;
            background: #e74c3c; 
            color: white; 
            padding: 2px 8px; 
            border-radius: 3px; 
            font-size: 0.8em; 
            margin-left: 10px;
        }
        .params { margin-top: 15px; }
        .params-title { font-weight: bold; color: #2c3e50; margin-bottom: 10px; }
        .param { 
            background: white; 
            padding: 10px; 
            margin-bottom: 8px; 
            border-radius: 4px;
            border: 1px solid #e0e0e0;
        }
        .param-name { 
            font-family: 'Courier New', monospace; 
            color: #8e44ad; 
            font-weight: bold;
        }
        .param-type { 
            color: #16a085; 
            font-style: italic; 
            margin-left: 8px;
        }
        .param-required { 
            color: #e74c3c; 
            font-size: 0.85em; 
            margin-left: 8px;
        }
        .param-desc { color: #666; margin-top: 4px; font-size: 0.95em; }
        .example { 
            background: #2c3e50; 
            color: #ecf0f1; 
            padding: 15px; 
            border-radius: 4px; 
            margin-top: 15px;
            overflow-x: auto;
        }
        .example-title { color: #3498db; font-weight: bold; margin-bottom: 8px; }
        pre { margin: 0; }
        code { font-family: 'Courier New', monospace; }
        .intro { 
            background: #e8f4f8; 
            padding: 20px; 
            border-radius: 4px; 
            margin-bottom: 30px;
            border-left: 4px solid #3498db;
        }
        .intro ul { margin-left: 20px; margin-top: 10px; }
        .intro li { margin-bottom: 5px; }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>PKHeX WebSocket API Documentation</h1>
        <p class=""subtitle"">Cross-platform WebSocket API for Pokémon save file manipulation</p>
        
        <div class=""intro"">
            <h3>Getting Started</h3>
            <p>Connect to the WebSocket server and send JSON messages with an <code>action</code> field:</p>
            <ul>
                <li><strong>Load a save file:</strong> Send <code>{""action"": ""loadSave"", ""base64Data"": ""...""}</code></li>
                <li><strong>Use the returned sessionId:</strong> Include it in subsequent requests</li>
                <li><strong>Example:</strong> <code>{""action"": ""getPokemon"", ""sessionId"": ""your-session-id"", ""box"": 0, ""slot"": 0}</code></li>
            </ul>
            <p style=""margin-top: 10px;""><strong>API Schema:</strong> Send an OPTIONS request to <code>/</code> for the complete AsyncAPI 2.6.0 schema</p>
        </div>
";

        foreach (var category in categorized)
        {
            html += $"\n        <h2>{category.Key}</h2>\n";
            
            foreach (var action in category.OrderBy(a => a.Name))
            {
                var sessionBadge = action.RequiresSession ? "<span class=\"session-badge\">Requires sessionId</span>" : "";
                html += $@"
        <div class=""action"">
            <div class=""action-name"">{action.Name}{sessionBadge}</div>
            <div class=""action-desc"">{action.Description}</div>";

                if (action.Parameters.Any())
                {
                    html += @"
            <div class=""params"">
                <div class=""params-title"">Parameters:</div>";
                    
                    foreach (var param in action.Parameters)
                    {
                        var required = param.Required ? "<span class=\"param-required\">(required)</span>" : "";
                        var desc = !string.IsNullOrEmpty(param.Description) ? $"<div class=\"param-desc\">{param.Description}</div>" : "";
                        html += $@"
                <div class=""param"">
                    <span class=""param-name"">{param.Name}</span>
                    <span class=""param-type"">{param.Type}</span>{required}{desc}
                </div>";
                    }
                    html += "\n            </div>";
                }

                var exampleParams = string.Join(",\n  ", action.Parameters.Select(p => $"\"{p.Name}\": {GetExampleValue(p.Type)}"));
                if (action.RequiresSession)
                {
                    exampleParams = $"\"sessionId\": \"your-session-id\"" + (exampleParams.Length > 0 ? ",\n  " + exampleParams : "");
                }

                html += $@"
            <div class=""example"">
                <div class=""example-title"">Request Example:</div>
                <pre><code>{{
  ""action"": ""{action.Name}""{(exampleParams.Length > 0 ? ",\n  " + exampleParams : "")}
}}</code></pre>
            </div>
        </div>";
            }
        }

        html += @"
    </div>
</body>
</html>";

        return html;
    }

    private static string GetExampleValue(string type)
    {
        return type switch
        {
            "string" => "\"example\"",
            "integer" => "0",
            "number" => "0",
            "boolean" => "true",
            "object" => "{}",
            "array" => "[]",
            _ => "null"
        };
    }

    private static string GenerateErrorHtml(string error)
    {
        return $@"<!DOCTYPE html><html><body><h1>Error generating documentation</h1><p>{error}</p></body></html>";
    }

    private class ActionDoc
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public List<ParamDoc> Parameters { get; set; } = new();
        public bool RequiresSession { get; set; }
    }

    private class ParamDoc
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Required { get; set; }
    }
}
