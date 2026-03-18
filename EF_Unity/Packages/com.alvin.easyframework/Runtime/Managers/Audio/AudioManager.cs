/*
 * ================================================
 * Describe:        The class is source m_managerLevel.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:33:01
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:33:01
 * Version:         1.0
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Sources manager.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>, IManager, IUpdate
    {
        private bool _isPausing;
        private bool _isMute;
        private AudioSource _bgm;
        private float _bgmVolumScale = 1f;
        private float _effectVolume = 1.0f;

        Transform _traget;

        private List<Action> _audioCallback;
        private List<AudioSource> _audioInUse;
        private Queue<AudioSource> _audioCanUse;
        void ISingleton.Init()
        {
            _traget = new GameObject("Source").transform;
            _traget.SetParent(EF.Managers);
            _bgm = _traget.gameObject.AddComponent<AudioSource>();

            _audioCallback = new List<Action>();
            _audioInUse = new List<AudioSource>();
            _audioCanUse = new Queue<AudioSource>();

            if (PlayerPrefs.HasKey(EF.Projects.AppConst.AppPrefix + "bgmVolum"))
                _bgmVolumScale = PlayerPrefs.GetFloat(EF.Projects.AppConst.AppPrefix + "bgmVolum");
            if (PlayerPrefs.HasKey(EF.Projects.AppConst.AppPrefix + "effectVolum"))
                _effectVolume = PlayerPrefs.GetFloat(EF.Projects.AppConst.AppPrefix + "effectVolum");

            _bgm.playOnAwake = true;
            _bgm.loop = true;
            _bgm.volume = _bgmVolumScale;
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (_isPausing) 
                return;
            for (int i = 0; i < _audioInUse.Count; i++)
            {
                //if ((m_flt_AudioTimer[i] - m_lst_AudioSources[i].time) >= 0.06f) continue;
                if (_audioInUse[i].isPlaying || _audioInUse[i].loop) continue;

                StopEffectExCallbcak(i);
            }
        }

        void ISingleton.Quit()
        {
            _isPausing = true;
            _bgm.clip = null;
            for (int i = _audioInUse.Count - 1; i >= 0; i--)
            {
                AudioSource audio = _audioInUse[i];
                audio.clip = null;
                Object.Destroy(audio);
                Object.Destroy(audio.gameObject);
            }
            for (int i = _audioCanUse.Count - 1; i >= 0; i--)
            {
                AudioSource audio = _audioCanUse.Dequeue();
                audio.clip = null;
                Object.Destroy(audio);
                Object.Destroy(audio.gameObject);
            }

            _audioCallback.Clear();
            _audioInUse.Clear();
            _audioCanUse.Clear();

            _audioCallback = null;
            _audioInUse = null;
            _audioCanUse = null;
        }

        #region Play
        /// <summary>
        /// Play bgm by name.
        /// <para>通过名字播放背景音乐</para>
        /// </summary>
        /// <param name="name">the bgm name.<para>音乐名称</para></param>
        /// <param name="isLoop">the bgm is loop.<para>是否循环</para></param>
        public void PlayBGMByName(string name, bool isLoop = false)
        {
            if (IsPlayingBGM(name)) return;
            AudioClip clip = GetClipByName(name);
            PlayBGM(clip, isLoop);
        }

        /// <summary>
        /// Play effect source by clip.
        /// <para>通过clip播放特效声音</para>
        /// </summary>
        /// <param name="clip">The effect source clip.<para>音频的clip</para></param>
        /// <param name="callback">The completion callback for source played.<para>音频播放结束的回调</para></param>
        public void Play2DEffectSouceByClip(AudioClip clip, Action callback = null)
        {
            PlayEffect(clip);
            _audioCallback.Add(callback);
        }

        /// <summary>
        /// Play effect source by clip.
        /// <para>通过clip播放音乐</para>
        /// </summary>
        /// <param name="clip">The effect source clip.<para>音频的clip</para></param>
        /// <param name="pos">The sources play position. <para>音频位于空间的位置</para></param>
        /// <param name="callback">The completion callback for source played.<para>音频播放结束的回调</para></param>
        public void Play3DEffectSouceByClip(AudioClip clip, Vector3 pos, Action callback = null)
        {
            PlayEffect(clip, false, pos);
            _audioCallback.Add(callback);
        }

        /// <summary>
        /// Play effect source by name.
        /// <para>通过名字播放特效声音</para>
        /// </summary>
        /// <param name="name">The effect source name.<para>音乐名称</para></param>
        /// <param name="callback">The completion callback for source played.<para>音频播放结束的回调</para></param>
        public void Play2DEffectSouceByName(string name, Action callback = null)
        {
            AudioClip clip = GetClipByName(name);
            PlayEffect(clip);
            _audioCallback.Add(callback);
        }

        /// <summary>
        /// Play effect source by name.
        /// <para>通过名字播放特效声音</para>
        /// </summary>
        /// <param name="name">The effect source name.<para>音乐名称</para></param>
        /// <param name="pos">The sources play position.<para>音频位于空间的位置</para></param>
        /// <param name="callback">The completion callback for source played.<para>音频播放结束的回调</para></param>
        public void Play3DEffectSouceByName(string name, Vector3 pos, Action callback = null)
        {
            AudioClip clip = GetClipByName(name);
            PlayEffect(clip, false, pos);
            _audioCallback.Add(callback);
        }
        #endregion

        #region Set
        /// <summary>
        /// Pause the all audio. 
        /// <para>暂停全部声音</para>
        /// </summary>
        public void PauseAll()
        {
            _bgm.Pause();
            foreach (var item in _audioInUse)
                item.Pause();
            _isPausing = true;
        }

        /// <summary>
        /// UnPause the all audio.
        /// <para>解除暂停</para>
        /// </summary>
        public void UnPauseAll()
        {
            _bgm.UnPause();
            foreach (var item in _audioInUse)
                item.UnPause();
            _isPausing = false;
        }

        /// <summary>
        /// Set all audio source mute.
        /// <para>设置静音</para>
        /// </summary>
        /// <param name="isSetMute"></param>
        public void MuteAll(bool isSetMute)
        {
            _isMute = isSetMute;
            _bgm.mute = isSetMute;
            for (int i = 0, imax = _audioInUse.Count; i < imax; i++)
            {
                _audioInUse[i].mute = isSetMute;
            }
        }

        /// <summary>
        /// Stop BGM.
        /// <para>停止背景音乐</para>
        /// </summary>
        public void StopBGM()
        {
            _bgm.Stop();
        }

        /// <summary>
        /// Stop a effect audio source by name.
        /// <para>通过名字停止播放</para>
        /// </summary>
        public void StopEffectSourceByName(string effectName)
        {
            for (int i = _audioInUse.Count - 1; i >= 0; i--)
            {
                if (_audioInUse[i].isPlaying && _audioInUse[i].clip.name == effectName)
                {
                    _audioInUse[i].Stop();
                    StopEffectExCallbcak(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Stop a effect audio source by clip.
        /// <para>通过clip停止播放</para>
        /// </summary>
        public void StopEffectSourceByClip(AudioClip clip)
        {
            for (int i = _audioInUse.Count - 1; i >= 0; i--)
            {
                if (_audioInUse[i].isPlaying && _audioInUse[i].clip == clip)
                {
                    _audioInUse[i].Stop();
                    StopEffectExCallbcak(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Stop all effect audio source.
        /// <para>所有音效停止播放</para>
        /// </summary>
        public void StopAllEffectSources()
        {
            for (int i = _audioInUse.Count - 1; i >= 0; i--)
            {
                _audioInUse[i].Stop();
                StopEffectExCallbcak(i);
            }
        }

        /// <summary>
        /// Set all audio source stop.
        /// <para>全部停止播放</para>
        /// </summary>
        public void StopAll()
        {
            _bgm.Stop();
            StopAllEffectSources();
        }
        #endregion

        #region Get
        /// <summary>
        /// Determine whether the current audio is playing.
        /// <para>判断当前音频是否在播放中</para>
        /// </summary>
        public bool IsPlayingBGM(string bgmName)
        {
            if (_bgm.isPlaying)
            {
                return _bgm.clip.name == bgmName;
            }

            return false;
        }

        /// <summary>
        /// Determine whether the current effect source is playing. 
        /// <para>判断当前特效音频是否在播放中</para>
        /// </summary>
        public bool IsPlayingEffectSouce(string effectSourceName)
        {
            for (int i = 0; i < _audioInUse.Count; i++)
            {
                var audio = _audioInUse[i];
                if (audio.isPlaying && audio.clip.name == effectSourceName)
                    return true;
            }
            return false;
        }
        #endregion

        #region Volume
        /// <summary>
        /// Set the bgm volume. value by 0 to 1.
        /// <para>设置背景音量大小。取值为0 ~ 1</para>
        /// </summary>
        public void SetBgmVolum(float volume)
        {
            _bgmVolumScale = volume;
            _bgm.volume = _bgmVolumScale;
            PlayerPrefs.SetFloat(EF.Projects.AppConst.AppPrefix + "bgmVolum", _bgmVolumScale);
        }

        /// <summary>
        /// Set the effect volume. value by 0 to 1.
        /// <para>设置效果音量。取值为0 ~ 1</para>
        /// </summary>
        public void SetEffectVolum(float volume)
        {
            _effectVolume = volume;
            PlayerPrefs.SetFloat(EF.Projects.AppConst.AppPrefix + "effectVolum", _effectVolume);
        }

        /// <summary>
        /// Get the bgm volume.
        /// <para>获取背景音量大小</para>
        /// </summary>
        public float GetBgmVolume()
        {
            return _bgmVolumScale;
        }

        /// <summary>
        /// Get the effect volume.
        /// <para>获取音效音量大小</para>
        /// </summary>
        /// <returns></returns>
        public float GetEffectVolume()
        {
            return _effectVolume;
        }
        #endregion

        #region PRIVATE FUNCTION
        private void PlayBGM(AudioClip clip, bool isLoop)
        {
            _bgm.loop = isLoop;
            _bgm.clip = clip;
            _bgm.mute = _isMute;
            _bgm.Play();
        }

        private void PlayEffect(AudioClip clip, bool loop = false, Vector3 pos = default)
        {
            if (clip == null) return;

            AudioSource audioSrc = GetOneAudioSourceToUse();
            _audioInUse.Add(audioSrc);
            audioSrc.transform.localPosition = pos;
            audioSrc.loop = loop;
            audioSrc.clip = clip;
            audioSrc.volume = _effectVolume;
            audioSrc.mute = _isMute;
            audioSrc.Play();
        }

        private void StopEffectExCallbcak(int index)
        {
            _audioCallback[index]?.Invoke();
            _audioInUse[index].gameObject.SetActive(false);
            _audioCanUse.Enqueue(_audioInUse[index]);
            _audioInUse.RemoveAt(index);
            _audioCallback.RemoveAt(index);
        }

        private AudioSource GetOneAudioSourceToUse()
        {
            AudioSource audioSource;
            if (_audioCanUse.Count != 0)
                audioSource = _audioCanUse.Dequeue();
            else
                audioSource = CreateAudioSource();
            audioSource.gameObject.SetActive(true);
            return audioSource;
        }

        private AudioSource CreateAudioSource()
        {
            GameObject obj = new GameObject("AudioSourceItem" + _traget.childCount);
            obj.transform.SetParent(_traget);
            AudioSource audio = obj.AddComponent<AudioSource>();
            audio.playOnAwake = true;
            audio.loop = false;
            return audio;
        }

        private AudioClip GetClipByName(string name)
        {
            return EF.Load.LoadInResources<AudioClip>($"{EF.Projects.AppConst.AudioPath}{name}");
        }
        #endregion
    }
}
