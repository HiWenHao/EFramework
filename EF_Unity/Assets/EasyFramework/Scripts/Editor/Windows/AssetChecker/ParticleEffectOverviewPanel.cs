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
        int _sortName = 1;
        int _sortScore = 1;
        int _sortTexture = 1;
        int _sortDrawCall = 1;
        int _sortParticles = 1;

        SettingView<ParticleEffectSetting> _ruleView;
        List<EffectInformation> _showEffectInfo;

        internal override void Initialize()
        {
            _showEffectInfo = new List<EffectInformation>();
            _ruleView = new SettingView<ParticleEffectSetting>();

            ObjectInfoList = new string[]
            {
                LC.Combine(new Lc[] { Lc.Effects, Lc.Name} ),
                LC.Combine(new Lc[] { Lc.Resource, Lc.Type} ),
                "DrawCall",
                LC.Combine(new Lc[] { Lc.Texture, Lc.Count} ),
                LC.Combine(new Lc[] { Lc.Particle, Lc.Count} ),
                LC.Combine(Lc.Score),
            };
            base.Initialize();
        }

        internal override void OnDestroy()
        {
            base.OnDestroy();

            _showEffectInfo.Clear();
            _showEffectInfo = null;
            _ruleView = null;
        }

        protected override void OnClickInfoList(int index)
        {
            if (index == 0)
            {
                _sortName *= -1;
                _showEffectInfo.Sort((x, y) => x.Name.CompareTo(y.Name) * _sortName);
            }
            else if (index == 2)
            {
                _sortDrawCall *= -1;
                _showEffectInfo.Sort((x, y) => x.DrawCallCount.CompareTo(y.DrawCallCount) * _sortDrawCall);
            }
            else if (index == 3)
            {
                _sortTexture *= -1;
                _showEffectInfo.Sort((x, y) => x.TextureCount.CompareTo(y.TextureCount) * _sortTexture);
            }
            else if (index == 4)
            {
                _sortParticles *= -1;
                _showEffectInfo.Sort((x, y) => x.ParticelCount.CompareTo(y.ParticelCount) * _sortParticles);
            }
            else if (index == 5)
            {
                _sortScore *= -1;
                _showEffectInfo.Sort((x, y) => x.Score.CompareTo(y.Score) * _sortScore);
            }
        }

        protected override void FiltrateChanged(int index, bool fflush)
        {
            #region FindAllEffect                
            List<string> filtrates = new List<string>
            {
                "ALL"
            };

            List<string> filePaths = new List<string>();
            Dictionary<ParticleEffectSetting, string[]> fileMaps = new Dictionary<ParticleEffectSetting, string[]>();

            float _count = 0;
            if (_ruleView.Settings != null)
            {
                for (int i = 0; i < _ruleView.Settings.Count; i++)
                {
                    filePaths.Clear();
                    for (int j = 0; j < _ruleView.Settings[i].Folder.Count; j++)
                    {
                        string rootFolder = _ruleView.Settings[i].Folder[j];
                        if (string.IsNullOrEmpty(rootFolder)) continue;
                        string[] fileArr = Directory.GetFiles(rootFolder, "*.prefab", SearchOption.AllDirectories);
                        filePaths.AddRange(fileArr);
                        _count += fileArr.Length;
                    }
                    fileMaps[_ruleView.Settings[i]] = filePaths.ToArray();
                    filtrates.Add(_ruleView.Settings[i].AssetDesc);
                }
            }

            Filtrates = filtrates.ToArray();

            int curFileIndex = 0;
            _showEffectInfo.Clear();
            foreach (ParticleEffectSetting msb in fileMaps.Keys)
            {
                string[] childFiles = fileMaps[msb];
                for (int i = 0; i < childFiles.Length; i++)
                {
                    curFileIndex++;
                    EditorUtility.DisplayProgressBar(LC.Combine(Lc.Holdon), LC.Combine(Lc.BeingProcessed), curFileIndex / _count);
                    EffectInformation _effect = ParseEffectAsset(childFiles[i]);
                    _effect.AssetDesc = msb.AssetDesc;

                    _effect.DrawCallScore = _effect.DrawCallCount / (float)msb.MaxMatrials;
                    _effect.ParticeScore = _effect.ParticelCount / (float)msb.MaxParticels;
                    _effect.Score = (_effect.DrawCallScore + _effect.ParticeScore) * 0.5f;

                    _showEffectInfo.Add(_effect);
                }
            }

            ListCount = curFileIndex;
            EditorUtility.ClearProgressBar();
            #endregion

            if (index == 0)
                return;

            for (int i = _showEffectInfo.Count - 1; i >= 0; i--)
            {
                if (!Filtrates[index].Equals(_showEffectInfo[i].AssetDesc))
                    _showEffectInfo.RemoveAt(i);
            }
            ListCount = _showEffectInfo.Count;
        }

        protected override void Refresh()
        {
            FiltrateChanged(FiltrateIndex, true);
        }

        protected override void RuleViewOnGUI()
        {
            _ruleView.OnGUI();
        }

        protected override void DrawOne(int index)
        {
            EffectInformation effect = _showEffectInfo[index];

            if (GUILayout.Button(effect.Name, AssetsCheckerConfig.ButtonStyle, GUILayout.MaxWidth(140f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(effect.FilePath);
            }

            float width = (Screen.width - 150f) / (ObjectInfoList.Length - 1);

            GUILayout.Label(effect.AssetDesc, AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));

            SetGUIColor(effect.DrawCallScore);
            GUILayout.Label(effect.DrawCallCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));
            GUI.color = Color.white;

            GUILayout.Label(effect.TextureCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));
            GUI.color = Color.white;

            SetGUIColor(effect.ParticeScore);
            GUILayout.Label(effect.ParticelCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));

            int lv = AssetsCheckerConfig.CalScoreLevel(effect.Score);
            GUI.color = AssetsCheckerConfig.ScoreColors[lv];
            GUILayout.Label(AssetsCheckerConfig.ScoreNames[lv], AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));
            GUI.color = Color.white;
        }

        /// <summary>
        /// 分析粒子特效
        /// </summary>
        private EffectInformation ParseEffectAsset(string filePath)
        {
            EffectInformation effect = new EffectInformation
            {
                Name = Path.GetFileName(filePath),
                FilePath = filePath
            };

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            GameObject go = Object.Instantiate(obj) as GameObject;

            ParticleSystem[] particleArr = go.GetComponentsInChildren<ParticleSystem>();

            Dictionary<string, bool> materialsDic = new Dictionary<string, bool>();
            Dictionary<string, bool> texturesDic = new Dictionary<string, bool>();
            int particels = 0;
            for (int i = 0; i < particleArr.Length; i++)
            {
                Renderer renderer = particleArr[i].GetComponent<Renderer>();
                Material mat = renderer.sharedMaterial;
                if (mat != null)
                {
                    materialsDic[mat.name] = true;
                    if (mat.mainTexture != null)
                        texturesDic[mat.mainTexture.name] = true;
                }
                particels += particleArr[i].main.maxParticles;
            }
            effect.DrawCallCount = materialsDic.Count;
            effect.TextureCount = texturesDic.Count;
            effect.ParticelCount = particels;
            Object.DestroyImmediate(go);
            return effect;
        }
    }
}
