using UnityEditor;
using static Game.Editor.MixamoClipBaker;

namespace Game.Editor
{
    public static class HuntressClipBaker
    {
        private const string MenuPath = "Game/Bake Huntress Clips";
        private const int WholeClip = -1;

        public static readonly Profile Profile = new Profile(
            "Assets/_Project/Art/Animations/Huntress/",
            "Assets/_Project/Art/Animations/Huntress/Baked/",
            "Assets/_Project/Prefabs/Heroes/Huntress.prefab",
            "Assets/_Project/Art/Animations/Huntress/Huntress.controller",
            "Idle");

        public static readonly Entry[] Entries =
        {
            new Entry("Huntress_Idle", "standing idle 01.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_WalkForward", "standing walk forward.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_WalkBack", "standing walk back.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_WalkLeft", "standing walk left.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_WalkRight", "standing walk right.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_RunForward", "standing run forward.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_RunBack", "standing run back.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_RunLeft", "standing run left.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_RunRight", "standing run right.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_RunStop", "standing run forward stop.fbx", 0, WholeClip, Options.InPlace),
            new Entry("Huntress_AimWalkForward", "standing aim walk forward.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_AimWalkBack", "standing aim walk back.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_AimWalkLeft", "standing aim walk left.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_AimWalkRight", "standing aim walk right.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_TurnLeft", "standing turn 90 left.fbx", 0, WholeClip, Options.InPlace | Options.RemoveYaw),
            new Entry("Huntress_TurnRight", "standing turn 90 right.fbx", 0, WholeClip, Options.InPlace | Options.RemoveYaw),
            new Entry("Huntress_DrawArrow", "standing draw arrow.fbx", 0, WholeClip, Options.InPlace),
            new Entry("Huntress_AimHold", "standing aim overdraw.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_Release", "standing aim recoil.fbx", 0, WholeClip, Options.InPlace),
            new Entry("Huntress_Backflip", "Erika Archer@Backflip.fbx", 0, WholeClip, Options.InPlace),
            new Entry("Huntress_Jump", "Erika Archer@Jump.fbx", 0, WholeClip, Options.InPlace),
            new Entry("Huntress_JumpUp", "Erika Archer@Jumping Up.fbx", 0, WholeClip, Options.InPlace),
            new Entry("Huntress_FallLoop", "Erika Archer@Fall A Loop.fbx", 0, WholeClip, Options.InPlace | Options.Loop),
            new Entry("Huntress_Land", "Erika Archer@Fall A Land To Standing Idle 01.fbx", 0, WholeClip, Options.InPlace),
        };

        [MenuItem(MenuPath, false, 123)]
        public static void BakeAll()
        {
            MixamoClipBaker.BakeAll(Profile, Entries, nameof(HuntressClipBaker));
        }
    }
}
