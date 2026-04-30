#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Managers.Pool.Editor
{
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (PoolManager)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Dump Leaks"))
            {
                mgr.DumpAllLeaks();
            }
        }
    }
}
#endif