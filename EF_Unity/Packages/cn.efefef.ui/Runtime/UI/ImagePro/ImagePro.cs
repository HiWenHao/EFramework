/*
 * ================================================
 * Describe:        升级版本的Image组件
 * Author:          Alvin8412
 * CreationTime:    2026-05-23 15:19:01
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-23 16:30:00
 * ScriptVersion:   0.2
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
    /// 升级版本的Image组件，支持圆角/灰度材质与图集、网络图片的异步加载与释放。
    /// <para>Upgraded Image component with rounded/grayscale materials and async loading of atlas or remote images.</para>
    /// </summary>
    public class ImagePro : Image
    {
        /// <summary>灰度着色器名称 <para>Name of the grayscale shader</para></summary>
        public const string DefaultGrayShaderName = "UI/DefaultGray";

        /// <summary>圆角着色器名称 <para>Name of the rounded-corner shader</para></summary>
        public const string RoundedRectangleShaderName = "UI/RoundedRectangle";

        //  http 地址前缀与图集内部去重键前缀（不会与 http URL 或资源路径冲突）
        private const string HttpPrefix = "http";
        private const string AtlasKeyPrefix = "atlas://";

        //  圆角弧度上限、默认值与几何常量
        private const int MaxCornerArc = 200;
        private const float DefaultCornerArc = 10f;
        private const float HalfRatio = 0.5f;
        private const float EdgeInset = 1f;
        private const float GrayOff = 0f;

        [HeaderPro("设置", "Settings", HeaderProType.Title)] [HeaderPro("资源地址", "Address")] [SerializeField]
        private string address; //  当前图片地址或图集去重键

        [HeaderPro("圆角弧度", "Corner Arc")] [SerializeField] [Range(0, MaxCornerArc)]
        private float cornerArc = DefaultCornerArc; //  圆角弧度

        [HeaderPro("材质类型", "Material Type")] [SerializeField]
        private ImageProMaterialType imageProMaterialType = ImageProMaterialType.Default; //  材质类型

        private static readonly int GrayScaleAmount = Shader.PropertyToID("_GrayScaleAmount"); //  灰度属性 ID

        private static Shader _roundedRectangleShader; //  圆角 shader 缓存

        private static Shader _defaultGrayShader; //  灰度 shader 缓存

        //  圆角参数全走顶点通道，材质无逐图状态，可全局共享；灰度只有开关两态，拆两个共享实例切换。
        private static Material _sharedRoundMaterial;
        private static Material _sharedGrayOnMaterial;
        private static Material _sharedGrayOffMaterial;

        private bool _isGray = true; //  当前灰度状态，决定引用哪个共享灰度材质
        private UIVertex _vertexScratch; //  mesh 遍历复用，避免逐顶点 GC
        private CancellationTokenSource _loadCts; // HTTP 下载取消令牌
        private IDisposable _assetHandle; // 当前资源加载句柄，换图/销毁时释放
        private bool _isHttpSprite; // 当前 sprite 是否由 HTTP 下载生成（纹理归本组件所有）
        private bool _isAtlasSprite; // 当前 sprite 是否来自图集，用于区分同名的地址键

        /// <summary>
        /// 默认纹理下载器 —— 所有 <see cref="ImagePro"/> 实例共享。通过 <see cref="SetTextureDownloader"/> 替换
        /// <para>Default texture downloader shared by all <see cref="ImagePro"/> instances; replace via <see cref="SetTextureDownloader"/></para>
        /// </summary>
        public static Func<string, CancellationToken, UniTask<Texture2D>> DefaultTextureDownloader
        {
            get;
            private set;
        } = DefaultLoadTextureAsync;

        /// <summary>圆角弧度 <para>Corner arc radius</para></summary>
        public float CornerArc
        {
            get => cornerArc;
            set
            {
                cornerArc = Mathf.Clamp(value, 0f, MaxCornerArc);
                if (imageProMaterialType == ImageProMaterialType.Round)
                    SetVerticesDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            EnableCanvasShaderChannels();
            ApplyMaterialType();
        }

        protected override void OnDestroy()
        {
            CancelLoading();
            //  共享材质为静态缓存，不随单个组件销毁，故此处不再 Destroy。
            UnloadInternal();
            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (imageProMaterialType != ImageProMaterialType.Round) return;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            if (imageProMaterialType != ImageProMaterialType.Round) return;

            //  逐顶点写入圆角参数。position 是相对 pivot 的本地坐标，减去 center 得相对中心坐标；
            //  UV 通道不受 Canvas batching 变换，位移/缩放/图集都不会污染它。
            Rect rect = rectTransform.rect;
            float width = Mathf.Abs(rect.width);
            float height = Mathf.Abs(rect.height);
            Vector2 center = rect.center;
            Vector2 halfSize = new Vector2(width, height) * HalfRatio;
            float radius = CheckConnerArc();
            float pixelWidth = CalculatePixelWidth();

            int count = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref _vertexScratch, i);
                Vector2 localPos = (Vector2)_vertexScratch.position - center;
                _vertexScratch.uv1 = localPos;
                _vertexScratch.uv2 = halfSize;
                _vertexScratch.uv3 = new Vector2(radius, pixelWidth);
                vh.SetUIVertex(_vertexScratch, i);
            }
        }

        //  uv1/uv2/uv3 必须让 Canvas 保留到顶点缓冲，否则合批时会被丢弃导致圆角失效。
        //  编辑器预览也要调用，故为 public。
        public void EnableCanvasShaderChannels()
        {
            Canvas myCanvas = GetComponentInParent<Canvas>();
            if (myCanvas == null) return;
            myCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                                 | AdditionalCanvasShaderChannels.TexCoord2
                                                 | AdditionalCanvasShaderChannels.TexCoord3;
        }

        #region 内部函数 - Private Functions

        //  按当前材质类型创建并应用材质
        private void ApplyMaterialType()
        {
            switch (imageProMaterialType)
            {
                case ImageProMaterialType.Default:
                    //  默认 UI 材质是全局共享的，交给 Graphic 自己处理，不写 m_Material
                    return;
                case ImageProMaterialType.Round:
                {
                    Shader shader = GetShader(RoundedRectangleShaderName, ref _roundedRectangleShader);
                    if (shader == null)
                        return;

                    if (_sharedRoundMaterial == null)
                        _sharedRoundMaterial = new Material(shader);
                    material = _sharedRoundMaterial;
                    SetVerticesDirty();
                    break;
                }
                case ImageProMaterialType.Gray:
                {
                    Shader shader = GetShader(DefaultGrayShaderName, ref _defaultGrayShader);
                    if (shader == null)
                        return;

                    material = GetSharedGrayMaterial(_isGray);
                    break;
                }
            }
        }

        //  查找并缓存 shader，缺失时报错
        private Shader GetShader(string shaderName, ref Shader cache)
        {
            if (cache != null)
                return cache;

            cache = Shader.Find(shaderName);
            if (cache == null)
                D.Error($"[ UiManager ] [ ImagePro ] {gameObject.name} shader {shaderName} not found");
            return cache;
        }

        //  检查圆角弧度，不能超过短边的一半
        private float CheckConnerArc()
        {
            //  shader 使用 RectTransform 本地坐标，因此这里也用本地尺寸，不再乘 lossyScale
            float edgeSize = Mathf.Min(Mathf.Abs(rectTransform.rect.width), Mathf.Abs(rectTransform.rect.height));
            float maxConnerSize = edgeSize * HalfRatio - EdgeInset;
            return Mathf.Clamp(cornerArc, 0f, Mathf.Max(0f, maxConnerSize));
        }

        //  灰度材质只有开关两态，用两个静态共享实例切换，避免 clone 材质打断合批。
        private Material GetSharedGrayMaterial(bool isGray)
        {
            if (isGray)
            {
                if (_sharedGrayOnMaterial == null)
                    _sharedGrayOnMaterial = new Material(_defaultGrayShader);
                return _sharedGrayOnMaterial;
            }

            if (_sharedGrayOffMaterial == null)
            {
                _sharedGrayOffMaterial = new Material(_defaultGrayShader);
                _sharedGrayOffMaterial.SetFloat(GrayScaleAmount, GrayOff);
            }

            return _sharedGrayOffMaterial;
        }

        //  计算一个屏幕像素对应的本地单位数，供 shader 做恒定宽度的抗锯齿
        //  CanvasScaler.scaleFactor 不体现在 lossyScale 中，需显式相乘
        private float CalculatePixelWidth()
        {
            float scale = Mathf.Max(Mathf.Abs(rectTransform.lossyScale.x), 0.0001f);
            Canvas canvas = GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null ? Mathf.Max(Mathf.Abs(canvas.scaleFactor), 0.0001f) : 1f;
            return 1f / (scale * scaleFactor);
        }

        //  取消正在进行的 HTTP 下载，并释放令牌源
        private void CancelLoading()
        {
            //  先摘除引用再操作，避免令牌回调重入时二次释放
            CancellationTokenSource cts = _loadCts;
            _loadCts = null;
            if (cts == null)
                return;

            cts.Cancel();
            cts.Dispose();
        }

        //  释放当前资源句柄（幂等，Dispose 内部已保证）
        private void ReleaseAssetHandle()
        {
            _assetHandle?.Dispose();
            _assetHandle = null;
        }

        //  清理当前 sprite：HTTP 纹理归本组件所有需显式销毁，图集资源只释放句柄
        private void ClearCurrentSprite(bool wasHttp)
        {
            if (wasHttp)
                DestroyHttpSprite();
            else
            {
                sprite = null;
                ReleaseAssetHandle();
            }

            _isHttpSprite = false;
            _isAtlasSprite = false;
        }

        //  卸载当前图片，无论 address 是否为空都要释放可能存在的句柄
        private void UnloadInternal()
        {
            ClearCurrentSprite(_isHttpSprite);
            address = null;
        }

        //  销毁 HTTP 下载生成的 Sprite + Texture
        private void DestroyHttpSprite()
        {
            _isHttpSprite = false;
            if (sprite == null)
                return;

            if (sprite.texture != null)
                Destroy(sprite.texture);
            Destroy(sprite);
            sprite = null;
        }

        //  默认实现：使用 UnityWebRequest 下载纹理
        private static async UniTask<Texture2D> DefaultLoadTextureAsync(string url, CancellationToken token)
        {
            using var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest().ToUniTask(cancellationToken: token);

            return request.result != UnityEngine.Networking.UnityWebRequest.Result.Success
                ? throw new Exception($"Texture load failed: {request.error} (URL: {url})")
                : UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
        }

        //  加载图集精灵：地址由两个参数分别记录，精灵名中的分隔符不会造成误解析
        private async UniTask<Sprite> SetAtlasSpriteInternal(string atlasName, string spriteName)
        {
            string key = $"{AtlasKeyPrefix}{atlasName}/{spriteName}";
            //  只有当前确实来自同一图集图时才去重，避免资源路径恰好等于 key 时误命中
            if (_isAtlasSprite && address == key && sprite != null)
                return sprite;

            CancelLoading();
            string oldAddress = address;
            bool oldWasHttp = _isHttpSprite;
            address = key;
            if (!string.IsNullOrEmpty(oldAddress) && oldAddress != key)
                ClearCurrentSprite(oldWasHttp);

            AssetHandle<SpriteAtlas> atlasHandle = await AssetsManager.Instance.LoadAsync<SpriteAtlas>(atlasName);
            if (atlasHandle is not { IsValid: true })
            {
                atlasHandle?.Dispose();
                D.Warning($"[ UiManager ] [ ImagePro ] Atlas '{atlasName}' not found.");
                return null;
            }

            if (atlasHandle.Asset == null)
            {
                atlasHandle.Dispose();
                D.Warning($"[ UiManager ] [ ImagePro ] Atlas '{atlasName}' asset is null.");
                return null;
            }

            Sprite target = atlasHandle.Asset.GetSprite(spriteName);
            if (target == null)
            {
                atlasHandle.Dispose();
                D.Warning($"[ UiManager ] [ ImagePro ] {spriteName} not found in atlas '{atlasName}'.");
                return null;
            }

            if (this == null || address != key)
            {
                atlasHandle.Dispose();
                return null;
            }

            ReleaseAssetHandle();
            _assetHandle = atlasHandle;
            sprite = target;
            _isHttpSprite = false;
            _isAtlasSprite = true;
            return target;
        }

        #endregion

        /// <summary>
        /// 设置灰度。需先将材质类型设为 Gray 才生效。
        /// <para>Sets grayscale. Set the material type to Gray first to take effect.</para>
        /// </summary>
        /// <param name="isGray">是否置灰 <para>Whether to apply grayscale</para></param>
        public void SetGray(bool isGray)
        {
            //  默认类型用的是不含灰度属性的共享 UI 材质，切换无效，与旧语义一致。
            if (imageProMaterialType != ImageProMaterialType.Gray) return;
            if (_isGray == isGray) return;

            if (GetShader(DefaultGrayShaderName, ref _defaultGrayShader) == null) return;
            _isGray = isGray;
            //  只切共享材质引用，不写材质属性，避免污染其他灰度图。
            material = GetSharedGrayMaterial(isGray);
        }

        /// <summary>
        /// 替换默认纹理下载器
        /// <para>Replaces the default texture downloader</para>
        /// </summary>
        /// <param name="downloader">自定义下载器，传 null 恢复默认 <para>Custom downloader, null restores the default</para></param>
        public static void SetTextureDownloader(Func<string, CancellationToken, UniTask<Texture2D>> downloader)
        {
            DefaultTextureDownloader = downloader ?? DefaultLoadTextureAsync;
        }

        /// <summary>
        /// 通过链接地址设置精灵图 - 支持 HTTP 地址和资源路径
        /// <para>Sets sprite by URL - accepts HTTP addresses and resource paths</para>
        /// </summary>
        /// <param name="url">图片地址 / 资源路径 <para>Image URL or resource path</para></param>
        public async UniTask<Sprite> SetSpriteByUrl(string url)
        {
            //  资源路径恰好等于图集 key 时不能命中图集 sprite
            if (!_isAtlasSprite && address == url && sprite != null)
                return sprite;

            CancelLoading();
            string oldAddress = address;
            bool oldWasHttp = _isHttpSprite;
            address = url;

            //  空地址：清理旧图后立即返回（UnloadInternal 不再因 address 为空而跳过释放）
            if (string.IsNullOrEmpty(url))
            {
                ClearCurrentSprite(oldWasHttp);
                return null;
            }

            if (!string.IsNullOrEmpty(oldAddress) && oldAddress != url)
                ClearCurrentSprite(oldWasHttp);

            if (url.StartsWith(HttpPrefix, StringComparison.Ordinal))
            {
                var cts = new CancellationTokenSource();
                _loadCts = cts;
                Texture2D tex = null;
                try
                {
                    tex = await DefaultTextureDownloader(url, cts.Token);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    D.Error($"[ UiManager ] [ ImagePro ] Load texture failed: {e.Message}");
                }
                finally
                {
                    //  仅释放本次令牌，避免误伤后续请求
                    if (ReferenceEquals(_loadCts, cts))
                        _loadCts = null;
                    cts.Dispose();
                }

                //  被取消、加载失败、组件已销毁或被新请求覆盖时都不要创建 sprite，避免纹理泄漏
                if (cts.IsCancellationRequested || tex == null || this == null || address != url)
                {
                    if (tex != null)
                        Destroy(tex);
                    return null;
                }

                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(HalfRatio, HalfRatio));
                sprite.name = $"HTTP_{url.GetHashCode():X8}";
                _isHttpSprite = true;
                _isAtlasSprite = false;
                return sprite;
            }

            AssetHandle<Sprite> spriteHandle = await AssetsManager.Instance.LoadAsync<Sprite>(url);
            if (spriteHandle is not { IsValid: true })
            {
                spriteHandle?.Dispose();
                D.Warning($"[ UiManager ] [ ImagePro ] Not found {url} sprite.");
                return null;
            }

            Sprite newSprite = spriteHandle.Asset;
            if (newSprite == null)
            {
                spriteHandle.Dispose();
                return null;
            }

            //  组件已销毁或地址已被后续请求覆盖时丢弃本次结果
            if (this == null || address != url)
            {
                spriteHandle.Dispose();
                return null;
            }

            ReleaseAssetHandle();
            _assetHandle = spriteHandle;
            sprite = newSprite;
            _isHttpSprite = false;
            _isAtlasSprite = false;
            return newSprite;
        }

        /// <summary>
        /// 通过图集类型和精灵名设置精灵图
        /// <para>Sets sprite via atlas name and sprite name</para>
        /// </summary>
        /// <param name="uiAtlasName">图集名 <para>Atlas name</para></param>
        /// <param name="spriteName">精灵名称 <para>Sprite name</para></param>
        public async UniTask<Sprite> SetSpriteByAtlas(string uiAtlasName, string spriteName)
        {
            if (string.IsNullOrEmpty(uiAtlasName) || string.IsNullOrEmpty(spriteName))
            {
                D.Warning("[ UiManager ] [ ImagePro ] Atlas name or sprite name is empty.");
                return null;
            }

            //  走独立的图集加载，避免把两个参数拼成字符串后再按分隔符反解
            return await SetAtlasSpriteInternal(uiAtlasName, spriteName);
        }
    }
}