/*
 * ================================================
 * Describe:         子面板标记接口。勾选 UiBinding.IsChild 后生成的 View 类实现此接口，
 *                   供 UiSystem 在实例化前零成本识别子面板，避免 OpenView 白创建。
 * Author:           Alvin5100
 * CreationTime:     2026-11-20 10:40:18
 * ModifyAuthor:     Alvin5100
 * ModifyTime:       2026-11-20 10:40:18
 * ScriptVersion:    0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 子面板标记接口：实现此接口的 View 只能由 UiSystem.AddChild 创建，无法用 OpenView 独立打开。
    /// <para>Marker interface for child views: implementers can only be created via UiSystem.AddChild, not opened via OpenView.</para>
    /// </summary>
    public interface IUiChildView : IUiView
    {
    }
}