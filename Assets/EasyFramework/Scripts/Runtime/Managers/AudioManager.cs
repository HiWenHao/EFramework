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
        int m_managerLevel = -99;
        int IManager.ManagerLevel { get
            {
                if (m_managerLevel < -1)
                    m_managerLevel = EF.Projects.AppConst.ManagerLevels.IndexOf(Name);
                return m_managerLevel;
            }
        }
        private bool isPausing;
        private bool isMute;
        private AudioSource m_as_BGM;
        private float m_flt_BgmVolumScale = 1f;
        private float m_flt_EffectVolume = 1.0f;

        Transform m_Traget;

        private List<float> m_flt_AudioTimer;
        private List<Action> m_act_AudioCallback;
        private List<AudioSource> m_lst_AudioSources;
        private Queue<AudioSource> m_que_AudioSources;
        void ISingleton.Init()
        {
            m_Traget = new GameObject("Source").transform;
            m_Traget.SetParent(EF.Managers);
            m_as_BGM = m_Traget.gameObject.AddComponent<AudioSource>();

            m_flt_AudioTimer = new List<float>();
            m_act_AudioCallback = new List<Action>();
            m_lst_AudioSources = new List<AudioSource>();
            m_que_AudioSources = new Queue<AudioSource>();

            if (PlayerPrefs.HasKey(EF.Projects.AppConst.AppPrefix + "bgmVolum"))
                m_flt_BgmVolumScale = PlayerPrefs.GetFloat(EF.Projects.AppConst.AppPrefix + "bgmVolum");
            if (PlayerPrefs.HasKey(EF.Projects.AppConst.AppPrefix + "effectVolum"))
                m_flt_EffectVolume = PlayerPrefs.GetFloat(EF.Projects.AppConst.AppPrefix + "effectVolum");

            m_as_BGM.playOnAwake = true;
            m_as_BGM.loop = true;
            m_as_BGM.volume = m_flt_BgmVolumScale;
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            if (isPausing) 
                return;
            for (int i = 0; i < m_flt_AudioTimer.Count; i++)
            {
                if ((m_flt_AudioTimer[i] - m_lst_AudioSources[i].time) >= 0.06f) continue;
                if (!m_lst_AudioSources[i].isPlaying || m_lst_AudioSources[i].loop) continue;

                StopEffectExCallbcak(i);
            }
        }

        void ISingleton.Quit()
        {
            isPausing = true;
            m_as_BGM.clip = null;
            for (int i = m_flt_AudioTimer.Count - 1; i >= 0; i--)
            {
                AudioSource _audio = m_lst_AudioSources[i];
                _audio.clip = null;
                Object.Destroy(_audio);
                Object.Destroy(_audio.gameObject);
            }
            for (int i = m_que_AudioSources.Count - 1; i >= 0; i--)
            {
                AudioSource _audio = m_que_AudioSources.Dequeue();
                _audio.clip = null;
                Object.Destroy(_audio);
                Object.Destroy(_audio.gameObject);
            }

            m_flt_AudioTimer.Clear();
            m_act_AudioCallback.Clear();
            m_lst_AudioSources.Clear();
            m_que_AudioSources.Clear();

            m_flt_AudioTimer = null;
            m_act_AudioCallback = null;
            m_lst_AudioSources = null;
            m_que_AudioSources = null;
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
            m_act_AudioCallback.Add(callback);
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
            m_act_AudioCallback.Add(callback);
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
            m_act_AudioCallback.Add(callback);
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
            m_act_AudioCallback.Add(callback);
        }
        #endregion

        #region Set
        /// <summary>
        /// Pause the all audio. 
        /// <para>暂停全部声音</para>
        /// </summary>
        public void PauseAll()
        {
            m_as_BGM.Pause();
            foreach (var item in m_lst_AudioSources)
                item.Pause();
            isPausing = true;
        }

        /// <summary>
        /// UnPause the all audio.
        /// <para>解除暂停</para>
        /// </summary>
        public void UnPauseAll()
        {
            m_as_BGM.UnPause();
            foreach (var item in m_lst_AudioSources)
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
            m_as_BGM.mute = isSetMute;
            for (int i = 0, imax = m_lst_AudioSources.Count; i < imax; i++)
            {
                m_lst_AudioSources[i].mute = isSetMute;
            }
        }

        /// <summary>
        /// Stop BGM.
        /// <para>停止背景音乐</para>
        /// </summary>
        public void StopBGM()
        {
            m_as_BGM.Stop();
        }

        /// <summary>
        /// Stop a effect audio source by name.
        /// <para>通过名字停止播放</para>
        /// </summary>
        public void StopEffectSourceByName(string effectName)
        {
            for (int i = m_lst_AudioSources.Count - 1; i >= 0; i--)
            {
                if (m_lst_AudioSources[i].isPlaying && m_lst_AudioSources[i].clip.name == effectName)
                {
                    m_lst_AudioSources[i].Stop();
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
            for (int i = m_lst_AudioSources.Count - 1; i >= 0; i--)
            {
                if (m_lst_AudioSources[i].isPlaying && m_lst_AudioSources[i].clip == clip)
                {
                    m_lst_AudioSources[i].Stop();
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
            for (int i = m_lst_AudioSources.Count - 1; i >= 0; i--)
            {
                m_lst_AudioSources[i].Stop();
                StopEffectExCallbcak(i);
            }
        }

        /// <summary>
        /// Set all audio source stop.
        /// <para>全部停止播放</para>
        /// </summary>
        public void StopAll()
        {
            m_as_BGM.Stop();
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
            if (m_as_BGM.isPlaying)
            {
                return m_as_BGM.clip.name == bgmName;
            }

            return false;
        }

        /// <summary>
        /// Determine whether the current effect source is playing. 
        /// <para>判断当前特效音频是否在播放中</para>
        /// </summary>
        public bool IsPlayingEffectSouce(string effectSourceName)
        {
            for (int i = 0; i < m_lst_AudioSources.Count; i++)
            {
                var audio = m_lst_AudioSources[i];
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
            m_flt_BgmVolumScale = volume;
            m_as_BGM.volume = m_flt_BgmVolumScale;
            PlayerPrefs.SetFloat(EF.Projects.AppConst.AppPrefix + "bgmVolum", m_flt_BgmVolumScale);
        }

        /// <summary>
        /// Set the effect volume. value by 0 to 1.
        /// <para>设置效果音量。取值为0 ~ 1</para>
        /// </summary>
        public void SetEffectVolum(float volume)
        {
            m_flt_EffectVolume = volume;
            PlayerPrefs.SetFloat(EF.Projects.AppConst.AppPrefix + "effectVolum", m_flt_EffectVolume);
        }

        /// <summary>
        /// Get the bgm volume.
        /// <para>获取背景音量大小</para>
        /// </summary>
        public float GetBgmVolume()
        {
            return m_flt_BgmVolumScale;
        }

        /// <summary>
        /// Get the effect volume.
        /// <para>获取音效音量大小</para>
        /// </summary>
        /// <returns></returns>
        public float GetEffectVolume()
        {
            return m_flt_EffectVolume;
        }
        #endregion

        #region PRIVATE FUNCTION
        private void PlayBGM(AudioClip clip, bool isLoop)
        {
            m_as_BGM.loop = isLoop;
            m_as_BGM.clip = clip;
            m_as_BGM.mute = isMute;
            m_as_BGM.Play();
        }

        private void PlayEffect(AudioClip clip, bool loop = false, Vector3 pos = default)
        {
            if (clip == null) return;

            AudioSource audioSrc = GetOneAudioSourceToUse();
            m_lst_AudioSources.Add(audioSrc);
            m_flt_AudioTimer.Add(clip.length);
            audioSrc.transform.localPosition = pos;
            audioSrc.loop = loop;
            audioSrc.clip = clip;
            audioSrc.volume = m_flt_EffectVolume;
            audioSrc.mute = isMute;
            audioSrc.Play();
        }

        private void StopEffectExCallbcak(int index)
        {
            m_act_AudioCallback[index]?.Invoke();
            m_lst_AudioSources[index].gameObject.SetActive(false);
            m_que_AudioSources.Enqueue(m_lst_AudioSources[index]);
            m_flt_AudioTimer.RemoveAt(index);
            m_lst_AudioSources.RemoveAt(index);
            m_act_AudioCallback.RemoveAt(index);
        }

        private AudioSource GetOneAudioSourceToUse()
        {
            AudioSource _audioSource;
            if (m_que_AudioSources.Count != 0)
                _audioSource = m_que_AudioSources.Dequeue();
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
