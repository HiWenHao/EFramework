/* 
 * ================================================
 * Describe:      This script is used to set a ui page interface.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-02 10:31:28
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-02 10:31:28
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEngine;

namespace EasyFramework.UI
{
    /// <summary>
    /// UI界面接口
    /// </summary>
    public interface IUIPageBase
	{
        /// <summary>
        /// Initialize this page in created.在当前页面被创建时调用
        /// </summary>
        /// <param name="obj">The GameObject with current page. 这个页面的游戏物体.</param>
        /// <param name="args">Send this params to current ui page.给这个页面传递的参数.</param>
        void Awake(GameObject obj, params object[] args);

        /// <summary>
        /// On open current ui page.当打开当前UI页面时
        /// </summary>
        /// <param name="args">Send this params to current ui page.给这个页面传递的参数.</param>
        void Open(params object[] args);

        /// <summary>
        /// 轮询刷新
        /// </summary>
        void Update(float elapse, float realElapse);

        /// <summary>
        /// Called when on non-first entering or leaving the page.在非首次进入或离开该页面时调用.
        /// </summary>
        /// <param name="focus">When enter current page, this value is true.当进入该页面时，为真.</param>
        /// <param name="args">When last page quit and pass the args, the args is poosible have value.当上一个页面退出并传递参数时，参数才可能有值.</param>
        void OnFocus(bool focus, params object[] args);

        /// <summary>
        /// On close current ui page.关闭当前页面时被调用
        /// </summary>
        void Close();

        /// <summary>
        /// On quit current ui page.退出当前页面时被调用
        /// </summary>
        void Quit();
    }
}
