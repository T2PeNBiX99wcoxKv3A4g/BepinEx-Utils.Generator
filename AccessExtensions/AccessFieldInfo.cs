namespace BepInExUtils.Generator.AccessExtensions;

public readonly record struct AccessFieldInfo(string TypeName, string FieldName)
{
    public readonly string FieldName = FieldName;
    public readonly string TypeName = TypeName;
}