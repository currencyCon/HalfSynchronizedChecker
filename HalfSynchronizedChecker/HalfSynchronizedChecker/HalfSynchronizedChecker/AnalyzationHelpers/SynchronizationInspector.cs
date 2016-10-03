using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HalfSynchronizedChecker.AnalyzationHelpers
{
    public class SynchronizationInspector
    {
        public static IEnumerable<MethodDeclarationSyntax> GetMethodsWithHalfSynchronizedProperties(HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var methodsWithHalfSynchronizedProperties = new List<MethodDeclarationSyntax>().ToList();
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

        public static bool PropertyNeedsSynchronization(PropertyDeclarationSyntax propertyDeclaration, HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var identifiersInLockStatements = halfSynchronizedClass.GetIdentifiersInLockStatements();
            return identifiersInLockStatements.Contains(propertyDeclaration.Identifier.Text);
        }

        public static bool MethodHasHalfSynchronizedProperties(MethodDeclarationSyntax method, HalfSynchronizedClassRepresentation halfSynchronizedClass)
        {
            var methodsWithHalfSynchronizedProperties = GetMethodsWithHalfSynchronizedProperties(halfSynchronizedClass);
            return methodsWithHalfSynchronizedProperties.Select(e => e.Identifier.Text).Contains(method.Identifier.Text);
        }
    }
}