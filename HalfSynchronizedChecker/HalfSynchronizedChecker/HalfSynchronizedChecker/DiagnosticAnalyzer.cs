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
        public const string InnerLockingDiagnosticId = "HSC001";
        public const string HalfSynchronizedChildDiagnosticId = "HSC002";
        public const string UnsynchronizedPropertyId = "HSC000";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormatHalfSynchronized = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormatHalfSynchronized), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormatUnsychronizedProperty = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormatUnsynchronizedProperty), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Synchronization";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(InnerLockingDiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleHalfSynchronized = new DiagnosticDescriptor(HalfSynchronizedChildDiagnosticId, Title, MessageFormatHalfSynchronized, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);
        private static readonly DiagnosticDescriptor RuleUnsynchronizedProperty = new DiagnosticDescriptor(UnsynchronizedPropertyId, Title, MessageFormatUnsychronizedProperty, Category, DiagnosticSeverity.Warning, isEnabledByDefault:true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule, RuleHalfSynchronized, RuleUnsynchronizedProperty);//, RuleUnsynchronizedProperty);

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

            var halfSynchronizedClass = new HalfSynchronizedClassRepresentation(classDeclaration);
            if (!halfSynchronizedClass.SynchronizedMethods.Any() && !halfSynchronizedClass.SynchronizedProperties.Any())
            {
                return;
            }

            if (!halfSynchronizedClass.Properties.Any() && !halfSynchronizedClass.Methods.Any())
            {
                return;
            }
            if (root is PropertyDeclarationSyntax)
            {
                var property = (PropertyDeclarationSyntax) root;
                if (SynchronizationInspector.PropertyNeedsSynchronization(property,
                    halfSynchronizedClass))
                {
                    ReportUnsynchronizationPropertyDiagnostic(context, property, "Property", property.Identifier.Text);

                }
            }
            else if (root is MethodDeclarationSyntax)
            {
                var method = (MethodDeclarationSyntax) root;
                if (SynchronizationInspector.MethodHasHalfSynchronizedProperties(method, halfSynchronizedClass))
                {
                    var propUsed = SynchronizationInspector.GetHalSynchronizedPropertyUsed(halfSynchronizedClass, method);
                    ReportHalfSynchronizationDiagnostic(context, method, "Property", propUsed.Identifier.Text);
                }
            }
        }

        private static void ReportUnsynchronizationPropertyDiagnostic(SyntaxNodeAnalysisContext context,
    CSharpSyntaxNode propertyDeclarationSyntax, string elementType, string elementTypeName)
        {
            object[] messageArguments = { elementType, elementTypeName };
            var diagnostic = Diagnostic.Create(RuleUnsynchronizedProperty, propertyDeclarationSyntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static void ReportSynchronizationDiagnostic(SyntaxNodeAnalysisContext context,
            CSharpSyntaxNode propertyDeclarationSyntax, string elementType, string elementTypeName)
        {
            object[] messageArguments = {elementType, elementTypeName};
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
