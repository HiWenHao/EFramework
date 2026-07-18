/*
 * ================================================
 * Describe:        系统进程工具包 —— 跨平台启动外部进程。
 *                  公开 API 收敛为两个函数：
 *                    • RunAsync    非阻塞启动，退出后在主线程回调 ProcessResult（含退出码 / 标准输出 / 标准错误）
 *                    • RunCaptured 阻塞启动并捕获输出，返回 ProcessResult
 * Author:          Alvin8412
 * CreationTime:    2026-07-07 14:42:57
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-08 16:25:00
 * ScriptVersion:   1.3
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using Path = System.IO.Path;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 系统进程工具包
    /// </summary>
    public static class ProcessToolkit
    {
        /// <summary>
        /// 进程执行结果（仅在等待/捕获模式下有意义）
        /// <para>Process execution result, meaningful only in wait/capture mode.</para>
        /// </summary>
        public sealed class ProcessResult
        {
            /// <summary>进程退出码（0 表示成功）<para>Process exit code (0 = success).</para></summary>
            public int ExitCode;

            /// <summary>标准输出内容<para>Standard output text.</para></summary>
            public string Output = string.Empty;

            /// <summary>标准错误内容<para>Standard error text.</para></summary>
            public string Error = string.Empty;

            /// <summary>是否因超时被终止<para>True if the process was killed due to timeout.</para></summary>
            public bool TimedOut;
        }

        /// <summary>
        /// 非阻塞启动外部进程（脚本自动包裹 shell）。进程在后台线程等待退出并捕获输出，
        /// 退出后通过 <see cref="EditorApplication.delayCall"/> 回到主线程调用 <paramref name="onExited"/> 回调，
        /// 因此回调内可安全调用 <c>AssetDatabase.Refresh()</c> 等必须在主线程执行的 Unity API。
        /// <para>Launch a process without blocking the editor. Waits and drains output on a background thread,
        /// then invokes <paramref name="onExited"/> on the main thread (via EditorApplication.delayCall),
        /// so it is safe to call Unity APIs such as AssetDatabase.Refresh inside the callback.</para>
        /// </summary>
        /// <param name="command">可执行文件或脚本（.bat/.cmd/.sh）路径<para>Executable or script (.bat/.cmd/.sh) path.</para></param>
        /// <param name="workingDirectory">工作目录，默认当前目录 "."；当目标是 .bat/.cmd/.sh 且使用默认 "." 时，自动回退到脚本自身所在目录<para>Working directory, "." by default; for script targets using default ".", auto-falls back to the script's own directory.</para></param>
        /// <param name="arguments">命令行参数列表<para>Command-line arguments.</para></param>
        /// <param name="onExited">退出后的回调（在主线程执行，参数为 <see cref="ProcessResult"/>）<para>Callback invoked on the main thread after exit (receives the <see cref="ProcessResult"/>).</para></param>
        /// <param name="showWindow">是否显示命令窗口（仅 Windows 生效；非 Windows 始终隐藏）<para>Show the command window (Windows only; always hidden on other platforms).</para></param>
        public static void RunAsync(string command, string workingDirectory = ".", string[] arguments = null,
            Action<ProcessResult> onExited = null, bool showWindow = false)
        {
            string message = $"cmd:\n{command}\nargs:\n{string.Join("\n", arguments ?? Array.Empty<string>())}\nwd:\n{workingDirectory}";
            D.Log($"Process RunAsync {message}");
            try
            {
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                bool useShell = showWindow && isWindows;

                string fileName = NormalizeScriptLaunch(command, isWindows, workingDirectory, useShell,
                    out List<string> effectiveArgs, out string effectiveWorkingDir, arguments);

                var info = new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = effectiveWorkingDir,
                    UseShellExecute = useShell,
                    CreateNoWindow = !useShell,
                    RedirectStandardOutput = !useShell,
                    RedirectStandardError = !useShell,
                    // 显示窗口分支（Shell 执行）显式 Normal；隐藏分支 Hidden（CreateNoWindow 也会兜底）。
                    WindowStyle = useShell
                        ? (showWindow ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden)
                        : ProcessWindowStyle.Hidden,
                };

                if (!useShell)
                {
                    // 隐藏/捕获模式需要重定向，统一用 UTF-8 读取，避免中文乱码。
                    info.StandardOutputEncoding = Encoding.UTF8;
                    info.StandardErrorEncoding = Encoding.UTF8;

                    foreach (string argument in effectiveArgs)
                    {
                        info.ArgumentList.Add(argument);
                    }
                }
                else
                {
                    // Shell 执行模式下 ArgumentList 在 Windows 上被忽略/抛异常，必须写入 Arguments 字符串。
                    info.Arguments = BuildArgumentString(effectiveArgs);
                }

                Process process = Process.Start(info);
                if (process == null)
                {
                    D.Error($"[ ProcessToolkit ] Failed to start process: {command}");
                    onExited?.Invoke(new ProcessResult { ExitCode = -1 });
                    return;
                }

                // 后台线程等待 + 读输出，避免阻塞 Unity 主线程（编辑器工具绝不能卡 UI）。
                // 注意：此处不能用 using 包裹 process，否则方法返回即释放句柄，后台线程读取会抛 ObjectDisposedException。
                Task.Run(() =>
                {
                    int exitCode;
                    string stdout = string.Empty;
                    string stderr = string.Empty;
                    bool timedOut = false;

                    try
                    {
                        if (info.RedirectStandardOutput)
                        {
                            ReadOutput(process, timeoutMs: -1, out stdout, out stderr, out timedOut);
                            exitCode = process.ExitCode;
                        }
                        else
                        {
                            // 显示窗口（Shell 执行）模式不重定向，仅等待退出。
                            process.WaitForExit();
                            exitCode = process.ExitCode;
                        }
                    }
                    finally
                    {
                        process.Dispose();
                    }

                    ProcessResult result = new ProcessResult
                    {
                        ExitCode = exitCode,
                        Output = stdout,
                        Error = stderr,
                        TimedOut = timedOut,
                    };

                    EditorApplication.CallbackFunction handler = null;
                    handler = () =>
                    {
                        EditorApplication.delayCall -= handler;
                        onExited?.Invoke(result);
                    };
                    EditorApplication.delayCall += handler;
                });
            }
            catch (Exception e)
            {
                throw new Exception(message, e);
            }
        }

        /// <summary>
        /// 跨平台启动外部进程并捕获标准输出/错误，支持超时。
        /// <para>Cross-platform launcher that captures stdout/stderr and supports timeout.</para>
        /// </summary>
        /// <param name="command">可执行文件路径或命令<para>Executable path or command.</para></param>
        /// <param name="workingDirectory">工作目录，默认当前目录 "."<para>Working directory, "." by default.</para></param>
        /// <param name="arguments">命令行参数列表<para>Command-line arguments.</para></param>
        /// <param name="timeoutMs">超时毫秒数，-1 表示无限等待<para>Timeout in ms, -1 for infinite wait.</para></param>
        public static ProcessResult RunCaptured(string command, string workingDirectory = ".",
            string[] arguments = null, int timeoutMs = -1)
        {
            string message = $"cmd:\n{command}\nargs:\n{string.Join("\n", arguments ?? Array.Empty<string>())}\nwd:\n{workingDirectory}";
            D.Log($"Process RunCaptured {message}");
            try
            {
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                string fileName = NormalizeScriptLaunch(command, isWindows, workingDirectory, false,
                    out List<string> effectiveArgs, out string effectiveWorkingDir, arguments);

                var info = new ProcessStartInfo
                {
                    FileName = fileName,
                    WorkingDirectory = effectiveWorkingDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                };

                foreach (string argument in effectiveArgs)
                {
                    info.ArgumentList.Add(argument);
                }

                using Process process = Process.Start(info);
                if (process == null)
                {
                    D.Error($"[ ProcessToolkit ] Failed to start process: {command}");
                    return new ProcessResult { ExitCode = -1 };
                }

                ReadOutput(process, timeoutMs, out string stdout, out string stderr, out bool timedOut);
                return new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    TimedOut = timedOut,
                };
            }
            catch (Exception e)
            {
                throw new Exception(message, e);
            }
        }

        /// <summary>
        /// 在 PATH 环境变量中查找可执行文件，不实际启动进程（无副作用）。*nix 下额外校验可执行权限。
        /// <para>Resolve an executable from the PATH environment variable without spawning a process. On *nix also checks the exec bit.</para>
        /// </summary>
        private static string FindExecutable(string name)
        {
            // 若已包含目录分隔符，视为完整路径，直接校验存在性。
            if (name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
            {
                return File.Exists(name) && IsUnixExecutable(name) ? name : null;
            }

            string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var dirs = pathEnv.Split(Path.PathSeparator);
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var extensions = isWindows
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM").Split(';')
                : Array.Empty<string>();

            foreach (string dir in dirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;

                string full = Path.Combine(dir, name);
                if (extensions.Length == 0)
                {
                    // *nix：需具备可执行权限。
                    if (File.Exists(full) && IsUnixExecutable(full)) return full;
                }
                else
                {
                    if (File.Exists(full)) return full;
                    // Windows：按 PATHEXT 尝试补齐扩展名。
                    foreach (string ext in extensions)
                    {
                        string candidate = full + ext;
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 校验 *nix 下文件是否具备可执行权限（通过 libc 的 access()）；Windows 或非 P/Invoke 可用时直接返回 true。
        /// <para>Check the Unix exec bit via libc access(); returns true on Windows or when P/Invoke is unavailable.</para>
        /// </summary>
        private static bool IsUnixExecutable(string path)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return true;
            try
            {
                return AccessNative(path, 1) == 0;
            }
            catch (Exception)
            {
                // P/Invoke 不可用（极少数 *nix 环境）时退化为仅检查文件存在。
                return true;
            }
        }

        [DllImport("libc", EntryPoint = "access")]
        private static extern int AccessNative(string path, int mode);

        /// <summary>
        /// 在独立线程读取 stdout/stderr，避免 <c>WaitForExit</c> 重定向死锁，并支持超时整树终止。
        /// <para>Drain stdout/stderr on background threads to avoid redirect deadlock, with optional timeout kill (entire tree).</para>
        /// </summary>
        private static void ReadOutput(Process process, int timeoutMs, out string stdout, out string stderr,
            out bool timedOut)
        {
            var outBuilder = new StringBuilder();
            var errBuilder = new StringBuilder();

            // 在独立线程读取，防止主线程 WaitForExit 与子进程写管道相互阻塞。
            Task outTask = Task.Run(() =>
            {
                try
                {
                    outBuilder.Append(process.StandardOutput.ReadToEnd());
                }
                catch
                {
                    /* 进程已被终止时忽略 */
                }
            });
            Task errTask = Task.Run(() =>
            {
                try
                {
                    errBuilder.Append(process.StandardError.ReadToEnd());
                }
                catch
                {
                    /* 进程已被终止时忽略 */
                }
            });

            int ms = timeoutMs < 0 ? -1 : timeoutMs;
            timedOut = !process.WaitForExit(ms);
            if (timedOut)
            {
                // 超时：终止整棵进程树（含派生的子命令），避免僵尸进程。
                KillProcessTree(process);
            }

            try
            {
                Task.WaitAll(new[] { outTask, errTask }, 2000);
            }
            catch
            {
                /* 读取线程兜底等待 */
            }

            stdout = outBuilder.ToString();
            stderr = errBuilder.ToString();
        }

        /// <summary>
        /// 终止进程：优先整棵树终止（.NET 5+ 的 <c>Process.Kill(bool)</c>，通过反射调用以兼容旧运行时），失败回退到仅终止本进程。
        /// <para>Kill a process: prefer entire process tree (.NET 5+ Kill(bool) via reflection for old-runtime compatibility), fall back to self only.</para>
        /// </summary>
        private static void KillProcessTree(Process process)
        {
            try
            {
                MethodInfo killWithTree = typeof(Process).GetMethod("Kill", new[] { typeof(bool) });
                if (killWithTree != null)
                {
                    killWithTree.Invoke(process, new object[] { true });
                    return;
                }
            }
            catch (Exception)
            {
                /* 反射调用失败，回退 */
            }

            try
            {
                process.Kill();
            }
            catch (Exception)
            {
                /* 进程已退出 */
            }
        }

        /// <summary>
        /// 将参数列表拼为命令行字符串（供 UseShellExecute=true 的 Arguments 使用）。
        /// 含空格的参数自动加引号，避免被 Shell 错误切分。
        /// <para>Join arguments into a command-line string for UseShellExecute=true. Args containing spaces are quoted.</para>
        /// </summary>
        private static string BuildArgumentString(List<string> args)
        {
            if (args == null || args.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            foreach (string arg in args)
            {
                if (sb.Length > 0) sb.Append(' ');
                // 含空格的参数加引号，避免被 Shell 错误切分。
                if (arg.IndexOf(' ') >= 0) sb.Append('"').Append(arg).Append('"');
                else sb.Append(arg);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 若目标是批处理/Shell 脚本（.bat/.cmd/.sh），且需要以 UseShellExecute=false 启动（等待/捕获/跨平台模式），
        /// 则包裹为 cmd.exe /c（Windows）或 bash -c（*nix），使脚本在两种模式下均可运行。
        /// 对普通可执行文件完全无影响（原样返回）。
        /// <para>If the target is a batch/shell script (.bat/.cmd/.sh) and must be launched with UseShellExecute=false
        /// (wait/capture/cross-platform), wrap it as cmd.exe /c (Windows) or bash -c (*nix) so it runs in both modes.
        /// Plain executables are returned unchanged.</para>
        /// </summary>
        /// <param name="command">原始可执行文件或脚本路径<para>Original executable or script path.</para></param>
        /// <param name="isWindows">是否 Windows 平台<para>Whether running on Windows.</para></param>
        /// <param name="workingDirectory">调用方传入的工作目录（用于脚本模式下的回退判断）<para>Caller-supplied working directory (used to decide script-dir fallback).</para></param>
        /// <param name="useShellExecute">使用shell窗口执行<para>Use the shell windows execute.</para></param>
        /// <param name="effectiveArgs">处理后的参数列表（脚本模式下为 /c、-c 等包裹参数）<para>Resolved argument list (wrapped args in script mode).</para></param>
        /// <param name="effectiveWorkingDirectory">实际用于启动的工作目录（脚本 + 默认 "." 时回退到脚本所在目录）<para>Effective working directory (falls back to the script's own dir for scripts using default ".").</para></param>
        /// <param name="arguments">原始附加参数<para>Original extra arguments.</para></param>
        /// <returns>实际用于启动的 FileName（脚本模式为 cmd.exe / bash）<para>Effective FileName to launch (cmd.exe / bash in script mode).</para></returns>
        private static string NormalizeScriptLaunch(string command, bool isWindows, string workingDirectory,
            bool useShellExecute, out List<string> effectiveArgs, out string effectiveWorkingDirectory,
            string[] arguments)
        {
            effectiveWorkingDirectory = workingDirectory;

            if (!IsBatchOrShellScript(command))
            {
                effectiveArgs = arguments == null ? new List<string>() : new List<string>(arguments);
                // UseShellExecute=false 时 WorkingDirectory 不参与定位可执行文件（CreateProcess 只在调用方目录 / CWD / PATH 查找），
                // 因此裸名必须先按 workingDirectory 解析为完整路径，否则报“系统找不到指定的文件”。
                return ResolveExecutablePath(command, workingDirectory);
            }

            // 脚本目标：若调用方使用默认工作目录 "."，回退到脚本自身所在目录。
            // 贴近双击 bat 的行为，避免脚本内相对路径（输入/输出）因 CWD 错位而找不到文件或写错位置。
            // 仅当 command 是带目录的路径时回退；调用方显式传入非 "." 的工作目录时尊重其意图。
            // 该回退对 Shell 执行与非 Shell 执行均生效，保证两种模式下脚本内的相对路径解析一致。
            if (workingDirectory == "." &&
                command.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) >= 0)
            {
                effectiveWorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(command)) ?? workingDirectory;
            }

            // 解析为完整路径：UseShellExecute=false 时 WorkingDirectory 不参与定位，裸名必须解析（见非脚本分支说明）。
            string resolvedCommand = ResolveExecutablePath(command, effectiveWorkingDirectory);

            if (useShellExecute)
            {
                effectiveArgs = arguments == null ? new List<string>() : new List<string>(arguments);
                return resolvedCommand;
            }

            if (isWindows)
            {
                // .bat/.cmd 不是 PE 文件，无法在 UseShellExecute=false（等待/捕获）模式下被 CreateProcess 直接执行。
                // 统一包裹为 cmd.exe /c "<script> <args>"，Shell 与等待模式均可正常启动。
                effectiveArgs = new List<string> { "/c", resolvedCommand };
                if (arguments != null) effectiveArgs.AddRange(arguments);
                return FindExecutable("cmd.exe") ?? "cmd.exe";
            }

            // *nix：.sh 需经 shell 执行（直接 exec 要求具备 shebang 与 +x 权限）。包裹为 bash -c "<script> <args>"。
            // 注意：参数为单一 -c 字符串，含空格的参数不会被单独引用（脚本调用通常无需空格参数）。
            var sb = new StringBuilder(resolvedCommand);
            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    sb.Append(' ');
                    sb.Append(argument);
                }
            }

            effectiveArgs = new List<string> { "-c", sb.ToString() };
            return FindExecutable("bash") ?? FindExecutable("sh") ?? "sh";
        }

        /// <summary>
        /// 将命令解析为可定位的完整路径。当命令是裸名（不含目录分隔符）时，优先在 <paramref name="workingDirectory"/> 中查找，
        /// 其次在 PATH 中查找；均失败则原样返回（交由 CreateProcess / Shell 自行搜索）。
        /// 关键点：<c>UseShellExecute=false</c> 时 <c>ProcessStartInfo.WorkingDirectory</c> 不参与定位可执行文件，
        /// 裸名必须解析为完整路径，否则 CreateProcess 报“系统找不到指定的文件”。
        /// <para>Resolve a command to a locatable full path. For a bare name (no directory separator), prefer
        /// <paramref name="workingDirectory"/> then PATH; if neither finds it, return the name unchanged
        /// (let CreateProcess / Shell search on its own).</para>
        /// </summary>
        private static string ResolveExecutablePath(string command, string workingDirectory)
        {
            // 已含目录分隔符视为相对/绝对路径，直接返回（是否存在交由进程启动器判断）。
            if (command.Contains(Path.DirectorySeparatorChar) || command.Contains(Path.AltDirectorySeparatorChar))
            {
                return command;
            }

            // 优先：workingDirectory 下是否存在该文件（符合调用方“工作目录即程序所在目录”的意图）。
            string inWorkingDir = Path.Combine(workingDirectory, command);
            if (File.Exists(inWorkingDir))
            {
                return inWorkingDir;
            }

            // 其次：PATH 中查找。
            string fromPath = FindExecutable(command);
            return !string.IsNullOrEmpty(fromPath) ? fromPath : command;
        }

        /// <summary>
        /// 判断路径是否为批处理/Shell 脚本（.bat/.cmd/.sh，大小写不敏感）。
        /// <para>True if the path is a batch/shell script (.bat/.cmd/.sh, case-insensitive).</para>
        /// </summary>
        private static bool IsBatchOrShellScript(string path)
        {
            string ext = Path.GetExtension(path);
            return ext.Equals(".bat", StringComparison.OrdinalIgnoreCase)
                   || ext.Equals(".cmd", StringComparison.OrdinalIgnoreCase)
                   || ext.Equals(".sh", StringComparison.OrdinalIgnoreCase);
        }
    }
}