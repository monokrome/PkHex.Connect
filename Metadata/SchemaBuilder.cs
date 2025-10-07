using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PKHeX.Core;

namespace PKHeXWebSocketServer.Metadata;

public class SchemaBuilder
{
    private class ActionInfo
    {
        public string Name { get; set; } = "";
        public ApiCategory Category { get; set; }
        public string Description { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public object Returns { get; set; } = new { };
        public bool RequiresSession { get; set; } = true;
    }

    public static string GenerateJsonSchema()
    {
        var messages = new Dictionary<string, object>();
        var publishOneOf = new List<object>();
        
        var actions = GatherActions();
        foreach (var action in actions)
        {
            var message = BuildMessage(action);
            messages[action.Name] = message;
            publishOneOf.Add(new Dictionary<string, object> 
            { 
                ["$ref"] = $"#/components/messages/{action.Name}" 
            });
        }
        
        messages["errorResponse"] = new
        {
            name = "errorResponse",
            title = "Error Response",
            summary = "Error message returned when an action fails",
            tags = new[] { new { name = "system" } },
            payload = new
            {
                type = "object",
                required = new[] { "error" },
                properties = new Dictionary<string, object>
                {
                    ["error"] = new
                    {
                        type = "string",
                        description = "Error message describing what went wrong"
                    }
                }
            }
        };
        
        messages["successResponse"] = new
        {
            name = "successResponse",
            title = "Success Response",
            summary = "Successful response from an action",
            tags = new[] { new { name = "system" } },
            payload = new
            {
                type = "object",
                description = "Response varies by action"
            }
        };
        
        var channels = new Dictionary<string, object>
        {
            ["/"] = new
            {
                subscribe = new
                {
                    description = "Receive responses from the server",
                    message = new
                    {
                        oneOf = new object[]
                        {
                            new Dictionary<string, object> { ["$ref"] = "#/components/messages/successResponse" },
                            new Dictionary<string, object> { ["$ref"] = "#/components/messages/errorResponse" }
                        }
                    }
                },
                publish = new
                {
                    description = "Send action requests to the server",
                    message = new
                    {
                        oneOf = publishOneOf.ToArray()
                    }
                }
            }
        };

        var schema = new
        {
            asyncapi = "2.6.0",
            info = new
            {
                title = "PKHeX WebSocket API",
                version = "1.0.0",
                description = "WebSocket API for reading and manipulating Pokémon save files"
            },
            servers = new Dictionary<string, object>
            {
                ["production"] = new
                {
                    url = "ws://localhost:8080",
                    protocol = "ws",
                    description = "Local development server"
                }
            },
            channels = channels,
            components = new
            {
                messages = messages,
                schemas = new Dictionary<string, object>
                {
                    ["saveId"] = new
                    {
                        type = "string",
                        @default = "default",
                        description = "Identifier for the save file"
                    }
                }
            }
        };

        return JsonConvert.SerializeObject(schema, Formatting.Indented);
    }

    private static List<ActionInfo> GatherActions()
    {
        var actions = new List<ActionInfo>();
        var assembly = Assembly.GetExecutingAssembly();
        
        var serviceTypes = assembly.GetTypes()
            .Where(t => t.Namespace == "PKHeXWebSocketServer.Services" 
                     && !t.IsAbstract 
                     && t.Name.EndsWith("Service"));

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            foreach (var method in methods)
            {
                var actionAttr = method.GetCustomAttribute<ApiActionAttribute>();
                if (actionAttr == null) continue;

                var parameters = BuildParameters(method);
                var returns = BuildReturnInfo(method);

                actions.Add(new ActionInfo
                {
                    Name = actionAttr.Name,
                    Category = actionAttr.Category,
                    Description = actionAttr.Description,
                    Parameters = parameters,
                    Returns = returns,
                    RequiresSession = actionAttr.RequiresSession
                });
            }
        }

        return actions.OrderBy(a => a.Category)
                      .ThenBy(a => a.Name)
                      .ToList();
    }

    private static object BuildMessage(ActionInfo action)
    {
        var properties = new Dictionary<string, object>
        {
            ["action"] = new
            {
                type = "string",
                @const = action.Name
            }
        };
        
        var required = new List<string> { "action" };
        
        foreach (var param in action.Parameters)
        {
            properties[param.Key] = param.Value;
            
            var paramDict = param.Value as Dictionary<string, object>;
            if (paramDict != null && paramDict.TryGetValue("required", out var reqValue) && (bool)reqValue)
            {
                required.Add(param.Key);
            }
        }
        
        // ADD sessionId to required only if action requires a session
        if (action.RequiresSession)
        {
            required.Add("sessionId");
            properties["sessionId"] = new Dictionary<string, object>
            {
                { "type", "string" },
                { "description", "Session identifier from loadSave" },
                { "required", true }
            };
        }
        
        return new
        {
            name = action.Name,
            title = FormatTitle(action.Name),
            summary = action.Description,
            tags = new[] { new { name = action.Category.ToString().ToLower() } },
            payload = new
            {
                type = "object",
                required = required.ToArray(),
                properties = properties
            }
        };
    }

    private static string FormatTitle(string actionName)
    {
        var result = System.Text.RegularExpressions.Regex.Replace(
            actionName,
            "([a-z])([A-Z])",
            "$1 $2"
        );
        return char.ToUpper(result[0]) + result.Substring(1);
    }

    private static Dictionary<string, object> BuildParameters(MethodInfo method)
    {
        var parameters = new Dictionary<string, object>();
        
        foreach (var param in method.GetParameters())
        {
            if (param.ParameterType == typeof(SaveFile))
                continue;

            var paramAttr = param.GetCustomAttribute<ApiParameterAttribute>();
            var paramName = paramAttr?.NameOverride ?? param.Name ?? "unknown";
            
            var paramInfo = new Dictionary<string, object>
            {
                { "type", GetTypeName(param.ParameterType) },
                { "required", paramAttr?.Required ?? !param.HasDefaultValue }
            };

            if (paramAttr?.Description != null)
                paramInfo["description"] = paramAttr.Description;

            if (paramAttr?.Default != null || param.HasDefaultValue)
                paramInfo["default"] = paramAttr?.Default ?? param.DefaultValue?.ToString() ?? "null";

            if (paramAttr?.Format != null)
                paramInfo["format"] = paramAttr.Format;

            parameters[paramName] = paramInfo;
        }

        return parameters;
    }

    private static object BuildReturnInfo(MethodInfo method)
    {
        var responseAttrs = method.GetCustomAttributes<ApiResponseAttribute>().ToArray();
        
        if (responseAttrs.Length == 0)
        {
            return new { type = "object", description = "Response varies by action" };
        }

        var fields = new Dictionary<string, object>();
        foreach (var attr in responseAttrs)
        {
            var fieldInfo = new Dictionary<string, object>
            {
                { "type", attr.Type }
            };
            
            if (attr.Description != null)
                fieldInfo["description"] = attr.Description;

            fields[attr.FieldName] = fieldInfo;
        }

        return new { type = "object", fields };
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(JObject)) return "object";
        if (type.IsArray) return "array";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return "array";
        
        return "object";
    }
}
