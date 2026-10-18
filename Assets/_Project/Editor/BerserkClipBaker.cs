using UnityEditor;
using static Game.Editor.MixamoClipBaker;

namespace Game.Editor
{
    public static class BerserkClipBaker
    {
        private const string MenuPath = "Game/Bake Berserk Clips";

        public static readonly Profile Profile = new Profile(
            "Assets/_Project/Art/Animations/Berserk/",
            "Assets/_Project/Art/Animations/Berserk/Baked/",
            "Assets/_Project/Prefabs/Heroes/Berserk.prefab",
            "Assets/_Project/Art/Animations/Berserk/Berserk.controller",
            "Idle");

        public static readonly Entry[] Entries =
        {
            new Entry("Berserk_Charge", "Charge_FinalSlash_Blend.fbx", 0, 18, Options.InPlace),
            new Entry("Berserk_DashMove", "Charge_FinalSlash_Blend.fbx", 18, 27, Options.InPlace | Options.Recenter),
            new Entry("Berserk_FinalSlash", "Charge_FinalSlash_Blend.fbx", 71, 91, Options.InPlace | Options.Recenter),
            new Entry("Berserk_ChargeRun", "ChareRun_Blend.fbx", 0, 18, Options.InPlace | Options.Loop),
            new Entry("Berserk_DashFinalAttack", "Dash_FinalAttack_Archers_PERK.fbx", 26, 56, Options.InPlace | Options.Recenter),
            new Entry("Berserk_ExecutionExtra", "ExecutionExtraAnim.fbx", 0, 65, Options.InPlace),
            new Entry("Berserk_HitReact", "Hit_React_v1.fbx", 0, 23, Options.None),
            new Entry("Berserk_SlashDownUp", "SlashFromDownToUp.fbx", 0, 35, Options.InPlace),
            new Entry("Berserk_SpinLoop", "SpinAttackLoop_Need_Cut.fbx", 3, 51, Options.InPlace | Options.Loop),
            new Entry("Berserk_SpinDownUpSlash", "SpinDownToUpSlashAttack.fbx", 0, 54, Options.InPlace),
            new Entry("Berserk_SpinPowerSlashFlame", "SpinPowerSlashRightAttack_Flame.fbx", 0, 54, Options.InPlace),
            new Entry("Berserk_StrafeLeft", "Strafe_new.fbx", 0, 33, Options.InPlace | Options.Loop),
            new Entry("Berserk_StrafeRight", "Strafe_new.fbx", 0, 33, Options.InPlace | Options.Loop | Options.Mirror),
            new Entry("Berserk_TurnRight", "Turn.fbx", 0, 22, Options.InPlace | Options.RemoveYaw),
            new Entry("Berserk_TurnLeft", "Turn.fbx", 0, 22, Options.InPlace | Options.RemoveYaw | Options.Mirror),
        };

        [MenuItem(MenuPath, false, 122)]
        public static void BakeAll()
        {
            MixamoClipBaker.BakeAll(Profile, Entries, nameof(BerserkClipBaker));
        }
    }
}
