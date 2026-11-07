using UnityEngine;

[CreateAssetMenu(fileName = "NewToolItem", menuName = "Inventory/Tool Item Data")]
public class ToolItemData : ItemData
{
    [Header("Visual Equipment")]
    public GameObject equipPrefab;
    public bool isWeapon;
    public bool isHoe;

    [Header("Combat Stats")]
    public int baseDamage = 25;
    public float knockbackForce = 6f;

    [Header("Ranged Weapon Settings")]
    [Tooltip("If enabled, this weapon fires projectiles (e.g. Corn Cob Repeater) instead of melee sweeps.")]
    public bool isRangedWeapon = false;

    [Tooltip("Projectile travel speed in units per second.")]
    public float projectileSpeed = 22f;

    [Tooltip("Effective firing range indicator radius.")]
    public float projectileRange = 12f;

    [Tooltip("Color of the launched projectile and point light.")]
    public Color projectileColor = new Color(1f, 0.85f, 0.2f, 1f);

    private static Sprite cachedSlingshotSprite;

    void OnEnable()
    {
        type = ItemType.Tool;
        category = ItemCategory.Tool;
        if (isRangedWeapon && itemIcon == null)
        {
            itemIcon = GetDefaultRangedSlingshotSprite();
        }
    }

    public static Sprite GetDefaultRangedSlingshotSprite()
    {
        if (cachedSlingshotSprite != null) return cachedSlingshotSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Sprite_HomesteadSlingshot",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color woodDark = new Color(0.36f, 0.21f, 0.09f, 1f);
        Color woodLight = new Color(0.58f, 0.36f, 0.16f, 1f);
        Color gripWrap = new Color(0.22f, 0.15f, 0.10f, 1f);
        Color bandGold = new Color(0.92f, 0.72f, 0.20f, 1f);
        Color pouchLeather = new Color(0.45f, 0.28f, 0.15f, 1f);
        Color ammoGlow = new Color(1.0f, 0.88f, 0.32f, 1f);

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

        DrawThickSegment(pixels, size, new Vector2(32f, 8f), new Vector2(32f, 28f), 4.2f, woodDark, woodLight);
        DrawThickSegment(pixels, size, new Vector2(28f, 14f), new Vector2(36f, 14f), 1.6f, gripWrap, gripWrap);
        DrawThickSegment(pixels, size, new Vector2(28f, 19f), new Vector2(36f, 19f), 1.6f, gripWrap, gripWrap);
        DrawThickSegment(pixels, size, new Vector2(28f, 24f), new Vector2(36f, 24f), 1.6f, gripWrap, gripWrap);
        DrawThickSegment(pixels, size, new Vector2(32f, 27f), new Vector2(16f, 48f), 3.6f, woodDark, woodLight);
        DrawThickSegment(pixels, size, new Vector2(32f, 27f), new Vector2(48f, 48f), 3.6f, woodDark, woodLight);
        DrawThickSegment(pixels, size, new Vector2(16f, 47f), new Vector2(32f, 56f), 1.8f, bandGold, bandGold);
        DrawThickSegment(pixels, size, new Vector2(48f, 47f), new Vector2(32f, 56f), 1.8f, bandGold, bandGold);
        DrawCircle(pixels, size, new Vector2(32f, 56f), 4.5f, pouchLeather);
        DrawCircle(pixels, size, new Vector2(32f, 56f), 2.8f, ammoGlow);

        tex.SetPixels(pixels);
        tex.Apply(false, false);

        cachedSlingshotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        cachedSlingshotSprite.name = "Icon_HomesteadSlingshot";
        return cachedSlingshotSprite;
    }

    private static void DrawThickSegment(Color[] pixels, int size, Vector2 a, Vector2 b, float radius, Color edgeCol, Color coreCol)
    {
        Vector2 ab = b - a;
        float lenSq = Mathf.Max(0.0001f, ab.sqrMagnitude);
        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - radius - 1));
        int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + radius + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - radius - 1));
        int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + radius + 1));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
                Vector2 proj = a + ab * t;
                float dist = Vector2.Distance(p, proj);
                if (dist <= radius)
                {
                    float blend = 1f - (dist / radius);
                    pixels[y * size + x] = Color.Lerp(edgeCol, coreCol, blend * 0.75f);
                }
            }
        }
    }

    private static void DrawCircle(Color[] pixels, int size, Vector2 center, float radius, Color col)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 1));
        int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius + 1));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 1));
        int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius + 1));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                if (Vector2.Distance(p, center) <= radius)
                {
                    pixels[y * size + x] = col;
                }
            }
        }
    }
}
