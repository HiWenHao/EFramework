/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin5100
 * CreationTime:    2026-11-25 13:06:01
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-25 13:06:01
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Collections.Generic;
using EasyFramework.GameMaster;

namespace ExampleGame
{
    /// <summary>
    /// GM数据类
    /// </summary>
    public static class GmData
    {
        /// <summary>
        /// 标题列表
        /// </summary>
        public static List<string> SectionList;

        /// <summary>
        /// 命令列表
        /// </summary>
        public static List<GmCommandInfo> CurrentCommandList;

        /// <summary>
        /// 标题与命令列表映射图
        /// </summary>
        public static Dictionary<string, List<GmCommandInfo>> Map;

        /// <summary>
        /// 命令参数输入缓存：命令 ID → 输入框原始文本（可为 JSON）。
        /// <para>Argument input cache: command ID → raw input text (may be JSON).</para>
        /// <br/> 虚拟滚动 item 会被回收复用，输入值必须集中缓存，避免滚出视口后丢失。
        /// <para>Virtual-scroll items are recycled, so input values must be cached centrally to survive recycling.</para>
        /// </summary>
        public static Dictionary<string, string> ArgInputs;

        public static void Init()
        {
            SectionList = new List<string>();
            CurrentCommandList = new List<GmCommandInfo>();
            Map = new Dictionary<string, List<GmCommandInfo>>();
            ArgInputs = new Dictionary<string, string>();
        }

        /// <summary>
        /// 取指定命令的参数输入文本。
        /// <para>Gets the argument input text for a command.</para>
        /// </summary>
        /// <param name="commandId">命令 ID<para>Command ID</para></param>
        /// <returns>输入文本，无输入时为空串<para>Input text, empty string when absent</para></returns>
        public static string GetArgInput(string commandId)
        {
            return ArgInputs.TryGetValue(commandId, out string raw) ? raw : string.Empty;
        }

        public static void Clear()
        {
            SectionList.Clear();
            CurrentCommandList.Clear();
            foreach (KeyValuePair<string, List<GmCommandInfo>> pair in Map)
            {
                pair.Value.Clear();
            }

            Map.Clear();
            // 不清 ArgInputs：Clear 用于 RefreshAll 重建列表，用户输入的参数应保留
        }

        public static void SetToNull()
        {
            Clear();
            SectionList = null;
            CurrentCommandList = null;
            Map = null;
            ArgInputs = null;
        }
    }
}