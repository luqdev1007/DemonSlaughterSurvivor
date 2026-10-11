using Game.Simulation.Components;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace Game.Simulation.Systems
{
    public sealed class SyncLeapLiftViewSystem : IEcsRunSystem
    {
        private readonly EcsWorldInject _world = default;

        private readonly EcsFilterInject<Inc<Player, View>> _heroes = default;
        private readonly EcsFilterInject<Inc<HeroicLeap>> _leaps = default;

        private readonly EcsPoolInject<HeroicLeap> _leapPool = default;
        private readonly EcsPoolInject<View> _views = default;

        public void Run(IEcsSystems systems)
        {
            EcsWorld world = _world.Value;

            foreach (int hero in _heroes.Value)
            {
                ref View view = ref _views.Value.Get(hero);

                if (view.Value == null)
                    continue;

                view.Value.SetLift(ResolveLift(world, hero));
            }
        }

        private float ResolveLift(EcsWorld world, int hero)
        {
            foreach (int entity in _leaps.Value)
            {
                ref HeroicLeap leap = ref _leapPool.Value.Get(entity);

                if (leap.Owner.Unpack(world, out int owner) == false || owner != hero)
                    continue;

                return Arc(leap.Config.ArcHeight, leap.ElapsedTicks, leap.Config.LandingTick + 1);
            }

            return 0f;
        }

        public static float Arc(float height, int elapsedTicks, int landingTicks)
        {
            if (height <= 0f || landingTicks <= 0 || elapsedTicks <= 0 || elapsedTicks >= landingTicks)
                return 0f;

            float progress = (float)elapsedTicks / landingTicks;

            return 4f * height * progress * (1f - progress);
        }
    }
}
