/*
 * ================================================
 * Describe:      升级版本的Image组件
 * Author:        Alvin8412
 * CreationTime:  2026-05-23 15:19:01
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-30 01:39:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Assets;
using UnityEngine;
using UnityEngine.U2D;
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
        [SerializeField] private string address;
        [SerializeField] [Range(0, 200)] private float cornerArc = 10;
        [SerializeField] private ImageProMaterialType imageProMaterialType;

        private static readonly int Width = Shader.PropertyToID("_Width");
        private static readonly int Height = Shader.PropertyToID("_Height");
        private static readonly int CornerSize = Shader.PropertyToID("_CornerSize");
        private static readonly int GrayScaleAmount = Shader.PropertyToID("_GrayScaleAmount");

        private Material _dynamicMaterial; // 动态创建的材质，OnDestroy 时释放
        private CancellationTokenSource _loadCts; // HTTP 下载取消令牌

        /// <summary>
        /// 默认纹理下载器 —— 所有 ImagePro 实例共享， 用户可替换为自己的实现（例如使用 Http 包）
        /// <para>Default Texture Downloader - Shared among all ImagePro instances.
        /// Users can replace it with their own implementation<br/>(for example, using the Http package)</para>
        /// </summary>
        public static Func<string, CancellationToken, UniTask<Texture2D>> DefaultTextureDownloader { get; set; } =
            DefaultLoadTextureAsync;

        /// <summary> 圆角弧度 </summary>
        public float CornerArc
        {
            get => cornerArc;
            set
            {
                cornerArc = Mathf.Clamp(value, 0, 200);
                if (imageProMaterialType == ImageProMaterialType.Round)
                    ResetRoundRectangleSize();
            }
        }

        protected override void Awake()
        {
            switch (imageProMaterialType)
            {
                case ImageProMaterialType.Default:
                    material = defaultGraphicMaterial;
                    break;
                case ImageProMaterialType.Round:
                {
                    var shader = Shader.Find("UI/RoundedRectangle");
                    if (shader == null)
                    {
                        D.Error($"{gameObject.name} shader UI/RoundedRectangle not found");
                        break;
                    }

                    _dynamicMaterial = new Material(shader);
                    _dynamicMaterial.SetFloat(Width, Mathf.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x);
                    _dynamicMaterial.SetFloat(Height,
                        Mathf.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
                    _dynamicMaterial.SetFloat(CornerSize, CheckConnerArc());
                    material = _dynamicMaterial;
                    break;
                }
                case ImageProMaterialType.Gray:
                {
                    var shader = Shader.Find("UI/DefaultGray");
                    if (shader == null)
                    {
                        D.Error($"{gameObject.name} shader UI/DefaultGray not found");
                        break;
                    }

                    _dynamicMaterial = new Material(shader);
                    material = _dynamicMaterial;
                    break;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 取消正在进行的 HTTP 下载
            CancelLoading();

            // 释放动态材质
            if (_dynamicMaterial != null)
            {
                Destroy(_dynamicMaterial);
                _dynamicMaterial = null;
            }

            // 释放图片资源
            UnloadInternal();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (imageProMaterialType != ImageProMaterialType.Round) return;
            ResetRoundRectangleSize();
        }

        #region 内部函数 - Private Functions

        // 检查圆角弧度
        private float CheckConnerArc()
        {
            float edgeSize = Mathf.Min(Mathf.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x,
                Mathf.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
            float maxConnerSize = edgeSize * 0.5f - 1f;
            return cornerArc > maxConnerSize ? maxConnerSize : cornerArc;
        }

        // 取消正在进行的 HTTP 下载
        private void CancelLoading()
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
        }

        // 卸载图片
        private void UnloadInternal()
        {
            if (string.IsNullOrEmpty(address)) return;

            if (address.StartsWith("http"))
            {
                DestroyHttpSprite();
            }
            else
            {
                sprite = null;
                // OnDestroy 时不能走异步 Release，已由 AssetsSystem 统一管理
            }

            address = null;
        }

        // 卸载图片
        private async UniTask UnloadAsync()
        {
            if (string.IsNullOrEmpty(address)) return;

            if (address.StartsWith("http"))
            {
                DestroyHttpSprite();
            }
            else
            {
                sprite = null;
                await AssetsManager.Instance.Release(address);
            }

            address = null;
        }

        // 销毁 HTTP 下载生成的 Sprite + Texture
        private void DestroyHttpSprite()
        {
            if (sprite == null) return;
            if (sprite.texture != null)
                Destroy(sprite.texture);
            Destroy(sprite);
            sprite = null;
        }

        /// <summary>
        /// 默认实现：使用 UnityWebRequest 下载纹理
        /// </summary>
        private static async UniTask<Texture2D> DefaultLoadTextureAsync(string url, CancellationToken token)
        {
            using var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest().ToUniTask(cancellationToken: token);

            return request.result != UnityEngine.Networking.UnityWebRequest.Result.Success
                ? throw new Exception($"Texture load failed: {request.error} (URL: {url})")
                : UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
        }

        #endregion

        /// <summary>
        /// 设置灰度
        /// <para>Set to gray</para>
        /// </summary>
        /// <param name="isGray">是否置灰</param>
        public void SetGray(bool isGray)
        {
            if (material == null) return;
            material.SetFloat(GrayScaleAmount, isGray ? 1 : 0);
        }

        /// <summary>
        /// 重置矩形圆角尺寸
        /// <para>Reset the size of the rectangular rounded corners</para>
        /// </summary>
        public void ResetRoundRectangleSize()
        {
            if (_dynamicMaterial == null || !_dynamicMaterial.shader.name.Equals("UI/RoundedRectangle")) return;
            _dynamicMaterial.SetFloat(Width, Mathf.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x);
            _dynamicMaterial.SetFloat(Height, Mathf.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
            _dynamicMaterial.SetFloat(CornerSize, CheckConnerArc());
        }

        /// <summary>
        /// 通过链接地址设置精灵图
        /// <para>Set the sprite image through the link address</para>
        /// </summary>
        /// <param name="url">图片地址<para>Image URL</para></param>
        public async UniTask<Sprite> SetSpriteByUrl(string url)
        {
            await UniTask.CompletedTask;
            if (address == url && sprite != null)
                return sprite;

            CancelLoading();
            var oldAddress = address;
            address = url;

            if (string.IsNullOrEmpty(url))
            {
                if (!string.IsNullOrEmpty(oldAddress))
                    await UnloadAsync();
                return null;
            }

            if (!string.IsNullOrEmpty(oldAddress) && oldAddress != url)
            {
                if (oldAddress.StartsWith("http"))
                    DestroyHttpSprite();
                else
                {
                    sprite = null;
                    AssetsManager.Instance.Release(oldAddress).Forget();
                }
            }

            if (url.StartsWith("http"))
            {
                _loadCts = new CancellationTokenSource();
                var token = _loadCts.Token;

                Texture2D tex;
                try
                {
                    tex = await DefaultTextureDownloader(url, token);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }

                if (token.IsCancellationRequested || tex == null)
                    return null;

                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f));
                sprite.name = $"HTTP_{url.GetHashCode():X8}";
                return sprite;
            }

            Sprite newSprite;
            var hasAtlas = false;
            var index = url.LastIndexOf('?');
            if (index > 0)
            {
                string atlasName = url[..index];
                string spriteName = url[(index + 1)..];
                SpriteAtlas spriteAtlas = await AssetsManager.Instance.LoadAsync<SpriteAtlas>(atlasName);
                if (null == spriteAtlas)
                {
                    D.Warning($"Not found {atlasName} atlas..");
                    return null;
                }

                hasAtlas = true;
                newSprite = spriteAtlas.GetSprite(spriteName);
                if (newSprite == null)
                    D.Warning($"{spriteName} not found in {atlasName} atlas.");
            }
            else
                newSprite = await AssetsManager.Instance.LoadAsync<Sprite>(url);

            if (null == newSprite)
            {
                if (!hasAtlas) D.Warning($"Not found {url} sprite.");
                return null;
            }

            if (address == url && this)
                sprite = newSprite;
            return newSprite;
        }
    }
}