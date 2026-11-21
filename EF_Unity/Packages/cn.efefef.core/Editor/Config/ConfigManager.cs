/*
 * ================================================
 * Describe:      Easy framework setting utils.框架设置工具
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 20:41:57
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-21
 * ScriptVersion: 1.0
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using EasyFramework.Edit.TodoList;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// Easy framework setting utils.框架设置工具
    /// <para>统一配置入口：缓存 → AssetDatabase 查找 → 创建。全编辑器共用这一份</para>
    /// </summary>
    public static class ConfigManager
    {
        public const string ConfigEditPath = "Assets/Editor Resources/Configs";
        public const string ConfigRuntimePath = "Assets/Resources/Configs";

        // 按类型缓存：同类配置全局一份，避免每次 Get 都查 AssetDatabase
        private static readonly Dictionary<Type, ScriptableObject> Cache = new();
        // 正在创建的类型：防止创建过程里又 Get 同类型导致递归
        private static Type _creating;

        public static PathConfig Path => Get<PathConfig>(ConfigEditPath);
        public static ProjectConfig Project => Get<ProjectConfig>(ConfigRuntimePath);

        /// <summary>
        /// 获取或创建配置：先查缓存，再查 AssetDatabase，最后创建。全局唯一入口。
        /// <para>Get or create a config: cache → AssetDatabase lookup → create. Single global entry point.</para>
        /// </summary>
        /// <typeparam name="T">配置类型<para>Config type</para></typeparam>
        /// <param name="path">创建时所在目录；为空则用 Resources/Configs<para>Create folder; empty means Resources/Configs</para></param>
        /// <param name="onCreated">仅新建时调用的初始化回调（如生成唯一盐）<para>Init callback invoked only when newly created</para></param>
        /// <param name="forceCreate">强制新建：跳过缓存与查找直接创建（用于"重建配置"菜单）<para>Force creation, skipping cache and lookup</para></param>
        /// <param name="focusExisting">资产已存在时选中并 Ping（仅创建菜单传 true；普通 Get 传 true 会抢焦点）<para>Select and ping an existing asset; only creation menus should pass true</para></param>
        public static T Get<T>(string path = null, Action<T> onCreated = null, bool forceCreate = false,
            bool focusExisting = false) where T : ScriptableObject, new()
        {
            // 强制创建时跳过缓存与 AssetDatabase 查找，直接走新建分支
            if (!forceCreate)
            {
                // cached != null 用 Unity 重载的 == 识别 fake-null：资产被重新导入后旧引用
                // 仍在字典里但 Unity 层已销毁，`is T` 会误判有效并返回坏对象
                if (Cache.TryGetValue(typeof(T), out ScriptableObject cached) && cached != null && cached is T typed)
                {
                    if (focusExisting) Focus(typed);
                    return typed;
                }

                if (EditorUtils.CheckAssets<T>(out string configPath))
                {
                    var loaded = EditorUtils.LoadSettingAtPath<T>(configPath);
                    if (loaded != null)
                    {
                        Cache[typeof(T)] = loaded;
                        if (focusExisting) Focus(loaded);
                        return loaded;
                    }
                }
            }

            if (_creating == typeof(T))
                return null;

            _creating = typeof(T);
            try
            {
                string createPath = path ?? ConfigRuntimePath;
                var created = CreateAsset<T>(createPath);
                if (created != null)
                {
                    onCreated?.Invoke(created);
                    EditorUtility.SetDirty(created);
                    AssetDatabase.SaveAssets();
                    Cache[typeof(T)] = created; // 强制创建时覆盖旧缓存，后续 Get 拿到新资产
                }
                else
                    D.Error($"[ConfigManager] 创建 {typeof(T).Name} 失败：路径 '{createPath}' 无效或目录只读。");
                return created;
            }
            finally
            {
                _creating = null;
            }
        }

        /// <summary>
        /// 直接创建配置资产（不查缓存）。一般由 Get&lt;T&gt;() 内部调用。
        /// <para>Create a config asset without cache lookup.</para>
        /// </summary>
        private static T CreateAsset<T>(string folderPath) where T : ScriptableObject
        {
            string configPath = System.IO.Path.Combine(folderPath, $"{typeof(T).Name}.asset");

            T asset = ScriptableObject.CreateInstance<T>();
            string folder = System.IO.Path.GetDirectoryName(configPath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            AssetDatabase.CreateAsset(asset, configPath);
            AssetDatabase.SaveAssets();

            // CreateInstance 的引用会在该 .asset 随后被重新导入（reimport）时变成 fake-null，
            // 且新资产的 FindAssets 索引在导入完成前不可用——这正是"创建一次后马上创建别的
            // 没反应、切换窗口触发 Refresh 后又正常"的根因。主动同步导入 + 从路径取回稳定引用，
            // 避免把即将失效的引用写进缓存。
            AssetDatabase.ImportAsset(configPath);
            T stable = AssetDatabase.LoadAssetAtPath<T>(configPath);
            Focus(stable);
            return stable;
        }

        // 选中并在 Project 窗口定位资产（创建菜单、已存在资产聚焦时用）
        private static void Focus(UnityEngine.Object asset)
        {
            if (asset == null) return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        #region Assets/Create 菜单

        [MenuItem("Assets/Create/EF/Project Config", priority = 200)]
        private static void CreateProjectConfig() => Get<ProjectConfig>(ConfigRuntimePath, focusExisting: true);

        [MenuItem("Assets/Create/EF/Path Config", priority = 211)]
        private static void CreatePathConfig() => Get<PathConfig>(ConfigEditPath, focusExisting: true);

        [MenuItem("Assets/Create/EF/Todo List Config", priority = 310)]
        // 强制新建：TodoList 允许多份，保留原 Instance(false) 的语义
        private static void CreateTodoListConfig() =>
            Get<TodoListConfig>(Utility.Path.GetCurrentFolderPath(), forceCreate: true);

        #endregion
    }
}
