/*
 * ================================================
 * Describe:      Per-user editor preferences (author name), kept out of version control.
 * Author:        Alvin8412
 * CreationTime:  2026-11-17 18:45:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-17 18:50:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using UnityEditor;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 逐用户的编辑器本地设置。存在 <see cref="EditorPrefs"/> 里，天然不入版本库，
    /// 也不需要宿主工程额外维护 .gitignore。
    /// <para>Per-user editor preferences stored in EditorPrefs: never committed, and consuming projects
    /// need no extra .gitignore rule.</para>
    /// </summary>
    /// <remarks>
    /// 为什么不放 <c>ProjectConfig</c>：作者名是**个人信息**，写进随包资产会跟着提交同步给全组，
    /// 结果是谁最后改谁覆盖所有人，而且每次提交都在同一个字段上打架。
    /// 命名空间、脚本版本号是**团队契约**（生成的代码必须一致），仍留在 <c>ProjectConfig</c>。
    /// <para>Author is personal data and must not live in a committed asset; namespace and script version
    /// are team contracts and stay in ProjectConfig.</para>
    /// </remarks>
    public static class LocalUserSettings
    {
        // 两个 key 分工明确：EditorUser 是 Unity 登录名（配置面板自动写入，只读展示）；
        // ScriptAuthor 是用户自定义署名，为空时回落到 EditorUser。
        // 分开是为了面板每次打开重写登录名时，不会把用户自定义的名字冲掉。
        private const string AuthorKeySuffix = "ScriptAuthor";
        private const string UnityUserKeySuffix = "EditorUser";
        private const string FallbackAuthor = "Unknown";

        private static string _author;

        /// <summary>
        /// 当前用户署名：自定义署名 → Unity 登录名 → 系统用户名。
        /// <para>The current user's display name: custom value, then the Unity account name, then the OS user.</para>
        /// </summary>
        public static string Author
        {
            get
            {
                if (_author != null)
                    return _author;

                _author = Load();
                return _author;
            }
        }

        /// <summary>
        /// 写入署名；传空表示恢复默认（下次读取回落到 Unity 登录名）。
        /// <para>Persists the display name; an empty value restores the default fallback.</para>
        /// </summary>
        /// <param name="author">署名<para>Display name</para></param>
        public static void SaveAuthor(string author)
        {
            string value = author == null ? string.Empty : author.Trim();
            _author = string.IsNullOrEmpty(value) ? Load() : value;
            EditorPrefs.SetString(Key(AuthorKeySuffix), _author);
        }

        private static string Load()
        {
            string custom = EditorPrefs.GetString(Key(AuthorKeySuffix), string.Empty).Trim();
            if (!string.IsNullOrEmpty(custom))
                return custom;

            string unityUser = EditorPrefs.GetString(Key(UnityUserKeySuffix), string.Empty).Trim();
            if (!string.IsNullOrEmpty(unityUser))
                return unityUser;

            string systemUser = Environment.UserName;
            return string.IsNullOrWhiteSpace(systemUser) ? FallbackAuthor : systemUser.Trim();
        }

        // 以 AppPrefix 分区，避免同一台机器上不同项目互相串名
        private static string Key(string suffix)
        {
            string prefix = ConfigManager.Project != null && ConfigManager.Project.AppConst != null
                ? ConfigManager.Project.AppConst.AppPrefix
                : AppConstConfig.DefaultAppPrefix;

            return prefix + suffix;
        }
    }
}
