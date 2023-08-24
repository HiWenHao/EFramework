/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-08-22 14:42:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-08-22 14:42:29
 * ScriptVersion: 0.1
 * ===============================================
*/

namespace AimGame
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class Standard : Gun
    {
        public override string Name => "标配";

        public override GunType GunsType => GunType.Pistol;

        public override string Description => "最普通的手枪";

        public override BFireType FireType => BFireType.Single;

        public override float FiringRate => 100;

        public override int TotalAmmo => 36;

        public override int Magazine => 12;

        public override int InjuryHead => 80;

        public override int InjuryBody => 55;

        public override int InjuryLimbs => 25;
    }
}
