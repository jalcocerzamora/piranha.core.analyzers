using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Piranha.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InvalidSingleFieldComplexRegionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PA0002";
        private const string Category = "Usage";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.InvalidSingleFieldComplexRegionAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.InvalidSingleFieldComplexRegionAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.InvalidSingleFieldComplexRegionAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.Attribute);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is AttributeSyntax attribute))
            {
                return;
            }

            var regionAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.PiranhaExtendRegionAttribute);

            if (regionAttributeType == null)
            {
                return;
            }

            var attributeType = context.SemanticModel.GetTypeInfo(attribute, context.CancellationToken);

            if (!regionAttributeType.Equals(attributeType.ConvertedType, SymbolEqualityComparer.IncludeNullability))
            {
                return;
            }

            if (!(attribute.Parent is AttributeListSyntax attributeList))
            {
                return;
            }

            if (!(attributeList.Parent is PropertyDeclarationSyntax property))
            {
                return;
            }

            var references = context.SemanticModel.GetTypeInfo(property.Type).Type.DeclaringSyntaxReferences;

            if (references.IsEmpty)
            {
                return;
            }

            var reference = references.First();

            var node = reference.GetSyntax(context.CancellationToken);

            if (!(node is ClassDeclarationSyntax @class))
            {
                return;
            }

            var fieldAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.PiranhaExtendFieldAttribute);

            var fieldProperties = @class.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.AttributeLists
                    .Any(al => al.Attributes
                        .Any(a => fieldAttributeType.Equals(context.SemanticModel.GetTypeInfo(a, context.CancellationToken).Type, SymbolEqualityComparer.IncludeNullability))))
                .ToImmutableArray();

            if (fieldProperties.Length == 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, property.GetLocation()));
            }
        }
    }
}