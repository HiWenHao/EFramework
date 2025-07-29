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

using System.IO;
using System.Xml;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 检查器配置
    /// </summary>
    internal static class AssetsCheckerConfig
    {
        private static string _assetsPath;

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
            int offset = (int)((score - 1) * 100);
            offset = offset == 0 ? 1 : offset;
            offset /= Mathf.Abs(offset);
            int lv = score > 1 ? 1 : 0;

            float offsetScore = Mathf.Abs(score - 1f);
            if (offsetScore >= 0.29f) lv = 2;
            else if (offsetScore >= 0.19f) lv = 1;

            lv = 2 + lv * offset;
            return Mathf.Clamp(lv, 0, 4);
        }

        /// <summary>
        /// 初始化配置信息
        /// </summary>
        internal static void Initialize()
        {
            _assetsPath = Path.Combine(Utility.Path.GetEFAssetsPath(), "Description/AssetCheckerConfigs.xml");

            if (!File.Exists(_assetsPath))
            {
                return;
            }

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(File.ReadAllText(_assetsPath));

            XmlNode model = xml.SelectSingleNode("/CheckerConfigs/Model");
            if (model != null)
            {
                int maxBones = int.Parse(model.Attributes["MaxBones"].Value);
                int maxTriangs = int.Parse(model.Attributes["MaxTriangs"].Value);
                ModelMaxBones = maxBones;
                ModelMaxTriangs = maxTriangs;
            }

            XmlNode effect = xml.SelectSingleNode("/CheckerConfigs/Effect");
            if (effect != null)
            {
                int maxMaterials = int.Parse(effect.Attributes["MaxMatrials"].Value);
                int maxParticles = int.Parse(effect.Attributes["MaxParticles"].Value);
                EffectMaxMatrials = maxMaterials;
                EffectMaxParticles = maxParticles;
            }

            XmlNode common = xml.SelectSingleNode("/CheckerConfigs/Common");
            if (common != null)
            {
                XmlNodeList names = common.SelectNodes("ScoreNames");
                ScoreNames = new string[names.Count];
                for (int i = 0; i < names.Count; i++)
                {
                    string scoreNameAttr = names[i].Attributes["ScoreName_" + i].Value;
                    ScoreNames[i] = scoreNameAttr;
                }

                XmlNodeList colors = common.SelectNodes("ScoreColors");
                ScoreColors = new Color[colors.Count];
                for (int i = 0; i < colors.Count; i++)
                {
                    string[] scoreColorAttr = colors[i].Attributes["ScoreColor_" + i].Value.Split(',');
                    ScoreColors[i] = new Color(float.Parse(scoreColorAttr[0]), float.Parse(scoreColorAttr[1]), float.Parse(scoreColorAttr[2]));
                }
            }
        }

        /// <summary>
        /// 保存配置信息
        /// </summary>
        internal static void SaveConfig()
        {
            XmlDocument xml = new XmlDocument();
            XmlElement root = xml.CreateElement("CheckerConfigs");

            XmlElement modelEle = xml.CreateElement("Model");
            {
                modelEle.SetAttribute("MaxBones", ModelMaxBones.ToString());
                modelEle.SetAttribute("MaxTriangs", ModelMaxTriangs.ToString());
            }

            XmlElement effectEle = xml.CreateElement("Effect");
            {
                effectEle.SetAttribute("MaxMatrials", EffectMaxMatrials.ToString());
                effectEle.SetAttribute("MaxParticles", EffectMaxParticles.ToString());

            }

            XmlElement common = xml.CreateElement("Common");
            {
                for (int i = 0; i < ScoreNames.Length; i++)
                {
                    XmlElement scoreNames = xml.CreateElement("ScoreNames");
                    scoreNames.SetAttribute($"ScoreName_{i}", ScoreNames[i]);
                    common.AppendChild(scoreNames);
                }

                for (int i = 0; i < ScoreColors.Length; i++)
                {
                    XmlElement scoreColors = xml.CreateElement("ScoreColors");
                    string col = $"{ScoreColors[i].r},{ScoreColors[i].g},{ScoreColors[i].b}";
                    scoreColors.SetAttribute($"ScoreColor_{i}", col);
                    common.AppendChild(scoreColors);
                }
            }

            root.AppendChild(modelEle);
            root.AppendChild(effectEle);
            root.AppendChild(common);
            xml.AppendChild(root);
            xml.Save(_assetsPath);
        }
    }
}
