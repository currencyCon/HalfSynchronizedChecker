using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HalfSynchronizedChecker.AnalyzationHelpers
{
    public class SynchronizationInspector
    {
        public static IEnumerable<PropertyDeclarationSyntax> GetPropertiesWithSynchronizedMethods(HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var identifiersUsedInLockStatements =
                SyntaxNodeFilter.GetIdentifiersInLockStatements(halfSynchronizedClass.SynchronizedMethods);
            var propertiesWithSynchronizedMethods = new List<PropertyDeclarationSyntax>();
            foreach (var propertyDeclarationSyntax in halfSynchronizedClass.UnsynchronizedProperties)
            {
                if (identifiersUsedInLockStatements.Any(e => e.Text == propertyDeclarationSyntax.Identifier.Text))
                {
                    propertiesWithSynchronizedMethods.Add(propertyDeclarationSyntax);
                }
            }
            return propertiesWithSynchronizedMethods;
        }

        public static IEnumerable<PropertyDeclarationSyntax> GetPropertiesWithSynchronizedProperties(HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var identifiersUsedInLockStatements =
                SyntaxNodeFilter.GetIdentifiersInLockStatements(halfSynchronizedClass.SynchronizedProperties);
            var propertiesWithSynchronizedProperties = new List<PropertyDeclarationSyntax>();
            foreach (var propertyDeclarationSyntax in halfSynchronizedClass.UnsynchronizedProperties)
            {
                if (identifiersUsedInLockStatements.Any(e => e.Text == propertyDeclarationSyntax.Identifier.Text))
                {
                    propertiesWithSynchronizedProperties.Add(propertyDeclarationSyntax);
                }
            }
            return propertiesWithSynchronizedProperties;
        }

        public static IEnumerable<MethodDeclarationSyntax> GetMethodsWithSynchronizedProperties(HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var identifiersUsedInLockStatements =
                SyntaxNodeFilter.GetIdentifiersInLockStatements(halfSynchronizedClass.SynchronizedProperties);
            var methodsWithSynchronizedProperties = new List<MethodDeclarationSyntax>();
            foreach (var methodDeclarationSyntax in halfSynchronizedClass.UnsynchronizedMethods)
            {
                if (identifiersUsedInLockStatements.Any(e => e.Text == methodDeclarationSyntax.Identifier.Text))
                {
                    methodsWithSynchronizedProperties.Add(methodDeclarationSyntax);
                }
            }
            return methodsWithSynchronizedProperties;
        }

        public static IEnumerable<MethodDeclarationSyntax> GetMethodsWithSynchronizedMethods(HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var identifiersUsedInLockStatements =
                SyntaxNodeFilter.GetIdentifiersInLockStatements(halfSynchronizedClass.SynchronizedMethods);
            var methodsWithSynchronizedMethods = new List<MethodDeclarationSyntax>();
            foreach (var methodDeclarationSyntax in halfSynchronizedClass.UnsynchronizedMethods)
            {
                if (identifiersUsedInLockStatements.Any(e => e.Text == methodDeclarationSyntax.Identifier.Text))
                {
                    methodsWithSynchronizedMethods.Add(methodDeclarationSyntax);
                }
            }
            return methodsWithSynchronizedMethods;
        }

        public static IEnumerable<MethodDeclarationSyntax> GetMethodsWithHalfSynchronizedProperties(HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var methodsWithHalfSynchronizedProperties = new List<MethodDeclarationSyntax>();
            foreach (var methodDeclarationSyntax in halfSynchronizedClass.UnsynchronizedMethods)
            {
                var identifiersInMethods =
                    methodDeclarationSyntax.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Select(e => e.Identifier.Text);
                if (
                    halfSynchronizedClass.UnsynchronizedPropertiesInSynchronizedMethods.ToList()
                        .Select(e => e.Identifier.Text)
                        .Any(e => identifiersInMethods.Contains(e)))
                {
                    methodsWithHalfSynchronizedProperties.Add(methodDeclarationSyntax);
                }
            }
            return methodsWithHalfSynchronizedProperties;
        }

        public static PropertyDeclarationSyntax GetHalSynchronizedPropertyUsed(
            HalfSynchronizedClassRepresentation halfSynchronizedClass,
            MethodDeclarationSyntax methodWithHalfSynchronizedProperties)
        {
            var identifiersInMethods =
                methodWithHalfSynchronizedProperties.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Select(e => e.Identifier.Text);

            var propUsed =
                halfSynchronizedClass.UnsynchronizedPropertiesInSynchronizedMethods.ToList()
                    .First(e => identifiersInMethods.Contains(e.Identifier.Text));
            return propUsed;
        }
    }
}