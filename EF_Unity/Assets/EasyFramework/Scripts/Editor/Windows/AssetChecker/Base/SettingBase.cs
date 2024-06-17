/*
 * ================================================
 * Describe:      This script is used to show the resource detection overview. Let's thank LiangZG!!!!!
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-06-06 15:28:48
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-06-06 15:28:48
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows.AssetChecker
{
    /// <summary>
    /// 设置基类
    /// </summary>
    internal abstract class SettingBase
    {
        internal string AssetDesc = LC.Combine("Assets", "Description");

        /// <summary>
        /// 目录
        /// </summary>
        internal List<string> Folder = new List<string>();

        /// <summary>
        /// 筛选的文件后缀
        /// </summary>
        internal string MatchFile;

        /// <summary>
        /// 信息是否展开
        /// </summary>
        internal bool IsUnfold = true;

        /// <summary>
        /// 额外的GUI绘制
        /// </summary>
        internal virtual void OnGUI() { }

        /// <summary>
        /// 保存配置
        /// </summary>
        internal virtual void Write(XmlDocument doc, XmlElement ele)
        {
            ele.SetAttribute("AssetDesc", AssetDesc);
            ele.SetAttribute("MatchFile", MatchFile);

            if (Folder.Count > 0)
            {
                for (int i = 0; i < Folder.Count; i++)
                {
                    if (string.IsNullOrEmpty(Folder[i])) continue;

                    XmlElement folderEle = doc.CreateElement("Folder");
                    folderEle.SetAttribute("Path", Folder[i]);
                    ele.AppendChild(folderEle);
                }
            }
        }

        /// <summary>
        /// 读取配置
        /// </summary>
        internal virtual void Read(XmlElement ele)
        {
            AssetDesc = ele.GetAttribute("AssetDesc");
            MatchFile = ele.GetAttribute("MatchFile");

            XmlNodeList childNodels = ele.ChildNodes;
            if (childNodels.Count > 0)
            {
                foreach (XmlNode childNode in childNodels)
                {
                    XmlElement childEle = childNode as XmlElement;
                    if (childEle != null) Folder.Add(childEle.GetAttribute("Path"));
                }
            }
            else
                Folder.Add(string.Empty);
        }

    }

    internal class TextureSetting : SettingBase
    {
        internal bool MipMaps;

        internal override void OnGUI()
        {
            GUILayout.Label("MipMaps", GUILayout.Width(100F));
            MipMaps = GUILayout.Toggle(MipMaps, LC.Combine("Open"));
        }

        internal override void Write(XmlDocument doc, XmlElement ele)
        {
            base.Write(doc, ele);

            ele.SetAttribute("MipMaps", MipMaps.ToString());
        }

        internal override void Read(XmlElement ele)
        {
            base.Read(ele);

            string minmaps = ele.GetAttribute("MipMaps");
            if (!string.IsNullOrEmpty(minmaps))
                MipMaps = Convert.ToBoolean(minmaps);
        }
    }

    internal class ModelSetting : SettingBase
    {
        public ModelSetting()
        {
            MaxBones = AssetsCheckerConfig.ModelMaxBones;
            MaxTriangs = AssetsCheckerConfig.ModelMaxTriangs;
        }

        internal int MaxTriangs = 1;
        internal int MaxBones = 1;
        internal int MaxTextureSize;

        internal override void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            MaxTriangs = EditorGUILayout.IntSlider(LC.Combine("Model", "Max", "Triangular", "Count"), MaxTriangs, 1, AssetsCheckerConfig.ModelMaxTriangs);
            GUILayout.FlexibleSpace();
            MaxBones = EditorGUILayout.IntSlider(LC.Combine("Model", "Max", "Bone", "Count"), MaxBones, 1, AssetsCheckerConfig.ModelMaxBones);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 保存模型设置配置
        /// </summary>
        internal override void Write(XmlDocument doc, XmlElement ele)
        {
            base.Write(doc, ele);

            ele.SetAttribute("MaxTriang", MaxTriangs.ToString());
            ele.SetAttribute("MaxBones", MaxBones.ToString());
            ele.SetAttribute("MaxTextrueSize", MaxTextureSize.ToString());
        }

        internal override void Read(XmlElement ele)
        {
            base.Read(ele);

            MaxTriangs = Convert.ToInt32(ele.GetAttribute("MaxTriang"));
            MaxBones = Convert.ToInt32(ele.GetAttribute("MaxBones"));
            MaxTextureSize = Convert.ToInt32(ele.GetAttribute("MaxTextrueSize"));
        }
    }

    internal class ParticleEffectSetting : SettingBase
    {
        public ParticleEffectSetting()
        {
            MaxMatrials = AssetsCheckerConfig.EffectMaxMatrials;
            MaxParticels = AssetsCheckerConfig.EffectMaxParticles;
        }

        internal int MaxMatrials;
        internal int MaxParticels;

        internal override void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            MaxMatrials = EditorGUILayout.IntSlider(LC.Combine("Effects", "Max", "Matrials", "Count"), MaxMatrials, 1, AssetsCheckerConfig.EffectMaxMatrials);
            GUILayout.FlexibleSpace();
            MaxParticels = EditorGUILayout.IntSlider(LC.Combine("Effects", "Max", "Particle", "Count"), MaxParticels, 1, AssetsCheckerConfig.EffectMaxParticles);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 保存模型设置配置
        /// </summary>
        internal override void Write(XmlDocument doc, XmlElement ele)
        {
            base.Write(doc, ele);

            ele.SetAttribute("MaxMatrials", MaxMatrials.ToString());
            ele.SetAttribute("MaxParticels", MaxParticels.ToString());
        }

        internal override void Read(XmlElement ele)
        {
            base.Read(ele);

            MaxMatrials = Convert.ToInt32(ele.GetAttribute("MaxMatrials"));
            MaxParticels = Convert.ToInt32(ele.GetAttribute("MaxParticels"));
        }
    }
}
