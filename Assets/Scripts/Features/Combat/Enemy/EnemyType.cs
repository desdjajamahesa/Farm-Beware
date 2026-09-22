namespace FeaturesCombat
{
    /// <summary>
    /// 6 Varian musuh hasil evolusi 3 bibit tanaman (Sweet Potato, Taro, Corn) sesuai panduan MVP.
    /// </summary>
    public enum EnemyType
    {
        // Sweet Potato Evolution
        TuberMaw,           // Normal: Lincah, burrow strike, tunnel rush
        CyclopsTuberMaw,    // Boss: Laser tracking beam, summon minion

        // Taro Evolution
        TaroBrute,          // Normal: Tangguh, knockback hit, root guard
        TaroColossus,       // Boss: Airborne slam, grapple slam

        // Corn Evolution
        CornMusketeer,      // Normal: Ranged, kernel shot, burst shot
        TheRanger           // Boss: Stationary turret, heavens fall, kernel burst
    }
}
