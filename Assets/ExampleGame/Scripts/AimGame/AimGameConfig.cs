/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-18 16:19:46
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-18 16:19:46
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework;
using UnityEngine;

namespace AimGame
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class AimGameConfig : Singleton<AimGameConfig>, ISingleton
    {
        public EAction<float> MouseChanged;
        public bool SideswayOpen { private set;get; }
        public float MouseSpeed { get; private set; }
        void ISingleton.Init()
        {
            SideswayOpen = PlayerPrefs.GetInt($"{EF.Projects.AppConst.AppPrefix}Sidesway") == 1;

            MouseSpeed = PlayerPrefs.GetFloat($"{EF.Projects.AppConst.AppPrefix}MouseSpeed", 1);
        }

        void ISingleton.Quit()
        {

        }

        /// <summary>
        /// 设置是否开启横移
        /// </summary>
        public void SetSidesway(bool bol)
        {
            SideswayOpen = bol;
            PlayerPrefs.SetInt($"{EF.Projects.AppConst.AppPrefix}Sidesway",bol ? 1 : 0);
        }

        /// <summary>
        /// 设置鼠标移动速度
        /// </summary>
        public void SetMouseSpeed(float speed)
        {
            MouseSpeed = speed;
            PlayerPrefs.SetFloat($"{EF.Projects.AppConst.AppPrefix}MouseSpeed", speed);
            MouseChanged?.Invoke(speed);
        }
    }
}
