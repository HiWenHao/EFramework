/*
 * ================================================
 * Describe:        CircularScrollListPro 交互演示面板，三列循环滚轮 + 选中高亮 + 实时反馈。
 * Author:          Alvin8412
 * CreationTime:    2026-06-05 15:56:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-05 15:56:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using EasyFramework.Managers.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Test
{
    /// <summary>
    /// 循环滚动列表演示面板。运行时自建完整 UI。
    /// <para>三列：月份(1-12) / 日期(1-31) / 小时(0-23)，全部循环滚动。</para>
    /// </summary>
    public class CircularScrollListProPanel : MonoBehaviour
    {
        public Text selectionText;
        public CircularScrollListPro monthColumn;
        public CircularScrollListPro dayColumn;
        public CircularScrollListPro hourColumn;

        private int _monthVal = 6;
        private int _dayVal = 15;
        private int _hourVal = 12;

        private void Start()
        {
            InitializeColumns();
            UpdateSelectionDisplay();
        }

        private void UpdateSelectionDisplay()
        {
            selectionText.text = $"选中: {_monthVal:D2}月  {_dayVal:D2}日  {_hourVal:D2}时";
        }

        #region 列初始化

        private void InitializeColumns()
        {
            if (monthColumn)
            {
                InitColumn(monthColumn, (idx) => $"{idx + 1:D2}月");
                monthColumn.OnSelectedIndexChanged += OnMonthChanged;
                monthColumn.Initialize(12);
                monthColumn.ScrollTo(5); // 默认 6月
            }

            if (dayColumn)
            {
                InitColumn(dayColumn, (idx) => $"{idx + 1:D2}日");
                dayColumn.OnSelectedIndexChanged += OnDayChanged;
                dayColumn.Initialize(31);
                dayColumn.ScrollTo(14); // 默认 15日
            }

            if (hourColumn)
            {
                InitColumn(hourColumn, (idx) => $"{idx:D2}时");
                hourColumn.OnSelectedIndexChanged += OnHourChanged;
                hourColumn.Initialize(24);
                hourColumn.ScrollTo(12); // 默认 12时
            }
        }

        private void InitColumn(CircularScrollListPro list, Func<int, string> formatter)
        {
            list.OnFillItem = (dataIndex, go) =>
            {
                var txt = go.transform.Find("Label")?.GetComponent<Text>();
                if (txt != null)
                    txt.text = formatter(dataIndex);
            };
        }

        private void OnMonthChanged(int idx)
        {
            _monthVal = idx + 1;
            UpdateSelectionDisplay();
        }

        private void OnDayChanged(int idx)
        {
            _dayVal = idx + 1;
            UpdateSelectionDisplay();
        }

        private void OnHourChanged(int idx)
        {
            _hourVal = idx;
            UpdateSelectionDisplay();
        }

        #endregion
    }
}
