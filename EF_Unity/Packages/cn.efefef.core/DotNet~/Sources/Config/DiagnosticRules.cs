using Microsoft.CodeAnalysis;

namespace EasyFramework
{
    /// <summary>
    /// 所有依赖相关诊断规则
    /// </summary>
    internal static class DependencyRules
    {
        public static readonly DiagnosticDescriptor MissingDependencyType = new DiagnosticDescriptor(
            id: DiagnosticIds.DependencyTypeMissingId,
            title: "依赖类型不存在",
            messageFormat: "类型 '{0}' 依赖的类型 '{1}' 在当前编译中不存在",
            category: "Dependency",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CyclicDependency = new DiagnosticDescriptor(
            id: DiagnosticIds.CyclicDependencyId,
            title: "检测到循环依赖",
            messageFormat: "循环依赖: {0}",
            category: "Dependency",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        public static readonly DiagnosticDescriptor UseGetTypeInstead = new DiagnosticDescriptor(
            id: DiagnosticIds.UseGetTypeInsteadId,
            title: "不推荐使用 EF.Get()",
            messageFormat: "EF.Get() 不推荐使用，请改用 EF.GetType()",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "将 EF.Get() 替换为 EF.GetType() 以获取类型信息.");
    }
}