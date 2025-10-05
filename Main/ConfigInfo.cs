namespace BepInExUtils.Generator.Main;

public readonly record struct ConfigInfo(string Type, string Key, string[] OtherArgs)
{
    public readonly string Key = Key;
    public readonly string[] OtherArgs = OtherArgs;
    public readonly string Type = Type;
}