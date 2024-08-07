﻿#if UNITY_EDITOR
using System.Reflection;

namespace YooAsset
{
    public static class EditorSimulateModeHelper
    {
        private static System.Type _classType;

        /// <summary>
        /// 编辑器下模拟构建清单
        /// </summary>
        public static SimulateBuildResult SimulateBuild(string buildPipelineName, string packageName)
        {
            if (_classType == null)
                _classType = Assembly.Load("YooAsset.Editor").GetType("YooAsset.Editor.AssetBundleSimulateBuilder");

            return (SimulateBuildResult)InvokePublicStaticMethod(_classType, "SimulateBuild", buildPipelineName, packageName);
        }

        /// <summary>
        /// 编辑器下模拟构建清单
        /// </summary>
        public static SimulateBuildResult SimulateBuild(EDefaultBuildPipeline buildPipeline, string packageName)
        {
            return SimulateBuild(buildPipeline.ToString(), packageName);
        }

        private static object InvokePublicStaticMethod(System.Type type, string method, params object[] parameters)
        {
            var methodInfo = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                UnityEngine.Debug.LogError($"{type.FullName} not found method : {method}");
                return null;
            }
            return methodInfo.Invoke(null, parameters);
        }
    }
}
#else
namespace YooAsset
{ 
    public static class EditorSimulateModeHelper
    {
        public static SimulateBuildResult SimulateBuild(string buildPipelineName, string packageName) 
        {
            throw new System.Exception("Only support in unity editor !");
        }

        public static SimulateBuildResult SimulateBuild(EDefaultBuildPipeline buildPipeline, string packageName)
        {
            throw new System.Exception("Only support in unity editor !");
        }
    }
}
#endif