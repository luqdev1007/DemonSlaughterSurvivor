using Game.Configs;
using Game.Core;
using Game.Simulation.Components;
using Leopotam.EcsLite;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class ArrowExplosion
    {
        private const float FullCircleDegrees = 180f;

        public static float Chance(ExplosiveArrowConfig config, ExplosiveArrowLevel entry, bool demonic)
        {
            return demonic ? Mathf.Min(1f, entry.Chance * config.DemonicChanceScale) : entry.Chance;
        }

        public static void Detonate(
            EcsWorld world,
            EcsPackedEntity owner,
            ExplosiveArrowConfig config,
            ExplosiveArrowLevel entry,
            bool demonic,
            float baseDamage,
            Vector3 at,
            EcsPool<AreaStrike> strikes,
            EcsPool<LeapImpactFx> impacts,
            EcsPool<Position> positions,
            EcsPool<NotIndexed> notIndexed,
            EcsPool<View> views,
            IViewFactory viewFactory,
            float delta)
        {
            ref AreaStrike strike = ref strikes.Add(world.NewEntity());
            strike.Owner = owner;
            strike.Origin = at;
            strike.Forward = Vector3.forward;
            strike.Reach = config.Radius * (demonic ? config.DemonicRadiusScale : 1f);
            strike.HalfAngleDegrees = FullCircleDegrees;
            strike.Damage = baseDamage * entry.DamageScale * (demonic ? config.DemonicDamageScale : 1f);
            strike.Kind = DamageKind.Perk;
            strike.SourcePerk = config;
            strike.PushSpeed = config.PushSpeed;
            strike.PushSeconds = config.PushSeconds;
            strike.PushArcHeight = config.PushArcHeight;

            int fx = world.NewEntity();
            impacts.Add(fx).RemainingTicks = Mathf.Max(1, Mathf.RoundToInt(config.FxSeconds / delta));
            positions.Add(fx).Value = at;
            notIndexed.Add(fx);
            views.Add(fx).Value = viewFactory.Create(config.FxPrefab, at);
        }
    }
}
