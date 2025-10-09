namespace BepInExUtils.Generator.AccessExtensions;

public readonly record struct AccessPropertyInfo(string TypeName, string Name)
{
    public readonly string Name = Name;
    public readonly string TypeName = TypeName;
}