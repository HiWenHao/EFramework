/*
 * ================================================
 * Describe:        多语言 Key 清单的数据模型与读写（只罗列 key，不含 value）
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 15:22:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:45:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using EasyFramework.Edit;
using Newtonsoft.Json;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 单条多语言 Key（只描述 key 本身的元信息，不含任何语言译文）
    /// <para>A single localization key entry; metadata only, no translations.</para>
    /// </summary>
    [Serializable]
    public class LocalizationKeyEntry
    {
        #region Fields

        /// <summary>
        /// Key 名（唯一标识）：建议「域_用途」格式，如 Btn_Confirm；改名会让引用处失效
        /// <para>Unique key identifier, recommended as Domain_Usage; renaming breaks references.</para>
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// 用途说明（给人看，不进包）：必填，翻译与接入时靠它判断这条 key 用在哪、什么语气
        /// <para>Human-readable description for translators and integrators; required.</para>
        /// </summary>
        public string des = string.Empty;

        /// <summary>
        /// 分组（用于面板折叠，一般按 UI 模块划分；留空则归入「无分组」）
        /// <para>Group for folding in the panel; empty means ungrouped.</para>
        /// </summary>
        public string group = "Default";

        /// <summary>
        /// Key 类型（静态 / 动态）：动态文案需考虑复数规则（德语 / 俄语等），翻译前务必确认
        /// <para>Type: static or dynamic text; dynamic requires plural rules.</para>
        /// </summary>
        public LocalizationKeyKind kind = LocalizationKeyKind.Static;

        /// <summary>
        /// 备注（可选，补充语气要求、术语约定、与其它 key 的关系等）
        /// <para>Optional note: tone requirements, terminology, relations.</para>
        /// </summary>
        public string note = string.Empty;

        #endregion

        #region Methods

        /// <summary>
        /// 名称是否符合命名规范（仅字母、数字、下划线、点号）
        /// <para>Whether the name matches the naming convention.</para>
        /// </summary>
        /// <returns>符合返回 true，否则 false<para>True when the name is valid.</para></returns>
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

        #endregion
    }

    /// <summary>
    /// 多语言 Key 清单文件（唯一手写源，只罗列 key 元信息，value 由另一套 value 表管理）
    /// <para>The key manifest file; the single hand-written source of keys.</para>
    /// </summary>
    [Serializable]
    public class LocalizationKeyFile
    {
        #region Fields

        // JSON 序列化配置：缩进 + 忽略 null，避免无意义字段污染文件
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// 元信息（文件级说明与分组登记表）
        /// <para>File-level metadata and the group registry.</para>
        /// </summary>
        public LocalizationKeyMeta meta = new LocalizationKeyMeta();

        /// <summary>
        /// 全部 key 条目
        /// <para>All key entries.</para>
        /// </summary>
        public List<LocalizationKeyEntry> keys = new List<LocalizationKeyEntry>();

        /// <summary>
        /// 未登记语言时使用的默认语言集合（与 core 编辑器 LC 的内置语言保持一致）
        /// <para>Default languages used when meta.languages is empty.</para>
        /// </summary>
        public static readonly List<string> DefaultLanguages = new List<string> { "en", "zh" };

        /// <summary>
        /// 兜底语言代码：运行时找不到目标语言表时回落到它，因此该语言必须始终可用。
        /// 与 core 编辑器 LC 的英文回落语言（LC.EnglishCode）保持一致。
        /// <para>Fallback language code: the runtime falls back to it when a target language table
        /// is missing, so it must always be available.</para>
        /// </summary>
        public const string FallbackLanguage = "en";

        #endregion

        #region Methods

        /// <summary>
        /// 补齐缺失字段，避免空引用（手改 JSON 漏字段后仍能正常打开面板）
        /// <para>Fills missing fields so hand-edited JSON still opens in the panel.</para>
        /// </summary>
        public void EnsureDefaults()
        {
            meta ??= new LocalizationKeyMeta();
            keys ??= new List<LocalizationKeyEntry>();
            meta.groups ??= new List<string>();
            meta.languages ??= new List<string>();

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
        /// <para>Gets the group registry; infers from actual keys when empty.</para>
        /// </summary>
        /// <returns>分组名列表<para>The group name list.</para></returns>
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
        /// <para>Counts the keys under a group.</para>
        /// </summary>
        /// <param name="group">分组名<para>Group name.</para></param>
        /// <returns>该分组下的 key 数量<para>Number of keys in the group.</para></returns>
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
        /// 取目标语言列表。
        /// <para>Gets the target language list.</para>
        /// </summary>
        /// <remarks>
        /// 兜底语言（<see cref="FallbackLanguage"/>）恒定排在最前且不可缺席 —— 运行时找不到
        /// 目标语言表时会回落到它，缺了它整套装就不可用。
        /// 登记表为空时回落默认集合（en + zh）；登记表非空时按登记顺序返回，并确保兜底语言在最前。
        /// <para>The fallback language is always first and never absent: the runtime falls back to it
        /// when a target table is missing. Falls back to the default set (en + zh) when the registry
        /// is empty; otherwise keeps the registered order with the fallback language first.</para>
        /// </remarks>
        /// <returns>语言代码列表（返回新 List，调用方改动不会污染登记表）<para>The language code list (a new List; caller edits do not affect the registry).</para></returns>
        public List<string> GetLanguagesOrDefault()
        {
            var list = new List<string>();

            if (meta?.languages != null && meta.languages.Count > 0)
            {
                foreach (var language in meta.languages)
                {
                    if (string.IsNullOrEmpty(language)) continue;
                    if (list.Contains(language)) continue;
                    list.Add(language);
                }
            }

            if (list.Count == 0)
            {
                list.AddRange(DefaultLanguages);
                return list;
            }

            // 兜底语言必须在最前：登记表里没有就补上，有但不在首位就挪到首位
            int fallbackIndex = list.IndexOf(FallbackLanguage);
            if (fallbackIndex > 0)
            {
                list.RemoveAt(fallbackIndex);
                list.Insert(0, FallbackLanguage);
            }
            else if (fallbackIndex < 0)
            {
                list.Insert(0, FallbackLanguage);
            }

            return list;
        }

        /// <summary>
        /// 确保登记表含兜底语言且位于首位（保存前调用，使落盘的登记表与面板显示一致）。
        /// <para>Ensures the registry contains the fallback language first (called before saving so
        /// the persisted registry matches what the panel shows).</para>
        /// </summary>
        public void EnsureFallbackLanguage()
        {
            meta ??= new LocalizationKeyMeta();
            meta.languages ??= new List<string>();

            int index = meta.languages.IndexOf(FallbackLanguage);
            if (index > 0)
            {
                meta.languages.RemoveAt(index);
                meta.languages.Insert(0, FallbackLanguage);
            }
            else if (index < 0)
            {
                meta.languages.Insert(0, FallbackLanguage);
            }
        }

        /// <summary>
        /// 生成新清单的模板内容（仅在面板首次创建文件时使用，之后以磁盘内容为准）
        /// <para>Creates a template manifest with sample keys.</para>
        /// </summary>
        /// <returns>含示例 key 的新清单<para>A new manifest with sample keys.</para></returns>
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
        /// <para>Loads the manifest from disk.</para>
        /// </summary>
        /// <param name="path">json 绝对路径<para>Absolute path of the JSON file.</para></param>
        /// <returns>读取结果；文件不存在返回 null（调用方据此决定是否建模板）<para>The loaded file, or null when it does not exist.</para></returns>
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
                D.Error(LC.Tr("[ Localization ] 读取 Key 清单失败：{0}\n{1}",
                    "[ Localization ] Failed to read the Key manifest: {0}\n{1}",
                    path, e.Message));
                return null;
            }
        }

        /// <summary>
        /// 覆写磁盘文件（UTF-8 无 BOM，缺失目录会自动创建）
        /// <para>Overwrites the file on disk (UTF-8 without BOM).</para>
        /// </summary>
        /// <param name="path">目标绝对路径<para>Target absolute path.</para></param>
        /// <returns>写入成功返回 true；失败（已输出错误日志）返回 false<para>True on success; false on failure (logged).</para></returns>
        public bool Save(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                EnsureDefaults();

                // 兜底语言恒在登记表首位（数据层兜底：任何调用路径保存都不会漏掉它）
                EnsureFallbackLanguage();

                var json = JsonConvert.SerializeObject(this, Settings);
                File.WriteAllText(path, json, new System.Text.UTF8Encoding(false));
                return true;
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[ Localization ] 写入 Key 清单失败：{0}\n{1}",
                    "[ Localization ] Failed to write the Key manifest: {0}\n{1}",
                    path, e.Message));
                return false;
            }
        }

        /// <summary>
        /// 取出全部非空 key 名（保持清单中的原始顺序，供外部比对清单与实际引用）
        /// <para>Gets all non-empty key names in original order.</para>
        /// </summary>
        /// <returns>key 名列表<para>The key name list.</para></returns>
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

        #endregion
    }

    /// <summary>
    /// 清单元信息
    /// <para>Manifest metadata.</para>
    /// </summary>
    [Serializable]
    public class LocalizationKeyMeta
    {
        #region Fields

        /// <summary>
        /// 用途说明（写给未来读这份文件的人）
        /// <para>Note about this file, for whoever reads it later.</para>
        /// </summary>
        public string note = "本文件只登记多语言 key，不含译文。译文见 value 表。";

        /// <summary>
        /// 分组登记表（可选写入；为空时面板会自动从 key 实际用到的 group 反向补全）
        /// <para>Group registry, optional; inferred from keys when empty.</para>
        /// </summary>
        public List<string> groups = new List<string>();

        /// <summary>
        /// 目标语言登记表（决定导出几份 value 表；为空时面板回落到默认语言）
        /// <para>Target languages; each one produces an L10n_{lang}.csv value table.</para>
        /// </summary>
        public List<string> languages = new List<string>();

        #endregion
    }
}
