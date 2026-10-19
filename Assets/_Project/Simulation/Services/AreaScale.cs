using Game.Core;
using UnityEngine;

namespace Game.Simulation.Services
{
    public static class AreaScale
    {
        public static IView Apply(IView view, float scale)
        {
            if (view is IAreaView area && Mathf.Abs(scale - 1f) > 0.001f)
                area.SetAreaScale(scale);

            return view;
        }
    }
}
