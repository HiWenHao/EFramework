/*
 * ================================================
 * Describe:        This is singleton interface.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-14:42:36
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-05-14:42:36
 * Version:         1.0
 * ===============================================
 */
namespace EasyFramework.Framework.Core{
    public interface ISingleton 
    {
        internal void Init();
        internal void Quit();
    }
}
