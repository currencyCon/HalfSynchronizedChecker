using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Rename;

namespace HalfSynchronizedChecker
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HalfSynchronizedCheckerCodeFixProvider)), Shared]
    public class HalfSynchronizedCheckerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Synchronize Member";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HalfSynchronizedCheckerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
            if (syntaxNode is MethodDeclarationSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(Title, c => SynchronizeMethod(context.Document, (MethodDeclarationSyntax) syntaxNode, c), Title), diagnostic);
            }
            if (syntaxNode is PropertyDeclarationSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(Title, c => SynchronizeProperty(context.Document, (PropertyDeclarationSyntax)syntaxNode, c), Title), diagnostic);
            }
        }

        private async Task<Document> SynchronizeProperty(Document document, PropertyDeclarationSyntax property,
    CancellationToken cancellationToken)
        {
            var x = property.AccessorList;
            PropertyDeclarationSyntax blu = property;
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxList<AccessorDeclarationSyntax> newDecl = new SyntaxList<AccessorDeclarationSyntax>();
            foreach (var accessorDeclarationSyntax in x.Accessors)
            {
                var openParans = SyntaxFactory.Token(SyntaxKind.OpenParenToken);
                var closingParans = SyntaxFactory.Token(SyntaxKind.CloseParenToken);
                var thisExpression = SyntaxFactory.ThisExpression();
                var lockStatement = SyntaxFactory.LockStatement(SyntaxFactory.Token(SyntaxKind.LockKeyword),
                    openParans,
                    thisExpression,
                    closingParans, accessorDeclarationSyntax.Body);
                newDecl.Add(accessorDeclarationSyntax.ReplaceNode(accessorDeclarationSyntax.Body, lockStatement));
                blu = property.ReplaceNode(accessorDeclarationSyntax,
                    accessorDeclarationSyntax.ReplaceNode(accessorDeclarationSyntax.Body, lockStatement));

            }
            return document.WithSyntaxRoot(root.ReplaceNode(property, blu));
        }

        private async Task<Document> SynchronizeMethod(Document document, MethodDeclarationSyntax method,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var body = method.Body;
            var openParans = SyntaxFactory.Token(SyntaxKind.OpenParenToken);
            var closingParans = SyntaxFactory.Token(SyntaxKind.CloseParenToken);
            var thisExpression = SyntaxFactory.ThisExpression();
            var lockStatement = SyntaxFactory.LockStatement(SyntaxFactory.Token(SyntaxKind.LockKeyword),
                openParans,
                thisExpression,
                closingParans, body);
            var l =
                SyntaxFactory.Block(lockStatement);
            var newMeth = method.ReplaceNode(method, method.WithBody(l));
            return document.WithSyntaxRoot(root.ReplaceNode(method, newMeth));

        }
    }
}