/* 
 * ================================================
 * Describe:      This script is used to update project.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-03-02 16:24:48
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-03-02 16:24:48
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace EasyFramework
{
    /// <summary>
    /// Update interface in project.
    /// </summary>
    public interface IUpdate
	{
        /// <summary>
        /// 轮询更新
        /// </summary>
        /// <param name="elapse">The interval in seconds from the last frame to the current one.逻辑流逝时间，以秒为单位</param>
        /// <param name="realElapse">The timeScale-independent interval in seconds from the last frame to the current one真实流逝时间，以秒为单位</param>
        void Update(float elapse, float realElapse);
    }
}
