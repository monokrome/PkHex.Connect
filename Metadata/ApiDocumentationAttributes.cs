using System;

namespace PKHeXWebSocketServer.Metadata;

public enum ApiCategory
{
    Save,
    Pokemon,
    Storage,
    Items,
    Pokedex,
    Trainer,
    Progress,
    Communication,
    World,
    Knowledge
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ApiActionAttribute : Attribute
{
    public string Name { get; }
    public ApiCategory Category { get; }
    public string Description { get; }
    public string? SuccessSummary { get; set; }
    public bool RequiresSession { get; set; } = true;

    public ApiActionAttribute(string name, ApiCategory category, string description)
    {
        Name = name;
        Category = category;
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ApiParameterAttribute : Attribute
{
    public string? NameOverride { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; } = true;
    public string? Default { get; set; }
    public string? Format { get; set; }

    public ApiParameterAttribute()
    {
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ApiResponseAttribute : Attribute
{
    public string FieldName { get; }
    public string Type { get; }
    public string? Description { get; set; }

    public ApiResponseAttribute(string fieldName, string type)
    {
        FieldName = fieldName;
        Type = type;
    }
}
