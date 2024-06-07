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
        int m_SortName = 1,
            m_SortBond = 1,
            m_SortScore = 1,
            m_SortVertex = 1,
            m_SortTriangle = 1;

        SettingView<ModelSetting> m_RuleView;
        List<ModelInformation> m_ShowModelInfo;

        internal override void Initialize()
        {
            m_ShowModelInfo = new List<ModelInformation>();
            m_RuleView = new SettingView<ModelSetting>();

            ObjectInfoList = new string[]
            {
                LC.Combine(LC.Language.Model, LC.Language.Name),
                LC.Language.ResourceType,
                LC.Language.Vertex,
                LC.Language.Triangular,
                LC.Language.BoneCount,
                LC.Combine(LC.Language.Model, LC.Language.Size),
                LC.Language.Score
            };
            base.Initialize();
        }

        internal override void OnDestroy()
        {
            base.OnDestroy();

            m_ShowModelInfo.Clear();
            m_ShowModelInfo = null;
            m_RuleView = null;
        }

        protected override void OnClickInfoList(int index)
        {
            if (index == 0)
            {
                m_SortName *= -1;
                m_ShowModelInfo.Sort((x, y) => x.Name.CompareTo(y.Name) * m_SortName);
            }
            else if (index == 2)
            {
                m_SortVertex *= -1;
                m_ShowModelInfo.Sort((x, y) => x.VertexCount.CompareTo(y.VertexCount) * m_SortVertex);
            }
            else if (index == 3)
            {
                m_SortTriangle *= -1;
                m_ShowModelInfo.Sort((x, y) => x.TriangleCount.CompareTo(y.TriangleCount) * m_SortTriangle);
            }
            else if (index == 4)
            {
                m_SortBond *= -1;
                m_ShowModelInfo.Sort((x, y) => x.BondCount.CompareTo(y.BondCount) * m_SortBond);
            }
            else if (index == 6)
            {
                m_SortScore *= -1;
                m_ShowModelInfo.Sort((x, y) => x.Score.CompareTo(y.Score) * m_SortScore);
            }
        }

        protected override void FiltrateChanged(int index, bool fflush)
        {
            #region FindAllModels                
            List<string> _filtrates = new List<string>
            {
                "ALL"
            };
            List<string> _filePaths = new List<string>();
            Dictionary<ModelSetting, string[]> _fileMaps = new Dictionary<ModelSetting, string[]>();

            int _count = 0;
            if (m_RuleView.Settings != null)
            {
                for (int i = 0; i < m_RuleView.Settings.Count; i++)
                {
                    _filePaths.Clear();
                    for (int j = 0; j < m_RuleView.Settings[i].Folder.Count; j++)
                    {
                        string rootFolder = m_RuleView.Settings[i].Folder[j];
                        if (string.IsNullOrEmpty(rootFolder)) continue;
                        string[] fileArr = Directory.GetFiles(rootFolder, "*.FBX", SearchOption.AllDirectories);
                        _filePaths.AddRange(fileArr);
                        _count += fileArr.Length;
                    }
                    _fileMaps[m_RuleView.Settings[i]] = _filePaths.ToArray();
                    _filtrates.Add(m_RuleView.Settings[i].AssetDesc);
                }
            }
            Filtrates = _filtrates.ToArray();

            int _curFileIndex = 0;
            m_ShowModelInfo.Clear();
            foreach (ModelSetting msb in _fileMaps.Keys)
            {
                string[] childFiles = _fileMaps[msb];
                for (int i = 0; i < childFiles.Length; i++)
                {
                    _curFileIndex++;
                    EditorUtility.DisplayProgressBar(LC.Language.Holdon, LC.Language.BeingProcessed, _curFileIndex / _count);
                    ModelInformation mb = ParseModel(childFiles[i]);
                    mb.AssetDesc = msb.AssetDesc;

                    mb.TriangleScore = mb.TriangleCount / (float)msb.MaxTriangs;
                    mb.BondScore = mb.BondCount / (float)msb.MaxBones;
                    mb.Score = (mb.TriangleScore + mb.BondScore) * 0.5f;

                    m_ShowModelInfo.Add(mb);
                }
            }
            ListCount = _curFileIndex;
            EditorUtility.ClearProgressBar();
            #endregion

            if (index == 0)
                return;

            for (int i = m_ShowModelInfo.Count - 1; i >= 0; i--)
            {
                if (!Filtrates[index].Equals(m_ShowModelInfo[i].AssetDesc))
                    m_ShowModelInfo.RemoveAt(i);
            }
            ListCount = m_ShowModelInfo.Count;
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
            ModelInformation _model = m_ShowModelInfo[index];
            if (GUILayout.Button(_model.Name, AssetsCheckerConfig.ButtonStyle, GUILayout.Width(140f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(_model.FilePath);
            }

            float _width = (Screen.width - 150f) / (ObjectInfoList.Length - 1);

            GUILayout.Label(_model.AssetDesc, AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            SetGUIColor(_model.VetexScore);
            GUILayout.Label(_model.VertexCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;

            SetGUIColor(_model.TriangleScore);
            GUILayout.Label(_model.TriangleCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;

            SetGUIColor(_model.BondScore);
            GUILayout.Label(_model.BondCount.ToString(), AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));
            GUI.color = Color.white;

            GUILayout.Label($"{_model.TextureSize.x} x {_model.TextureSize.y}", AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            int _lv = AssetsCheckerConfig.CalScoreLevel(_model.Score);
            GUI.color = AssetsCheckerConfig.ScoreColors[_lv];
            GUILayout.Label(AssetsCheckerConfig.ScoreNames[_lv], AssetsCheckerConfig.LabelStyle, GUILayout.Width(_width));

            GUI.color = Color.white;
        }

        /// <summary>
        /// 解析模型数量
        /// </summary>
        private ModelInformation ParseModel(string filePath)
        {
            ModelInformation _modelInfo = new ModelInformation
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath
            };

            Object _resObj = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            GameObject _go = Object.Instantiate(_resObj) as GameObject;
            SkinnedMeshRenderer _skin = _go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (_skin != null)
                ReadSkinMeshRender(_skin, _modelInfo);
            else
            {
                MeshRenderer _mesh = _go.GetComponentInChildren<MeshRenderer>();
                if (_mesh != null) ReadMeshRender(_mesh, _modelInfo);
            }

            Object.DestroyImmediate(_go);
            return _modelInfo;
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
            MeshFilter _meshFilter = modelMesh.GetComponent<MeshFilter>();
            if (_meshFilter.sharedMesh != null)
            {
                model.VertexCount = _meshFilter.sharedMesh.vertexCount;
                model.TriangleCount = _meshFilter.sharedMesh.triangles.Length / 3;
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
