using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EasyFramework
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EFDependencyAnalyzer : DiagnosticAnalyzer
    {
        private const string DependencyAttributeFullName = "EasyFramework.DependencyAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(DependencyRules.SelfDependency, DependencyRules.CyclicDependency);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var typeDeps = new Dictionary<string, (INamedTypeSymbol symbol, List<(string dep, Location location)> deps)>();

                // 收集并立即检查自依赖
                compilationStartContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    var attributes = namedType.GetAttributes();

                    var depsWithLocation = new List<(string, Location)>();
                    foreach (var attr in attributes)
                    {
                        if (attr.AttributeClass?.ToString() != DependencyAttributeFullName) continue;
                        // 获取特性在源代码中的位置
                        Location attrLocation = GetAttributeLocation(attr, symbolContext.CancellationToken);
                        if (attr.ConstructorArguments.Length != 1 || attr.ConstructorArguments[0].Kind != TypedConstantKind.Type) continue;
                        var typeArg = (INamedTypeSymbol)attr.ConstructorArguments[0].Value!;
                        string depFullName = typeArg.ToString();
                        string currentFullName = namedType.ToString();

                        // 自依赖检查
                        if (currentFullName == depFullName)
                        {
                            var diag = Diagnostic.Create(DependencyRules.SelfDependency, attrLocation, currentFullName);
                            symbolContext.ReportDiagnostic(diag);
                        }
                        else
                            depsWithLocation.Add((depFullName, attrLocation));
                    }

                    if (!depsWithLocation.Any()) return;
                    typeDeps[namedType.ToString()] = (namedType, depsWithLocation);
                }, SymbolKind.NamedType);

                // 编译结束时检测间接循环
                compilationStartContext.RegisterCompilationEndAction(endContext =>
                {
                    // 构建图（仅包含非自依赖的边）
                    var graph = new DependencyGraph();
                    foreach (var kvp in typeDeps)
                    {
                        string from = kvp.Key;
                        foreach (var (dep, _) in kvp.Value.deps)
                        {
                            graph.AddDependency(from, dep);
                        }
                    }

                    var cycles = graph.FindAllCycles();
                    if (!cycles.Any()) return;

                    // 为每个参与循环的依赖特性上报错误
                    foreach (var cycle in cycles)
                    {
                        // cycle 是一个列表: [A, B, C, A] (首尾相同)
                        for (int i = 0; i < cycle.Count - 1; i++)
                        {
                            string from = cycle[i];
                            string to = cycle[i + 1];

                            // 找到从 from 指向 to 的那个 Dependency 特性的位置
                            if (!typeDeps.TryGetValue(from, out var info)) continue;
                            var location = info.deps.FirstOrDefault(d => d.dep == to).location;
                            if (location == null || location == Location.None) continue;
                            string cycleDesc = string.Join(" -> ", cycle);
                            var diag = Diagnostic.Create(DependencyRules.CyclicDependency, location, cycleDesc);
                            endContext.ReportDiagnostic(diag);
                        }
                    }
                });
            });
        }

        // 获取特性的具体使用位置
        private static Location GetAttributeLocation(AttributeData attribute, CancellationToken cancellationToken)
        {
            var syntaxRef = attribute.ApplicationSyntaxReference;
            if (syntaxRef == null) return Location.None;
            var syntax = syntaxRef.GetSyntax(cancellationToken);
            return syntax.GetLocation();
        }
    }
}