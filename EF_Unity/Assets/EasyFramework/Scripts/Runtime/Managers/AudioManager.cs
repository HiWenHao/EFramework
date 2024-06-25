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
        private bool isPausing;
        private bool isMute;
        private AudioSource m_bGM;
        private float m_bgmVolumScale = 1f;
        private float m_effectVolume = 1.0f;

        Transform m_Traget;

        private List<Action> m_audioCallback;
        private List<AudioSource> m_audioInUse;
        private Queue<AudioSource> m_audioCanUse;
        void ISingleton.Init()
        {
            m_Traget = new GameObject("Source").transform;
            m_Traget.SetParent(EF.Managers);
            m_bGM = m_Traget.gameObject.AddComponent<AudioSource>();

            m_audioCallback = new List<Action>();
            m_audioInUse = new List<AudioSource>();
            m_audioCanUse = new Queue<AudioSource>();

            if (PlayerPrefs.HasKey(EF.Projects.AppConst.AppPrefix + "bgmVolum"))
                m_bgmVolumScale = PlayerPrefs.GetFloat(EF.Projects.AppConst.AppPrefix + "bgmVolum");
            if (PlayerPrefs.HasKey(EF.Projects.AppConst.AppPrefix + "effectVolum"))
                m_effectVolume = PlayerPrefs.GetFloat(EF.Projects.AppConst.AppPrefix + "effectVolum");

            m_bGM.playOnAwake = true;
            m_bGM.loop = true;
            m_bGM.volume = m_bgmVolumScale;
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (isPausing) 
                return;
            for (int i = 0; i < m_audioInUse.Count; i++)
            {
                //if ((m_flt_AudioTimer[i] - m_lst_AudioSources[i].time) >= 0.06f) continue;
                if (m_audioInUse[i].isPlaying || m_audioInUse[i].loop) continue;

                StopEffectExCallbcak(i);
            }
        }

        void ISingleton.Quit()
        {
            isPausing = true;
            m_bGM.clip = null;
            for (int i = m_audioInUse.Count - 1; i >= 0; i--)
            {
                AudioSource _audio = m_audioInUse[i];
                _audio.clip = null;
                Object.Destroy(_audio);
                Object.Destroy(_audio.gameObject);
            }
            for (int i = m_audioCanUse.Count - 1; i >= 0; i--)
            {
                AudioSource _audio = m_audioCanUse.Dequeue();
                _audio.clip = null;
                Object.Destroy(_audio);
                Object.Destroy(_audio.gameObject);
            }

            m_audioCallback.Clear();
            m_audioInUse.Clear();
            m_audioCanUse.Clear();

            m_audioCallback = null;
            m_audioInUse = null;
            m_audioCanUse = null;
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
            m_audioCallback.Add(callback);
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
            m_audioCallback.Add(callback);
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
            m_audioCallback.Add(callback);
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
            m_audioCallback.Add(callback);
        }
        #endregion

        #region Set
        /// <summary>
        /// Pause the all audio. 
        /// <para>暂停全部声音</para>
        /// </summary>
        public void PauseAll()
        {
            m_bGM.Pause();
            foreach (var item in m_audioInUse)
                item.Pause();
            isPausing = true;
        }

        /// <summary>
        /// UnPause the all audio.
        /// <para>解除暂停</para>
        /// </summary>
        public void UnPauseAll()
        {
            m_bGM.UnPause();
            foreach (var item in m_audioInUse)
                item.UnPause();
            isPausing = false;
        }

        /// <summary>
        /// Set all audio source mute.
        /// <para>设置静音</para>
        /// </summary>
        /// <param name="isSetMute"></param>
        public void MuteAll(bool isSetMute)
        {
            isMute = isSetMute;
            m_bGM.mute = isSetMute;
            for (int i = 0, imax = m_audioInUse.Count; i < imax; i++)
            {
                m_audioInUse[i].mute = isSetMute;
            }
        }

        /// <summary>
        /// Stop BGM.
        /// <para>停止背景音乐</para>
        /// </summary>
        public void StopBGM()
        {
            m_bGM.Stop();
        }

        /// <summary>
        /// Stop a effect audio source by name.
        /// <para>通过名字停止播放</para>
        /// </summary>
        public void StopEffectSourceByName(string effectName)
        {
            for (int i = m_audioInUse.Count - 1; i >= 0; i--)
            {
                if (m_audioInUse[i].isPlaying && m_audioInUse[i].clip.name == effectName)
                {
                    m_audioInUse[i].Stop();
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
            for (int i = m_audioInUse.Count - 1; i >= 0; i--)
            {
                if (m_audioInUse[i].isPlaying && m_audioInUse[i].clip == clip)
                {
                    m_audioInUse[i].Stop();
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
            for (int i = m_audioInUse.Count - 1; i >= 0; i--)
            {
                m_audioInUse[i].Stop();
                StopEffectExCallbcak(i);
            }
        }

        /// <summary>
        /// Set all audio source stop.
        /// <para>全部停止播放</para>
        /// </summary>
        public void StopAll()
        {
            m_bGM.Stop();
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
            if (m_bGM.isPlaying)
            {
                return m_bGM.clip.name == bgmName;
            }

            return false;
        }

        /// <summary>
        /// Determine whether the current effect source is playing. 
        /// <para>判断当前特效音频是否在播放中</para>
        /// </summary>
        public bool IsPlayingEffectSouce(string effectSourceName)
        {
            for (int i = 0; i < m_audioInUse.Count; i++)
            {
                var audio = m_audioInUse[i];
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
            m_bgmVolumScale = volume;
            m_bGM.volume = m_bgmVolumScale;
            PlayerPrefs.SetFloat(EF.Projects.AppConst.AppPrefix + "bgmVolum", m_bgmVolumScale);
        }

        /// <summary>
        /// Set the effect volume. value by 0 to 1.
        /// <para>设置效果音量。取值为0 ~ 1</para>
        /// </summary>
        public void SetEffectVolum(float volume)
        {
            m_effectVolume = volume;
            PlayerPrefs.SetFloat(EF.Projects.AppConst.AppPrefix + "effectVolum", m_effectVolume);
        }

        /// <summary>
        /// Get the bgm volume.
        /// <para>获取背景音量大小</para>
        /// </summary>
        public float GetBgmVolume()
        {
            return m_bgmVolumScale;
        }

        /// <summary>
        /// Get the effect volume.
        /// <para>获取音效音量大小</para>
        /// </summary>
        /// <returns></returns>
        public float GetEffectVolume()
        {
            return m_effectVolume;
        }
        #endregion

        #region PRIVATE FUNCTION
        private void PlayBGM(AudioClip clip, bool isLoop)
        {
            m_bGM.loop = isLoop;
            m_bGM.clip = clip;
            m_bGM.mute = isMute;
            m_bGM.Play();
        }

        private void PlayEffect(AudioClip clip, bool loop = false, Vector3 pos = default)
        {
            if (clip == null) return;

            AudioSource audioSrc = GetOneAudioSourceToUse();
            m_audioInUse.Add(audioSrc);
            audioSrc.transform.localPosition = pos;
            audioSrc.loop = loop;
            audioSrc.clip = clip;
            audioSrc.volume = m_effectVolume;
            audioSrc.mute = isMute;
            audioSrc.Play();
        }

        private void StopEffectExCallbcak(int index)
        {
            m_audioCallback[index]?.Invoke();
            m_audioInUse[index].gameObject.SetActive(false);
            m_audioCanUse.Enqueue(m_audioInUse[index]);
            m_audioInUse.RemoveAt(index);
            m_audioCallback.RemoveAt(index);
        }

        private AudioSource GetOneAudioSourceToUse()
        {
            AudioSource _audioSource;
            if (m_audioCanUse.Count != 0)
                _audioSource = m_audioCanUse.Dequeue();
            else
                _audioSource = CreateAudioSource();
            _audioSource.gameObject.SetActive(true);
            return _audioSource;
        }

        private AudioSource CreateAudioSource()
        {
            GameObject _obj = new GameObject("AudioSourceItem" + m_Traget.childCount);
            _obj.transform.SetParent(m_Traget);
            AudioSource _audio = _obj.AddComponent<AudioSource>();
            _audio.playOnAwake = true;
            _audio.loop = false;
            return _audio;
        }

        private AudioClip GetClipByName(string name)
        {
            return EF.Load.LoadInResources<AudioClip>($"{EF.Projects.AppConst.AudioPath}{name}");
        }
        #endregion
    }
}
