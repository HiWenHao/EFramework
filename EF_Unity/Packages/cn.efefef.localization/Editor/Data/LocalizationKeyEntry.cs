/*
 * ================================================
 * Describe:        多语言 Key 清单的数据模型与读写（只罗列 key，不含 value）
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 15:22:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-10 15:22:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Key 类型
    /// <para>标记该文案是否需要运行时拼接参数，用于提前筛出需要复数规则处理的条目。</para>
    /// Key kind: marks whether the text needs runtime parameter formatting.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LocalizationKeyKind
    {
        /// <summary>
        /// 静态文案：整条翻译，无参数
        /// <para>Static text: whole-string translation, no parameters</para>
        /// </summary>
        [EnumMember(Value = "Static")] Static = 0,

        /// <summary>
        /// 动态文案：运行时拼接参数，需考虑复数规则（德语/俄语必查）
        /// <para>Dynamic text: formatted at runtime, plural rules required</para>
        /// </summary>
        [EnumMember(Value = "Dynamic")] Dynamic = 1,
    }

    /// <summary>
    /// 单条多语言 Key
    /// <para>只描述 key 本身的元信息，不含任何语言译文（value 另行管理）。</para>
    /// </summary>
    [Serializable]
    public class LocalizationKeyEntry
    {
        /// <summary>
        /// Key 名（唯一标识）
        /// <para>建议用域_用途，如 Btn_Confirm；改名会让引用处失效。</para>
        /// <para>Unique key identifier, recommended as Domain_Usage.</para>
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// 用途说明（给人看，不进包）
        /// <para>必填。翻译与接入时靠它判断这条 key 用在哪、什么语气。</para>
        /// <para>Human-readable description for translators and integrators.</para>
        /// </summary>
        public string des = string.Empty;

        /// <summary>
        /// 分组（用于面板折叠，一般按 UI 模块划分）
        /// <para>Group for folding in the panel, usually by UI module.</para>
        /// </summary>
        public string group = "Default";

        /// <summary>
        /// Key 类型（静态 / 动态）
        /// <para>Type: static or dynamic text.</para>
        /// </summary>
        public LocalizationKeyKind kind = LocalizationKeyKind.Static;

        /// <summary>
        /// 备注（可选，补充语气要求、术语约定、与其它 key 的关系等）
        /// <para>Optional note: tone requirements, terminology, relations.</para>
        /// </summary>
        public string note = string.Empty;

        /// <summary>
        /// 名称是否符合命名规范（仅字母、数字、下划线、点号）
        /// <para>是否仅含字母、数字、下划线、点号。</para>
        /// </summary>
        public bool IsValidName()
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (!char.IsLetter(name[0]) && name[0] != '_') return false;

            foreach (var c in name)
            {
                var ok = char.IsLetterOrDigit(c) || c == '_' || c == '.';
                if (!ok) return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 多语言 Key 清单文件（唯一手写源）
    /// <para>只罗列 key 元信息，value 由另一套 value 表管理。</para>
    /// </summary>
    [Serializable]
    public class LocalizationKeyFile
    {
        /// <summary>
        /// 元信息
        /// </summary>
        public LocalizationKeyMeta meta = new LocalizationKeyMeta();

        /// <summary>
        /// 全部 key 条目
        /// </summary>
        public List<LocalizationKeyEntry> keys = new List<LocalizationKeyEntry>();

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// 补齐缺失字段，避免空引用
        /// </summary>
        public void EnsureDefaults()
        {
            meta ??= new LocalizationKeyMeta();
            keys ??= new List<LocalizationKeyEntry>();
            meta.groups ??= new List<string>();

            foreach (var entry in keys)
            {
                entry.name ??= string.Empty;
                entry.des ??= string.Empty;
                entry.group ??= "Default";
                entry.note ??= string.Empty;
            }
        }

        /// <summary>
        /// 取分组登记表；为空时从 key 实际用到的 group 反向补全（不修改 meta.groups）
        /// <para>Get group registry; infer from actual keys when the registry is empty.</para>
        /// <para>纯手改 JSON 未维护 groups 时，靠这里保证功能完整。</para>
        /// </summary>
        public List<string> GetGroupsOrInfer()
        {
            // 登记表有值时以登记表为准
            if (meta?.groups != null && meta.groups.Count > 0)
                return new List<string>(meta.groups);

            // 否则从 key 反推，保持首次出现顺序
            var inferred = new List<string>();
            foreach (var entry in keys)
            {
                if (string.IsNullOrEmpty(entry.group)) continue;
                if (!inferred.Contains(entry.group))
                    inferred.Add(entry.group);
            }

            return inferred;
        }

        /// <summary>
        /// 统计某分组下的 key 数量
        /// </summary>
        /// <param name="group">分组名</param>
        public int GetGroupKeyCount(string group)
        {
            if (string.IsNullOrEmpty(group)) return 0;

            int count = 0;
            foreach (var entry in keys)
            {
                if (string.Equals(entry.group, group, StringComparison.Ordinal)) count++;
            }

            return count;
        }

        /// <summary>
        /// 生成新清单的模板内容
        /// </summary>
        public static LocalizationKeyFile CreateTemplate()
        {
            return new LocalizationKeyFile
            {
                meta = new LocalizationKeyMeta(),
                keys = new List<LocalizationKeyEntry>
                {
                    new LocalizationKeyEntry
                    {
                        name = "Btn_Confirm",
                        des = "通用确认按钮",
                        group = "Common",
                        kind = LocalizationKeyKind.Static,
                        note = string.Empty,
                    },
                    new LocalizationKeyEntry
                    {
                        name = "Btn_Cancel",
                        des = "通用取消按钮",
                        group = "Common",
                        kind = LocalizationKeyKind.Static,
                        note = string.Empty,
                    },
                    new LocalizationKeyEntry
                    {
                        name = "Title_Welcome",
                        des = "欢迎页标题",
                        group = "MainUI",
                        kind = LocalizationKeyKind.Static,
                        note = "标题短句，勿加句号",
                    },
                    new LocalizationKeyEntry
                    {
                        name = "Dlg_Save_Failed_Count",
                        des = "保存失败数量提示",
                        group = "Dialog",
                        kind = LocalizationKeyKind.Dynamic,
                        note = "需复数规则：英语 1 item / 2 items，德语另有形式",
                    },
                },
            };
        }

        /// <summary>
        /// 从磁盘读取
        /// </summary>
        /// <param name="path">json 绝对路径</param>
        /// <returns>读取结果；文件不存在返回 null（调用方据此决定是否建模板）</returns>
        public static LocalizationKeyFile Load(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                var json = File.ReadAllText(path);
                var file = JsonConvert.DeserializeObject<LocalizationKeyFile>(json);
                if (file == null) return null;

                file.EnsureDefaults();
                return file;
            }
            catch (Exception e)
            {
                D.Error($"[Localization] 读取 Key 清单失败：{path}\n{e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 覆写磁盘文件（UTF-8 无 BOM）
        /// </summary>
        public bool Save(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                EnsureDefaults();
                var json = JsonConvert.SerializeObject(this, Settings);
                File.WriteAllText(path, json, new System.Text.UTF8Encoding(false));
                return true;
            }
            catch (Exception e)
            {
                D.Error($"[Localization] 写入 Key 清单失败：{path}\n{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取出全部 key 名
        /// </summary>
        public List<string> GetKeyNames()
        {
            var names = new List<string>(keys.Count);
            foreach (var entry in keys)
            {
                if (!string.IsNullOrEmpty(entry.name))
                    names.Add(entry.name);
            }

            return names;
        }
    }

    /// <summary>
    /// 清单元信息
    /// </summary>
    [Serializable]
    public class LocalizationKeyMeta
    {
        /// <summary>
        /// 用途说明（写给未来读这份文件的人）
        /// <para>Note about this file, for whoever reads it later.</para>
        /// </summary>
        public string note = "本文件只登记多语言 key，不含译文。译文见 value 表。";

        /// <summary>
        /// 分组登记表（可选写入，用于面板下拉与分组管理）
        /// <para>Group registry, optional. Used by the panel dropdown and group management.</para>
        /// <para>· 登记表为空时，面板会自动从 key 实际用到的 group 反向补全。</para>
        /// <para>· 因此纯手改 JSON 的人不维护此处也不影响功能。</para>
        /// </summary>
        public List<string> groups = new List<string>();
    }
}