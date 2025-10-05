namespace BepInExUtils.Generator.Main;

public readonly record struct ConfigInfo(
    string Type,
    string Key,
    string Section,
    string DefaultValue,
    string Description,
    string? MinValue,
    string? MaxValue)
{
    public readonly string DefaultValue = DefaultValue;
    public readonly string Description = Description;
    public readonly string Key = Key;
    public readonly string? MaxValue = MaxValue;
    public readonly string? MinValue = MinValue;
    public readonly string Section = Section;
    public readonly string Type = Type;
}