using Game.Configs;
using Game.Core;
using NUnit.Framework;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.View.Tests
{
    public sealed class DemonLordViewTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/Bosses/DemonLord.prefab";
        private const string EnemyPath = "Assets/_Project/Configs/Enemies/Enemy_DemonLord.asset";
        private const string BossPath = "Assets/_Project/Configs/Enemies/Boss_DemonLord.asset";
        private const string LevelPath = "Assets/_Project/Configs/Levels/Level_TrollLair.asset";

        [Test]
        public void PrefabHasRootViewControlledAnimatorAndUrpMaterials()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EnemyConfig enemy = AssetDatabase.LoadAssetAtPath<EnemyConfig>(EnemyPath);
            BossConfig boss = AssetDatabase.LoadAssetAtPath<BossConfig>(BossPath);

            Assert.NotNull(prefab);
            Assert.AreSame(prefab, enemy.ViewPrefab);
            Assert.AreSame(boss, enemy.Boss);
            Assert.AreEqual("enemy.demon_lord", enemy.Id);
            Assert.AreEqual("boss.demon_lord", boss.Id);
            Assert.NotNull(prefab.GetComponent<ViewInterpolator>());
            Assert.AreEqual(1, prefab.transform.childCount);
            Assert.NotNull(PrefabUtility.GetCorrespondingObjectFromSource(prefab.transform.GetChild(0).gameObject));

            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.NotNull(animator);
            Assert.IsFalse(animator.applyRootMotion);
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.NotNull(controller);
            Assert.AreEqual("Idle", controller.layers[0].stateMachine.defaultState.name);
            Assert.IsTrue(HasParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool));
            Assert.IsTrue(HasParameter(controller, "Death", AnimatorControllerParameterType.Trigger));
            Assert.IsTrue(HasParameter(controller, "Attack1", AnimatorControllerParameterType.Trigger));
            Assert.IsTrue(HasParameter(controller, "Attack2", AnimatorControllerParameterType.Trigger));

            foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            {
                if (child.state.name == "Idle" || child.state.name == "Move")
                    Assert.IsTrue(((AnimationClip)child.state.motion).isLooping);

                if (child.state.name == "Death")
                    Assert.AreEqual(0, child.state.transitions.Length);
            }

            foreach (AnimatorStateTransition transition in controller.layers[0].stateMachine.anyStateTransitions)
                Assert.Greater(transition.conditions.Length, 0);

            foreach (Transform transform in prefab.GetComponentsInChildren<Transform>(true))
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    Assert.NotNull(material);
                    StringAssert.StartsWith("Universal Render Pipeline/", material.shader.name);
                }
            }
        }

        [Test]
        public void ExistingViewPoolCanCreateHitAndRetireDemonLord()
        {
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelPath);
            ContentDatabase database = AssetDatabase.LoadAssetAtPath<ContentDatabase>("Assets/_Project/Configs/ContentDatabase.asset");
            ViewPool pool = null;
            ViewInterpolator view = null;

            try
            {
                pool = new ViewPool(level, new ContentRegistry(database.Entries), null, null, null);
                view = (ViewInterpolator)pool.Create(level.Waves.Waves[2].Guaranteed[0].Enemy.ViewPrefab, Vector3.zero);

                Assert.NotNull(view);
                Assert.NotNull(view.GetComponentInChildren<Animator>(true));
                Assert.DoesNotThrow(() => view.PlayHit(Vector3.forward));
                Assert.DoesNotThrow(() => view.SetRunning(true));
                Assert.DoesNotThrow(() => pool.Retire(view, 4.5f, default));

                Animator animator = view.GetComponentInChildren<Animator>(true);
                animator.Update(0.1f);
                animator.Update(0.1f);
                Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Death"));
            }
            finally
            {
                if (view != null)
                    Object.DestroyImmediate(view.gameObject);

                if (pool != null)
                {
                    FieldInfo field = typeof(ViewPool).GetField("_ticker", BindingFlags.NonPublic | BindingFlags.Instance);
                    ViewPoolTicker ticker = (ViewPoolTicker)field.GetValue(pool);

                    if (ticker != null)
                        Object.DestroyImmediate(ticker.gameObject);
                }
            }
        }

        private static bool HasParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name && parameter.type == type)
                    return true;
            }

            return false;
        }
    }
}
