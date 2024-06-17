/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:40
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:40
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 粒子信息面板
    /// </summary>
    internal class ParticleEffectOverviewPanel : OverviewPanelBase
    {
        int m_SortName = 1,
            m_SortScore = 1,
            m_SortTexture = 1,
            m_SortDrawCall = 1,
            m_SortParticles = 1;

        SettingView<ParticleEffectSetting> m_RuleView;
        List<EffectInformation> m_ShowEffectInfo;

        internal override void Initialize()
        {
            m_ShowEffectInfo = new List<EffectInformation>();
            m_RuleView = new SettingView<ParticleEffectSetting>();

            ObjectInfoList = new string[]
            {
                LC.Combine("Effects", "Name"),
                LC.Combine("Resource", "Type"),
                "DrawCall",
                LC.Combine("Texture", "Count"),
                LC.Combine("Particle", "Count"),
                LC.Combine("Score"),
            };
            base.Initialize();
        }

        internal override void OnDestroy()
        {
            base.OnDestroy();

            m_ShowEffectInfo.Clear();
            m_ShowEffectInfo = null;
            m_RuleView = null;
        }

        protected override void OnClickInfoList(int index)
        {
            if (index == 0)
            {
                m_SortName *= -1;
                m_ShowEffectInfo.Sort((x, y) => x.Name.CompareTo(y.Name) * m_SortName);
            }
            else if (index == 2)
            {
                m_SortDrawCall *= -1;
                m_ShowEffectInfo.Sort((x, y) => x.DrawCallCount.CompareTo(y.DrawCallCount) * m_SortDrawCall);
            }
            else if (index == 3)
            {
                m_SortTexture *= -1;
                m_ShowEffectInfo.Sort((x, y) => x.TextureCount.CompareTo(y.TextureCount) * m_SortTexture);
            }
            else if (index == 4)
            {
                m_SortParticles *= -1;
                m_ShowEffectInfo.Sort((x, y) => x.ParticelCount.CompareTo(y.ParticelCount) * m_SortParticles);
            }
            else if (index == 5)
            {
                m_SortScore *= -1;
                m_ShowEffectInfo.Sort((x, y) => x.Score.CompareTo(y.Score) * m_SortScore);
            }
        }

        protected override void FiltrateChanged(int index, bool fflush)
        {
            #region FindAllEffect                
            List<string> _filtrates = new List<string>
            {
                "ALL"
            };

            List<string> _filePaths = new List<string>();
            Dictionary<ParticleEffectSetting, string[]> fileMaps = new Dictionary<ParticleEffectSetting, string[]>();

            float _count = 0;
            if (m_RuleView.Settings != null)
            {
                for (int i = 0; i < m_RuleView.Settings.Count; i++)
                {
                    _filePaths.Clear();
                    for (int j = 0; j < m_RuleView.Settings[i].Folder.Count; j++)
                    {
                        string rootFolder = m_RuleView.Settings[i].Folder[j];
                        if (string.IsNullOrEmpty(rootFolder)) continue;
                        string[] fileArr = Directory.GetFiles(rootFolder, "*.prefab", SearchOption.AllDirectories);
                        _filePaths.AddRange(fileArr);
                        _count += fileArr.Length;
                    }
                    fileMaps[m_RuleView.Settings[i]] = _filePaths.ToArray();
                    _filtrates.Add(m_RuleView.Settings[i].AssetDesc);
                }
            }

            Filtrates = _filtrates.ToArray();

            int _curFileIndex = 0;
            m_ShowEffectInfo.Clear();
            foreach (ParticleEffectSetting msb in fileMaps.Keys)
            {
                string[] childFiles = fileMaps[msb];
                for (int i = 0; i < childFiles.Length; i++)
                {
                    _curFileIndex++;
                    EditorUtility.DisplayProgressBar(LC.Combine("Holdon"), LC.Combine("BeingProcessed"), _curFileIndex / _count);
                    EffectInformation _effect = ParseEffectAsset(childFiles[i]);
                    _effect.AssetDesc = msb.AssetDesc;

                    _effect.DrawCallScore = _effect.DrawCallCount / (float)msb.MaxMatrials;
                    _effect.ParticeScore = _effect.ParticelCount / (float)msb.MaxParticels;
                    _effect.Score = (_effect.DrawCallScore + _effect.ParticeScore) * 0.5f;

                    m_ShowEffectInfo.Add(_effect);
                }
            }

            ListCount = _curFileIndex;
            EditorUtility.ClearProgressBar();
            #endregion

            if (index == 0)
                return;

            for (int i = m_ShowEffectInfo.Count - 1; i >= 0; i--)
            {
                if (!Filtrates[index].Equals(m_ShowEffectInfo[i].AssetDesc))
                    m_ShowEffectInfo.RemoveAt(i);
            }
            ListCount = m_ShowEffectInfo.Count;
        }

        protected override void Refresh()
        {
            FiltrateChanged(FiltrateIndex, true);
        }

        protected override void RuleViewOnGUI()
        {
            m_RuleView.OnGUI();
        }

        protected override void DrawOne(int index)
        {
            EffectInformation _effect = m_ShowEffectInfo[index];

            if (GUILayout.Button(_effect.Name, AssetsCheckerConfig.ButtonStyle, GUILayout.MaxWidth(140f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_effect.FilePath);
            }

            float _width = (Screen.width - 150f) / (ObjectInfoList.Length - 1);

            GUILayout.Label(_effect.AssetDesc, AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            SetGUIColor(_effect.DrawCallScore);
            GUILayout.Label(_effect.DrawCallCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;

            GUILayout.Label(_effect.TextureCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;

            SetGUIColor(_effect.ParticeScore);
            GUILayout.Label(_effect.ParticelCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            int lv = AssetsCheckerConfig.CalScoreLevel(_effect.Score);
            GUI.color = AssetsCheckerConfig.ScoreColors[lv];
            GUILayout.Label(AssetsCheckerConfig.ScoreNames[lv], AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;
        }

        /// <summary>
        /// 分析粒子特效
        /// </summary>
        private EffectInformation ParseEffectAsset(string filePath)
        {
            EffectInformation _effect = new EffectInformation
            {
                Name = Path.GetFileName(filePath),
                FilePath = filePath
            };

            Object _obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            GameObject _go = Object.Instantiate(_obj) as GameObject;

            ParticleSystem[] _particleArr = _go.GetComponentsInChildren<ParticleSystem>();

            Dictionary<string, bool> _materialsDic = new Dictionary<string, bool>();
            Dictionary<string, bool> _texturesDic = new Dictionary<string, bool>();
            int _particels = 0;
            for (int i = 0; i < _particleArr.Length; i++)
            {
                Renderer _renderer = _particleArr[i].GetComponent<Renderer>();
                Material _mat = _renderer.sharedMaterial;
                if (_mat != null)
                {
                    _materialsDic[_mat.name] = true;
                    if (_mat.mainTexture != null)
                        _texturesDic[_mat.mainTexture.name] = true;
                }
                _particels += _particleArr[i].main.maxParticles;
            }
            _effect.DrawCallCount = _materialsDic.Count;
            _effect.TextureCount = _texturesDic.Count;
            _effect.ParticelCount = _particels;
            Object.DestroyImmediate(_go);
            return _effect;
        }
    }
}
