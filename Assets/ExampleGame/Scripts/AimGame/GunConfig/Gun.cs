/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-22 14:10:25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-22 14:10:25
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace AimGame
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public abstract class Gun
	{
		/// <summary>
		/// 名字
		/// </summary>
		public abstract string Name { get; }

        /// <summary>
        /// 类型
        /// </summary>
        public abstract GunType GunsType { get; }

        /// <summary>
        /// 介绍
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 开火方式
        /// </summary>
        public abstract BFireType FireType { get; }

		/// <summary>
		/// 射速
		/// </summary>
		public abstract float FiringRate { get; }

		/// <summary>
		/// 总弹药数量
		/// </summary>
		public abstract int TotalAmmo { get; }

		/// <summary>
		/// 弹夹数量
		/// </summary>
		public abstract int Magazine { get; }

        /// <summary>
        /// 头部伤害
        /// </summary>
        public abstract int InjuryHead { get; }

        /// <summary>
        /// 身体伤害
        /// </summary>
        public abstract int InjuryBody { get; }

        /// <summary>
        /// 四肢伤害
        /// </summary>
        public abstract int InjuryLimbs { get; }

    }
}
