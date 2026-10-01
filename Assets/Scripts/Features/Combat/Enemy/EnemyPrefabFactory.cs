using System.Collections.Generic;
using UnityEngine;

namespace FeaturesCombat
{
    /// <summary>
    /// Factory builder for creating and configuring enemy instances for the Night Brawl combat phase.
    /// Strictly adheres to the Zero-Runtime-Material-Duplication rule to maintain GPU Resident Drawer
    /// and BatchRendererGroup (BRG) instancing efficiency.
    /// </summary>
    public static class EnemyPrefabFactory
    {
        private static readonly Dictionary<EnemyType, GameObject> _prefabCache = new Dictionary<EnemyType, GameObject>();

        public static string GetPrefabResourceName(EnemyType type)
        {
            return type switch
            {
                EnemyType.TuberMaw => "Enemy_TuberMaw",
                EnemyType.CyclopsTuberMaw => "Boss_CyclopsTuberMaw",
                EnemyType.TaroBrute => "Enemy_TaroBrute",
                EnemyType.TaroColossus => "Boss_TaroColossus",
                EnemyType.CornMusketeer => "Enemy_CornMusketeer",
                EnemyType.TheRanger => "Boss_TheRanger",
                _ => "Enemy_TuberMaw"
            };
        }

        public static GameObject CreateEnemy(EnemyType type, Vector3 spawnPosition)
        {
            if (!_prefabCache.TryGetValue(type, out GameObject prefab) || prefab == null)
            {
                string resourceName = GetPrefabResourceName(type);
                prefab = Resources.Load<GameObject>($"Enemies/{resourceName}");
                if (prefab != null)
                {
                    _prefabCache[type] = prefab;
                }
            }

            GameObject instance;

            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);
                instance.name = prefab.name;
            }
            else
            {
                Debug.LogWarning($"[EnemyPrefabFactory] Resource prefab not found for {type}. Spawning primitive fallback.");
                instance = CreateFallbackPrimitive(type, spawnPosition);
            }

            // Ensure EnemyBase component and stats are present
            var enemyBase = instance.GetComponent<EnemyBase>();
            if (enemyBase == null)
            {
                enemyBase = instance.AddComponent<EnemyBase>();
                enemyBase.enemyType = type;
                enemyBase.InitializeStatsByType();
            }

            return instance;
        }

        private static GameObject CreateFallbackPrimitive(EnemyType type, Vector3 spawnPosition)
        {
            PrimitiveType primitive = (type == EnemyType.TaroBrute || type == EnemyType.TaroColossus)
                ? PrimitiveType.Cube
                : (type == EnemyType.CornMusketeer || type == EnemyType.TheRanger)
                    ? PrimitiveType.Cylinder
                    : PrimitiveType.Capsule;

            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = GetPrefabResourceName(type);
            go.transform.position = spawnPosition;

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass = (type == EnemyType.CyclopsTuberMaw || type == EnemyType.TaroColossus || type == EnemyType.TheRanger) ? 50f : 5f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            return go;
        }
    }
}
