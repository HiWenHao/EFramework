/*
 * ================================================
 * Describe:      Custom inspector for ImagePro.
 * Author:        Alvin8412
 * CreationTime:  2026-05-23 15:50:04
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-23 17:55:00
 * ScriptVersion: 0.2
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
        //  浮点比较容差与圆角上限余量
        private const float FloatEpsilon = 0.000000001f;
        private const float CornerEpsilon = 0.0001f;
        private const float HalfRatio = 0.5f;

        private SerializedProperty _cornerSize;     //  圆角弧度属性
        private SerializedProperty _materialTypes;  //  材质类型属性
        private ImageProMaterialType _imageProMaterialType; //  上次应用的材质类型
        private float _roundSize;   //  上次应用的圆角弧度
        private float _widthAtLastFrame;    //  上次应用的宽度
        private float _heightAtLastFrame;   //  上次应用的高度

        //  圆角材质缓存（按 shader 独立，避免跨类型复用）
        private Material _roundMaterial;
        //  灰度材质缓存
        private Material _grayMaterial;

        private static readonly int Width = Shader.PropertyToID("_Width");    //  宽度属性 ID
        private static readonly int Height = Shader.PropertyToID("_Height");  //  高度属性 ID
        private static readonly int CornerSize = Shader.PropertyToID("_CornerSize");   //  圆角属性 ID
        private static readonly int RectCenter = Shader.PropertyToID("_RectCenter");   //  矩形中心属性 ID
        private static readonly int PixelWidth = Shader.PropertyToID("_PixelWidth");   //  每像素对应本地单位 ID
        private static readonly int GrayScaleAmount = Shader.PropertyToID("_GrayScaleAmount");    //  灰度属性 ID

        protected override void OnEnable()
        {
            base.OnEnable();
            _cornerSize = serializedObject.FindProperty("cornerArc");
            _materialTypes = serializedObject.FindProperty("imageProMaterialType");
            _imageProMaterialType = (ImageProMaterialType)_materialTypes.enumValueIndex;
            _roundSize = _cornerSize.floatValue;

            RectTransform rect = (target as ImagePro)?.rectTransform;
            _widthAtLastFrame = rect != null ? Mathf.Abs(rect.rect.width) : 0f;
            _heightAtLastFrame = rect != null ? Mathf.Abs(rect.rect.height) : 0f;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            if (_cornerSize.floatValue < 0f)
            {
                _cornerSize.floatValue = 0f;
                _roundSize = 0f;
            }

            ImageProMaterialType selected = (ImageProMaterialType)EditorGUILayout.EnumPopup(
                LC.Combine("Ui.SelectMaterials"), _imageProMaterialType);
            _materialTypes.enumValueIndex = (int)selected;

            switch (selected)
            {
                case ImageProMaterialType.Default:
                    if (_imageProMaterialType != ImageProMaterialType.Default && m_Material != null)
                        m_Material.objectReferenceValue = null;
                    break;
                case ImageProMaterialType.Round:
                    DrawRoundSettings();
                    break;
                case ImageProMaterialType.Gray:
                    if (_imageProMaterialType != ImageProMaterialType.Gray)
                        ResetGrayMaterial();
                    break;
            }

            _imageProMaterialType = selected;
            if (GUI.changed)
                serializedObject.ApplyModifiedProperties();
        }

        //  绘制圆角参数并在变化时重建圆角材质
        private void DrawRoundSettings()
        {
            _cornerSize.floatValue = EditorGUILayout.FloatField(LC.Combine("Ui.RadiusSize"), _cornerSize.floatValue);

            RectTransform rect = (target as ImagePro)?.rectTransform;
            float newWidth = rect != null ? Mathf.Abs(rect.rect.width) : 0f;
            float newHeight = rect != null ? Mathf.Abs(rect.rect.height) : 0f;
            bool typeChanged = _imageProMaterialType != ImageProMaterialType.Round;
            if (typeChanged || !FloatEqual(_roundSize, _cornerSize.floatValue) ||
                !FloatEqual(newWidth, _widthAtLastFrame) || !FloatEqual(newHeight, _heightAtLastFrame))
            {
                ResetRoundRectangleMaterial();
            }

            _roundSize = _cornerSize.floatValue;
            _widthAtLastFrame = newWidth;
            _heightAtLastFrame = newHeight;
        }

        // 浮点比较，容忍极小误差
        private static bool FloatEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < FloatEpsilon;
        }

        //  创建或同步圆角材质
        private void ResetRoundRectangleMaterial()
        {
            Shader shader = Shader.Find(ImagePro.RoundedRectangleShaderName);
            if (shader == null)
                return;

            if (_roundMaterial == null || _roundMaterial.shader != shader)
            {
                if (_roundMaterial != null)
                    DestroyImmediate(_roundMaterial);
                _roundMaterial = new Material(shader);
            }

            if (m_Material != null && m_Material.objectReferenceValue != _roundMaterial)
                m_Material.objectReferenceValue = _roundMaterial;

            float width = _widthAtLastFrame;
            float height = _heightAtLastFrame;
            float maxCorner = Mathf.Max(0f, Mathf.Min(width, height) * HalfRatio - CornerEpsilon);
            if (_cornerSize.floatValue > maxCorner)
            {
                _cornerSize.floatValue = maxCorner;
                EditorUtility.SetDirty(target);
            }

            RectTransform rect = (target as ImagePro)?.rectTransform;
            Vector2 center = rect != null ? rect.rect.center : Vector2.zero;
            //  CanvasScaler.scaleFactor 不体现在 lossyScale 中，需显式相乘
            float scale = rect != null ? Mathf.Max(Mathf.Abs(rect.lossyScale.x), 0.0001f) : 1f;
            Canvas canvas = rect != null ? rect.GetComponentInParent<Canvas>() : null;
            float scaleFactor = canvas != null ? Mathf.Max(Mathf.Abs(canvas.scaleFactor), 0.0001f) : 1f;
            _roundMaterial.SetFloat(Width, width);
            _roundMaterial.SetFloat(Height, height);
            _roundMaterial.SetVector(RectCenter, new Vector4(center.x, center.y, 0f, 0f));
            _roundMaterial.SetFloat(PixelWidth, 1f / (scale * scaleFactor));
            _roundMaterial.SetFloat(CornerSize, _cornerSize.floatValue);
        }

        //  创建灰度材质并直接把灰度参数写进材质本身，避免依赖运行时 SetGray 的 clone 状态
        private void ResetGrayMaterial()
        {
            Shader shader = Shader.Find(ImagePro.DefaultGrayShaderName);
            if (shader == null)
                return;

            if (_grayMaterial == null || _grayMaterial.shader != shader)
            {
                if (_grayMaterial != null)
                    DestroyImmediate(_grayMaterial);
                _grayMaterial = new Material(shader);
            }

            //  直接设置材质属性，保证往返切换后灰度依然有效
            _grayMaterial.SetFloat(GrayScaleAmount, 1f);

            if (m_Material != null && m_Material.objectReferenceValue != _grayMaterial)
                m_Material.objectReferenceValue = _grayMaterial;
        }
    }
}
