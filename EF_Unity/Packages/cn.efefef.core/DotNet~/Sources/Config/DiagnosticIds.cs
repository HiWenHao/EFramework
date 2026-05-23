namespace EasyFramework
{
    /// <summary>
    /// 所有的诊断ID
    /// </summary>
    public record DiagnosticIds
    {
        // 测试函数使用报错
        public const string UseGetTypeInsteadId        = "EF0001";
        // 依赖自身
        public const string SelfDependencyId           = "EF0002";
        // 循环依赖
        public const string CyclicDependencyId         = "EF0003";
        
        
    }
}