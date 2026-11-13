/*
 * ================================================
 * Describe:      默认音频播放器：AudioMixer 分组、对象池、2D/3D/循环音效、空闲回收。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 默认音频播放器，负责池化播放与空闲回收
    /// <para>Default audio player handling pooled playback and recycling</para>
    /// </summary>
    public sealed class DefaultAudioPlayer : IAudioPlayer, IAudioTickable
    {
        // 共享运行时上下文，由门面在 Initialize 时注入
        private AudioContext _context;

        // 音效 ID 自增计数器
        private uint _autoIncrementID;

        // 当前活动的音效列表，Tick 中回收播放完毕项
        private List<ActiveEffect> _activeEffects;

        // 音效 ID 到活动记录的映射，供按 ID 暂停 / 停止
        private Dictionary<uint, ActiveEffect> _activeEffectDict;

        // 音效对象池预制体
        private GameObject _effectPrefab;

        /// <summary>
        /// 当前活跃音效数量
        /// <para>Count of active effects</para>
        /// </summary>
        public int ActiveEffectCount => _activeEffects?.Count ?? 0;

        /// <summary>
        /// BGM 播放器
        /// <para>Dedicated BGM AudioSource.</para>
        /// </summary>
        public AudioSource BgmSource => _context?.BgmSource;

        #region 生命周期

        /// <summary>
        /// 创建播放器与音效对象池
        /// <para>Create players and the effect pool</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context</para></param>
        public void Initialize(AudioContext context)
        {
            _context = context;
            _activeEffects = new List<ActiveEffect>();
            _activeEffectDict = new Dictionary<uint, ActiveEffect>();

            // 音效对象池根节点
            _context.EffectRoot = new GameObject("EffectRoot").transform;
            _context.EffectRoot.SetParent(_context.OwnerRoot);

            // BGM 专用播放器
            var bgmGo = new GameObject("BGM_Source");
            bgmGo.transform.SetParent(_context.EffectRoot);
            _context.BgmSource = bgmGo.AddComponent<AudioSource>();
            _context.BgmSource.outputAudioMixerGroup = _context.BgmGroup;
            _context.BgmSource.loop = true;
            _context.BgmSource.playOnAwake = false;

            _effectPrefab = CreateEffectPrefab();
            Pool.PoolManager.Instance.CreateGameObjectPool(_effectPrefab, _context.EffectRoot,
                _context.PrewarmCount, _context.MaxEffectPoolSize, _context.EffectIdleTimeout);

            _context.EffectGroupChanged += OnEffectGroupChanged;
            _context.MuteChanged += OnMuteChanged;
        }

        /// <summary>
        /// 停止播放并释放资源
        /// <para>Stop playback and release resources</para>
        /// </summary>
        public void Dispose()
        {
            if (_context != null)
            {
                _context.EffectGroupChanged -= OnEffectGroupChanged;
                _context.MuteChanged -= OnMuteChanged;
            }

            StopAllEffects();
            StopBGM();

            if (_activeEffects != null)
            {
                _activeEffects.Clear();
                _activeEffectDict.Clear();
            }

            if (_context != null && _context.BgmSource)
                UnityEngine.Object.Destroy(_context.BgmSource.gameObject);

            if (_context != null && _context.EffectRoot)
                UnityEngine.Object.Destroy(_context.EffectRoot.gameObject);

            if (_effectPrefab)
                Pool.PoolManager.Instance.DestroyGameObjectPool(_effectPrefab);

            _effectPrefab = null;
            _activeEffects = null;
            _activeEffectDict = null;
            _context = null;
        }

        // 混音器更换后刷新全部输出组
        private void OnEffectGroupChanged()
        {
            if (_context == null) return;
            if (_context.BgmSource)
                _context.BgmSource.outputAudioMixerGroup = _context.BgmGroup;
            if (_activeEffects == null) return;
            foreach (var effect in _activeEffects)
            {
                if (effect.Source)
                    effect.Source.outputAudioMixerGroup = _context.EffectGroup;
            }
        }

        // 静音变更后刷新已有播放器
        private void OnMuteChanged()
        {
            if (_context == null) return;
            if (_context.BgmSource)
                _context.BgmSource.mute = _context.IsMuted;
            if (_activeEffects == null) return;
            foreach (var effect in _activeEffects)
            {
                if (effect.Source)
                    effect.Source.mute = _context.IsMuted;
            }
        }

        /// <summary>
        /// 回收播放完毕的音效
        /// <para>Recycle finished effects</para>
        /// </summary>
        /// <param name="elapse">缩放后的帧间隔 <para>Scaled delta time</para></param>
        /// <param name="realElapse">真实帧间隔 <para>Unscaled delta time</para></param>
        public void Tick(float elapse, float realElapse)
        {
            if (_activeEffects == null) return;
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

        #endregion

        #region 对象池辅助

        // 创建用于对象池的预制体
        private GameObject CreateEffectPrefab()
        {
            var go = new GameObject("EffectPrototype");
            go.transform.SetParent(_context.EffectRoot);
            var source = go.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = _context.EffectGroup;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            go.SetActive(false);
            return go;
        }

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

            D.Error("[ AudioSystem ] Failed to get AudioSource from pool. Creating fallback.");
            var goFallback = new GameObject("EffectFallback");
            goFallback.transform.SetParent(_context.EffectRoot);
            var fallback = goFallback.AddComponent<AudioSource>();
            fallback.outputAudioMixerGroup = _context.EffectGroup;
            fallback.playOnAwake = false;
            return fallback;
        }

        // 归还音效播放器到对象池
        private void ReturnEffectToPool(AudioSource source)
        {
            if (!source || _context == null) return;
            source.Stop();
            source.gameObject.SetActive(false);
            source.transform.SetParent(_context.EffectRoot, false);
            Pool.PoolManager.Instance.Despawn(source.gameObject);
        }

        // 重置从池中取出的 AudioSource 状态
        private void ResetEffectSource(AudioSource source)
        {
            if (!source || _context == null) return;
            source.Stop();
            source.clip = null;
            source.loop = false;
            source.spatialBlend = 0f;
            source.mute = false;
            source.volume = 1f;
            source.priority = 128;
            source.pitch = 1f;
            source.outputAudioMixerGroup = _context.EffectGroup;
            source.transform.localPosition = Vector3.zero;
            source.transform.localRotation = Quaternion.identity;
        }

        // 内部音效播放实现
        private ActiveEffect PlayEffect(AudioClip clip, bool loop, Vector3 pos, CancellationToken token,
            bool is3D, float startTime = 0f)
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

            source.volume = _context.EffectVolume;
            source.mute = _context.IsMuted;
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

        #endregion

        #region BGM

        /// <summary>
        /// 播放 BGM
        /// <para>Play BGM</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="loop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        public void PlayBGM(AudioClip clip, bool loop, float startTime)
        {
            if (!clip || _context == null || !_context.BgmSource) return;
            var source = _context.BgmSource;
            if (source.clip == clip && source.isPlaying) return;

            source.clip = clip;
            source.loop = loop;
            source.Play();
            source.time = Mathf.Clamp(startTime, 0f, clip.length);
        }

        /// <summary>
        /// 停止 BGM
        /// <para>Stop BGM.</para>
        /// </summary>
        public void StopBGM()
        {
            if (_context != null && _context.BgmSource)
                _context.BgmSource.Stop();
        }

        /// <summary>
        /// 暂停 BGM
        /// <para>Pause BGM.</para>
        /// </summary>
        public void PauseBGM()
        {
            if (_context != null && _context.BgmSource)
                _context.BgmSource.Pause();
        }

        /// <summary>
        /// 恢复 BGM
        /// <para>Unpause BGM.</para>
        /// </summary>
        public void UnPauseBGM()
        {
            if (_context != null && _context.BgmSource)
                _context.BgmSource.UnPause();
        }

        #endregion

        #region Effect

        /// <summary>
        /// 播放 2D 音效
        /// <para>Play a 2D effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        public async UniTask<uint> Play2DEffect(AudioClip clip, CancellationToken token, float startTime)
        {
            if (!clip) return 0;
            var effect = PlayEffect(clip, false, Vector3.zero, token, is3D: false, startTime: startTime);
            if (null == effect) return 0;
            if (effect.CompletionSource != null)
                await effect.CompletionSource.Task;
            return effect.ID;
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
        public async UniTask<uint> Play3DEffect(AudioClip clip, Vector3 worldPos, CancellationToken token,
            float startTime)
        {
            if (!clip) return 0;
            var effect = PlayEffect(clip, false, worldPos, token, is3D: true, startTime: startTime);
            if (null == effect) return 0;
            if (effect.CompletionSource != null)
                await effect.CompletionSource.Task;
            return effect.ID;
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
        public AudioSource PlayLoopEffect(AudioClip clip, bool is3D, Vector3 pos, float startTime)
        {
            if (!clip) return null;
            return PlayEffect(clip, true, pos, CancellationToken.None, is3D, startTime).Source;
        }

        /// <summary>
        /// 通过 ID 停止指定的音效
        /// <para>Stop an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
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
        /// <para>Pause an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        public void PauseEffect(uint id)
        {
            if (!_activeEffectDict.TryGetValue(id, out var effect)) return;
            if (effect.IsReleased || effect.IsPaused) return;
            effect.IsPaused = true;
            effect.Source.Pause();
        }

        /// <summary>
        /// 通过 ID 恢复指定的音效
        /// <para>Unpause an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
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
        /// <para>Pause all effects.</para>
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
        /// 恢复所有被暂停的音效
        /// <para>Unpause all paused effects.</para>
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

        /// <summary>
        /// 停止所有音效
        /// <para>Stop all effects</para>
        /// </summary>
        public void StopAllEffects()
        {
            if (_activeEffects == null) return;
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
        /// 停止指定的循环音效
        /// <para>Stop the given looping effect.</para>
        /// </summary>
        /// <param name="source">目标播放器 <para>Target AudioSource</para></param>
        public void StopLoopEffect(AudioSource source)
        {
            if (source == null || _activeEffects == null) return;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].Source != source) continue;
                StopEffect(_activeEffects[i].ID);
                break;
            }
        }

        #endregion

        #region 查询

        /// <summary>
        /// 指定名字的 BGM 是否正在播放
        /// <para>Whether the named BGM is playing.</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <returns>是否处于目标状态 <para>Whether the condition holds</para></returns>
        public bool IsPlayingBGM(string clipName)
        {
            if (_context == null || !_context.BgmSource) return false;
            var source = _context.BgmSource;
            return source.isPlaying && source.clip != null && source.clip.name == clipName;
        }

        /// <summary>
        /// 指定名字的音效是否正在播放
        /// <para>Whether any effect with the given name is playing.</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <returns>是否处于目标状态 <para>Whether the condition holds</para></returns>
        public bool IsPlayingEffect(string clipName)
        {
            if (_activeEffects == null) return false;
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