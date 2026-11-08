using Game.Configs;
using Game.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.View.Tests
{
    public sealed class LevelEnvironmentTests
    {
        private const string LevelPath = "Assets/_Project/Configs/Levels/Level_Gameplay.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/Environment/CastleCourtyardEnvironment.prefab";

        [Test]
        public void CastleLevelReferencesSerializedEnvironmentWithTerrainCollidersAndLight()
        {
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.NotNull(level);
            Assert.NotNull(prefab);
            Assert.AreSame(prefab, level.EnvironmentPrefab);
            Assert.AreEqual(3, prefab.transform.childCount);
            Assert.NotNull(prefab.transform.Find("Courtyard"));
            Assert.NotNull(prefab.transform.Find("Directional Light").GetComponent<Light>());

            Terrain terrain = prefab.transform.Find("CourtyardTerrain").GetComponent<Terrain>();
            TerrainCollider collider = prefab.transform.Find("CourtyardTerrain").GetComponent<TerrainCollider>();
            Assert.NotNull(terrain.terrainData);
            Assert.AreSame(terrain.terrainData, collider.terrainData);
            Assert.AreEqual(107, prefab.GetComponentsInChildren<Collider>(true).Length);
        }

        [Test]
        public void OwnerCreatesExactlyOneInstanceAndDisposeIsIdempotent()
        {
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelPath);
            LevelEnvironment owner = new LevelEnvironment(level);

            try
            {
                Assert.NotNull(owner.Instance);
                Assert.AreEqual(1, owner.Instance.GetComponentsInChildren<Terrain>(true).Length);
                Assert.AreEqual(1, owner.Instance.GetComponentsInChildren<Light>(true).Length);
                Assert.AreEqual(107, owner.Instance.GetComponentsInChildren<Collider>(true).Length);
            }
            finally
            {
                owner.Dispose();
                owner.Dispose();
            }

            Assert.IsNull(owner.Instance);
        }

        [Test]
        public void MissingPrefabCreatesNoInstance()
        {
            LevelConfig level = ScriptableObject.CreateInstance<LevelConfig>();

            try
            {
                using (LevelEnvironment owner = new LevelEnvironment(level))
                    Assert.IsNull(owner.Instance);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }
    }
}
