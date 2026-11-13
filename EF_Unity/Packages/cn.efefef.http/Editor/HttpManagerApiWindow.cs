// /*
//  * ================================================
//  * Describe:      HttpManager 公开 API 速查窗口：反射列出全部可直接访问的成员，一键复制单行调用。
//  * Author:        Alvin8412
//  * CreationTime:  2026-11-13 14:40
//  * ModifyAuthor:  Alvin8412
//  * ModifyTime:    2026-11-13 14:40
//  * ScriptVersion: 1.0
//  * ================================================
//  */
//
// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using System.Text;
// using System.Threading;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.Networking;
//
// namespace EasyFramework.Systems.Http.Editor
// {
//     /// <summary>
//     /// <see cref="HttpManager"/> 公开 API 速查窗口：反射列出全部可直接访问的成员，一键复制单行调用。
//     /// <para>Editor window that lists every publicly reachable <see cref="HttpManager"/> member via reflection and
//     /// copies a single-line call to the clipboard.</para>
//     /// </summary>
//     public class HttpManagerApiWindow : EditorWindow
//     {
//         #region 字段 / Fields
//
//         // 滚动视图位置
//         private Vector2 _scroll;
//
//         // 成员名筛选关键字
//         private string _search = "";
//
//         // 按分类归组的成员列表
//         private readonly Dictionary<string, List<Entry>> _groups = new Dictionary<string, List<Entry>>();
//
//         // 分类展示顺序
//         private static readonly string[] CategoryOrder =
//         {
//             "属性 / 诊断", "请求编排", "服务注入", "运行时配置", "其它"
//         };
//
//         private sealed class Entry
//         {
//             // 方法 / 属性签名
//             public string Signature;
//
//             // 可一键复制的单行调用片段
//             public string Snippet;
//         }
//
//         #endregion
//
//         #region 方法 / Methods
//
//         private void OnEnable() => Collect();
//
//         private void OnGUI()
//         {
//             EditorGUILayout.HelpBox(
//                 "通过 HttpManager.Instance 访问以下全部 API。点击右侧「复制」即可把一行调用写入剪贴板，" +
//                 "将占位参数（如 \"url\"、yourClip）改成你自己的即可直接使用。",
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
//                 var shown = new List<Entry>();
//                 foreach (var e in list)
//                 {
//                     if (string.IsNullOrEmpty(filter) || e.Signature.ToLowerInvariant().Contains(filter))
//                         shown.Add(e);
//                 }
//
//                 if (shown.Count == 0) continue;
//
//                 EditorGUILayout.Space(6);
//                 EditorGUILayout.LabelField(cat, EditorStyles.boldLabel);
//                 foreach (var e in shown)
//                 {
//                     EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
//                     EditorGUILayout.LabelField(e.Signature, EditorStyles.wordWrappedLabel);
//                     if (GUILayout.Button("复制", GUILayout.Width(56),
//                             GUILayout.Height(EditorGUIUtility.singleLineHeight)))
//                     {
//                         EditorGUIUtility.systemCopyBuffer = e.Snippet;
//                         ShowNotification(new GUIContent("已复制:\n" + e.Snippet));
//                     }
//
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
//             var type = typeof(HttpManager);
//
//             foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
//                                               BindingFlags.DeclaredOnly))
//             {
//                 if (m.IsSpecialName) continue; // 跳过属性访问器，属性单独列出
//                 AddEntry(CategoryOf(m.Name, false), MethodSignature(m), MethodSnippet(m));
//             }
//
//             foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
//                                                  BindingFlags.DeclaredOnly))
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
//
//             list.Add(new Entry { Signature = sig, Snippet = snippet });
//         }
//
//         private static string CategoryOf(string n, bool isProp)
//         {
//             if (isProp) return "属性 / 诊断";
//             if (n == "Configure" || n == "ResetToDefaults" || n.StartsWith("SetSender") ||
//                 n.StartsWith("SetCache") || n.StartsWith("SetRetry") || n.StartsWith("SetSettings") ||
//                 n.StartsWith("Current"))
//                 return "服务注入";
//             if (n.StartsWith("Default") || n == "SetDefaultHeader" || n == "RemoveDefaultHeader" ||
//                 n == "ClearDefaultHeaders" || n == "OpenDebug")
//                 return "运行时配置";
//             if (n.Contains("Async") || n.Contains("Get") || n.Contains("Post") || n.Contains("Put") ||
//                 n.Contains("Delete") || n.Contains("Download") || n.Contains("Upload"))
//                 return "请求编排";
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
//             var argParts = new List<string>(ps.Length);
//             foreach (var p in ps)
//                 argParts.Add(FriendlyName(p.ParameterType) + " " + p.Name);
//             var args = string.Join(", ", argParts);
//             return $"{FriendlyName(m.ReturnType)} {m.Name}({args})";
//         }
//
//         private static string MethodSnippet(MethodInfo m)
//         {
//             // 带默认值的参数（如 CancellationToken / timeout）自动省略，保证片段最小
//             var psAll = m.GetParameters();
//             var required = new List<ParameterInfo>(psAll.Length);
//             foreach (var p in psAll)
//             {
//                 if (!p.HasDefaultValue)
//                     required.Add(p);
//             }
//
//             var sb = new StringBuilder();
//             sb.Append("HttpManager.Instance.").Append(m.Name).Append("(");
//             for (int i = 0; i < required.Count; i++)
//             {
//                 if (i > 0) sb.Append(", ");
//                 sb.Append(Placeholder(required[i].ParameterType));
//             }
//
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
//                 ? $"HttpManager.Instance.{p.Name} = {DefaultLiteral(p.PropertyType)};"
//                 : $"HttpManager.Instance.{p.Name}";
//         }
//
//         private static bool IsUniTask(Type t) => t.Name.StartsWith("UniTask");
//
//         private static string Placeholder(Type t)
//         {
//             if (t == typeof(string)) return "\"url\"";
//             if (t == typeof(Texture2D)) return "yourTexture";
//             if (t == typeof(AssetBundle)) return "yourBundle";
//             if (t == typeof(bool)) return "false";
//             if (t == typeof(float)) return "0f";
//             if (t == typeof(int)) return "0";
//             if (t == typeof(Dictionary<string, string>)) return "yourHeaders";
//             if (t == typeof(List<IMultipartFormSection>)) return "yourFormData";
//             if (t == typeof(IProgress<float>)) return "yourProgress";
//             if (t == typeof(CancellationToken)) return "token";
//             if (t == typeof(HttpRetryPolicy)) return "yourRetryPolicy";
//             if (t == typeof(IHttpSender) || t == typeof(IHttpCache) || t == typeof(IHttpRetryPolicy) ||
//                 t == typeof(IHttpSettings) || t == typeof(HttpServices))
//                 return "yourService";
//             return "default";
//         }
//
//         private static string DefaultLiteral(Type t)
//         {
//             if (t == typeof(bool)) return "false";
//             if (t == typeof(float)) return "0f";
//             if (t == typeof(int)) return "0";
//             if (t == typeof(string)) return "\"\"";
//             if (t == typeof(CertificateHandler)) return "null";
//             return "default";
//         }
//
//         [MenuItem("Tools/EasyFramework/HTTP Manager API")]
//         public static void Open()
//         {
//             var win = GetWindow<HttpManagerApiWindow>("HTTP Manager API");
//             win.minSize = new Vector2(460, 520);
//             win.Collect();
//         }
//
//         #endregion
//     }
// }