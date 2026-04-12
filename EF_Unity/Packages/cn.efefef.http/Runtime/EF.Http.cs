/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-11 21:04:13
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-11 21:04:13
 * ScriptVersion: 0.1
 * ===============================================
 */

using EasyFramework.Managers;

public sealed partial class EF
{
    /// <summary> Network (HTTP) manager.<para>网络HTTP管理器</para></summary>
    public static HttpManager Http => HttpManager.Instance;
}