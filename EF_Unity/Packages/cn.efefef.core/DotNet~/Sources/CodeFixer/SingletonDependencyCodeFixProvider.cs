using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EasyFramework
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SingletonDependencyCodeFixProvider)), Shared]
    public class SingletonDependencyCodeFixProvider : CodeFixProvider
    {
        private const string SingletonAttributeFullName = "EasyFramework.SingletonAttribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticIds.DependencyTypeMissingId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // 找到触发诊断的特性节点（应该是 DependencyAttribute 的应用位置）
            var attributeSyntax = root?.FindNode(diagnosticSpan)?.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
            if (attributeSyntax == null) return;

            // 获取被依赖的类型名称（从诊断消息中解析或从语法树中提取，这里简化）
            var classDecl = attributeSyntax.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (classDecl == null) return;

            // 提供修复：跳转到缺少 Singleton 标记的类（这里需要定位到被依赖的类，较为复杂，示例略）
            // 实际使用中，可以提供一个“添加 SingletonAttribute”的修复，但需要定位到目标类型。
            // 为简洁，该示例提供一个简单的提示修复。
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "为依赖类型添加 Singleton 特性（需要手动定位）",
                    createChangedDocument: c => Task.FromResult(context.Document),
                    equivalenceKey: "AddSingletonAttribute"),
                diagnostic);
        }
    }
}