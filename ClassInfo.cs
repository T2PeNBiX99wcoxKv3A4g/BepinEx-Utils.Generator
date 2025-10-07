using Microsoft.CodeAnalysis;

namespace BepInExUtils.Generator;

public readonly record struct ClassInfo(
    string NamespaceName,
    string ClassName,
    string UsingsText,
    string GeneratorName,
    SyntaxToken Identifier)
{
    public readonly string ClassName = ClassName;
    public readonly string GeneratorName = GeneratorName;
    public readonly SyntaxToken Identifier = Identifier;
    public readonly string NamespaceName = NamespaceName;
    public readonly string UsingsText = UsingsText;
}