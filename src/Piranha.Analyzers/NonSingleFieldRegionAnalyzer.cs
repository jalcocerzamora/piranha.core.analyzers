/*
 * Copyright (c) 2020 Mikael Lindemann
 *
 * This software may be modified and distributed under the terms
 * of the MIT license.  See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core.analyzers
 *
 */

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Piranha.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NonSingleFieldRegionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "PA0001";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NonSingleFieldRegionAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.NonSingleFieldRegionAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NonSingleFieldRegionAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
        

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // List of built-in field types that, per documentation, is primarily intended for complex regions.
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsAudioField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsCheckBoxField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsDateField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsDocumentField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsImageField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsMediaField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsNumberField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsPageField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsPostField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsStringField), SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(c => AnalyzeSyntaxNode(c, Constants.Types.PiranhaExtendFieldsVideoField), SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context, string fieldName)
        {
            if (!(context.Node is PropertyDeclarationSyntax pds))
            {
                return;
            }

            var fieldTypeSymbol = context.Compilation.GetTypeByMetadataName(fieldName);
            var s = context.SemanticModel.GetTypeInfo(pds.Type, context.CancellationToken);
            
            if (!s.Type.Equals(fieldTypeSymbol, SymbolEqualityComparer.IncludeNullability))
            {
                return;
            }

            // Verify that the property is marked with RegionAttribute.
            var regionAttributeSymbol = context.Compilation.GetTypeByMetadataName("Piranha.Extend.RegionAttribute");

            foreach (var attributeList in pds.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var attributeTypeInfo = context.SemanticModel.GetTypeInfo(attribute, context.CancellationToken);

                    if (regionAttributeSymbol.Equals(attributeTypeInfo.ConvertedType, SymbolEqualityComparer.IncludeNullability))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, pds.GetLocation(), fieldName.Split('.').Last()));
                    }
                }
            }
        }
    }
}
