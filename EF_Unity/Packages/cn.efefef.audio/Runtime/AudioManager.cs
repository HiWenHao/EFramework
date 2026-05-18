/*
 * ================================================
 * Describe:      现代音频系统，使用 AudioMixer、UniTask 异步、对象池（EF.Pool）与 3D 音效，支持音效播放器空闲自动销毁。
 * Author:        Alvin8412
 * CreationTime:  2026-05-09 23:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-10
 * ScriptVersion: 2.0
 * ================================================
 */

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using EasyFramework.Systems.Assets;
using UnityEngine;
using UnityEngine.Audio;

namespace EasyFramework.Systems.Audio
{
    /// <summary>
    /// 音频系统，统一管理 BGM 与音效，支持混音器、对象池、异步播放。
    /// <para>Audio system, managing BGM and SFX with mixer, pool and async playback.</para>
    /// </summary>
    [Manager(Order = 89700)]
    public sealed class AudioManager : MonoSingleton<AudioManager>, ISingleton, IUpdate
    {
        #region 配置字段（仅通过 Editor 或运行时设置）

        [HideInInspector] [SerializeField] private AudioMixer audioMixer; // 主混音器资源

        [HideInInspector] [SerializeField] [Range(0f, 1f)]
        private float defaultBgmVolume = 1f; // 默认背景音乐音量

        [HideInInspector] [SerializeField] [Range(0f, 1f)]
        private float defaultSfxVolume = 1f; // 默认音效音量

        [HideInInspector] [SerializeField] private int maxSfxPoolSize = 20; // 音效对象池最大空闲数量
        [HideInInspector] [SerializeField] private int prewarmCount = 1; // 音效对象池预热数量
        [HideInInspector] [SerializeField] private float sfxIdleTimeout = 30f; // 音效播放器闲置超时销毁时间（秒），≤0 不启用

        #endregion

        #region 私有变量

        private bool _isPaused; // 是否全局暂停
        private bool _isMuted; // 是否全局静音

        private AudioMixerGroup _masterGroup; // 主混音器组
        private AudioMixerGroup _bgmGroup; // 背景音乐混音器组
        private AudioMixerGroup _sfxGroup; // 音效混音器组

        private AudioSource _bgmSource; // BGM 专用播放器
        private Transform _sfxRoot; // 音效对象池根节点
        private List<ActiveEffect> _activeEffects; // 当前活动的音效列表

        private GameObject _sfxPrefab; // 音效对象池预制件

        private CancellationTokenSource _lifetimeCts; // 系统生命周期取消令牌

        // 混音器通道名称
        private const string MasterGroupName = "Master";
        private const string BgmGroupName = "BGM";
        private const string SfxGroupName = "SFX";

        // PlayerPrefs 键
        private string _muteKey; // 静音键
        private string _bgmVolumeKey; // BGM 音量键
        private string _sfxVolumeKey; // SFX 音量键
        private string _masterVolumeKey; // 主音量键

        #endregion

        public bool IsPaused { get; private set; }

        void ISingleton.Init()
        {
            _muteKey = Application.productName + "Audio_Muted";
            _bgmVolumeKey = Application.productName + "Audio_BGM_Volume";
            _sfxVolumeKey = Application.productName + "Audio_SFX_Volume";
            _masterVolumeKey = Application.productName + "Audio_Master_Volume";

            _lifetimeCts = new CancellationTokenSource();

            // 创建音效对象池根节点
            _sfxRoot = new GameObject("SfxRoot").transform;
            _sfxRoot.SetParent(EF.Managers);
            DontDestroyOnLoad(_sfxRoot.gameObject);

            // 初始化混音器组
            if (audioMixer)
            {
                _masterGroup = audioMixer.FindMatchingGroups(MasterGroupName)[0];
                _bgmGroup = audioMixer.FindMatchingGroups(BgmGroupName)[0];
                _sfxGroup = audioMixer.FindMatchingGroups(SfxGroupName)[0];
            }

            // 创建 BGM 播放器
            var bgmGo = new GameObject("BGM_Source");
            bgmGo.transform.SetParent(_sfxRoot);
            _bgmSource = bgmGo.AddComponent<AudioSource>();
            _bgmSource.outputAudioMixerGroup = _bgmGroup;
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            // 创建音效对象池（使用 GameObjectPool，支持空闲超时销毁）
            _sfxPrefab = CreateSfxPrefab();
            EF.Pool.CreateGameObjectPool(_sfxPrefab, _sfxRoot, prewarmCount, maxSfxPoolSize, sfxIdleTimeout);

            // 活动效果列表
            _activeEffects = new List<ActiveEffect>();

            // 从 PlayerPrefs 恢复音量等设置
            LoadSettings();
        }

        // 创建用于对象池的预制体（只有一个 AudioSource 的空物体）
        private GameObject CreateSfxPrefab()
        {
            var go = new GameObject("SfxPrototype");
            go.transform.SetParent(_sfxRoot);
            var source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _sfxGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            go.SetActive(false);
            return go;
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (_isPaused) return;

            // 回收播放完毕的非循环音效
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect.Source.loop || effect.Source.isPlaying) continue;

                effect.CompletionSource?.TrySetResult();
                effect.CompletionSource = null;
                effect.Cancellation.Dispose();
                ReturnSfxToPool(effect.Source);
                _activeEffects.RemoveAt(i);
            }

            // 空闲超时销毁由 PoolManager 的 Update 自动驱动，无需额外处理
        }

        void ISingleton.Quit()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();

            // 停止所有活跃音效并回收
            foreach (var effect in _activeEffects)
            {
                effect.CompletionSource?.TrySetCanceled();
                effect.CompletionSource = null;
                effect.Cancellation.Dispose();
                if (effect.Source)
                {
                    effect.Source.Stop();
                    ReturnSfxToPool(effect.Source);
                }
            }

            _activeEffects.Clear();

            if (_bgmSource)
            {
                _bgmSource.Stop();
                Destroy(_bgmSource.gameObject);
            }

            if (_sfxRoot)
                Destroy(_sfxRoot.gameObject);

            // 销毁音效对象池
            if (_sfxPrefab)
                EF.Pool.DestroyGameObjectPool(_sfxPrefab);

            SaveSettings();
        }

        #region 运行时配置方法

        /// <summary>
        /// 设置主混音器（运行时更换）
        /// <para>Set the main AudioMixer at runtime.</para>
        /// </summary>
        public void SetAudioMixer(AudioMixer newMixer)
        {
            if (newMixer == audioMixer) return;
            audioMixer = newMixer;

            if (audioMixer)
            {
                _masterGroup = GetMixerGroupSafe(MasterGroupName);
                _bgmGroup = GetMixerGroupSafe(BgmGroupName);
                _sfxGroup = GetMixerGroupSafe(SfxGroupName);
            }
            else
            {
                _masterGroup = null;
                _bgmGroup = null;
                _sfxGroup = null;
            }

            if (_bgmSource)
                _bgmSource.outputAudioMixerGroup = _bgmGroup;

            // 更新所有活动音效的输出组
            if (_activeEffects != null)
            {
                foreach (var effect in _activeEffects)
                {
                    if (effect.Source)
                        effect.Source.outputAudioMixerGroup = _sfxGroup;
                }
            }
        }

        /// <summary>
        /// 获取当前混音器
        /// <para>Get the current AudioMixer.</para>
        /// </summary>
        public AudioMixer CurrentMixer => audioMixer;

        /// <summary>
        /// 设置默认背景音乐音量（0~1），仅影响后续播放，已播放的不变。
        /// <para>Set default BGM volume, only affects future playback.</para>
        /// </summary>
        public void SetDefaultBgmVolume(float volume)
        {
            defaultBgmVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置默认音效音量（0~1），仅影响后续播放，已播放的不变。
        /// <para>Set default SFX volume, only affects future playback.</para>
        /// </summary>
        public void SetDefaultSfxVolume(float volume)
        {
            defaultSfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置音效对象池最大空闲数量（需在下次初始化前调用，或重建池）
        /// <para>Set max idle count of SFX pool (needs re-init to take effect).</para>
        /// </summary>
        public void SetMaxSfxPoolSize(int size)
        {
            maxSfxPoolSize = Mathf.Max(1, size);
        }

        /// <summary>
        /// 设置音效对象池预热数量（需在下次初始化前调用）
        /// <para>Set prewarm count of SFX pool.</para>
        /// </summary>
        public void SetPrewarmCount(int count)
        {
            prewarmCount = Mathf.Max(0, count);
        }

        /// <summary>
        /// 设置音效播放器闲置超时销毁时间（秒），≤0 关闭自动销毁。
        /// <para>Set idle timeout for SFX pool auto-destruction. ≤0 to disable.</para>
        /// </summary>
        public void SetSfxIdleTimeout(float timeout)
        {
            sfxIdleTimeout = timeout;
        }

        #endregion

        #region 对象池辅助

        // 从池中获取激活的 AudioSource
        private AudioSource GetSfxFromPool()
        {
            var go = EF.Pool.Spawn(_sfxPrefab);
            if (go)
            {
                var source = go.GetComponent<AudioSource>();
                // 重置必要属性（池化对象可能保留之前的状态）
                ResetSfxSource(source);
                return source;
            }
            else
            {
                Debug.LogError("[AudioSystem] Failed to get AudioSource from pool. Creating fallback.");
                // 极端情况：手动创建临时播放器（不会被池管理，用完即毁）
                var goFallback = new GameObject("SfxFallback");
                goFallback.transform.SetParent(_sfxRoot);
                var source = goFallback.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = _sfxGroup;
                source.playOnAwake = false;
                return source;
            }
        }

        // 归还音效播放器到对象池
        private void ReturnSfxToPool(AudioSource source)
        {
            if (!source) return;
            source.Stop();
            source.gameObject.SetActive(false);
            source.transform.SetParent(_sfxRoot, false);
            EF.Pool.Despawn(source.gameObject);
        }

        // 重置从池中取出的 AudioSource 状态
        private void ResetSfxSource(AudioSource source)
        {
            if (!source) return;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.spatialBlend = 0f;
            source.mute = false;
            source.volume = 1f;
            source.priority = 128;
            source.pitch = 1f;
            source.outputAudioMixerGroup = _sfxGroup;
        }

        #endregion

        #region 背景音乐 (BGM)

        /// <summary>
        /// 播放背景音乐（可指定起始时间，单位秒）
        /// <para>Play BGM with optional start time (in seconds).</para>
        /// </summary>
        public void PlayBGM(AudioClip clip, bool isLoop = true, float startTime = 0f)
        {
            if (!clip) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;
            _bgmSource.loop = isLoop;
            _bgmSource.Play();
            _bgmSource.time = Mathf.Clamp(startTime, 0f, clip.length);
        }

        /// <summary>
        /// 播放背景音乐（通过资源名）
        /// <para>Play BGM by asset name.</para>
        /// </summary>
        public void PlayBGM(string clipName, bool isLoop = true, float startTime = 0f)
        {
            var clip = LoadClip(clipName);
            PlayBGM(clip, isLoop, startTime);
        }

        /// <summary>
        /// 停止背景音乐
        /// <para>Stop BGM.</para>
        /// </summary>
        public void StopBGM() => _bgmSource.Stop();

        /// <summary>
        /// 暂停背景音乐
        /// <para>Pause BGM.</para>
        /// </summary>
        public void PauseBGM() => _bgmSource.Pause();

        /// <summary>
        /// 恢复背景音乐
        /// <para>Unpause BGM.</para>
        /// </summary>
        public void UnPauseBGM() => _bgmSource.UnPause();

        #endregion

        #region 音效播放 (2D & 3D)

        /// <summary>
        /// 播放 2D 音效，等待自然结束
        /// <para>Play 2D effect and await its completion.</para>
        /// </summary>
        public async UniTask Play2DEffect(AudioClip clip, CancellationToken token = default, float startTime = 0f)
        {
            if (!clip) return;
            var effect = PlayEffect(clip, false, Vector3.zero, token, is3D: false, startTime: startTime);
            if (effect?.CompletionSource != null)
                await effect.CompletionSource.Task;
        }

        /// <summary>
        /// 播放 2D 音效（通过资源名）
        /// <para>Play 2D effect by asset name.</para>
        /// </summary>
        public async UniTask Play2DEffect(string clipName, CancellationToken token = default, float startTime = 0f)
        {
            var clip = LoadClip(clipName);
            await Play2DEffect(clip, token, startTime);
        }

        /// <summary>
        /// 播放 3D 音效，等待自然结束
        /// <para>Play 3D effect at world position and await its completion.</para>
        /// </summary>
        public async UniTask Play3DEffect(AudioClip clip, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
        {
            if (!clip) return;
            var effect = PlayEffect(clip, false, worldPos, token, is3D: true, startTime: startTime);
            if (effect?.CompletionSource != null)
                await effect.CompletionSource.Task;
        }

        /// <summary>
        /// 播放 3D 音效（通过资源名）
        /// <para>Play 3D effect by asset name.</para>
        /// </summary>
        public async UniTask Play3DEffect(string clipName, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = LoadClip(clipName);
            await Play3DEffect(clip, worldPos, token, startTime);
        }

        /// <summary>
        /// 播放循环音效（返回 AudioSource 供手动停止）
        /// <para>Play looping effect, returns the AudioSource for manual control.</para>
        /// </summary>
        public AudioSource PlayLoopEffect(AudioClip clip, bool is3D = false, Vector3 pos = default,
            float startTime = 0f)
        {
            if (!clip) return null;
            return PlayEffect(clip, true, pos, CancellationToken.None, is3D, startTime).Source;
        }

        #endregion

        #region 播放核心

        // 内部音效播放实现
        private ActiveEffect PlayEffect(AudioClip clip, bool loop, Vector3 pos, CancellationToken token, bool is3D,
            float startTime = 0f)
        {
            var source = GetSfxFromPool();

            if (token.IsCancellationRequested)
            {
                ReturnSfxToPool(source);
                return null;
            }

            source.clip = clip;
            source.loop = loop;

            if (is3D)
            {
                source.spatialBlend = 1f;
                source.transform.position = pos;
            }
            else
            {
                source.spatialBlend = 0f;
                source.transform.localPosition = Vector3.zero;
            }

            source.volume = GetSfxVolume();
            source.mute = _isMuted;
            source.time = Mathf.Clamp(startTime, 0f, clip.length);
            source.Play();

            var effect = new ActiveEffect { Source = source, CompletionSource = null };
            if (!loop)
            {
                effect.CompletionSource = new UniTaskCompletionSource();
                if (token.CanBeCanceled)
                {
                    effect.Cancellation = token.RegisterWithoutCaptureExecutionContext(() =>
                    {
                        UniTask.Post(() =>
                        {
                            effect.CompletionSource?.TrySetCanceled();
                            if (effect.Source)
                            {
                                effect.Source.Stop();
                                ReturnSfxToPool(effect.Source);
                            }

                            _activeEffects.Remove(effect);
                        });
                    });
                }
                else
                    effect.Cancellation = default;
            }
            else
            {
                effect.Cancellation = token.CanBeCanceled
                    ? token.RegisterWithoutCaptureExecutionContext(() =>
                    {
                        UniTask.Post(() =>
                        {
                            if (effect.Source)
                            {
                                effect.Source.Stop();
                                ReturnSfxToPool(effect.Source);
                            }

                            _activeEffects.Remove(effect);
                        });
                    })
                    : default;
            }

            _activeEffects.Add(effect);
            return effect;
        }

        #endregion

        #region 全局控制

        /// <summary>
        /// 暂停所有音频（BGM + 音效）
        /// <para>Pause all audio (BGM and SFX).</para>
        /// </summary>
        public void PauseAll()
        {
            _isPaused = true;
            _bgmSource.Pause();
            foreach (var effect in _activeEffects)
                effect.Source.Pause();
        }

        /// <summary>
        /// 恢复所有音频
        /// <para>Unpause all audio.</para>
        /// </summary>
        public void UnPauseAll()
        {
            _isPaused = false;
            _bgmSource.UnPause();
            foreach (var effect in _activeEffects)
                effect.Source.UnPause();
        }

        /// <summary>
        /// 设置全局静音
        /// <para>Set global mute.</para>
        /// </summary>
        public void SetMute(bool mute)
        {
            _isMuted = mute;
            SetMixerVolume(_masterGroup, mute ? -80f : ToDecibel(GetMasterVolume()));
            PlayerPrefs.SetInt(_muteKey, mute ? 1 : 0);
        }

        /// <summary>
        /// 是否全局静音
        /// <para>Is global muted.</para>
        /// </summary>
        public bool IsMuted => _isMuted;

        /// <summary>
        /// 停止所有音效（不影响 BGM）
        /// <para>Stop all SFX (BGM untouched).</para>
        /// </summary>
        public void StopAllEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                effect.Source.Stop();
                effect.CompletionSource?.TrySetResult();
                effect.CompletionSource = null;
                effect.Cancellation.Dispose();
                ReturnSfxToPool(effect.Source);
                _activeEffects.RemoveAt(i);
            }
        }

        /// <summary>
        /// 停止循环音效
        /// <para>Stop the looping sound effect</para>
        /// </summary>
        /// <param name="source">音频源</param>
        public void StopLoopEffect(AudioSource source)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect.Source != source) continue;
                effect.Source.Stop();
                effect.Cancellation.Dispose();
                ReturnSfxToPool(effect.Source);
                _activeEffects.RemoveAt(i);
                break;
            }
        }

        /// <summary>
        /// 停止所有音频（BGM + 音效）
        /// <para>Stop all audio.</para>
        /// </summary>
        public void StopAll()
        {
            StopBGM();
            StopAllEffects();
        }

        #endregion

        #region 音量与淡入淡出

        /// <summary>
        /// 设置主音量（0~1），影响所有通道
        /// <para>Set master volume (0~1), affects all groups.</para>
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            SetMixerVolume(_masterGroup, ToDecibel(volume));
            PlayerPrefs.SetFloat(_masterVolumeKey, volume);
        }

        /// <summary>
        /// 设置背景音乐音量（0~1）
        /// <para>Set BGM volume (0~1).</para>
        /// </summary>
        public void SetBgmVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            SetMixerVolume(_bgmGroup, ToDecibel(volume));
            PlayerPrefs.SetFloat(_bgmVolumeKey, volume);
        }

        /// <summary>
        /// 设置音效音量（0~1）
        /// <para>Set SFX volume (0~1).</para>
        /// </summary>
        public void SetSfxVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            SetMixerVolume(_sfxGroup, ToDecibel(volume));
            PlayerPrefs.SetFloat(_sfxVolumeKey, volume);
        }

        /// <summary>
        /// 获取当前背景音乐音量（0~1）
        /// <para>Get current BGM volume.</para>
        /// </summary>
        public float GetBgmVolume() => PlayerPrefs.GetFloat(_bgmVolumeKey, defaultBgmVolume);

        /// <summary>
        /// 获取当前音效音量（0~1）
        /// <para>Get current SFX volume.</para>
        /// </summary>
        public float GetSfxVolume() => PlayerPrefs.GetFloat(_sfxVolumeKey, defaultSfxVolume);

        /// <summary>
        /// 获取当前主音量（0~1）
        /// <para>Get current master volume.</para>
        /// </summary>
        public float GetMasterVolume() => PlayerPrefs.GetFloat(_masterVolumeKey, 1f);

        /// <summary>
        /// 背景音乐淡入到目标音量
        /// <para>Fade BGM to a target volume.</para>
        /// </summary>
        public async UniTask FadeBGM(float targetVolume, float duration, CancellationToken token = default)
        {
            float start = _bgmSource.volume;
            await FadeVolume(_bgmSource, start, targetVolume, duration, token);
        }

        /// <summary>
        /// 背景音乐淡出并停止
        /// <para>Fade out BGM and stop.</para>
        /// </summary>
        public async UniTask FadeOutBGM(float duration, CancellationToken token = default)
        {
            await FadeVolume(_bgmSource, _bgmSource.volume, 0f, duration, token);
            _bgmSource.Stop();
            _bgmSource.volume = 0f;
        }

        /// <summary>
        /// 线性淡入淡出指定 AudioSource 的音量（静态工具方法）
        /// <para>Linearly fade an AudioSource volume.</para>
        /// </summary>
        public static async UniTask FadeVolume(AudioSource source, float from, float to, float duration,
            CancellationToken token = default)
        {
            if (!source) return;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            source.volume = to;
        }

        #endregion

        #region 查询

        /// <summary>
        /// 指定名字的 BGM 是否正在播放
        /// <para>Check if BGM with given name is playing.</para>
        /// </summary>
        public bool IsPlayingBGM(string clipName)
        {
            return _bgmSource.isPlaying && _bgmSource.clip != null && _bgmSource.clip.name == clipName;
        }

        /// <summary>
        /// 指定名字的音效是否正在播放
        /// <para>Check if any SFX with given name is playing.</para>
        /// </summary>
        public bool IsPlayingEffect(string clipName)
        {
            foreach (var effect in _activeEffects)
            {
                if (effect.Source.isPlaying && effect.Source.clip != null && effect.Source.clip.name == clipName)
                    return true;
            }

            return false;
        }

        #endregion

        #region 内部辅助

        // 设置混音器组音量（分贝）
        private void SetMixerVolume(AudioMixerGroup group, float db)
        {
            if (audioMixer && group)
                audioMixer.SetFloat(group.name + "Volume", db);
        }

        // 线性值转分贝
        private static float ToDecibel(float linear)
        {
            return linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
        }

        // 加载音频资源
        private AudioClip LoadClip(string clipName)
        {
            string path = EF.Assets.CurrentSystemType == AssetsSystemType.Default
                ? $"{EF.Projects.AppConst.AudioPath}{clipName}"
                : clipName;
            return EF.Assets.Load<AudioClip>(path);
        }

        // 从 PlayerPrefs 恢复设置
        private void LoadSettings()
        {
            float master = PlayerPrefs.GetFloat(_masterVolumeKey, 1f);
            float bgm = PlayerPrefs.GetFloat(_bgmVolumeKey, defaultBgmVolume);
            float sfx = PlayerPrefs.GetFloat(_sfxVolumeKey, defaultSfxVolume);
            bool muted = PlayerPrefs.GetInt(_muteKey, 0) == 1;

            SetMasterVolume(master);
            SetBgmVolume(bgm);
            SetSfxVolume(sfx);
            if (muted) SetMute(true);
        }

        // 保存设置到 PlayerPrefs
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(_masterVolumeKey, GetMasterVolume());
            PlayerPrefs.SetFloat(_bgmVolumeKey, GetBgmVolume());
            PlayerPrefs.SetFloat(_sfxVolumeKey, GetSfxVolume());
            PlayerPrefs.SetInt(_muteKey, _isMuted ? 1 : 0);
        }

        // 安全获取混音器组
        private AudioMixerGroup GetMixerGroupSafe(string groupName)
        {
            if (audioMixer == null) return null;
            var groups = audioMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0) return groups[0];
            Debug.LogWarning($"[AudioSystem] Mixer group '{groupName}' not found");
            return null;
        }

        #endregion
    }
}