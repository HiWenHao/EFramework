/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:29:13
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:29:13
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Xml;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 检查器配置
    /// </summary>
    internal static class AssetsCheckerConfig
    {
        internal static Color[] ScoreColors { get; set; } = new[]
        {
            Color.green,
            new Color(1.0f, .85f, 0.0f),
            new Color(1.0f, 0.5f, 0.0f),
            new Color(1.0f, 0.4f, 0.4f),
            new Color(0.6f, 0.0f, 0.0f)
        };

        internal static int ModelMaxBones { get; set; } = 60;
        internal static int ModelMaxTriangs { get; set; } = 5000;

        internal static int EffectMaxMatrials { get; set; } = 15;
        internal static int EffectMaxParticles { get; set; } = 500;

        internal static string[] ScoreNames { get; private set; } = new[] { "完 美", "优 秀", "合 格", "超 标", "想起飞?" };

        internal readonly static GUIStyle LabelStyle = new GUIStyle()
        {
            normal = new GUIStyleState()
            {
                textColor = Color.white,
            },
            clipping = TextClipping.Clip,
            padding = new RectOffset(0, 0, 4, 0),
            alignment = TextAnchor.MiddleCenter,

        };

        internal readonly static GUIStyle ButtonStyle = new GUIStyle("Button")
        {
            alignment = TextAnchor.MiddleLeft
        };

        /// <summary>
        /// 计算评分
        /// </summary>
        /// <param name="score"></param>
        internal static int CalScoreLevel(float score)
        {
            int _offset = (int)((score - 1) * 100);
            _offset = _offset == 0 ? 1 : _offset;
            _offset /= Mathf.Abs(_offset);
            int _lv = score > 1 ? 1 : 0;

            float _offsetScore = Mathf.Abs(score - 1f);
            if (_offsetScore >= 0.29f) _lv = 2;
            else if (_offsetScore >= 0.19f) _lv = 1;

            _lv = 2 + _lv * _offset;
            return Mathf.Clamp(_lv, 0, 4);
        }

        /// <summary>
        /// 初始化配置信息
        /// </summary>
        internal static void Initialize()
        {
            if (!System.IO.File.Exists(ProjectUtility.Path.FrameworkPath + "EFAssets/Configs/AssetCheckerConfigs.xml"))
            {
                return;
            }

            XmlDocument _xml = new XmlDocument();
            _xml.LoadXml(System.IO.File.ReadAllText(ProjectUtility.Path.FrameworkPath + "EFAssets/Configs/AssetCheckerConfigs.xml"));

            XmlNode _model = _xml.SelectSingleNode("/CheckerConfigs/Model");
            if (_model != null)
            {
                int _maxBones = int.Parse(_model.Attributes["MaxBones"].Value);
                int _maxTriangs = int.Parse(_model.Attributes["MaxTriangs"].Value);
                ModelMaxBones = _maxBones;
                ModelMaxTriangs = _maxTriangs;
            }

            XmlNode _effect = _xml.SelectSingleNode("/CheckerConfigs/Effect");
            if (_effect != null)
            {
                int _maxMaterials = int.Parse(_effect.Attributes["MaxMatrials"].Value);
                int _maxParticles = int.Parse(_effect.Attributes["MaxParticles"].Value);
                EffectMaxMatrials = _maxMaterials;
                EffectMaxParticles = _maxParticles;
            }

            XmlNode _common = _xml.SelectSingleNode("/CheckerConfigs/Common");
            if (_common != null)
            {
                XmlNodeList _names = _common.SelectNodes("ScoreNames");
                ScoreNames = new string[_names.Count];
                for (int i = 0; i < _names.Count; i++)
                {
                    string scoreNameAttr = _names[i].Attributes["ScoreName_" + i].Value;
                    ScoreNames[i] = scoreNameAttr;
                }

                XmlNodeList _colors = _common.SelectNodes("ScoreColors");
                ScoreColors = new Color[_colors.Count];
                for (int i = 0; i < _colors.Count; i++)
                {
                    string[] scoreColorAttr = _colors[i].Attributes["ScoreColor_" + i].Value.Split(',');
                    ScoreColors[i] = new Color(float.Parse(scoreColorAttr[0]), float.Parse(scoreColorAttr[1]), float.Parse(scoreColorAttr[2]));
                }
            }
        }

        /// <summary>
        /// 保存配置信息
        /// </summary>
        internal static void SaveConfig()
        {
            XmlDocument _xml = new XmlDocument();
            XmlElement _root = _xml.CreateElement("CheckerConfigs");

            XmlElement _modelEle = _xml.CreateElement("Model");
            {
                _modelEle.SetAttribute("MaxBones", ModelMaxBones.ToString());
                _modelEle.SetAttribute("MaxTriangs", ModelMaxTriangs.ToString());
            }

            XmlElement _effectEle = _xml.CreateElement("Effect");
            {
                _effectEle.SetAttribute("MaxMatrials", EffectMaxMatrials.ToString());
                _effectEle.SetAttribute("MaxParticles", EffectMaxParticles.ToString());

            }

            XmlElement _common = _xml.CreateElement("Common");
            {
                for (int i = 0; i < ScoreNames.Length; i++)
                {
                    XmlElement _scoreNames = _xml.CreateElement("ScoreNames");
                    _scoreNames.SetAttribute($"ScoreName_{i}", ScoreNames[i]);
                    _common.AppendChild(_scoreNames);
                }

                for (int i = 0; i < ScoreColors.Length; i++)
                {
                    XmlElement _scoreColors = _xml.CreateElement("ScoreColors");
                    string _col = $"{ScoreColors[i].r},{ScoreColors[i].g},{ScoreColors[i].b}";
                    _scoreColors.SetAttribute($"ScoreColor_{i}", _col);
                    _common.AppendChild(_scoreColors);
                }
            }

            _root.AppendChild(_modelEle);
            _root.AppendChild(_effectEle);
            _root.AppendChild(_common);
            _xml.AppendChild(_root);
            _xml.Save(ProjectUtility.Path.FrameworkPath + "EFAssets/Configs/AssetCheckerConfigs.xml");
        }
    }
}
