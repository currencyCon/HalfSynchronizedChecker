

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HalfSynchronizedChecker.AnalyzationHelpers
{
    public class HalfSynchronizedClassRepresentation
    {
        public HalfSynchronizedClassRepresentation(IList<PropertyDeclarationSyntax> properties,
            IList<MethodDeclarationSyntax> methods)
        {
            Properties = properties;
            Methods = methods;
            SynchronizedProperties = SyntaxNodeFilter.GetSynchronizedProperties(Properties);
            SynchronizedMethods = SyntaxNodeFilter.GetSynchronizedMethods(Methods);
            UnsynchronizedProperties = SyntaxNodeFilter.GetUnsynchronizedProperties(Properties);
            UnsynchronizedMethods = SyntaxNodeFilter.GetUnsynchronizedMethods(Methods);
            UnsynchronizedPropertiesInSynchronizedMethods =
                SyntaxNodeFilter.GetPropertiesInSynchronizedMethods(SynchronizedMethods, UnsynchronizedProperties);
        }
        public IEnumerable<PropertyDeclarationSyntax> Properties { get; set; }
        public IEnumerable<MethodDeclarationSyntax> Methods { get; set; }

        public IEnumerable<PropertyDeclarationSyntax> SynchronizedProperties { get; set; }
        public IEnumerable<MethodDeclarationSyntax> SynchronizedMethods { get; set; }
        public IEnumerable<PropertyDeclarationSyntax> UnsynchronizedProperties { get; set; }
        public IEnumerable<MethodDeclarationSyntax> UnsynchronizedMethods { get; set; }
        public IEnumerable<PropertyDeclarationSyntax> UnsynchronizedPropertiesInSynchronizedMethods { get; set; }


    }
}
