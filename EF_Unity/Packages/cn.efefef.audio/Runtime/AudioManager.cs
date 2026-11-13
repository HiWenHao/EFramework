/*
 * ================================================
 * Describe:      音频系统门面：统一对外暴露音频能力，编排「加载 → 缓存 → 播放」并转发框架生命周期。
 *                加载 / 播放 / 设置由可替换的接口实现提供默认实现；缓存接口无默认实现，由业务侧注入。
 * Author:        Alvin8412
 * CreationTime:  2026-05-09 23:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 2.0
 * ================================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Pool;
using EasyFramework.Managers.Assets;
using UnityEngine;
using UnityEngine.Audio;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 音频系统门面，编排加载、缓存与播放并对外提供统一入口
    /// <para>Audio facade orchestrating loading, caching and playback behind one entry point</para>
    /// </summary>
    [Manager(Order = 89700)]
    [Dependency(typeof(PoolManager)), Dependency(typeof(AssetsManager))]
    public sealed class AudioManager : MonoSingleton<AudioManager>, ISingleton, IUpdate
    {
        /// <summary>
        /// 获取当前混音器
        /// <para>Get the current AudioMixer.</para>
        /// </summary>
        public AudioMixer CurrentMixer => audioMixer;

        /// <summary>
        /// 是否全局静音
        /// <para>Is globally muted.</para>
        /// </summary>
        public bool IsMuted => _settings is { IsMuted: true };

        /// <summary>
        /// 音频系统暂停
        /// <para>Audio system paused</para>
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 开启调试日志
        /// <para>Enable debug log</para>
        /// </summary>
        public bool OpenDebug { get; set; }

        /// <summary>
        /// 当前活跃音效数量
        /// <para>Count of active effects</para>
        /// </summary>
        public int ActiveEffectCount => _player?.ActiveEffectCount ?? 0;

        #region 配置字段（序列化锚点，字段名被 AudioManagerInspector 依赖，勿改）

        // 主混音器资源
        [HideInInspector, SerializeField] private AudioMixer audioMixer;

        // 音效对象池最大空闲数量
        [HideInInspector, SerializeField] private int maxEffectPoolSize = 20;

        // 音效对象池预热数量
        [HideInInspector, SerializeField] private int prewarmCount = 1;

        // 音效播放器闲置超时销毁时间（秒），≤0 不启用
        [HideInInspector, SerializeField] private float effectIdleTimeout = 30f;

        // 默认背景音乐音量
        [HideInInspector, SerializeField, Range(0f, 1f)]
        private float defaultBgmVolume = 1f;

        // 默认音效音量
        [HideInInspector, SerializeField, Range(0f, 1f)]
        private float defaultEffectVolume = 1f;

        #endregion

        #region 四类服务实例

        // 主混音器组名称
        private const string MasterGroupName = "Master";

        // BGM 混音器组名称
        private const string BgmGroupName = "BGM";

        // 音效混音器组名称
        private const string EffectGroupName = "Effect";

        // 共享运行时上下文，由 Init 创建、Quit 置空
        private AudioContext _context;

        // 当前音频加载实现，默认 DefaultAudioClipLoader
        private IAudioClipLoader _loader;

        // 当前音频缓存实现，null 表示不缓存（包内无默认实现）
        private IAudioClipCache _cache;

        // 当前音频播放实现，默认 DefaultAudioPlayer
        private IAudioPlayer _player;

        // 当前音频设置实现，默认 DefaultAudioSettings
        private IAudioSettings _settings;

        // Init 之前到达的注入请求，Init 完成后补应用
        private AudioServices _pendingServices;

        /// <summary>
        /// 当前生效的音频加载实现
        /// <para>Active clip loader.</para>
        /// </summary>
        public IAudioClipLoader CurrentLoader => _loader;

        /// <summary>
        /// 当前生效的音频缓存实现
        /// <para>Active clip cache</para>
        /// </summary>
        public IAudioClipCache CurrentCache => _cache;

        /// <summary>
        /// 当前生效的音频播放实现
        /// <para>Active audio player.</para>
        /// </summary>
        public IAudioPlayer CurrentPlayer => _player;

        /// <summary>
        /// 当前生效的音频设置实现
        /// <para>Active audio settings.</para>
        /// </summary>
        public IAudioSettings CurrentSettings => _settings;

        #endregion

        #region 框架生命周期

        // 框架初始化：构建共享上下文并装配四类服务
        void ISingleton.Init()
        {
            _context = new AudioContext
            {
                Mixer = audioMixer,
                MaxEffectPoolSize = maxEffectPoolSize,
                PrewarmCount = prewarmCount,
                EffectIdleTimeout = effectIdleTimeout,
                DefaultBgmVolume = defaultBgmVolume,
                DefaultEffectVolume = defaultEffectVolume,
                OwnerRoot = transform,
                Warn = Warning
            };

            // 混音器分组先解析好，播放器创建 AudioSource 时即可直接绑定
            _context.MasterGroup = AudioContext.FindGroup(audioMixer, MasterGroupName, Warning);
            _context.BgmGroup = AudioContext.FindGroup(audioMixer, BgmGroupName, Warning);
            _context.EffectGroup = AudioContext.FindGroup(audioMixer, EffectGroupName, Warning);

            // 有提前注入的配置时，由 Configure 内的四个 Swap 各建一次默认实现即可；
            // 否则走 ResetToDefaults，避免同一帧内重复创建 / 销毁场景物体
            if (_pendingServices != null)
            {
                var pending = _pendingServices;
                _pendingServices = null;
                Configure(pending);
            }
            else
            {
                ResetToDefaults();
            }
        }

        // 逐帧转发给实现了 IAudioTickable 的播放器
        void IUpdate.Update(float elapse, float realElapse)
        {
            // 只有实现了逐帧契约的播放器需要驱动（默认实现用于回收播放完毕的音效）
            if (_player is not IAudioTickable tickable) return;
            tickable.Tick(elapse, realElapse);
        }

        // 框架退出：保存设置、逆序释放服务并清空引用
        void ISingleton.Quit()
        {
            _settings?.Save();
            StopAll();

            // 逆序释放，播放器最后（它持有场景物体，其余实现只读上下文）
            _settings?.Dispose();
            _cache?.Dispose();
            _loader?.Dispose();
            _player?.Dispose();
            _context?.ClearEvents();

            // 置空引用，避免 HybridCLR 域重载后残留热更侧实现实例
            _loader = null;
            _cache = null;
            _player = null;
            _settings = null;
            _context = null;
            _pendingServices = null;
        }

        // 统一警告入口，按 OpenDebug 过滤
        private void Warning(string message)
        {
            if (OpenDebug)
                D.Warning($"[ AudioSystem ] {message}");
        }

        #endregion

        #region 服务注入（四类接口均可单独或整体替换）

        /// <summary>
        /// 一次性配置四类服务
        /// <para>Configure all four audio services at once</para>
        /// </summary>
        /// <param name="services">四类服务配置包 <para>Services bundle</para></param>
        public void Configure(AudioServices services)
        {
            if (_context == null)
            {
                _pendingServices = services;
                return;
            }

            if (services == null)
            {
                ResetToDefaults();
                return;
            }

            SwapLoader(services.Loader);
            SwapCache(services.Cache);
            SwapPlayer(services.Player);
            SwapSettings(services.Settings);
        }

        /// <summary>
        /// 恢复全部默认实现
        /// <para>Restore all package defaults</para>
        /// </summary>
        public void ResetToDefaults()
        {
            if (_context == null) return;
            SwapLoader(null);
            SwapCache(null);
            SwapPlayer(null);
            SwapSettings(null);
        }

        /// <summary>
        /// 替换音频加载实现
        /// <para>Replace the clip loader</para>
        /// </summary>
        /// <param name="loader">下游加载器 <para>Downstream loader</para></param>
        public void SetClipLoader(IAudioClipLoader loader)
        {
            if (_context == null)
            {
                EnsurePending().Loader = loader;
                return;
            }

            SwapLoader(loader);
        }

        /// <summary>
        /// 替换音频缓存实现
        /// <para>Replace the clip cache</para>
        /// </summary>
        /// <param name="cache">缓存实现 <para>Cache implementation</para></param>
        public void SetClipCache(IAudioClipCache cache)
        {
            if (_context == null)
            {
                EnsurePending().Cache = cache;
                return;
            }

            SwapCache(cache);
        }

        /// <summary>
        /// 替换音频播放实现
        /// <para>Replace the audio player</para>
        /// </summary>
        /// <param name="player">播放实现 <para>Player implementation</para></param>
        public void SetPlayer(IAudioPlayer player)
        {
            if (_context == null)
            {
                EnsurePending().Player = player;
                return;
            }

            SwapPlayer(player);
        }

        /// <summary>
        /// 替换音频设置实现
        /// <para>Replace the audio settings</para>
        /// </summary>
        /// <param name="settings">设置实现 <para>Settings implementation</para></param>
        public void SetSettings(IAudioSettings settings)
        {
            if (_context == null)
            {
                EnsurePending().Settings = settings;
                return;
            }

            SwapSettings(settings);
        }

        // Init 之前到达的单次替换请求合并进配置包，Init 完成时统一应用
        private AudioServices EnsurePending() => _pendingServices ??= new AudioServices();

        // 局部替换加载器，只释放并初始化该服务
        private void SwapLoader(IAudioClipLoader next)
        {
            IAudioClipLoader resolved = next ?? new DefaultAudioClipLoader();
            if (ReferenceEquals(resolved, _loader) && _loader != null) return;
            _loader?.Dispose();
            _loader = resolved;
            _loader.Initialize(_context);
        }

        // 局部替换缓存实现，null 表示关闭缓存
        private void SwapCache(IAudioClipCache next)
        {
            if (ReferenceEquals(next, _cache)) return;
            _cache?.Dispose();
            _cache = next; // null = 不缓存
            _cache?.Initialize(_context);
        }

        // 局部替换播放器，会重建场景物体与对象池
        private void SwapPlayer(IAudioPlayer next)
        {
            IAudioPlayer resolved = next ?? new DefaultAudioPlayer();
            if (ReferenceEquals(resolved, _player) && _player != null) return;
            _player?.Dispose();
            _player = resolved;
            _player.Initialize(_context);
        }

        // 局部替换设置实现，并从偏好存储重新载入
        private void SwapSettings(IAudioSettings next)
        {
            IAudioSettings resolved = next ?? new DefaultAudioSettings();
            if (ReferenceEquals(resolved, _settings) && _settings != null) return;
            _settings?.Dispose();
            _settings = resolved;
            _settings.Initialize(_context);
            _settings.Load();
        }

        #endregion

        #region 加载编排（门面独有职责）

        /// <summary>
        /// 按资源名加载音频
        /// <para>Load an audio clip by asset key</para>
        /// </summary>
        /// <param name="key">资源键 <para>Asset key</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure</para></returns>
        public UniTask<AudioClip> LoadClipAsync(string key, CancellationToken token = default)
            => ResolveClipAsync(key, AudioLoadKind.Asset, token);

        /// <summary>
        /// 按 URL 加载音频
        /// <para>Load an audio clip by URL</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure</para></returns>
        public UniTask<AudioClip> LoadClipFromUrlAsync(string url, CancellationToken token = default)
            => ResolveClipAsync(url, AudioLoadKind.Url, token);

        // 门面唯一知道「加载 + 缓存」的地方；播放器与设置都不参与
        private UniTask<AudioClip> ResolveClipAsync(string key, AudioLoadKind kind, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(key) || _loader == null)
                return UniTask.FromResult<AudioClip>(null);

            if (_cache != null)
                return _cache.GetOrLoadAsync(key, _loader, kind, token);

            return kind == AudioLoadKind.Url
                ? _loader.LoadFromUrlAsync(key, token)
                : _loader.LoadAsync(key, token);
        }

        #endregion

        #region 运行时配置方法

        /// <summary>
        /// 设置主混音器
        /// <para>Set the main AudioMixer</para>
        /// </summary>
        /// <param name="newMixer">新的主混音器 <para>New main AudioMixer</para></param>
        public void SetAudioMixer(AudioMixer newMixer)
        {
            if (newMixer == audioMixer) return;
            audioMixer = newMixer;
            _settings?.SetAudioMixer(newMixer);
        }

        /// <summary>
        /// 设置默认背景音乐音量
        /// <para>Set default BGM volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetDefaultBgmVolume(float volume)
        {
            defaultBgmVolume = Mathf.Clamp01(volume);
            if (_context != null) _context.DefaultBgmVolume = defaultBgmVolume;
        }

        /// <summary>
        /// 设置默认音效音量
        /// <para>Set default effect volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetDefaultEffectVolume(float volume)
        {
            defaultEffectVolume = Mathf.Clamp01(volume);
            if (_context != null) _context.DefaultEffectVolume = defaultEffectVolume;
        }

        #endregion

        #region BGM - 背景音乐

        /// <summary>
        /// 播放背景音乐
        /// <para>Play BGM</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="isLoop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        public void PlayBGM(AudioClip clip, bool isLoop = true, float startTime = 0f)
            => _player.PlayBGM(clip, isLoop, startTime);

        /// <summary>
        /// 按资源名播放背景音乐
        /// <para>Play BGM by asset name</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <param name="isLoop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        public void PlayBGM(string clipName, bool isLoop = true, float startTime = 0f)
        {
            PlayBGMAsync(clipName, isLoop, startTime).Forget();
        }

        /// <summary>
        /// 按资源名播放背景音乐（可等待）
        /// <para>Play BGM by asset name, awaitable</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <param name="isLoop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public async UniTask PlayBGMAsync(string clipName, bool isLoop = true, float startTime = 0f,
            CancellationToken token = default)
        {
            var clip = await ResolveClipAsync(clipName, AudioLoadKind.Asset, token);
            _player.PlayBGM(clip, isLoop, startTime);
        }

        /// <summary>
        /// 按 URL 播放背景音乐
        /// <para>Play BGM by URL</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL</para></param>
        /// <param name="isLoop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        public void PlayBGMFromUrl(string url, bool isLoop = true, float startTime = 0f)
        {
            PlayBGMFromUrlAsync(url, isLoop, startTime).Forget();
        }

        /// <summary>
        /// 按 URL 播放背景音乐（可等待）
        /// <para>Play BGM by URL, awaitable</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL</para></param>
        /// <param name="isLoop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public async UniTask PlayBGMFromUrlAsync(string url, bool isLoop = true, float startTime = 0f,
            CancellationToken token = default)
        {
            var clip = await ResolveClipAsync(url, AudioLoadKind.Url, token);
            _player.PlayBGM(clip, isLoop, startTime);
        }

        /// <summary>
        /// 停止背景音乐
        /// <para>Stop BGM.</para>
        /// </summary>
        public void StopBGM() => _player.StopBGM();

        /// <summary>
        /// 暂停背景音乐
        /// <para>Pause BGM.</para>
        /// </summary>
        public void PauseBGM() => _player.PauseBGM();

        /// <summary>
        /// 恢复背景音乐
        /// <para>Unpause BGM.</para>
        /// </summary>
        public void UnPauseBGM() => _player.UnPauseBGM();

        #endregion

        #region Effect - 音效

        /// <summary>
        /// 播放 2D 音效
        /// <para>Play a 2D effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public UniTask<uint> Play2DEffect(AudioClip clip, CancellationToken token = default, float startTime = 0f)
            => _player.Play2DEffect(clip, token, startTime);

        /// <summary>
        /// 按资源名播放 2D 音效
        /// <para>Play a 2D effect by asset name</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public async UniTask<uint> Play2DEffect(string clipName, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = await ResolveClipAsync(clipName, AudioLoadKind.Asset, token);
            return await _player.Play2DEffect(clip, token, startTime);
        }

        /// <summary>
        /// 播放 3D 音效
        /// <para>Play a 3D effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="worldPos">世界坐标 <para>World position</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public UniTask<uint> Play3DEffect(AudioClip clip, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
            => _player.Play3DEffect(clip, worldPos, token, startTime);

        /// <summary>
        /// 按资源名播放 3D 音效
        /// <para>Play a 3D effect by asset name</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <param name="worldPos">世界坐标 <para>World position</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public async UniTask<uint> Play3DEffect(string clipName, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = await ResolveClipAsync(clipName, AudioLoadKind.Asset, token);
            return await _player.Play3DEffect(clip, worldPos, token, startTime);
        }

        /// <summary>
        /// 按 URL 播放 2D 音效
        /// <para>Play a 2D effect by URL</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public async UniTask<uint> Play2DEffectFromUrl(string url, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = await ResolveClipAsync(url, AudioLoadKind.Url, token);
            return await _player.Play2DEffect(clip, token, startTime);
        }

        /// <summary>
        /// 按 URL 播放 3D 音效
        /// <para>Play a 3D effect by URL</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL</para></param>
        /// <param name="worldPos">世界坐标 <para>World position</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public async UniTask<uint> Play3DEffectFromUrl(string url, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = await ResolveClipAsync(url, AudioLoadKind.Url, token);
            return await _player.Play3DEffect(clip, worldPos, token, startTime);
        }

        /// <summary>
        /// 播放循环音效
        /// <para>Play a looping effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="is3D">是否按 3D 空间播放 <para>Whether to play in 3D space</para></param>
        /// <param name="pos">世界坐标 <para>World position</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>循环音效播放器；失败为 null <para>Looping AudioSource, or null on failure</para></returns>
        public AudioSource PlayLoopEffect(AudioClip clip, bool is3D = false, Vector3 pos = default,
            float startTime = 0f)
            => _player.PlayLoopEffect(clip, is3D, pos, startTime);

        /// <summary>
        /// 通过 ID 停止指定的音效
        /// <para>Stop an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        public void StopEffect(uint id) => _player.StopEffect(id);

        /// <summary>
        /// 通过 ID 暂停指定的音效
        /// <para>Pause an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        public void PauseEffect(uint id) => _player.PauseEffect(id);

        /// <summary>
        /// 通过 ID 恢复指定的音效
        /// <para>Unpause an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        public UniTask UnPauseEffect(uint id) => _player.UnPauseEffect(id);

        /// <summary>
        /// 暂停所有当前正在播放的音效
        /// <para>Pause all effects.</para>
        /// </summary>
        public void PauseAllEffect() => _player.PauseAllEffect();

        /// <summary>
        /// 恢复所有被暂停的音效
        /// <para>Unpause all paused effects.</para>
        /// </summary>
        public UniTask UnPauseAllEffect() => _player.UnPauseAllEffect();

        #endregion

        #region Global Control - 全局控制

        /// <summary>
        /// 暂停所有音频
        /// <para>Pause all audio</para>
        /// </summary>
        public void PauseAll()
        {
            IsPaused = true;
            _player.PauseBGM();
            _player.PauseAllEffect();
        }

        /// <summary>
        /// 恢复所有音频
        /// <para>Unpause all audio.</para>
        /// </summary>
        public async UniTask UnPauseAll()
        {
            IsPaused = false;
            _player.UnPauseBGM();
            await _player.UnPauseAllEffect();
        }

        /// <summary>
        /// 设置全局静音
        /// <para>Set global mute.</para>
        /// </summary>
        /// <param name="mute">是否静音 <para>Whether to mute</para></param>
        public void SetMute(bool mute) => _settings.SetMute(mute);

        /// <summary>
        /// 停止所有音效
        /// <para>Stop all effects</para>
        /// </summary>
        public void StopAllEffects() => _player.StopAllEffects();

        /// <summary>
        /// 停止循环音效
        /// <para>Stop the looping sound effect.</para>
        /// </summary>
        /// <param name="source">待停止的循环音效播放器 <para>Looping AudioSource to stop.</para></param>
        public void StopLoopEffect(AudioSource source) => _player.StopLoopEffect(source);

        /// <summary>
        /// 停止所有音频
        /// <para>Stop all audio</para>
        /// </summary>
        public void StopAll()
        {
            if (_player == null) return;
            _player.StopBGM();
            _player.StopAllEffects();
        }

        #endregion

        #region 音量与淡入淡出

        /// <summary>
        /// 设置主音量
        /// <para>Set master volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetMasterVolume(float volume) => _settings.SetMasterVolume(volume);

        /// <summary>
        /// 设置 BGM 音量
        /// <para>Set BGM volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetBgmVolume(float volume) => _settings.SetBgmVolume(volume);

        /// <summary>
        /// 设置音效音量
        /// <para>Set effect volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetEffectVolume(float volume) => _settings.SetEffectVolume(volume);

        /// <summary>
        /// 获取 BGM 音量
        /// <para>Get BGM volume</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        public float GetBgmVolume() => _settings.GetBgmVolume();

        /// <summary>
        /// 获取音效音量
        /// <para>Get effect volume</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        public float GetEffectVolume() => _settings.GetEffectVolume();

        /// <summary>
        /// 获取主音量
        /// <para>Get master volume</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        public float GetMasterVolume() => _settings.GetMasterVolume();

        /// <summary>
        /// 背景音乐淡入到目标音量
        /// <para>Fade BGM to a target volume.</para>
        /// </summary>
        /// <param name="targetVolume">目标音量 <para>Target volume</para></param>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public UniTask FadeBGM(float targetVolume, float duration, CancellationToken token = default)
            => _settings.FadeBGM(targetVolume, duration, token);

        /// <summary>
        /// 背景音乐淡出并停止
        /// <para>Fade out BGM and stop.</para>
        /// </summary>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public UniTask FadeOutBGM(float duration, CancellationToken token = default)
            => _settings.FadeOutBGM(duration, token);

        /// <summary>
        /// 淡入淡出 AudioSource 音量
        /// <para>Fade an AudioSource volume</para>
        /// </summary>
        /// <param name="source">目标播放器 <para>Target AudioSource</para></param>
        /// <param name="from">起始音量 <para>Starting volume</para></param>
        /// <param name="to">目标音量 <para>Target volume</para></param>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public UniTask FadeVolume(AudioSource source, float from, float to, float duration,
            CancellationToken token = default)
            => _settings.FadeVolume(source, from, to, duration, token);

        #endregion

        #region 查询

        /// <summary>
        /// 指定名字的 BGM 是否正在播放
        /// <para>Check if BGM with given name is playing.</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <returns>是否处于目标状态 <para>Whether the condition holds</para></returns>
        public bool IsPlayingBGM(string clipName) => _player.IsPlayingBGM(clipName);

        /// <summary>
        /// 指定名字的音效是否正在播放
        /// <para>Check if any Effect with given name is playing.</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <returns>是否处于目标状态 <para>Whether the condition holds</para></returns>
        public bool IsPlayingEffect(string clipName) => _player.IsPlayingEffect(clipName);

        #endregion
    }
}