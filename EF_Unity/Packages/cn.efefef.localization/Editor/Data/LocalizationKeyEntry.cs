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
using Newtonsoft.Json;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 单条多语言 Key
    /// <para>只描述 key 本身的元信息，不含任何语言译文（value 另行管理）。</para>
    /// </summary>
    [Serializable]
    public class LocalizationKeyEntry
    {
        /// <summary>
        /// Key 名（唯一标识）
        /// <para>建议用「域_用途」格式，如 Btn_Confirm；改名会让引用处失效。</para>
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
        /// <para>可选，登记在 <see cref="LocalizationKeyMeta.groups"/>；留空则归入「无分组」。</para>
        /// <para>Group for folding in the panel; empty means ungrouped.</para>
        /// </summary>
        public string group = "Default";

        /// <summary>
        /// Key 类型（静态 / 动态）
        /// <para>动态文案需考虑复数规则（德语 / 俄语等），翻译前务必确认。</para>
        /// <para>Type: static or dynamic text; dynamic requires plural rules.</para>
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
        /// 元信息（文件级说明与分组登记表）
        /// </summary>
        public LocalizationKeyMeta meta = new LocalizationKeyMeta();

        /// <summary>
        /// 全部 key 条目
        /// </summary>
        public List<LocalizationKeyEntry> keys = new List<LocalizationKeyEntry>();

        /// <summary> JSON 序列化配置：缩进 + 忽略 null，避免无意义字段污染文件 </summary>
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// 补齐缺失字段，避免空引用
        /// <para>手改 JSON 漏字段后仍能正常打开面板。</para>
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
        /// 未登记语言时使用的默认语言集合
        /// <para>与 core 编辑器的 ELanguage（English / 中文）保持一致，
        /// 让首次导出就有可用产物，不必先手动补 meta.languages。</para>
        /// <para>Default languages used when meta.languages is empty.</para>
        /// </summary>
        public static readonly List<string> DefaultLanguages = new List<string> { "en", "zh" };

        /// <summary>
        /// 取目标语言列表；登记表为空时回落默认值
        /// <para>返回新List，调用方改动不会污染 <see cref="LocalizationKeyMeta.languages"/>。</para>
        /// <para>Get target languages, falling back to <see cref="DefaultLanguages"/>.</para>
        /// </summary>
        public List<string> GetLanguagesOrDefault()
        {
            if (meta?.languages != null && meta.languages.Count > 0)
            {
                var list = new List<string>();
                foreach (var language in meta.languages)
                {
                    if (!string.IsNullOrEmpty(language)) list.Add(language);
                }

                if (list.Count > 0) return list;
            }

            return new List<string>(DefaultLanguages);
        }

        /// <summary>
        /// 生成新清单的模板内容
        /// <para>仅在面板首次创建文件时使用，之后以磁盘内容为准。</para>
        /// </summary>
        /// <returns>含 4 条示例 key 的新清单</returns>
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
        /// 覆写磁盘文件（UTF-8 无 BOM，缺失目录会自动创建）
        /// </summary>
        /// <param name="path">目标绝对路径</param>
        /// <returns>写入成功返回 true；失败（已输出错误日志）返回 false</returns>
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
        /// 取出全部非空 key 名
        /// <para>供外部（如预制体批量校验）比对清单与实际引用。</para>
        /// </summary>
        /// <returns>key 名列表，保持清单中的原始顺序</returns>
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
        /// <para>登记表为空时，面板会自动从 key 实际用到的 group 反向补全
        /// （见 <see cref="LocalizationKeyFile.GetGroupsOrInfer"/>），
        /// 因此纯手改 JSON 的人不维护此处也不影响功能。</para>
        /// <para>Group registry, optional; inferred from keys when empty.</para>
        /// </summary>
        public List<string> groups = new List<string>();

        /// <summary>
        /// 目标语言登记表（决定导出几份 value 表）
        /// <para>每项是一个语言代码，如 zh-CN / en-US，对应 value 表
        /// <c>L10n_{语言}.csv</c>（见 <see cref="LocalizationData.GetValuesPath"/>）。
        /// 为空时面板回落到默认语言 <see cref="LocalizationKeyFile.DefaultLanguages"/>，
        /// 所以手改 JSON 漏写此项也能正常导出。</para>
        /// <para>Target languages; each one produces an L10n_{lang}.csv value table.</para>
        /// </summary>
        public List<string> languages = new List<string>();
    }
}