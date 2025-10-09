using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInExUtils.Generator.AccessExtensions;
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

    internal static readonly DiagnosticDescriptor Test = new(
        "BIEU0001",
        "Test",
        "{0}",
        Category,
        DiagnosticSeverity.Warning,
        true
    );

    private static readonly DiagnosticDescriptor ClassMustBeMarkedPartial = new(
        "BIEU0002",
        "Class must be marked partial",
        "Class '{0}' must be marked partial",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor ClassMustBeMarkedStatic = new(
        "BIEU0003",
        "Class must be marked static",
        "Class '{0}' must be marked static",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    private static readonly DiagnosticDescriptor ClassMustNotBeMarkedStatic = new(
        "BIEU0004",
        "Class must not be marked static",
        "Class '{0}' must not be marked static",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor AccessInstanceUnknownType = new(
        "BIEU0005",
        "Unknown type inside [AccessInstance]",
        "Enter a valid type in class '{0}' with [AccessInstance]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor AccessInstanceNotFound = new(
        "BIEU0006",
        "Can't find instance type inside [AccessInstance]",
        "Add [AccessInstance] attribute and enter a valid type in class '{0}'",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor AccessInfoUnknownType = new(
        "BIEU0007",
        "Unknown type",
        "Enter a valid type in class '{0}' with [{1}]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor NullReferenceInBepInUtils = new(
        "BIEU0008",
        "Null reference in [BepInUtils]",
        "Enter a valid values in class '{0}' with [BepInUtils]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    internal static readonly DiagnosticDescriptor EmptyName = new(
        "BIEU0009",
        "Name is empty or null",
        "Enter a valid values in class '{0}' with [{1}]",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    private static readonly List<string> MustPartial =
        [BepInUtilsGenerator.BepInUtilsAttributeFullName, AccessExtensionsGenerator.AccessExtensionsAttributeFullName];

    private static readonly List<string> MustNonStatic =
        [BepInUtilsGenerator.BepInUtilsAttributeFullName];

    private static readonly List<string> MustStatic =
        [AccessExtensionsGenerator.AccessExtensionsAttributeFullName];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [ClassMustBeMarkedPartial, ClassMustNotBeMarkedStatic, Test];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(
            analysisContext => AnalyzeClass(analysisContext,
                syntax => syntax.Modifiers.Any(SyntaxKind.PartialKeyword), MustPartial, ClassMustBeMarkedPartial),
            SymbolKind.NamedType);
        context.RegisterSymbolAction(
            analysisContext => AnalyzeClass(analysisContext,
                syntax => !syntax.Modifiers.Any(SyntaxKind.StaticKeyword), MustNonStatic, ClassMustNotBeMarkedStatic),
            SymbolKind.NamedType);
        context.RegisterSymbolAction(
            analysisContext => AnalyzeClass(analysisContext,
                syntax => syntax.Modifiers.Any(SyntaxKind.StaticKeyword), MustStatic, ClassMustBeMarkedStatic),
            SymbolKind.NamedType);
    }

    private static AttributeData? GetAttribute(INamedTypeSymbol typeSymbol, List<string> list) =>
        typeSymbol.GetAttributes().FirstOrDefault(attr =>
        {
            var attributeClass = attr.AttributeClass;
            return list.Contains(attributeClass?.ToDisplayString() ?? string.Empty);
        });

    private static void AnalyzeClass(SymbolAnalysisContext context, ClassCheck classCheckFunc, List<string> list,
        DiagnosticDescriptor descriptor)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;
        var classCheck = typeSymbol.DeclaringSyntaxReferences.All(syntaxReference =>
            syntaxReference.GetSyntax() is not ClassDeclarationSyntax classSyntax ||
            classCheckFunc.Invoke(classSyntax));

        if (classCheck)
            return;

        var attribute = GetAttribute(typeSymbol, list);
        if (attribute?.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax) return;
        if (attributeSyntax.Parent is not AttributeListSyntax
            {
                Parent: ClassDeclarationSyntax classDeclaration
            }) return;

        var diagnostic = Diagnostic.Create(
            descriptor,
            classDeclaration.Identifier.GetLocation(),
            classDeclaration.Identifier.ToString()
        );

        context.ReportDiagnostic(diagnostic);
    }

    private delegate bool ClassCheck(ClassDeclarationSyntax classSyntax);
}