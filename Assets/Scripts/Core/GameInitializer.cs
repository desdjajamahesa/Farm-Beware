using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        FarmBeware.Core.Runtime.CoreBootstrapper.InitializeCoreSubsystems();
    }

    void Start()
    {
        Debug.Log("[GameInitializer] Systems initialized and verified.");
    }
}
