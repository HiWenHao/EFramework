/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:34
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:34
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
    /// 模型信息面板
    /// </summary>
    internal class ModelOverviewPanel : OverviewPanelBase
    {
        int _sortName = 1;
        int _sortBond = 1;
        int _sortScore = 1;
        int _sortVertex = 1;
        int _sortTriangle = 1;

        SettingView<ModelSetting> _ruleView;
        List<ModelInformation> _showModelInfo;

        internal override void Initialize()
        {
            _showModelInfo = new List<ModelInformation>();
            _ruleView = new SettingView<ModelSetting>();

            ObjectInfoList = new string[]
            {
                LC.Combine(new Lc[] { Lc.Model, Lc.Name }),
                LC.Combine(new Lc[] { Lc.Resource, Lc.Type }),
                LC.Combine(new Lc[] { Lc.Vertex, Lc.Count }),
                LC.Combine(Lc.Triangular),
                LC.Combine(new Lc[] { Lc.Bone, Lc.Count }),
                LC.Combine(new Lc[] { Lc.Model, Lc.Size }),
                LC.Combine(Lc.Score)
            };
            base.Initialize();
        }

        internal override void OnDestroy()
        {
            base.OnDestroy();

            _showModelInfo.Clear();
            _showModelInfo = null;
            _ruleView = null;
        }

        protected override void OnClickInfoList(int index)
        {
            if (index == 0)
            {
                _sortName *= -1;
                _showModelInfo.Sort((x, y) => x.Name.CompareTo(y.Name) * _sortName);
            }
            else if (index == 2)
            {
                _sortVertex *= -1;
                _showModelInfo.Sort((x, y) => x.VertexCount.CompareTo(y.VertexCount) * _sortVertex);
            }
            else if (index == 3)
            {
                _sortTriangle *= -1;
                _showModelInfo.Sort((x, y) => x.TriangleCount.CompareTo(y.TriangleCount) * _sortTriangle);
            }
            else if (index == 4)
            {
                _sortBond *= -1;
                _showModelInfo.Sort((x, y) => x.BondCount.CompareTo(y.BondCount) * _sortBond);
            }
            else if (index == 6)
            {
                _sortScore *= -1;
                _showModelInfo.Sort((x, y) => x.Score.CompareTo(y.Score) * _sortScore);
            }
        }

        protected override void FiltrateChanged(int index, bool fflush)
        {
            #region FindAllModels                
            List<string> filtrates = new List<string>
            {
                "ALL"
            };
            List<string> filePaths = new List<string>();
            Dictionary<ModelSetting, string[]> _fileMaps = new Dictionary<ModelSetting, string[]>();

            int _count = 0;
            if (_ruleView.Settings != null)
            {
                for (int i = 0; i < _ruleView.Settings.Count; i++)
                {
                    filePaths.Clear();
                    for (int j = 0; j < _ruleView.Settings[i].Folder.Count; j++)
                    {
                        string rootFolder = _ruleView.Settings[i].Folder[j];
                        if (string.IsNullOrEmpty(rootFolder)) continue;
                        string[] fileArr = Directory.GetFiles(rootFolder, "*.FBX", SearchOption.AllDirectories);
                        filePaths.AddRange(fileArr);
                        _count += fileArr.Length;
                    }
                    _fileMaps[_ruleView.Settings[i]] = filePaths.ToArray();
                    filtrates.Add(_ruleView.Settings[i].AssetDesc);
                }
            }
            Filtrates = filtrates.ToArray();

            int curFileIndex = 0;
            _showModelInfo.Clear();
            foreach (ModelSetting msb in _fileMaps.Keys)
            {
                string[] childFiles = _fileMaps[msb];
                for (int i = 0; i < childFiles.Length; i++)
                {
                    curFileIndex++;
                    EditorUtility.DisplayProgressBar(LC.Combine(Lc.Holdon), LC.Combine(Lc.BeingProcessed), curFileIndex / _count);
                    ModelInformation mb = ParseModel(childFiles[i]);
                    mb.AssetDesc = msb.AssetDesc;

                    mb.TriangleScore = mb.TriangleCount / (float)msb.MaxTriangs;
                    mb.BondScore = mb.BondCount / (float)msb.MaxBones;
                    mb.Score = (mb.TriangleScore + mb.BondScore) * 0.5f;

                    _showModelInfo.Add(mb);
                }
            }
            ListCount = curFileIndex;
            EditorUtility.ClearProgressBar();
            #endregion

            if (index == 0)
                return;

            for (int i = _showModelInfo.Count - 1; i >= 0; i--)
            {
                if (!Filtrates[index].Equals(_showModelInfo[i].AssetDesc))
                    _showModelInfo.RemoveAt(i);
            }
            ListCount = _showModelInfo.Count;
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
            ModelInformation model = _showModelInfo[index];
            if (GUILayout.Button(model.Name, AssetsCheckerConfig.ButtonStyle, GUILayout.Width(140f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(model.FilePath);
            }

            float width = (Screen.width - 150f) / (ObjectInfoList.Length - 1);

            GUILayout.Label(model.AssetDesc, AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));

            SetGUIColor(model.VetexScore);
            GUILayout.Label(model.VertexCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));
            GUI.color = Color.white;

            SetGUIColor(model.TriangleScore);
            GUILayout.Label(model.TriangleCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));
            GUI.color = Color.white;

            SetGUIColor(model.BondScore);
            GUILayout.Label(model.BondCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));
            GUI.color = Color.white;

            GUILayout.Label($"{model.TextureSize.x} x {model.TextureSize.y}", AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));

            int lv = AssetsCheckerConfig.CalScoreLevel(model.Score);
            GUI.color = AssetsCheckerConfig.ScoreColors[lv];
            GUILayout.Label(AssetsCheckerConfig.ScoreNames[lv], AssetsCheckerConfig.LabelStyle, GUILayout.Width(width));

            GUI.color = Color.white;
        }

        /// <summary>
        /// 解析模型数量
        /// </summary>
        private ModelInformation ParseModel(string filePath)
        {
            ModelInformation modelInfo = new ModelInformation
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
            };

            Object resObj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            GameObject go = Object.Instantiate(resObj) as GameObject;
            SkinnedMeshRenderer skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin != null)
                ReadSkinMeshRender(skin, modelInfo);
            else
            {
                MeshRenderer _mesh = go.GetComponentInChildren<MeshRenderer>();
                if (_mesh != null) ReadMeshRender(_mesh, modelInfo);
            }

            Object.DestroyImmediate(go);
            return modelInfo;
        }

        /// <summary>
        /// 读取蒙皮信息
        /// </summary>
        private void ReadSkinMeshRender(SkinnedMeshRenderer skin, ModelInformation model)
        {
            if (skin.sharedMesh != null)
            {
                model.VertexCount = skin.sharedMesh.vertexCount;
                model.TriangleCount = skin.sharedMesh.triangles.Length / 3;
            }

            if (skin.bones != null)
            {
                model.BondCount = skin.bones.Length;
            }

            if (skin.sharedMaterial && skin.sharedMaterial.mainTexture)
            {
                Texture _tex = skin.sharedMaterial.mainTexture;
                model.TextureName = _tex.name;
                model.TextureSize = new Vector2(_tex.width, _tex.height);
            }
        }

        /// <summary>
        /// 读取网格信息
        /// </summary>
        private void ReadMeshRender(MeshRenderer modelMesh, ModelInformation model)
        {
            MeshFilter meshFilter = modelMesh.GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh != null)
            {
                model.VertexCount = meshFilter.sharedMesh.vertexCount;
                model.TriangleCount = meshFilter.sharedMesh.triangles.Length / 3;
            }

            if (modelMesh.sharedMaterial && modelMesh.sharedMaterial.mainTexture)
            {
                Texture mainTex = modelMesh.sharedMaterial.mainTexture;
                model.TextureName = mainTex.name;
                model.TextureSize = new Vector2(mainTex.width, mainTex.height);
            }
        }
    }
}
