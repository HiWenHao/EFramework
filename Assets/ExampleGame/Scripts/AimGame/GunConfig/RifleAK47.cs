/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-22 14:28:27
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-22 14:28:27
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace AimGame
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class RifleAK47 : Gun
	{
        public override string Name => "AK47-A";

        public override GunType GunsType => GunType.Rifle;

        public override BFireType FireType => BFireType.Sustained;

        public override string Description => "一款高伤害的步枪，不过后座力很大";

        public override int FiringRate => 77;

        public override int TotalAmmo => 75;

        public override int Magazine => 25;

        public override int InjuryHead => 165;

        public override int InjuryBody => 165;

        public override int InjuryLimbs => 165;
    }
}
