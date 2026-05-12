/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-12 18:47:27
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-12 18:47:27
 * ScriptVersion: 0.1
 * ===============================================
 */


using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace EasyFramework.Managers.RedDot
{
    public class RedDotUI : MonoBehaviour
    {
        [Header("绑定红点节点Key")] 
        public string redDotKey;

        [Header("UI组件引用")]
        public GameObject dotObject; // 红点图片物体（单红点专用）
        
        public Text numberText; // 数字文本（数字红点专用）
        public Image customImage; // 自定义图片（图片红点、图片+数字红点专用）
        public Text imageNumberText; // 图片+数字时的数字文本

        [Header("动画设置")] 
        public float animationDuration = 0.2f;
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private bool lastHasRedDot = false;
        private RedDotNode node;
        private Vector3 originScale;
        
        private CancellationTokenSource _animationCts; // 添加到类字段

        private void Awake()
        {
            originScale = transform.localScale;
            HideAllChildren();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(redDotKey))
            {
                Debug.LogError("RedDotUI 未绑定红点Key", this);
                return;
            }

            node = RedDotManager.Instance.GetNode(redDotKey);
            if (node == null)
            {
                Debug.LogError($"红点节点 {redDotKey} 不存在，请先注册", this);
                return;
            }

            // 订阅节点变化事件
            node.OnValueChanged += OnRedDotChanged;
            // 立即刷新一次
            OnRedDotChanged(node);
        }

        private void OnDestroy()
        {
            if (node != null)
                node.OnValueChanged -= OnRedDotChanged;
        }

        private void OnRedDotChanged(RedDotNode changedNode)
        {
            bool hasRedDot = changedNode.Number > 0;
            if (!hasRedDot)
            {
                // 停止当前播放的动画
                _animationCts?.Cancel();
                _animationCts = null;
                // 重置缩放
                transform.localScale = originScale;
                HideAllChildren();
                lastHasRedDot = false;  // 重置标志
                return;
            }

            // 有红点，根据显示类型显示正确的UI组件并播放入场动画
            switch (changedNode.DisplayType)
            {
                case RedDotDisplayType.Dot:
                    ShowSingleDot();
                    break;
                case RedDotDisplayType.Number:
                    ShowNumber(changedNode.Number);
                    break;
                case RedDotDisplayType.Image:
                    ShowImage(changedNode.ImagePath);
                    break;
                case RedDotDisplayType.ImageNumber:
                    ShowImageAndNumber(changedNode.ImagePath, changedNode.Number);
                    break;
            }

            if (!lastHasRedDot && changedNode.EnableAnimation)
            {
                _animationCts?.Cancel();
                _animationCts = new CancellationTokenSource();
                PlayEnterAnimation(_animationCts.Token).Forget();
            }
            lastHasRedDot = true;
        }

        private void HideAllChildren()
        {
            if (dotObject) dotObject.SetActive(false);
            if (numberText) numberText.gameObject.SetActive(false);
            if (customImage) customImage.gameObject.SetActive(false);
            if (imageNumberText) imageNumberText.gameObject.SetActive(false);
        }

        private void ShowSingleDot()
        {
            HideAllChildren();
            if (dotObject) dotObject.SetActive(true);
        }

        private void ShowNumber(int number)
        {
            HideAllChildren();
            if (numberText)
            {
                numberText.text = number.ToString();
                numberText.gameObject.SetActive(true);
            }
        }

        private void ShowImage(string imagePath)
        {
            HideAllChildren();
            if (customImage && !string.IsNullOrEmpty(imagePath))
            {
                // 实际项目中可用 Resources.Load 或 Addressables 加载
                Sprite sprite = Resources.Load<Sprite>(imagePath);
                if (sprite != null) customImage.sprite = sprite;
                customImage.gameObject.SetActive(true);
            }
        }

        private void ShowImageAndNumber(string imagePath, int number)
        {
            HideAllChildren();
            if (customImage && !string.IsNullOrEmpty(imagePath))
            {
                Sprite sprite = Resources.Load<Sprite>(imagePath);
                if (sprite != null) customImage.sprite = sprite;
                customImage.gameObject.SetActive(true);
            }

            if (imageNumberText)
            {
                imageNumberText.text = number.ToString();
                imageNumberText.gameObject.SetActive(true);
            }
        }
        
        private async UniTask PlayEnterAnimation(CancellationToken token)
        {
            float timer = 0f;
            Vector3 startScale = originScale * 0.5f;
            transform.localScale = startScale;
            while (timer < animationDuration)
            {
                if (token.IsCancellationRequested) return;
                float t = timer / animationDuration;
                float curveT = scaleCurve.Evaluate(t);
                transform.localScale = Vector3.Lerp(startScale, originScale, curveT);
                timer += Time.deltaTime;
                await UniTask.Yield();
            }
            transform.localScale = originScale;
        }
    }
}