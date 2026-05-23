namespace EasyFramework
{
    /// <summary>
    /// 所有的诊断ID
    /// </summary>
    public record DiagnosticIds
    {
        // 依赖类型丢失
        public const string DependencyTypeMissingId    = "EF0001";
        // 循环依赖
        public const string CyclicDependencyId         = "EF0002";
        
        public const string UseGetTypeInsteadId = "EF0003";
        
    }
}