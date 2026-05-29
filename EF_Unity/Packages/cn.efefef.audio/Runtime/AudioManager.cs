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
using EasyFramework.Managers.Pool;
using EasyFramework.Systems.Assets;
using UnityEngine;
using UnityEngine.Audio;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 音频系统，统一管理 BGM 与音效，支持混音器、对象池、异步播放。
    /// <para>Audio system, managing BGM and Effect with mixer, pool and async playback.</para>
    /// </summary>
    [Manager(Order = 89700)]
    [Dependency(typeof(PoolManager)), Dependency(typeof(AssetsSystem))]
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
        public bool IsMuted { get; private set; }

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

        #region 配置字段（仅通过 Editor 或运行时设置）

        [HideInInspector, SerializeField] private AudioMixer audioMixer; // 主混音器资源
        [HideInInspector, SerializeField] private int maxEffectPoolSize = 20; // 音效对象池最大空闲数量
        [HideInInspector, SerializeField] private int prewarmCount = 1; // 音效对象池预热数量
        [HideInInspector, SerializeField] private float effectIdleTimeout = 30f; // 音效播放器闲置超时销毁时间（秒），≤0 不启用
        [HideInInspector, SerializeField, Range(0f, 1f)] private float defaultBgmVolume = 1f; // 默认背景音乐音量
        [HideInInspector, SerializeField, Range(0f, 1f)] private float defaultEffectVolume = 1f; // 默认音效音量

        #endregion

        #region 私有变量

        // 混音器通道名称
        private const string MasterGroupName = "Master";
        private const string BgmGroupName = "BGM";
        private const string EffectGroupName = "Effect";

        private uint _autoIncrementID;  // 音频自增ID

        // PlayerPrefs 键
        private string _muteKey;            // 静音键
        private string _bgmVolumeKey;       // BGM 音量键
        private string _effectVolumeKey;    // Effect 音量键
        private string _masterVolumeKey;    // 主音量键

        private AudioMixerGroup _masterGroup;   // 主混音器组
        private AudioMixerGroup _bgmGroup;      // 背景音乐混音器组
        private AudioMixerGroup _effectGroup;   // 音效混音器组

        private AudioSource _bgmSource; // BGM 专用播放器
        private List<ActiveEffect> _activeEffects; // 当前活动的音效列表
        private Dictionary<uint, ActiveEffect> _activeEffectDict; // 活动音效映射图

        private Transform _effectRoot;      // 音效对象池根节点
        private GameObject _effectPrefab;   // 音效对象池预制件

        private CancellationTokenSource _lifetimeCts; // 系统生命周期取消令牌

        #endregion

        void ISingleton.Init()
        {
            _muteKey = Application.productName + "Audio_Muted";
            _bgmVolumeKey = Application.productName + "Audio_BGM_Volume";
            _effectVolumeKey = Application.productName + "Audio_Effect_Volume";
            _masterVolumeKey = Application.productName + "Audio_Master_Volume";

            _lifetimeCts = new CancellationTokenSource();

            // 创建音效对象池根节点
            _effectRoot = new GameObject("EffectRoot").transform;
            _effectRoot.SetParent(transform);

            // 初始化混音器组
            if (audioMixer)
            {
                _masterGroup = audioMixer.FindMatchingGroups(MasterGroupName)[0];
                _bgmGroup = audioMixer.FindMatchingGroups(BgmGroupName)[0];
                _effectGroup = audioMixer.FindMatchingGroups(EffectGroupName)[0];
            }

            // 创建 BGM 播放器
            var bgmGo = new GameObject("BGM_Source");
            bgmGo.transform.SetParent(_effectRoot);
            _bgmSource = bgmGo.AddComponent<AudioSource>();
            _bgmSource.outputAudioMixerGroup = _bgmGroup;
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _effectPrefab = CreateEffectPrefab();
            Pool.PoolManager.Instance.CreateGameObjectPool(_effectPrefab, _effectRoot, prewarmCount, maxEffectPoolSize, effectIdleTimeout);

            _activeEffects = new List<ActiveEffect>();
            _activeEffectDict = new Dictionary<uint, ActiveEffect>();

            LoadSettings();
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            // 回收播放完毕的非循环音效
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect.IsPaused || effect.Source.loop || effect.Source.isPlaying) continue;
                if (effect.IsReleased) continue;
                effect.IsReleased = true;
                effect.CompletionSource?.TrySetResult();
                effect.CompletionSource = null;
                effect.Cancellation.Dispose();
                ReturnEffectToPool(effect.Source);
                _activeEffects.RemoveAt(i);
                _activeEffectDict.Remove(effect.ID);
            }
        }

        void ISingleton.Quit()
        {
            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();

            StopAll();

            _activeEffects.Clear();
            _activeEffectDict.Clear();

            if (_bgmSource)
                Destroy(_bgmSource.gameObject);

            if (_effectRoot)
                Destroy(_effectRoot.gameObject);

            // 销毁音效对象池
            if (_effectPrefab)
                Pool.PoolManager.Instance.DestroyGameObjectPool(_effectPrefab);

            SaveSettings();
        }

        #region 内部函数

        // 创建用于对象池的预制体
        private GameObject CreateEffectPrefab()
        {
            var go = new GameObject("EffectPrototype");
            go.transform.SetParent(_effectRoot);
            var source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _effectGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            go.SetActive(false);
            return go;
        }

        #region 对象池辅助

        // 从池中获取激活的 AudioSource
        private AudioSource GetEffectFromPool()
        {
            var go = Pool.PoolManager.Instance.Spawn(_effectPrefab);
            if (go)
            {
                var source = go.GetComponent<AudioSource>();
                ResetEffectSource(source);
                return source;
            }
            else
            {
                D.Error("[ AudioSystem ] Failed to get AudioSource from pool. Creating fallback.");
                var goFallback = new GameObject("EffectFallback");
                goFallback.transform.SetParent(_effectRoot);
                var source = goFallback.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = _effectGroup;
                source.playOnAwake = false;
                return source;
            }
        }

        // 归还音效播放器到对象池
        private void ReturnEffectToPool(AudioSource source)
        {
            if (!source) return;
            source.Stop();
            source.gameObject.SetActive(false);
            source.transform.SetParent(_effectRoot, false);
            Pool.PoolManager.Instance.Despawn(source.gameObject);
        }

        // 重置从池中取出的 AudioSource 状态
        private void ResetEffectSource(AudioSource source)
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
            source.outputAudioMixerGroup = _effectGroup;
            source.transform.localPosition = Vector3.zero;
            source.transform.localRotation = Quaternion.identity;
        }

        #endregion

        // 内部音效播放实现
        private ActiveEffect PlayEffect(AudioClip clip, bool loop, Vector3 pos, CancellationToken token, bool is3D, float startTime = 0f)
        {
            var source = GetEffectFromPool();

            if (token.IsCancellationRequested)
            {
                ReturnEffectToPool(source);
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

            source.volume = GetEffectVolume();
            source.mute = IsMuted;
            source.time = Mathf.Clamp(startTime, 0f, clip.length);
            source.Play();

            var effect = new ActiveEffect
            {
                ID = ++_autoIncrementID,
                IsPaused = false,
                IsReleased = false,
                Source = source,
                CompletionSource = null
            };

            if (!loop)
                effect.CompletionSource = new UniTaskCompletionSource();

            RegisterCancelCallback(effect, token);

            _activeEffects.Add(effect);
            _activeEffectDict.Add(effect.ID, effect);
            return effect;
        }

        // 注册取消回调
        private void RegisterCancelCallback(ActiveEffect effect, CancellationToken token)
        {
            if (!token.CanBeCanceled) return;

            effect.Cancellation = token.RegisterWithoutCaptureExecutionContext(() =>
            {
                UniTask.Post(() =>
                {
                    if (effect.IsReleased) return;
                    effect.IsReleased = true;
                    effect.IsPaused = false;

                    effect.CompletionSource?.TrySetCanceled();
                    effect.CompletionSource = null;

                    if (effect.Source)
                    {
                        effect.Source.Stop();
                        ReturnEffectToPool(effect.Source);
                    }

                    _activeEffects.Remove(effect);
                    _activeEffectDict.Remove(effect.ID);
                    effect.Cancellation.Dispose();
                });
            });
        }

        #region Assist - 辅助

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
            string path = AssetsSystem.Instance.CurrentSystemType == AssetsSystemType.Default
                ? $"{EFC.Projects.AppConst.AudioPath}{clipName}"
                : clipName;
            return AssetsSystem.Instance.Load<AudioClip>(path);
        }

        // 从 PlayerPrefs 恢复设置
        private void LoadSettings()
        {
            float master = PlayerPrefs.GetFloat(_masterVolumeKey, 1f);
            float bgm = PlayerPrefs.GetFloat(_bgmVolumeKey, defaultBgmVolume);
            float effect = PlayerPrefs.GetFloat(_effectVolumeKey, defaultEffectVolume);
            bool muted = PlayerPrefs.GetInt(_muteKey, 0) == 1;

            SetMasterVolume(master);
            SetBgmVolume(bgm);
            SetEffectVolume(effect);
            if (muted) SetMute(true);
        }

        // 保存设置到 PlayerPrefs
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat(_masterVolumeKey, GetMasterVolume());
            PlayerPrefs.SetFloat(_bgmVolumeKey, GetBgmVolume());
            PlayerPrefs.SetFloat(_effectVolumeKey, GetEffectVolume());
            PlayerPrefs.SetInt(_muteKey, IsMuted ? 1 : 0);
        }

        // 安全获取混音器组
        private AudioMixerGroup GetMixerGroupSafe(string groupName)
        {
            if (audioMixer == null) return null;
            var groups = audioMixer.FindMatchingGroups(groupName);
            if (groups.Length > 0) return groups[0];
            Warning($"Mixer group '{groupName}' not found");
            return null;
        }

        #endregion

        private void Warning(string message)
        {
            if (OpenDebug)
                D.Warning($"[ AudioSystem ] {message}");
        }

        #endregion

        #region 运行时配置方法

        /// <summary>
        /// 设置主混音器
        /// <para>Set the main AudioMixer at runtime.</para>
        /// </summary>
        public void SetAudioMixer(AudioMixer newMixer)
        {
            if (newMixer == audioMixer) return;
            audioMixer = newMixer;

            var newMaster = GetMixerGroupSafe(MasterGroupName);
            var newBgm = GetMixerGroupSafe(BgmGroupName);
            var newEffect = GetMixerGroupSafe(EffectGroupName);

            bool effectGroupChanged = (newEffect != _effectGroup);
            _masterGroup = newMaster;
            _bgmGroup = newBgm;
            _effectGroup = newEffect;

            if (_bgmSource)
                _bgmSource.outputAudioMixerGroup = _bgmGroup;

            if (!effectGroupChanged || _activeEffects == null) return;
            for (var i = 0; i < _activeEffects.Count; i++)
            {
                var effect = _activeEffects[i];
                if (effect.Source)
                    effect.Source.outputAudioMixerGroup = _effectGroup;
            }
        }

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
        /// <para>Set default Effect volume, only affects future playback.</para>
        /// </summary>
        public void SetDefaultEffectVolume(float volume)
        {
            defaultEffectVolume = Mathf.Clamp01(volume);
        }

        #endregion

        #region BGM - 背景音乐

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

        #region Effect - 音效

        /// <summary>
        /// 播放 2D 音效，等待自然结束
        /// <para>Play 2D effect and await its completion.</para>
        /// </summary>
        public async UniTask<uint> Play2DEffect(AudioClip clip, CancellationToken token = default, float startTime = 0f)
        {
            if (!clip) return 0;
            var effect = PlayEffect(clip, false, Vector3.zero, token, is3D: false, startTime: startTime);
            if (null == effect) return 0;
            if (effect.CompletionSource != null)
                await effect.CompletionSource.Task;
            return effect.ID;
        }

        /// <summary>
        /// 播放 2D 音效（通过资源名）
        /// <para>Play 2D effect by asset name.</para>
        /// </summary>
        public async UniTask<uint> Play2DEffect(string clipName, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = LoadClip(clipName);
            return await Play2DEffect(clip, token, startTime);
        }

        /// <summary>
        /// 播放 3D 音效，等待自然结束
        /// <para>Play 3D effect at world position and await its completion.</para>
        /// </summary>
        public async UniTask<uint> Play3DEffect(AudioClip clip, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
        {
            if (!clip) return 0;
            var effect = PlayEffect(clip, false, worldPos, token, is3D: true, startTime: startTime);
            if (null == effect) return 0;
            if (effect.CompletionSource != null)
                await effect.CompletionSource.Task;
            return effect.ID;
        }

        /// <summary>
        /// 播放 3D 音效（通过资源名）
        /// <para>Play 3D effect by asset name.</para>
        /// </summary>
        public async UniTask<uint> Play3DEffect(string clipName, Vector3 worldPos, CancellationToken token = default,
            float startTime = 0f)
        {
            var clip = LoadClip(clipName);
            return await Play3DEffect(clip, worldPos, token, startTime);
        }

        /// <summary>
        /// 播放循环音效（返回 AudioSource 供手动停止）
        /// <para>Play looping effect, returns the AudioSource for manual control.</para>
        /// </summary>
        public AudioSource PlayLoopEffect(AudioClip clip, bool is3D = false, Vector3 pos = default,
            float startTime = 0f)
        {
            return !clip ? null : PlayEffect(clip, true, pos, CancellationToken.None, is3D, startTime).Source;
        }

        /// <summary>
        /// 通过 ID 停止指定的音效
        /// <para>Stop the specified sound effect by id</para>
        /// </summary>
        public void StopEffect(uint id)
        {
            if (!_activeEffectDict.TryGetValue(id, out var effect)) return;
            if (effect.IsReleased) return;
            effect.IsReleased = true;
            effect.IsPaused = false;
            effect.Source.Stop();
            effect.CompletionSource?.TrySetCanceled();
            effect.CompletionSource = null;
            effect.Cancellation.Dispose();
            ReturnEffectToPool(effect.Source);
            _activeEffects.Remove(effect);
            _activeEffectDict.Remove(id);
        }

        /// <summary>
        /// 通过 ID 暂停指定的音效
        /// <para>Pause the specified sound effect by ID</para>
        /// </summary>
        public void PauseEffect(uint id)
        {
            if (!_activeEffectDict.TryGetValue(id, out var effect)) return;
            if (effect.IsReleased || effect.IsPaused) return;
            effect.IsPaused = true;
            effect.Source.Pause();
        }

        /// <summary>
        /// 通过 ID 恢复指定的音效
        /// <para>Restore the specified sound effect through the ID</para>
        /// </summary>
        public async UniTask UnPauseEffect(uint id)
        {
            if (!_activeEffectDict.TryGetValue(id, out var effect)) return;
            if (effect.IsReleased || !effect.IsPaused) return;
            effect.Source.UnPause();
            await UniTask.Yield(PlayerLoopTiming.Update);
            if (!effect.IsReleased)
                effect.IsPaused = false;
        }

        /// <summary>
        /// 暂停所有当前正在播放的音效
        /// <para>Pause all the currently playing sound effects</para>
        /// </summary>
        public void PauseAllEffect()
        {
            foreach (var effect in _activeEffects)
            {
                if (effect.IsReleased || effect.IsPaused) continue;
                effect.IsPaused = true;
                effect.Source.Pause();
            }
        }

        /// <summary>
        /// 恢复所有音效（仅恢复被暂停的，不会重新开始已停止的音效）
        /// <para>Restore all sound effects (only restore those that were paused; existing stopped effects will not be restarted)</para>
        /// </summary>
        public async UniTask UnPauseAllEffect()
        {
            var toUnpause = new List<ActiveEffect>();
            foreach (var effect in _activeEffects)
            {
                if (!effect.IsReleased && effect.IsPaused)
                    toUnpause.Add(effect);
            }

            foreach (var effect in toUnpause)
                effect.Source.UnPause();

            await UniTask.Yield(PlayerLoopTiming.Update);

            foreach (var effect in toUnpause)
            {
                if (!effect.IsReleased)
                    effect.IsPaused = false;
            }
        }

        #endregion

        #region Global Control - 全局控制

        /// <summary>
        /// 暂停所有音频（BGM + 音效）
        /// <para>Pause all audio (BGM and Effect).</para>
        /// </summary>
        public void PauseAll()
        {
            IsPaused = true;
            _bgmSource.Pause();
            PauseAllEffect();
        }

        /// <summary>
        /// 恢复所有音频
        /// <para>Unpause all audio.</para>
        /// </summary>
        public async UniTask UnPauseAll()
        {
            IsPaused = false;
            _bgmSource.UnPause();
            await UnPauseAllEffect();
        }

        /// <summary>
        /// 设置全局静音
        /// <para>Set global mute.</para>
        /// </summary>
        public void SetMute(bool mute)
        {
            IsMuted = mute;
            SetMixerVolume(_masterGroup, mute ? -80f : ToDecibel(GetMasterVolume()));
            PlayerPrefs.SetInt(_muteKey, mute ? 1 : 0);
        }

        /// <summary>
        /// 停止所有音效， BGM不受影响
        /// <para>Stop all Effect, BGM untouched.</para>
        /// </summary>
        public void StopAllEffects()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeEffects[i];
                if (effect.IsReleased) continue;
                effect.IsReleased = true;
                effect.Source.Stop();
                effect.CompletionSource?.TrySetResult();
                effect.CompletionSource = null;
                effect.Cancellation.Dispose();
                ReturnEffectToPool(effect.Source);
                _activeEffects.RemoveAt(i);
                _activeEffectDict.Remove(effect.ID);
            }
        }

        /// <summary>
        /// 停止循环音效
        /// <para>Stop the looping sound effect</para>
        /// </summary>
        /// <param name="source">音频源</param>
        public void StopLoopEffect(AudioSource source)
        {
            if (source == null) return;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].Source != source) continue;
                StopEffect(_activeEffects[i].ID);
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
        /// <para>Set Effect volume (0~1).</para>
        /// </summary>
        public void SetEffectVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            SetMixerVolume(_effectGroup, ToDecibel(volume));
            PlayerPrefs.SetFloat(_effectVolumeKey, volume);
        }

        /// <summary>
        /// 获取当前背景音乐音量（0~1）
        /// <para>Get current BGM volume.</para>
        /// </summary>
        public float GetBgmVolume() => PlayerPrefs.GetFloat(_bgmVolumeKey, defaultBgmVolume);

        /// <summary>
        /// 获取当前音效音量（0~1）
        /// <para>Get current Effect volume.</para>
        /// </summary>
        public float GetEffectVolume() => PlayerPrefs.GetFloat(_effectVolumeKey, defaultEffectVolume);

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
        public async UniTask FadeVolume(AudioSource source, float from, float to, float duration,
            CancellationToken token = default)
        {
            if (!source) return;

            if (duration <= 0f)
            {
                source.volume = to;
                return;
            }

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
        /// <para>Check if any Effect with given name is playing.</para>
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
    }
}