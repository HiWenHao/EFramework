using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace PrefabsCompare
    {
        public class PrefabsCompareStyle
        {
            public static readonly Texture2D failImg = EditorGUIUtility.FindTexture("TestFailed");
                    
            public static readonly Texture2D successImg = EditorGUIUtility.FindTexture("TestPassed");
                    
            public static readonly Texture2D inconclusiveImg = EditorGUIUtility.FindTexture("TestInconclusive");
        }
    }
}