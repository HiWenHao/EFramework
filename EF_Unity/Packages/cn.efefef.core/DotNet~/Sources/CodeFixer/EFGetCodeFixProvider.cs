using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace EasyFramework
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EfGetCodeFixProvider)), Shared]
    public class EfGetCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(DiagnosticIds.UseGetTypeInsteadId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // 找到调用表达式节点
            var invocation = root.FindNode(diagnosticSpan).AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocation == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "替换为 EF.GetType()",
                    createChangedDocument: c => ReplaceWithGetType(context.Document, invocation, c),
                    equivalenceKey: "ReplaceWithGetType"),
                diagnostic);
        }

        private async Task<Document> ReplaceWithGetType(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            // 构造新节点: EF.GetType() -> 成员访问 EF.GetType
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
            var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                memberAccess.Expression,
                SyntaxFactory.IdentifierName("GetType"));

            // 新的调用表达式，参数列表保持为空（也可以新建一个 ArgumentListSyntax）
            var newInvocation = SyntaxFactory.InvocationExpression(newMemberAccess)
                .WithArgumentList(SyntaxFactory.ArgumentList());

            editor.ReplaceNode(invocation, newInvocation);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }
    }
}