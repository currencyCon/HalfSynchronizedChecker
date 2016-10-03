using System.Collections.Generic;
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

namespace HalfSynchronizedChecker
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HalfSynchronizedCheckerCodeFixProvider)), Shared]
    public class HalfSynchronizedCheckerCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Synchronize Member";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(HalfSynchronizedCheckerAnalyzer.InnerLockingDiagnosticId, HalfSynchronizedCheckerAnalyzer.HalfSynchronizedChildDiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var syntaxNode = root.FindToken(diagnosticSpan.Start).Parent;
            if (syntaxNode is MethodDeclarationSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(Title, c => SynchronizeMethod(context.Document, (MethodDeclarationSyntax) syntaxNode, c), Title), context.Diagnostics.First(a => a.Id == HalfSynchronizedCheckerAnalyzer.HalfSynchronizedChildDiagnosticId));
            }
/*            if (syntaxNode is PropertyDeclarationSyntax)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(Title, c => SynchronizeProperty(context.Document, (PropertyDeclarationSyntax) syntaxNode, c), equivalenceKey: Title), diagnostic);
            }*/
        }

        private static async Task<Document> SynchronizeProperty(Document document, PropertyDeclarationSyntax property,
    CancellationToken cancellationToken)
        {
            var x = property.AccessorList;
            var blu = property;
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newDecl = new SyntaxList<AccessorDeclarationSyntax>();
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

        private static async Task<Document> SynchronizeMethod(Document document, MethodDeclarationSyntax method,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var body = method.Body.WithLeadingTrivia(SyntaxTriviaList.Create(SyntaxFactory.Tab));
            var openParans = SyntaxFactory.Token(SyntaxKind.OpenParenToken);
            var closingParans = SyntaxFactory.Token(SyntaxKind.CloseParenToken);
            var thisExpression = SyntaxFactory.ThisExpression();
            var lockStatement = SyntaxFactory.LockStatement(SyntaxFactory.Token(SyntaxKind.LockKeyword),
                openParans,
                thisExpression,
                closingParans, body.WithoutLeadingTrivia());
            var l =
                SyntaxFactory.Block(lockStatement).WithoutLeadingTrivia();
            l = l.ReplaceNode(thisExpression, thisExpression.WithLeadingTrivia());
            var newMeth = method.ReplaceNode(method, method.WithBody(l));
            var x = newMeth.ToFullString();
            return document.WithSyntaxRoot(root.ReplaceNode(method, newMeth));

        }
    }
}