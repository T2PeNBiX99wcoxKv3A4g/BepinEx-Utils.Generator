using BepInExUtils.Generator.Main;

namespace BepInExUtils.Generator;

public readonly record struct ClassInfo(string NamespaceName, string ClassName, string UsingsText)
{
    public readonly string ClassName = ClassName;
    public readonly string NamespaceName = NamespaceName;
    public readonly string UniqueHintName = $"{NamespaceName}.{ClassName}_{nameof(BepInUtilsGenerator)}.generated.cs";
    public readonly string UsingsText = UsingsText;
}