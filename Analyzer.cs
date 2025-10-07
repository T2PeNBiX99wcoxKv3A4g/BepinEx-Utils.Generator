using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInExUtils.Generator.BepInUtils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BepInExUtils.Generator;

// Refs: https://github.com/Hamunii/BepInEx.AutoPlugin/blob/nuget/BepInEx.AutoPlugin/Analyzers/PluginClassAnalyzer.cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008")]
public class Analyzer : DiagnosticAnalyzer
{
    private const string Category = "BepInExUtils";

    private static readonly DiagnosticDescriptor ClassMustBeMarkedPartial = new(
        "BIEU0001",
        "Class must be marked partial",
        "Class '{0}' must be marked partial for use with [BepInUtils]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor ClassMustNotBeMarkedStatic = new(
        "BIEU0002",
        "Class must not be marked static",
        "Class '{0}' must not be marked static for use with [BepInUtils]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor AccessInstanceUnknownType = new(
        "BIEU0003",
        "Unknown type inside [AccessInstance]",
        "Enter a valid type in class '{0}' with [AccessInstance]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor AccessInstanceNotFound = new(
        "BIEU0004",
        "Can't find instance type inside [AccessInstance]",
        "Add [AccessInstance] attribute and enter a valid type in class '{0}'",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    
    internal static readonly DiagnosticDescriptor AccessFieldUnknownType = new(
        "BIEU0005",
        "Unknown type inside [AccessField]",
        "Enter a valid type in class '{0}' with [AccessField]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    // ReSharper disable once MemberCanBePrivate.Global
    internal static readonly DiagnosticDescriptor Test = new(
        "BIEU0100",
        "Test",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [ClassMustBeMarkedPartial, ClassMustNotBeMarkedStatic, Test];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeClassIsPartial, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeClassIsStatic, SymbolKind.NamedType);
    }

    private static AttributeData? GetAttribute(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().FirstOrDefault(attr =>
        {
            var attributeClass = attr.AttributeClass;
            return attributeClass?.Name == BepInUtilsGenerator.BepInUtilsAttributeClassName &&
                   attributeClass.ToDisplayString() == BepInUtilsGenerator.BepInUtilsAttributeFullName;
        });

    private static void AnalyzeClassIsPartial(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        var isNotClassOrIsPartialClass = typeSymbol.DeclaringSyntaxReferences.All(static syntaxReference =>
            syntaxReference.GetSyntax() is not ClassDeclarationSyntax classSyntax ||
            classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (isNotClassOrIsPartialClass)
            return;

        var attribute = GetAttribute(typeSymbol);
        if (attribute?.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax) return;
        if (attributeSyntax.Parent is not AttributeListSyntax
            {
                Parent: ClassDeclarationSyntax classDeclaration
            }) return;

        var diagnostic = Diagnostic.Create(
            ClassMustBeMarkedPartial,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.ToString()
        );

        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeClassIsStatic(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        var isNotClassOrIsNotStaticClass = typeSymbol.DeclaringSyntaxReferences.All(static syntaxReference =>
            syntaxReference.GetSyntax() is not ClassDeclarationSyntax classSyntax ||
            !classSyntax.Modifiers.Any(SyntaxKind.StaticKeyword));

        if (isNotClassOrIsNotStaticClass)
            return;

        var attribute = GetAttribute(typeSymbol);
        if (attribute?.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax) return;
        if (attributeSyntax.Parent is not AttributeListSyntax
            {
                Parent: ClassDeclarationSyntax classDeclaration
            }) return;

        var diagnostic = Diagnostic.Create(
            ClassMustNotBeMarkedStatic,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.ToString()
        );

        context.ReportDiagnostic(diagnostic);
    }
}