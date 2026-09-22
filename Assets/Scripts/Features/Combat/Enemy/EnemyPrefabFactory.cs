using UnityEngine;

namespace FeaturesCombat
{
    /// <summary>
    /// Factory builder untuk membangkitkan 6 varian musuh di arena pertempuran malam hari (Night Brawl).
    /// Mengonfigurasi visual primitif bertekstur/material khusus, collider, Rigidbody, dan komponen EnemyBase.
    /// </summary>
    public static class EnemyPrefabFactory
    {
        public static GameObject CreateEnemy(EnemyType type, Vector3 spawnPosition)
        {
            GameObject go;
            Color bodyColor;
            Vector3 scale;

            switch (type)
            {
                case EnemyType.TuberMaw:
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "Enemy_TuberMaw";
                    scale = new Vector3(0.9f, 0.9f, 0.9f);
                    bodyColor = new Color(0.55f, 0.15f, 0.35f); // Dark Yam / Tuber Purple
                    break;

                case EnemyType.CyclopsTuberMaw:
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "Boss_CyclopsTuberMaw";
                    scale = new Vector3(2.2f, 2.4f, 2.2f);
                    bodyColor = new Color(0.40f, 0.05f, 0.20f); // Menacing Deep Blood Purple
                    break;

                case EnemyType.TaroBrute:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Enemy_TaroBrute";
                    scale = new Vector3(1.2f, 1.5f, 1.2f);
                    bodyColor = new Color(0.45f, 0.35f, 0.30f); // Muddy Taro Earth
                    break;

                case EnemyType.TaroColossus:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Boss_TaroColossus";
                    scale = new Vector3(2.6f, 3.2f, 2.6f);
                    bodyColor = new Color(0.30f, 0.25f, 0.25f); // Heavy Granite Brown
                    break;

                case EnemyType.CornMusketeer:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = "Enemy_CornMusketeer";
                    scale = new Vector3(0.7f, 1.4f, 0.7f);
                    bodyColor = new Color(0.95f, 0.80f, 0.15f); // Golden Corn Yellow
                    break;

                case EnemyType.TheRanger:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = "Boss_TheRanger";
                    scale = new Vector3(1.8f, 3.5f, 1.8f);
                    bodyColor = new Color(0.90f, 0.65f, 0.10f); // Vibrant Amber Tower
                    break;

                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    go.name = "Enemy_Default";
                    scale = Vector3.one;
                    bodyColor = Color.red;
                    break;
            }

            go.transform.position = spawnPosition;
            go.transform.localScale = scale;
            go.tag = "Untagged";

            // Atur material
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                mat.color = bodyColor;
                renderer.material = mat;
            }

            // Atur Rigidbody
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
            }
            rb.mass = type.ToString().Contains("Boss") || type == EnemyType.TaroColossus || type == EnemyType.TheRanger ? 50f : 5f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Pasang EnemyBase dan inisialisasi stats
            var enemyBase = go.AddComponent<EnemyBase>();
            enemyBase.enemyType = type;
            enemyBase.InitializeStatsByType();

            return go;
        }
    }
}
