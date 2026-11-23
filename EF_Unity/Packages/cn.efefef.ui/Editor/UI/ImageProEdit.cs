/*
 * ================================================
 * Describe:      Custom inspector for ImagePro.
 * Author:        Alvin8412
 * CreationTime:  2026-05-23 15:50:04
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-23 22:10:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using EasyFramework.Edit;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 图片升级版编辑器，处理圆角与灰度材质的创建和参数同步。
    /// <para>Custom inspector for ImagePro that manages rounded and grayscale material creation and parameter sync.</para>
    /// </summary>
    [CustomEditor(typeof(ImagePro), true)]
    public class ImageProEdit : ImageEditor
    {
        private SerializedProperty _cornerSize;     //  圆角弧度属性
        private SerializedProperty _materialTypes;  //  材质类型属性
        private ImageProMaterialType _imageProMaterialType; //  上次应用的材质类型

        //  编辑器预览材质缓存（圆角参数已走顶点，材质仅作 shader 载体）
        private Material _roundMaterial;
        private Material _grayMaterial;

        protected override void OnEnable()
        {
            base.OnEnable();
            _cornerSize = serializedObject.FindProperty("cornerArc");
            _materialTypes = serializedObject.FindProperty("imageProMaterialType");
            _imageProMaterialType = (ImageProMaterialType)_materialTypes.enumValueIndex;

            //  编辑态也要开启 Canvas 顶点通道，否则预览时圆角参数被合批丢弃
            ImagePro targetPro = target as ImagePro;
            if (targetPro != null)
                targetPro.EnableCanvasShaderChannels();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (_cornerSize.floatValue < 0f)
                _cornerSize.floatValue = 0f;

            ImageProMaterialType selected = (ImageProMaterialType)EditorGUILayout.EnumPopup(
                LC.Combine("Ui.SelectMaterials"), _imageProMaterialType);
            _materialTypes.enumValueIndex = (int)selected;

            if (selected == ImageProMaterialType.Round)
                _cornerSize.floatValue = EditorGUILayout.FloatField(LC.Combine("Ui.RadiusSize"), _cornerSize.floatValue);

            bool dirty = false;
            if (selected != _imageProMaterialType)
            {
                ApplyPreviewMaterial(selected);
                dirty = true;
            }

            serializedObject.ApplyModifiedProperties();

            //  Apply 后再请求重建：此时组件字段已是最新值，OnPopulateMesh 按新类型/圆角写顶点参数。
            ImagePro targetPro = target as ImagePro;
            if (dirty && targetPro != null)
            {
                targetPro.SetVerticesDirty();
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }

            _imageProMaterialType = selected;
        }

        //  仅作 shader 载体切换，不写任何材质属性（圆角/灰度参数分别走顶点与固定值）
        private void ApplyPreviewMaterial(ImageProMaterialType type)
        {
            switch (type)
            {
                case ImageProMaterialType.Default:
                    if (m_Material != null)
                        m_Material.objectReferenceValue = null;
                    break;
                case ImageProMaterialType.Round:
                {
                    Shader shader = Shader.Find(ImagePro.RoundedRectangleShaderName);
                    if (shader == null) return;
                    if (_roundMaterial == null || _roundMaterial.shader != shader)
                    {
                        if (_roundMaterial != null) DestroyImmediate(_roundMaterial);
                        _roundMaterial = new Material(shader);
                    }
                    if (m_Material != null)
                        m_Material.objectReferenceValue = _roundMaterial;
                    break;
                }
                case ImageProMaterialType.Gray:
                {
                    Shader shader = Shader.Find(ImagePro.DefaultGrayShaderName);
                    if (shader == null) return;
                    if (_grayMaterial == null || _grayMaterial.shader != shader)
                    {
                        if (_grayMaterial != null) DestroyImmediate(_grayMaterial);
                        _grayMaterial = new Material(shader);
                    }
                    if (m_Material != null)
                        m_Material.objectReferenceValue = _grayMaterial;
                    break;
                }
            }
        }
    }
}
