namespace FeaturesCombat
{
    /// <summary>
    /// 6 enemy variants evolved from 3 crop seeds (Sweet Potato, Taro, Corn) per MVP specification.
    /// </summary>
    public enum EnemyType
    {
        // Sweet Potato Evolution
        TuberMaw,           // Normal: Agile, burrow strike, tunnel rush
        CyclopsTuberMaw,    // Boss: Laser tracking beam, summon minion

        // Taro Evolution
        TaroBrute,          // Normal: Tough, knockback hit, root guard
        TaroColossus,       // Boss: Airborne slam, grapple slam

        // Corn Evolution
        CornMusketeer,      // Normal: Ranged, kernel shot, burst shot
        TheRanger           // Boss: Stationary turret, heavens fall, kernel burst
    }
}
