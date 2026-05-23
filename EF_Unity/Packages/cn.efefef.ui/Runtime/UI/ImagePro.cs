/*
 * ================================================
 * Describe:      升级版本的Image组件
 * Author:        Alvin8412
 * CreationTime:  2026-05-23 15:19:01
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-23 15:19:01
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.IO;
using Cysharp.Threading.Tasks;
using EasyFramework.Systems.Assets;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework
{
    /// <summary>
    /// 升级版本Image组件的材质类型
    /// <para>The material type of the upgraded version of the Image component</para>
    /// </summary>
    public enum ImageProMaterialType
    {
        /// <summary>
        /// 默认UI材质
        /// </summary>
        Default = 0,

        /// <summary>
        /// 圆角
        /// </summary>
        Round = 1,

        /// <summary>
        /// 灰度图
        /// </summary>
        Gray = 2,
    }

    /// <summary>
    /// 升级版本的Image组件
    /// </summary>
    public class ImagePro : Image
    {
        private ImageProMaterialType _imageProMaterialType = ImageProMaterialType.Default;

        [SerializeField] private string url;

        public float cornerArc = 0;


        private static readonly int Width = Shader.PropertyToID("_Width");
        private static readonly int Height = Shader.PropertyToID("_Height");
        private static readonly int CornerSize = Shader.PropertyToID("_CornerSize");
        private static readonly int GrayScaleAmount = Shader.PropertyToID("_GrayScaleAmount");

        protected override void Awake()
        {
            if (!string.IsNullOrEmpty(url))
                SetSpriteByUrl(url).Forget();
            switch (_imageProMaterialType)
            {
                case ImageProMaterialType.Default:
                    material = defaultGraphicMaterial;
                    break;
                case ImageProMaterialType.Round:
                    material = new Material(Shader.Find("UI/RoundedRectangle"));
                    material.SetFloat(Width, Math.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x);
                    material.SetFloat(Height, Math.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
                    material.SetFloat(CornerSize, CheckConnerArc());
                    break;
                case ImageProMaterialType.Gray:
                    Shader shader = Shader.Find("UI/DefaultGray");
                    if (shader != null)
                        material = new Material(shader);
                    else
                        D.Error($"{gameObject.name} UI/DefaultGray not found");
                    break;
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            sprite = null;
            Unload().Forget();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (_imageProMaterialType != ImageProMaterialType.Round) return;
            ResetRoundRectangleSize();
        }

        // 检查圆角弧度
        private float CheckConnerArc()
        {
            float edgeSize = Math.Min(Math.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x,
                Math.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
            float maxConnerSize = edgeSize * 0.5f - 1f;
            return cornerArc > maxConnerSize ? maxConnerSize : cornerArc;
        }

        // 卸载图片
        private async UniTask Unload()
        {
            if (string.IsNullOrEmpty(url)) return;

            sprite = null;
            await AssetsSystem.Instance.Release(url);
            url = null;
        }
        
        /// <summary>
        /// 设置灰度
        /// <para>Set to gray</para>
        /// </summary>
        /// <param name="isGray">是否置灰</param>
        public void SetGray(bool isGray)
        {
            material?.SetFloat(GrayScaleAmount, isGray ? 1 : 0);
        }

        /// <summary>
        /// 重置矩形圆角尺寸
        /// </summary>
        public void ResetRoundRectangleSize()
        {
            if (_imageProMaterialType != ImageProMaterialType.Round || !material.shader.name.Equals("UI/RoundedRectangle")) return;
            material.SetFloat(Width, Math.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x);
            material.SetFloat(Height, Math.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
            material.SetFloat(CornerSize, CheckConnerArc());
        }

        /// <summary>
        /// 通过链接地址设置精灵图
        /// <para>Set the sprite image through the link address</para>
        /// </summary>
        /// <param name="address">地址</param>
        public async UniTask<Sprite> SetSpriteByUrl(string address)
        {
            await UniTask.CompletedTask;
            if (address.Equals(url)) return sprite;
            if (string.IsNullOrEmpty(address))
            {
                Unload().Forget();
                return null;
            }

            if (address.StartsWith("http"))
            {
                
            }

            
            return sprite;
        }



    }
}