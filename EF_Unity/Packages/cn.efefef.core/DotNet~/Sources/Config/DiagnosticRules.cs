using Microsoft.CodeAnalysis;

namespace EasyFramework
{
    /// <summary>
    /// 所有依赖相关诊断规则
    /// </summary>
    internal static class DependencyRules
    {
        // 自依赖错误
        public static readonly DiagnosticDescriptor SelfDependency = new DiagnosticDescriptor(
            id: DiagnosticIds.SelfDependencyId,
            title: "不能依赖自身",
            messageFormat: "类型 '{0}' 不能依赖自身",
            category: "Dependency",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // 依赖循环
        public static readonly DiagnosticDescriptor CyclicDependency = new DiagnosticDescriptor(
            id: DiagnosticIds.CyclicDependencyId,
            title: "检测到循环依赖",
            messageFormat: "循环依赖: {0}",
            category: "Dependency",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // 使用Get() 函数
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