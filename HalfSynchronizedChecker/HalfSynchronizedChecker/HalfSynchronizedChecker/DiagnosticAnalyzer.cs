using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Synchronization";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeForHalfSynchronizedProperties, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeForHalfSynchronizedProperties, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeForHalfSynchronizedProperties(SyntaxNodeAnalysisContext context)
        {
            //Get Syntax Node to Analyze
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

            var synchronizedProperties = GetSynchronizedProperties(properties).ToList();
            var synchronizedMethods = GetSynchronizedMethods(methods).ToList();
            var unsyncedProperties = GetUnsynchronizedProperties(properties).ToList();

            var unsynchronizedMethods = GetUnsynchronizedMethods(methods).ToList();
            if (!synchronizedMethods.Any() && !synchronizedProperties.Any())
            {
                return;
            }


            HandleUnsynchronizedPropertiesWithSynchronizedProperties(context, synchronizedProperties, unsyncedProperties);
            HandleUnsynchronizedPropertiesWithSynchronizedMethods(context, synchronizedMethods, unsyncedProperties);
            HandleUnsynchronizedMethodsWithSynchronizedMethods(context, synchronizedMethods, unsynchronizedMethods);
            HandleUnsynchronizedMethodsWithSynchronizedProperties(context, synchronizedProperties, unsynchronizedMethods);
        }

        private static void HandleUnsynchronizedMethodsWithSynchronizedMethods(SyntaxNodeAnalysisContext context,
    IEnumerable<MethodDeclarationSyntax> synchronizedMethods,
    IEnumerable<MethodDeclarationSyntax> unsyncedMethods)
        {
            var identifiersUsedInLockStatements = GetIdentifiersInLockStatements(synchronizedMethods);
            foreach (var methodDeclarationSyntax in unsyncedMethods)
            {
                if (identifiersUsedInLockStatements.All(e => e.Text != methodDeclarationSyntax.Identifier.Text))
                    continue;
                ReportSynchronizationDiagnostic(context, methodDeclarationSyntax, "Method", methodDeclarationSyntax.Identifier.Text, "Method");
            }
        }

        private static void HandleUnsynchronizedMethodsWithSynchronizedProperties(SyntaxNodeAnalysisContext context,
            IEnumerable<PropertyDeclarationSyntax> synchronizedProperties,
            IEnumerable<MethodDeclarationSyntax> unsyncedMethods)
        {
            var identifiersUsedInLockStatements = GetIdentifiersInLockStatements(synchronizedProperties);
            foreach (var methodDeclarationSyntax in unsyncedMethods)
            {
                if (identifiersUsedInLockStatements.All(e => e.Text != methodDeclarationSyntax.Identifier.Text))
                    continue;
                ReportSynchronizationDiagnostic(context, methodDeclarationSyntax, "Method", methodDeclarationSyntax.Identifier.Text, "Property");
            }
        }
        private static void HandleUnsynchronizedPropertiesWithSynchronizedProperties(SyntaxNodeAnalysisContext context,
            IEnumerable<PropertyDeclarationSyntax> synchronizedProperties, IEnumerable<PropertyDeclarationSyntax> unsyncedProperties)
        {
            var identifiersUsedInLockStatements = GetIdentifiersInLockStatements(synchronizedProperties);
            foreach (var propertyDeclarationSyntax in unsyncedProperties)
            {
                if (identifiersUsedInLockStatements.All(e => e.Text != propertyDeclarationSyntax.Identifier.Text))
                    continue;
                ReportSynchronizationDiagnostic(context, propertyDeclarationSyntax, "Property", propertyDeclarationSyntax.Identifier.Text, "Property");
            }
        }

        private static void HandleUnsynchronizedPropertiesWithSynchronizedMethods(SyntaxNodeAnalysisContext context,
            IEnumerable<MethodDeclarationSyntax> synchronizedMethods,
            IEnumerable<PropertyDeclarationSyntax> unsyncedProperties)
        {
            var identifiersUsedInLockStatements = GetIdentifiersInLockStatements(synchronizedMethods);
            foreach (var propertyDeclarationSyntax in unsyncedProperties)
            {
                if (identifiersUsedInLockStatements.All(e => e.Text != propertyDeclarationSyntax.Identifier.Text))
                    continue;
                ReportSynchronizationDiagnostic(context, propertyDeclarationSyntax, "Property", propertyDeclarationSyntax.Identifier.Text, "Method");
            }
        }

        private static List<SyntaxToken> GetIdentifiersInLockStatements(IEnumerable<SyntaxNode> synchronizedMethods)
        {
            var locksStatementsOfProperties = GetLockStatements(synchronizedMethods);
            return GetIdentifiersUsedInLocks(locksStatementsOfProperties);
        }

        private static IEnumerable<LockStatementSyntax> GetLockStatements<TSyntaxElement>(IEnumerable<TSyntaxElement> synchronizedElements) where TSyntaxElement : SyntaxNode
        {
            return synchronizedElements.SelectMany(a => a.DescendantNodes().OfType<LockStatementSyntax>()).ToList();
        }

        private static List<SyntaxToken> GetIdentifiersUsedInLocks(IEnumerable<LockStatementSyntax> locksStatementsOfProperties)
        {
            var identifiersUsedInLockStatements =
                locksStatementsOfProperties.ToList()
                    .SelectMany(a => a.DescendantNodes().OfType<IdentifierNameSyntax>())
                    .Select(e => e.Identifier)
                    .Distinct()
                    .ToList();
            return identifiersUsedInLockStatements;
        }

        private static void ReportSynchronizationDiagnostic(SyntaxNodeAnalysisContext context,
            CSharpSyntaxNode propertyDeclarationSyntax, string elementType, string elementTypeName, string synchronizedElement)
        {
            object[] messageArguments = {elementType, elementTypeName, synchronizedElement};
            var diagnostic = Diagnostic.Create(Rule, propertyDeclarationSyntax.GetLocation(), messageArguments);
            context.ReportDiagnostic(diagnostic);
        }

        private static IEnumerable<MethodDeclarationSyntax> GetUnsynchronizedMethods(IEnumerable<MethodDeclarationSyntax> methods) 
        {
            return methods.Where(e => e.Modifiers.Any(a => a.IsKind(SyntaxKind.PublicKeyword)))
                .Where(e => !e.DescendantNodes().OfType<LockStatementSyntax>().Any()).ToList();
        }

        private static IEnumerable<PropertyDeclarationSyntax> GetUnsynchronizedProperties(IEnumerable<PropertyDeclarationSyntax> properties)
        {
            var unsyncedProperties = properties.Where(e => e.Modifiers.Any(a => a.IsKind(SyntaxKind.PublicKeyword)))
                .Where(e => !e.DescendantNodes().OfType<LockStatementSyntax>().Any()).ToList();
            return unsyncedProperties;
        }

        private static IEnumerable<MethodDeclarationSyntax> GetSynchronizedMethods(IEnumerable<MethodDeclarationSyntax> methods)
        {
            var synchronizedMethods = methods.Where(e => e.Modifiers.Any(a => a.IsKind(SyntaxKind.PublicKeyword)))
                .Where(e => e.DescendantNodes().OfType<LockStatementSyntax>().Any()).ToList();
            return synchronizedMethods;
        }

        private static IEnumerable<PropertyDeclarationSyntax> GetSynchronizedProperties(IEnumerable<PropertyDeclarationSyntax> properties)
        {
            var synchronizedProperties =
                properties.Where(e => e.Modifiers.Any(a => a.IsKind(SyntaxKind.PublicKeyword)))
                    .Where(e => e.DescendantNodes().OfType<LockStatementSyntax>().Any()).ToList();
            return synchronizedProperties;
        }
    }
}
