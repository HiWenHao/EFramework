/*
 * ================================================
 * Describe:        升级版本的Image组件
 * Author:          Alvin8412
 * CreationTime:    2026-05-23 15:19:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-24 17:58:02
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Assets;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace EasyFramework.Ui
{
    /// <summary>
    /// 升级版本Image组件的材质类型
    /// <para>The material type of the upgraded version of the Image component</para>
    /// </summary>
    public enum ImageProMaterialType
    {
        /// <summary>
        /// 默认UI材质
        /// <para>Default UI material</para>
        /// </summary>
        Default = 0,

        /// <summary>
        /// 圆角
        /// <para>Rounded corners</para>
        /// </summary>
        Round = 1,

        /// <summary>
        /// 灰度图
        /// <para>Grayscale</para>
        /// </summary>
        Gray = 2,
    }

    /// <summary>
    /// 升级版本的Image组件
    /// </summary>
    public class ImagePro : Image
    {
        // 内置 shader 资源路径（单一真源）
        public const string RoundedRectangleShaderName = "UI/RoundedRectangle";
        public const string DefaultGrayShaderName = "UI/DefaultGray";

        [HeaderPro("设置", "Settings", HeaderProType.Title)]
        [HeaderPro("资源地址", "Address", HeaderProType.Note)]
        [SerializeField] private string address;

        [HeaderPro("圆角弧度", "Corner Arc", HeaderProType.Note)]
        [SerializeField] [Range(0, 200)] private float cornerArc = 10;

        [HeaderPro("材质类型", "Material Type", HeaderProType.Note)]
        [SerializeField] private ImageProMaterialType imageProMaterialType;

        private static readonly int Width = Shader.PropertyToID("_Width");
        private static readonly int Height = Shader.PropertyToID("_Height");
        private static readonly int CornerSize = Shader.PropertyToID("_CornerSize");
        private static readonly int GrayScaleAmount = Shader.PropertyToID("_GrayScaleAmount");

        private static Shader _roundedRectangleShader;
        private static Shader _defaultGrayShader;

        private Material _dynamicMaterial; // 动态创建的材质，OnDestroy 时释放
        private CancellationTokenSource _loadCts; // HTTP 下载取消令牌
        private IDisposable _assetHandle; // 当前资源加载句柄，换图/销毁时释放

        private static Func<string, CancellationToken, UniTask<Texture2D>> _defaultTextureDownloader =
            DefaultLoadTextureAsync;

        /// <summary>
        /// 默认纹理下载器 —— 所有 ImagePro 实例共享。通过 SetTextureDownloader() 替换为自己的实现
        /// <para>Default Texture Downloader - Shared among all ImagePro instances.
        /// Replace via SetTextureDownloader() with your own implementation</para>
        /// </summary>
        public static Func<string, CancellationToken, UniTask<Texture2D>> DefaultTextureDownloader =>
            _defaultTextureDownloader;

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
                    if (_roundedRectangleShader == null)
                        _roundedRectangleShader = Shader.Find(RoundedRectangleShaderName);
                    var shader = _roundedRectangleShader;
                    if (shader == null)
                    {
                        D.Error($"[ UiManager ] [ ImagePro ] {gameObject.name} shader UI/RoundedRectangle not found");
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
                    if (_defaultGrayShader == null)
                        _defaultGrayShader = Shader.Find(DefaultGrayShaderName);
                    var shader = _defaultGrayShader;
                    if (shader == null)
                    {
                        D.Error($"[ UiManager ] [ ImagePro ] {gameObject.name} shader UI/DefaultGray not found");
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

        // 释放当前资源句柄（幂等，Dispose 内部已保证）
        private void ReleaseAssetHandle()
        {
            _assetHandle?.Dispose();
            _assetHandle = null;
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
                ReleaseAssetHandle();
            }

            address = null;
        }

        // 卸载图片
        private async UniTask UnloadAsync()
        {
            if (string.IsNullOrEmpty(address)) return;

            await UniTask.CompletedTask;
            if (address.StartsWith("http"))
            {
                DestroyHttpSprite();
            }
            else
            {
                sprite = null;
                ReleaseAssetHandle();
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
            if (_dynamicMaterial == null || !_dynamicMaterial.shader.name.Equals(RoundedRectangleShaderName)) return;
            _dynamicMaterial.SetFloat(Width, Mathf.Abs(rectTransform.rect.width) * rectTransform.lossyScale.x);
            _dynamicMaterial.SetFloat(Height, Mathf.Abs(rectTransform.rect.height) * rectTransform.lossyScale.y);
            _dynamicMaterial.SetFloat(CornerSize, CheckConnerArc());
        }

        /// <summary>
        /// 替换默认纹理下载器
        /// <para>Replace the default texture downloader</para>
        /// </summary>
        /// <param name="downloader">自定义下载器<para>Custom downloader</para></param>
        public static void SetTextureDownloader(Func<string, CancellationToken, UniTask<Texture2D>> downloader)
        {
            _defaultTextureDownloader = downloader ?? DefaultLoadTextureAsync;
        }

        /// <summary>
        /// 通过链接地址设置精灵图 - 支持 HTTP 地址和资源路径
        /// <para>Set sprite via URL (HTTP) or resource path</para>
        /// </summary>
        /// <param name="url">图片地址 / 资源路径<para>Image URL or resource path</para></param>
        public async UniTask<Sprite> SetSpriteByUrl(string url)
        {
            return await SetSprite(url);
        }

        /// <summary>
        /// 通过图集类型和精灵名设置精灵图
        /// <para>Set sprite via atlas type and sprite name</para>
        /// </summary>
        /// <param name="uiAtlasName">图集名<para>Atlas name</para></param>
        /// <param name="spriteName">精灵名称<para>Sprite name in atlas</para></param>
        public async UniTask<Sprite> SetSprite(string uiAtlasName, string spriteName)
        {
            AssetHandle<SpriteAtlas> atlasHandle = await AssetsManager.Instance.LoadAsync<SpriteAtlas>(uiAtlasName);
            if (atlasHandle is not { IsValid: true })
            {
                atlasHandle?.Dispose();
                D.Warning($"[ UiManager ] [ ImagePro ] Atlas '{uiAtlasName}' not found for UIAtlasType.{type}");
                return null;
            }

            Sprite sp = atlasHandle.Asset.GetSprite(spriteName);
            if (sp == null)
            {
                // Sprite 取不到，句柄不再需要
                atlasHandle.Dispose();
                D.Warning($"[ UiManager ] [ ImagePro ] Sprite '{spriteName}' not found in atlas '{uiAtlasName}'.");
                return null;
            }

            // 换图：释放旧句柄，保留新句柄直到本组件销毁
            ReleaseAssetHandle();
            _assetHandle = atlasHandle;
            sprite = sp;
            return sp;
        }

        /// <summary>
        /// 通过链接地址设置精灵图（支持 HTTP 地址和资源路径）
        /// <para>Set sprite via URL (HTTP) or resource path</para>
        /// </summary>
        /// <param name="url">图片地址 / 资源路径<para>Image URL or resource path</para></param>
        public async UniTask<Sprite> SetSprite(string url)
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
                    ReleaseAssetHandle();
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
            IDisposable newHandle;
            var index = url.LastIndexOf('?');
            if (index > 0)
            {
                string atlasName = url[..index];
                string spriteName = url[(index + 1)..];
                var atlasHandle = await AssetsManager.Instance.LoadAsync<SpriteAtlas>(atlasName);
                if (atlasHandle == null || !atlasHandle.IsValid)
                {
                    atlasHandle?.Dispose();
                    D.Warning($"[ UiManager ] [ ImagePro ] Not found {atlasName} atlas..");
                    return null;
                }

                hasAtlas = true;
                newHandle = atlasHandle;
                newSprite = atlasHandle.Asset.GetSprite(spriteName);
                if (newSprite == null)
                {
                    D.Warning($"[ UiManager ] [ ImagePro ] {spriteName} not found in {atlasName} atlas.");
                    atlasHandle.Dispose();
                    return null;
                }
            }
            else
            {
                var spriteHandle = await AssetsManager.Instance.LoadAsync<Sprite>(url);
                if (spriteHandle == null)
                {
                    D.Warning($"[ UiManager ] [ ImagePro ] Not found {url} sprite.");
                    return null;
                }

                newHandle = spriteHandle;
                newSprite = spriteHandle.Asset;
            }

            if (null == newSprite)
            {
                newHandle?.Dispose();
                if (!hasAtlas) D.Warning($"[ UiManager ] [ ImagePro ] Not found {url} sprite.");
                return null;
            }

            if (address == url && this)
            {
                ReleaseAssetHandle();
                _assetHandle = newHandle;
                sprite = newSprite;
            }
            else
                newHandle?.Dispose();

            return newSprite;
        }
    }
}