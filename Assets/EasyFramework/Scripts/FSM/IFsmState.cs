/* 
 * ================================================
 * Describe:      This script is used to implemented. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-19 11:28:54
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-19 11:28:54
 * ScriptVersion: 0.1
 * ===============================================
*/
namespace EasyFramework.FSM
{
    /// <summary>
    /// The fsm state interface.
    /// </summary>
    public interface IFsmState
    {
        string Name { get; }
        float Timer { get; }
        float Duration { get; }

        void OnEnter();
        void OnUpdate();
        void OnExit();
    }
}