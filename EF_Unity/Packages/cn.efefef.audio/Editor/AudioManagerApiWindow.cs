// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System.Text;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Audio;
//
// namespace EasyFramework.Managers.Audio.Editor
// {
//     /// <summary>
//     /// AudioManager 公开 API 速查窗口：反射列出全部可直接访问的成员，一键复制单行调用。
//     /// <para>Editor window that lists every publicly reachable AudioManager member via reflection and
//     /// copies a single-line call to the clipboard.</para>
//     /// </summary>
//     public class AudioManagerApiWindow : EditorWindow
//     {
//         private Vector2 _scroll;
//         private string _search = "";
//         private readonly Dictionary<string, List<Entry>> _groups = new Dictionary<string, List<Entry>>();
//
//         private sealed class Entry
//         {
//             public string Signature;
//             public string Snippet;
//         }
//
//         private static readonly string[] CategoryOrder =
//         {
//             "属性 / 诊断", "加载编排", "服务注入", "运行时配置",
//             "BGM 背景音乐", "Effect 音效", "全局控制", "音量与淡入淡出", "查询", "其它"
//         };
//
//         [MenuItem("Tools/EasyFramework/Audio Manager API")]
//         public static void Open()
//         {
//             var win = GetWindow<AudioManagerApiWindow>("Audio Manager API");
//             win.minSize = new Vector2(460, 520);
//             win.Collect();
//         }
//
//         private void OnEnable() => Collect();
//
//         private void OnGUI()
//         {
//             EditorGUILayout.HelpBox(
//                 "通过 AudioManager.Instance 访问以下全部 API。点击右侧「复制」即可把一行调用写入剪贴板，" +
//                 "将占位参数（如 \"assetName\"、yourClip）改成你自己的即可直接使用。",
//                 MessageType.Info);
//
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.LabelField("筛选", GUILayout.Width(40));
//             _search = EditorGUILayout.TextField(_search);
//             EditorGUILayout.EndHorizontal();
//
//             _scroll = EditorGUILayout.BeginScrollView(_scroll);
//             var filter = _search.Trim().ToLowerInvariant();
//
//             foreach (var cat in CategoryOrder)
//             {
//                 if (!_groups.TryGetValue(cat, out var list) || list.Count == 0) continue;
//                 var shown = list.Where(e => string.IsNullOrEmpty(filter) || e.Signature.ToLowerInvariant().Contains(filter)).ToList();
//                 if (shown.Count == 0) continue;
//
//                 EditorGUILayout.Space(6);
//                 EditorGUILayout.LabelField(cat, EditorStyles.boldLabel);
//                 foreach (var e in shown)
//                 {
//                     EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
//                     EditorGUILayout.LabelField(e.Signature, EditorStyles.wordWrappedLabel);
//                     if (GUILayout.Button("复制", GUILayout.Width(56), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
//                     {
//                         EditorGUIUtility.systemCopyBuffer = e.Snippet;
//                         ShowNotification(new GUIContent("已复制:\n" + e.Snippet));
//                     }
//                     EditorGUILayout.EndHorizontal();
//                 }
//             }
//
//             EditorGUILayout.EndScrollView();
//         }
//
//         private void Collect()
//         {
//             _groups.Clear();
//             var type = typeof(AudioManager);
//
//             foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
//             {
//                 if (m.IsSpecialName) continue; // 跳过属性访问器，属性单独列出
//                 AddEntry(CategoryOf(m.Name, false), MethodSignature(m), MethodSnippet(m));
//             }
//
//             foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
//             {
//                 AddEntry("属性 / 诊断", PropertySignature(p), PropertySnippet(p));
//             }
//         }
//
//         private void AddEntry(string cat, string sig, string snippet)
//         {
//             if (!_groups.TryGetValue(cat, out var list))
//             {
//                 list = new List<Entry>();
//                 _groups[cat] = list;
//             }
//             list.Add(new Entry { Signature = sig, Snippet = snippet });
//         }
//
//         private static string CategoryOf(string n, bool isProp)
//         {
//             if (isProp) return "属性 / 诊断";
//             if (n.StartsWith("StopAll") || n.StartsWith("PauseAll") || n.StartsWith("UnPauseAll") ||
//                 n == "SetMute" || n == "StopLoopEffect")
//                 return "全局控制";
//             if (n.Contains("Fade") || n.Contains("Volume")) return "音量与淡入淡出";
//             if (n.Contains("BGM")) return "BGM 背景音乐";
//             if (n.Contains("Effect")) return "Effect 音效";
//             if (n.Contains("LoadClip")) return "加载编排";
//             if (n == "Configure" || n == "ResetToDefaults" || n.StartsWith("SetClip") ||
//                 n.StartsWith("SetPlayer") || n.StartsWith("SetSettings"))
//                 return "服务注入";
//             if (n.StartsWith("SetAudioMixer") || n.StartsWith("SetDefault")) return "运行时配置";
//             if (n.StartsWith("IsPlaying")) return "查询";
//             return "其它";
//         }
//
//         private static string FriendlyName(Type t)
//         {
//             if (t == typeof(void)) return "void";
//             if (t.Name.StartsWith("UniTask"))
//                 return t.IsGenericType
//                     ? "UniTask<" + FriendlyName(t.GetGenericArguments()[0]) + ">"
//                     : "UniTask";
//             return t.Name;
//         }
//
//         private static string MethodSignature(MethodInfo m)
//         {
//             var ps = m.GetParameters();
//             var args = string.Join(", ", ps.Select(p => FriendlyName(p.ParameterType) + " " + p.Name));
//             return $"{FriendlyName(m.ReturnType)} {m.Name}({args})";
//         }
//
//         private static string MethodSnippet(MethodInfo m)
//         {
//             // 带默认值的参数（如 CancellationToken / startTime）自动省略，保证片段最小
//             var required = m.GetParameters().Where(p => !p.HasDefaultValue).ToArray();
//             var sb = new StringBuilder();
//             sb.Append("AudioManager.Instance.").Append(m.Name).Append("(");
//             for (int i = 0; i < required.Length; i++)
//             {
//                 if (i > 0) sb.Append(", ");
//                 sb.Append(Placeholder(required[i].ParameterType));
//             }
//             sb.Append(")");
//             if (IsUniTask(m.ReturnType)) sb.Append(".Forget()");
//             sb.Append(";");
//             return sb.ToString();
//         }
//
//         private static string PropertySignature(PropertyInfo p)
//         {
//             var acc = (p.CanRead ? "get;" : "") + (p.CanWrite ? " set;" : "");
//             return $"{FriendlyName(p.PropertyType)} {p.Name} {{{acc}}}";
//         }
//
//         private static string PropertySnippet(PropertyInfo p)
//         {
//             return p.CanWrite
//                 ? $"AudioManager.Instance.{p.Name} = {DefaultLiteral(p.PropertyType)};"
//                 : $"AudioManager.Instance.{p.Name}";
//         }
//
//         private static bool IsUniTask(Type t) => t.Name.StartsWith("UniTask");
//
//         private static string Placeholder(Type t)
//         {
//             if (t == typeof(string)) return "\"assetName\"";
//             if (t == typeof(AudioClip)) return "yourClip";
//             if (t == typeof(Vector3)) return "Vector3.zero";
//             if (t == typeof(bool)) return "true";
//             if (t == typeof(float)) return "0f";
//             if (t == typeof(int) || t == typeof(uint)) return "0";
//             return "default";
//         }
//
//         private static string DefaultLiteral(Type t)
//         {
//             if (t == typeof(bool)) return "true";
//             if (t == typeof(float)) return "0f";
//             if (t == typeof(string)) return "\"\"";
//             if (t == typeof(AudioMixer)) return "null";
//             if (t == typeof(int) || t == typeof(uint)) return "0";
//             return "default";
//         }
//     }
// }
