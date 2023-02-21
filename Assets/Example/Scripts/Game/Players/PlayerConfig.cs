/* 
 * ================================================
 * Describe:      This script is used to . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-20 17:54:38
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-20 17:54:38
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework;
using ExampleGame.Controller;
using UnityEngine;

namespace ExampleGame
{
	namespace Configs
    {
        /// <summary>
        /// The players base config.
        /// 角色们的配置
        /// </summary>
        public abstract class PlayerConfig
        {
            /// <summary>
            /// The player blood
            /// 血量
            /// </summary>
            public int Blood => m_Blood;
            /// <summary>
            /// The player max blood
            /// 最大血量
            /// </summary>
            public int MaxBlood => m_MaxBlood;

            /// <summary>
            /// Blue bar, physical strength or whatever
            /// 能量（蓝条、体力 或者 其他）
            /// </summary>
            public int Energy => m_Energy;
            /// <summary>
            /// Max blue bar, physical strength or whatever
            /// 最大能量（蓝条、体力 或者 其他）
            /// </summary>
            public int MaxEnergy => m_MaxEnergy;


            /// <summary>
            /// The player move speed
            /// 玩家移动速度
            /// </summary>
            public int MoveSpeed => m_MoveSpeed;

            /// <summary>
            /// 角色配置
            /// </summary>
            /// <param name="blood">总血量</param>
            /// <param name="energy">总能量</param>
            /// <param name="moveSpeed">移动速度</param>
            public PlayerConfig(int maxBlood, int maxEnergy, int moveSpeed)
            {
                m_MaxBlood = maxBlood;
                m_MaxEnergy = maxEnergy;
                m_MoveSpeed = moveSpeed;
            }

            public OnPlayerValueChanged onBloodChanged;
            public OnPlayerValueChanged onEnergyChanged;

            private readonly int m_Blood;
            private readonly int m_MaxBlood;
            private readonly int m_Energy;
            private readonly int m_MaxEnergy;
            private readonly int m_MoveSpeed;


            public virtual void SetBolld(int value)
            {
                onBloodChanged?.Invoke(value);
            }
            public virtual void SetEnergy(int value)
            {
                onEnergyChanged?.Invoke(value);
            }
        }

        public class VBotPlayer : PlayerConfig
        {
            public VBotPlayer(int maxBlood, int maxEnergy, int moveSpeed) : base(maxBlood, maxEnergy, moveSpeed)
            {

            }
        }
    }

    public delegate void OnPlayerValueChanged(int value);
    public class PlayerObserver : Singleton<PlayerObserver>, ISingleton
    {
        public PlayerController Player => m_Player;
        public Configs.PlayerConfig Config => m_Config;

        PlayerController m_Player;
        Configs.PlayerConfig m_Config;
        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {
            m_Config = null;
            Object.Destroy(m_Player.gameObject);
        }

        public void SetPlayerConfig(Configs.PlayerConfig config, PlayerController player)
        {
            m_Player = player;
            m_Config = config;
            m_Player.SetConfig(config);
        }
    }
}
