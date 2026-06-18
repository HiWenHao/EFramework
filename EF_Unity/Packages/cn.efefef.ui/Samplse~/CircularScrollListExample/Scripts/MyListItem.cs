/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-06-18 14:19:33
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-18 14:19:33
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 你的业务 Item 脚本
    /// </summary>
    public class MyListItem : CircularScrollItem
    {
        [SerializeField] private Text _label;
        [SerializeField] private Image _icon;

        public override void OnSetup(int dataIndex)
        {
            base.OnSetup(dataIndex);
            _label.text = $"Item {dataIndex}";
            // 从你的数据源取数据...
        }

        public override void OnRecycle()
        {
            base.OnRecycle();
            // 移除事件监听等清理
        }
    }
}