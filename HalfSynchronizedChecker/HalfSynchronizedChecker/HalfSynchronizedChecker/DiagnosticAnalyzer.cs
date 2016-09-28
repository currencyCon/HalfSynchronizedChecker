using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HalfSynchronizedChecker.AnalyzationHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HalfSynchronizedChecker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class HalfSynchronizedCheckerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "HSC001";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormatHalfSynchronized = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormatHalfSynchronized), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Synchronization";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleHalfSynchronized = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormatHalfSynchronized, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeForHalfSynchronizedProperties, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeForHalfSynchronizedProperties, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeForHalfSynchronizedProperties(SyntaxNodeAnalysisContext context)
        {
            var root = context.Node;

            var classDeclaration = root.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDeclaration == null)
            {
                return;
            }

            var properties =
                (IList<PropertyDeclarationSyntax>)
                    classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>().ToList();
            var methods = (IList<MethodDeclarationSyntax>)
                classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
            if (!properties.Any() && !methods.Any())
            {
                return;
            }

            var halfSynchronizedClass = new HalfSynchronizedClassRepresentation(properties, methods);
            if (!halfSynchronizedClass.SynchronizedMethods.Any() && !halfSynchronizedClass.SynchronizedProperties.Any())
            {
                return;
            }
            InspectValues(context, halfSynchronizedClass);
        }

        private static void InspectValues(SyntaxNodeAnalysisContext context,
            HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            HandleUnsynchronizedMethodsWithHalfSynchronizedProperties(context, halfSynchronizedClass);
            HandleUnsynchronizedPropertiesWithSynchronizedProperties(context, halfSynchronizedClass);
            HandleUnsynchronizedPropertiesWithSynchronizedMethods(context, halfSynchronizedClass);
            HandleUnsynchronizedMethodsWithSynchronizedMethods(context, halfSynchronizedClass);
            HandleUnsynchronizedMethodsWithSynchronizedProperties(context, halfSynchronizedClass);
        }

        private static void HandleUnsynchronizedMethodsWithHalfSynchronizedProperties(SyntaxNodeAnalysisContext context, HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var methodsWithHalfSynchronizedProperties = SynchronizationInspector.GetMethodsWithHalfSynchronizedProperties(halfSynchronizedClass);
            foreach (var methodWithHalfSynchronizedProperties in methodsWithHalfSynchronizedProperties)
            {
                var propUsed = SynchronizationInspector.GetHalSynchronizedPropertyUsed(halfSynchronizedClass, methodWithHalfSynchronizedProperties);
                ReportHalfSynchronizationDiagnostic(context, methodWithHalfSynchronizedProperties, "Property", propUsed.Identifier.Text);
            }
        }

        private static void HandleUnsynchronizedMethodsWithSynchronizedMethods(SyntaxNodeAnalysisContext context, HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var methodsWithSynchronizedMethods = SynchronizationInspector.GetMethodsWithSynchronizedMethods(halfSynchronizedClass);
            foreach (var methodsWithSynchronizedMethod in methodsWithSynchronizedMethods)
            {
                ReportSynchronizationDiagnostic(context, methodsWithSynchronizedMethod, CustomDiagnosticsFormatter.GetKindRepresentation(methodsWithSynchronizedMethod.Kind().ToString()), methodsWithSynchronizedMethod.Identifier.Text, CustomDiagnosticsFormatter.GetKindRepresentation(methodsWithSynchronizedMethod.Kind().ToString()));
            }
        }

        private static void HandleUnsynchronizedMethodsWithSynchronizedProperties(SyntaxNodeAnalysisContext context,HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var methodsWithSynchronizedProperties = SynchronizationInspector.GetMethodsWithSynchronizedProperties(halfSynchronizedClass);
            foreach (var methodWithSynchronizedProperties in methodsWithSynchronizedProperties)
            {
                ReportSynchronizationDiagnostic(context, methodWithSynchronizedProperties, "Method", methodWithSynchronizedProperties.Identifier.Text, "Property");
            }
        }

        private static void HandleUnsynchronizedPropertiesWithSynchronizedProperties(SyntaxNodeAnalysisContext context, HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var propertiesWithSynchronizedProperties = SynchronizationInspector.GetPropertiesWithSynchronizedProperties(halfSynchronizedClass);
            foreach (var propertiyWithSynchronizedProperties in propertiesWithSynchronizedProperties)
            {
                ReportSynchronizationDiagnostic(context, propertiyWithSynchronizedProperties, "Property", propertiyWithSynchronizedProperties.Identifier.Text, "Property");
            }
        }

        private static void HandleUnsynchronizedPropertiesWithSynchronizedMethods(SyntaxNodeAnalysisContext context, HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var propertiesWithSynchronizedMethods = SynchronizationInspector.GetPropertiesWithSynchronizedMethods(halfSynchronizedClass);
            foreach (var propertiyWithSynchronizedMethod in propertiesWithSynchronizedMethods)
            {
                ReportSynchronizationDiagnostic(context, propertiyWithSynchronizedMethod, "Property", propertiyWithSynchronizedMethod.Identifier.Text, "Method");
            }
        }

        private static void ReportSynchronizationDiagnostic(SyntaxNodeAnalysisContext context,
            CSharpSyntaxNode propertyDeclarationSyntax, string elementType, string elementTypeName, string synchronizedElement)
        {
            object[] messageArguments = {elementType, elementTypeName, synchronizedElement};
            var diagnostic = Diagnostic.Create(Rule, propertyDeclarationSyntax.GetLocation(), messageArguments);
            context.ReportDiagnostic(diagnostic);
        }


        private static void ReportHalfSynchronizationDiagnostic(SyntaxNodeAnalysisContext context,
    CSharpSyntaxNode propertyDeclarationSyntax, string elementType, string elementTypeName)
        {
            object[] messageArguments = { elementType, elementTypeName };
            var diagnostic = Diagnostic.Create(RuleHalfSynchronized, propertyDeclarationSyntax.GetLocation(), messageArguments);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
