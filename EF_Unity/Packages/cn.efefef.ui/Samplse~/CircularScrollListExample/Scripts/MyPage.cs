/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-06-18 14:21:27
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-18 14:21:27
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 假设这是一个UI面板
    /// </summary>
    public class MyPage : MonoBehaviour
    {
        [SerializeField] private CircularScrollList _scrollList_H;
        [SerializeField] private CircularScrollList _scrollList_V;

        [SerializeField] private Button JumpToIndex;
        [SerializeField] private Button ScrollClick;
        [SerializeField] private Button DataChange;
        [SerializeField] private Button Refresh;

        private void Awake()
        {
            JumpToIndex.onClick.AddListener(OnBtnJumpToIndex);
            ScrollClick.onClick.AddListener(OnBtnScrollClick);
            DataChange.onClick.AddListener(OnBtnDataChange);
            Refresh.onClick.AddListener(OnBtnRefresh);
        }

        private void Start()
        {
            _scrollList_V.Initialize(50);
            _scrollList_H.Initialize(50);

            _scrollList_V.OnScrollStarted.AddListener(() => Debug.Log("开始滚动"));
            _scrollList_V.OnScrollEnded.AddListener(idx => Debug.Log($"停在 {idx}"));
            _scrollList_V.OnItemCreated.AddListener((go, idx) =>
            {
                // go 是 Item 实例，可以在这里做额外绑定
            });

            _scrollList_H.OnScrollStarted.AddListener(() => Debug.Log("开始滚动"));
            _scrollList_H.OnScrollEnded.AddListener(idx => Debug.Log($"停在 {idx}"));
            _scrollList_H.OnItemCreated.AddListener((go, idx) =>
            {
                // go 是 Item 实例，可以在这里做额外绑定
            });
        }

        private void OnDestroy()
        {
            JumpToIndex.onClick.RemoveAllListeners();
            ScrollClick.onClick.RemoveAllListeners();
            DataChange.onClick.RemoveAllListeners();
            Refresh.onClick.RemoveAllListeners();
        }

        [ContextMenu("瞬间跳转 - 10")]
        public void OnBtnJumpToIndex()
        {
            _scrollList_V.JumpToIndex(10);
            _scrollList_H.JumpToIndex(10);
        }

        [ContextMenu("3 秒动画滚过去 - 20")]
        public void OnBtnScrollClick()
        {
            _scrollList_V.ScrollToIndex(20, 3f);
            _scrollList_H.ScrollToIndex(20, 3f);
        }

        [ContextMenu(" 数据从 50 变成 30，保留选中位置")]
        public void OnBtnDataChange()
        {
            _scrollList_V.Reinitialize(30);
            _scrollList_H.Reinitialize(30);
        }

        [ContextMenu("强制所有可见项刷新 ")]
        public void OnBtnRefresh()
        {
            _scrollList_V.RefreshAllItems();
            _scrollList_H.RefreshAllItems();
        }
    }
}