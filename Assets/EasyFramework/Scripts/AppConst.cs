/*
 * ================================================
 * Describe:        The app all global config
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-05-05-16:29:27
 * ModifyAuthor:    Xiaohei.Wang(Wenhao)
 * ModifyTime:      2022-09-02-10:39:18
 * Version:         1.0
 * ===============================================
 */

/// <summary>
/// Global Configs
/// </summary>
public class AppConst
{
    #region Global全局配置
    public const string AppName = "EasyFramework";
    public const string AppPrefix = "EF_";
    public const string AppVersion = "1.0";
    #endregion

    #region 管理器运行顺序
    /// <summary>
    /// 数字越小更新越靠前，退出越靠后
    /// </summary>
    public class ManagerLevel
    {
        public const int TimeMgr = -100;
        public const int HttpMgr = -10;
        public const int SocketMgr = -9;
        public const int LoadMgr = -8;
        public const int ToolMgr = -7;
        public const int SceneMgr = -6;
        public const int ObjectToolMgr = -5;
        public const int SourceMgr = -4;
        public const int FolderMgr = -3;
        public const int UIMgr = -2;
    }
    #endregion

    #region Resource下的路径
    #region Prefabs
    /// <summary>
    /// Save the ui page path.存放 UI面板 的路径
    /// </summary>
    public const string UI = "Prefabs/UI/";

    /// <summary>
    /// Save the player path.存放 人物模型 的路径
    /// </summary>
    public const string Player = "Prefabs/Players/";
    #endregion

    #region Audio Sources
    /// <summary>
    /// Save the audio clip path.存放 AudioClip 的路径
    /// </summary>
    public const string Audio = "Sources/";
    #endregion
    #endregion
}
