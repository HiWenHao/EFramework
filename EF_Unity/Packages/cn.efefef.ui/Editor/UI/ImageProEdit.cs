/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-23 15:50:04
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-23 15:50:04
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using UnityEngine;
using EasyFramework;
using EasyFramework.Edit;
using UnityEditor;
using UnityEditor.UI;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 图片升级版编辑器
    /// </summary>
    [CustomEditor(typeof(ImagePro), true)]
    [CanEditMultipleObjects]
    public class ImageProEdit : ImageEditor
    {
        private SerializedProperty _cornerSize;
        private SerializedProperty _materialTypes;
        private ImageProMaterialType _imageProMaterialType;
        private float _roundSize;
        private ImagePro _graphic;
        private float _widthAtLastFrame;
        private float _heightAtLastFrame;
        private Material _material;

        private static readonly int Width = Shader.PropertyToID("_Width");
        private static readonly int Height = Shader.PropertyToID("_Height");
        private static readonly int CornerSize = Shader.PropertyToID("_CornerSize");

        protected override void OnEnable()
        {
            base.OnEnable();
            _graphic = target as ImagePro;
            _cornerSize = serializedObject.FindProperty("cornerArc");
            _materialTypes = serializedObject.FindProperty("imageProMaterialType");
            _imageProMaterialType = (ImageProMaterialType)_materialTypes.enumValueFlag;
            _roundSize = _cornerSize.floatValue;

            _widthAtLastFrame = _graphic.rectTransform.rect.width * _graphic.rectTransform.lossyScale.x;
            _heightAtLastFrame = _graphic.rectTransform.rect.height * _graphic.rectTransform.lossyScale.y;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (_cornerSize.floatValue < 0)
            {
                _cornerSize.floatValue = 0;
                _roundSize = 0;
            }

            _materialTypes.enumValueIndex = (int)(ImageProMaterialType)EditorGUILayout.EnumPopup(LC.Combine(Lc.Select, Lc.Materials), _imageProMaterialType);

            switch ((ImageProMaterialType)_materialTypes.enumValueIndex)
            {
                case ImageProMaterialType.Default:
                    if (_imageProMaterialType != ImageProMaterialType.Default)
                        m_Material.objectReferenceValue = null;
                    break;
                case ImageProMaterialType.Round:
                    _cornerSize.floatValue = EditorGUILayout.FloatField(LC.Combine(Lc.Radius, Lc.Size), _cornerSize.floatValue);
                    float newWidth = _graphic.rectTransform.rect.width * _graphic.rectTransform.lossyScale.x;
                    float newHeight = _graphic.rectTransform.rect.height * _graphic.rectTransform.lossyScale.y;
                    if (((ImageProMaterialType)_materialTypes.enumValueFlag != _imageProMaterialType) ||
                        !FloatEqual(_roundSize, _cornerSize.floatValue) || !FloatEqual(newWidth, _widthAtLastFrame) ||
                        !FloatEqual(newHeight, _heightAtLastFrame))
                    {
                        ResetRoundRectangleMaterial();
                    }

                    _roundSize = _cornerSize.floatValue;
                    _widthAtLastFrame = _graphic.rectTransform.rect.width * _graphic.rectTransform.lossyScale.x;
                    _heightAtLastFrame = _graphic.rectTransform.rect.height * _graphic.rectTransform.lossyScale.y;
                    break;
                case ImageProMaterialType.Gray:
                    if ((ImageProMaterialType)_materialTypes.enumValueFlag != _imageProMaterialType)
                        ResetGrayMaterial();
                    break;
                default:
                    break;
            }

            _imageProMaterialType = (ImageProMaterialType)_materialTypes.enumValueIndex;
            if (!GUI.changed) return;
            serializedObject.ApplyModifiedProperties();
        }

        bool FloatEqual(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.000000001f;
        }

        private void ResetRoundRectangleMaterial()
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find("UI/RoundedRectangle"));
            }

            if (m_Material.objectReferenceValue != _material)
            {
                m_Material.objectReferenceValue = _material;
            }

            float width = _graphic.rectTransform.rect.width * _graphic.rectTransform.lossyScale.x;
            float height = _graphic.rectTransform.rect.height * _graphic.rectTransform.lossyScale.y;
            if (_cornerSize.floatValue > Math.Min(width, height) * 0.5f - 0.0001f)
            {
                _cornerSize.floatValue = Math.Min(width, height) * 0.5f - 0.0001f;
                EditorUtility.SetDirty(_graphic);
            }

            _material.SetFloat(Width, width);
            _material.SetFloat(Height, height);
            _material.SetFloat(CornerSize, _cornerSize.floatValue);
        }

        private void ResetGrayMaterial()
        {
            if (_material == null)
                _material = new Material(Shader.Find("UI/DefaultGray"));

            if (m_Material.objectReferenceValue == _material) return;
            m_Material.objectReferenceValue = _material;
            _graphic.SetGray(true);
            // material.SetFloat("_GrayEnabled", 1);
        }
    }
}