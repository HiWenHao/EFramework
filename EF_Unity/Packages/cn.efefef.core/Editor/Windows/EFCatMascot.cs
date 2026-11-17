/*
 * ================================================
 * Describe:      编辑器面板通用的猫咪吉祥物：在矩形区域内左右往返奔跑，素材为 RunCat365(Apache-2.0)像素猫帧。
 * Author:        WorkBuddy
 * CreationTime:  2026-11-17 17:19:00
 * ScriptVersion: 2.0
 * ===============================================
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// 编辑器面板通用的猫咪吉祥物：在给定矩形内左右往返奔跑。
    /// <para>Reusable cat mascot for editor panels: runs back and forth inside a rect.</para>
    /// <remarks>
    /// 用法：面板持有一个实例，在 OnGUI 里调用 <see cref="Draw"/>，销毁时调用 <see cref="Dispose"/>；
    /// 面板需持续重绘才能看到动画（如 EFConfigPanelBase.NeedsContinuousRepaint）。
    /// 素材来自 RunCat365（Apache-2.0），为纯黑单色剪影，靠 alpha 成形，运行时用 <see cref="BodyColor"/> 着色。
    /// </remarks>
    /// </summary>
    public sealed class EFCatMascot
    {
        #region 可调参数 / Tunable parameters

        public bool Enabled = true; // 是否绘制
        public float DisplaySize = 46f; // 显示尺寸
        public float SideMargin = 18f; // 距左右边缘留白
        public float BottomMargin = -6f; // 距底部距离（负值用于抵消素材自带底部留白，使脚部贴近横幅底缘）
        public int BobAmplitude = 1; // 跑动起伏像素
        public float StepSpeed = 8f; // 跑动起伏频率
        public float WalkDuration = 3.5f; // 单程跑动时长（秒）
        public float FrameRate = 12f; // 奔跑帧率
        public Color BodyColor = new Color(0.91f, 0.64f, 0.36f, 1f); // 猫身颜色（暖橘，深灰底上醒目且与青绿 EF 互补）

        #endregion

        #region 属性 / Properties

        /// <summary>
        /// 当前使用的素材名（空串=包内置素材）；赋值后自动持久化，窗口重开仍保留
        /// <para>The mascot name in use (empty = builtin); persisted automatically so it survives window restarts.</para>
        /// </summary>
        public string MascotName
        {
            get => _mascotName;
            set
            {
                string name = value ?? string.Empty;
                if (_mascotName == name)
                    return;
                _mascotName = name;
                EFMascotConfig.SetSelectedMascot(name);
            }
        }

        #endregion

        #region 私有字段 / Private fields

        private readonly List<Texture2D> _frames = new List<Texture2D>();
        private bool _usingCustomFrames;   // 当前是否为自行创建的纹理（销毁时需区分 AssetDatabase 资产）
        private string _loadedMascotName;  // 已加载的素材名（用于检测切换）
        private string _mascotName;        // 当前素材名（经属性持久化）
        private bool _disposed;

        #endregion

        /// <summary>猫咪吉祥物实例：恢复上次选择的素材。</summary>
        public EFCatMascot()
        {
            _mascotName = EFMascotConfig.GetSelectedMascot();
        }

        /// <summary>
        /// 在指定矩形区域内绘制猫咪。
        /// <para>Draw the cat inside the given rect.</para>
        /// </summary>
        /// <param name="area">可用矩形区域<para>Available rect area.</para></param>
        /// <param name="time">绝对时间（秒），驱动动画相位<para>Absolute time in seconds driving animation phase.</para></param>
        public void Draw(Rect area, double time)
        {
            if (!Enabled || _disposed)
                return;

            // 素材名变化时重新加载（切换自定义素材后立即生效）
            if (_frames.Count == 0 || _loadedMascotName != MascotName)
                ReloadFrames();
            if (_frames.Count == 0)
                return;

            float size = DisplaySize;
            float minX = area.x + SideMargin;
            float maxX = area.xMax - size - SideMargin;
            if (maxX < minX)
                maxX = minX;
            float groundY = area.yMax - size - BottomMargin;

            // 三角波往返：0→1 为右行，1→0 为左行，到两端立即掉头，无停顿
            float cycle = WalkDuration * 2f;
            float phase = (float)(time % cycle);
            float progress = phase < WalkDuration ? phase / WalkDuration : 1f - (phase - WalkDuration) / WalkDuration;
            bool movingRight = phase < WalkDuration;

            float catX = Mathf.Lerp(minX, maxX, progress);
            float bob = Mathf.Sin((float)(time * StepSpeed)) * BobAmplitude;
            Rect catRect = new Rect(catX, groundY + bob, size, size);

            // 全程播放奔跑序列（帧数由素材决定）
            int frameIndex = (int)(time * FrameRate) % _frames.Count;
            Texture2D frame = _frames[frameIndex];
            if (!frame)
                return;

            // 素材为纯黑剪影，靠 alpha 成形：用 GUI.color 着色成目标色
            Color previousColor = GUI.color;
            GUI.color = BodyColor;

            // 素材朝右：往右走时直接画，往左走时镜像
            if (movingRight)
                GUI.DrawTexture(catRect, frame, ScaleMode.ScaleToFit, true);
            else
                DrawMirrored(catRect, frame);

            GUI.color = previousColor;
        }

        /// <summary>释放已加载的素材引用（HybridCLR 域重载 / 面板销毁安全）。</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            ReleaseFrames();
            _disposed = true;
        }

        // 释放帧纹理：自定义素材是自行创建的需销毁，内置素材来自 AssetDatabase 只清引用
        private void ReleaseFrames()
        {
            for (int i = 0; i < _frames.Count; i++)
            {
                if (_frames[i] && _usingCustomFrames)
                    Object.DestroyImmediate(_frames[i]);
            }
            _frames.Clear();
            _usingCustomFrames = false;
        }

        /// <summary>
        /// 重新加载帧序列（切换素材时调用，先释放旧纹理）
        /// <para>Reload the frame sequence, releasing the previous textures first.</para>
        /// </summary>
        private void ReloadFrames()
        {
            ReleaseFrames();
            LoadFrames();
            _loadedMascotName = MascotName;
        }

        /// <summary>
        /// 加载奔跑序列帧：优先使用 EFTools 下的自定义素材，无可用帧则回退到包内置素材
        /// <para>Load the run frames: custom assets under EFTools take priority, falling back to the builtin set.</para>
        /// </summary>
        private void LoadFrames()
        {
            // 自定义素材优先：帧数由目录内容决定（位于 Assets 之外，走磁盘读取）
            if (!string.IsNullOrWhiteSpace(MascotName))
            {
                if (EFMascotConfig.ValidateFrames(MascotName, out string[] missing))
                {
                    if (missing != null && missing.Length > 0)
                    {
                        Debug.LogWarning(LC.Tr(
                            $"[EF] 自定义素材 '{MascotName}' 存在序号缺口：{string.Join("、", missing)}。",
                            $"[EF] Custom mascot '{MascotName}' has index gaps: {string.Join(", ", missing)}."));
                    }
                    LoadCustomFrames(MascotName);
                    return;
                }
                Debug.LogWarning(LC.Tr(
                    $"[EF] 自定义素材 '{MascotName}' 不可用，已回退到内置素材。",
                    $"[EF] Custom mascot '{MascotName}' is unusable; falling back to the builtin set."));
                // 记录已失效（目录被删除或帧不全），清掉记录避免下次仍指向它
                EFMascotConfig.SetSelectedMascot(string.Empty);
                _mascotName = string.Empty;
            }

            LoadBuiltinFrames();
        }

        // 从 EFTools 自定义目录读取帧序列（Assets 之外，AssetDatabase 无法加载）
        private void LoadCustomFrames(string mascotName)
        {
            string[] files = EFMascotConfig.ScanCustomFrameFiles(mascotName);
            _usingCustomFrames = true;
            foreach (string file in files)
            {
                Texture2D texture = EFMascotConfig.LoadTextureFromFile(file);
                if (texture)
                    _frames.Add(texture);
                else
                    Debug.LogWarning(LC.Tr($"[EF] 猫咪素材读取失败：{file}",
                                           $"[EF] Failed to read mascot asset: {file}"));
            }
        }

        // 从包内置资源目录加载帧序列（Packages 内，可用 AssetDatabase；帧数按目录内容决定）
        private void LoadBuiltinFrames()
        {
            string folder = EFMascotConfig.GetBuiltinAssetDirectory();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(EFMascotConfig.FileExtension, System.StringComparison.OrdinalIgnoreCase))
                    paths.Add(path);
            }
            paths.Sort((a, b) => string.CompareOrdinal(a, b));

            foreach (string path in paths)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture)
                    _frames.Add(texture);
                else
                    Debug.LogWarning(LC.Tr($"[EF] 猫咪素材缺失：{path}",
                                           $"[EF] Mascot asset missing: {path}"));
            }
        }

        // 水平镜像绘制（掉头），用 GUI.matrix 做 X 翻转
        private static void DrawMirrored(Rect rect, Texture2D texture)
        {
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), new Vector2(rect.center.x, rect.center.y));
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = previous;
        }
    }
}
