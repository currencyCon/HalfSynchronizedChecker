using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HalfSynchronizedChecker.SyntaxBuilders
{
    public class LockBuilder
    {
        public static BlockSyntax BuildLockBlock(StatementSyntax body)
        {
            var openParans = SyntaxFactory.Token(SyntaxKind.OpenParenToken);
            var closingParans = SyntaxFactory.Token(SyntaxKind.CloseParenToken);
            var thisExpression = SyntaxFactory.ThisExpression();
            var lockStatement = SyntaxFactory.LockStatement(SyntaxFactory.Token(SyntaxKind.LockKeyword),
                openParans,
                thisExpression,
                closingParans, body);
            var lockStatementBlock =
                SyntaxFactory.Block(lockStatement);
            return lockStatementBlock;
        }
    }
}
