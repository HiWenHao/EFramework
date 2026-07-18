using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EasyFramework
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EfGetAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(DependencyRules.UseGetTypeInstead);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            // 检查是否为成员访问表达式
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;

            if (memberAccess.Name.Identifier.Text != "Get" || invocation.ArgumentList.Arguments.Count != 0)
                return;

            // 使用语义模型获取左侧表达式的类型信息
            ExpressionSyntax leftExpression = memberAccess.Expression;
            TypeInfo typeInfo = context.SemanticModel.GetTypeInfo(leftExpression, context.CancellationToken);
            var typeSymbol = typeInfo.Type;

            // 如果类型为 null 或错误，跳过
            if (typeSymbol == null || typeSymbol.TypeKind == TypeKind.Error) return;

            // 检查类型名称是否为 "EF"（忽略命名空间）
            if (typeSymbol.Name != "EF") return;
            var diagnostic = Diagnostic.Create(
                DependencyRules.UseGetTypeInstead,
                invocation.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}