using UnityEngine;

/// <summary>
/// Simple helper to wire CloseButton onClick to KitchenSinkInteractable.ClosePanel().
/// Attached at runtime to Panel_Sink.
/// </summary>
public class SinkCloseButtonHandler : MonoBehaviour
{
    [HideInInspector] public KitchenSinkInteractable sinkInteractable;

    public void Close()
    {
        if (sinkInteractable != null)
            sinkInteractable.ClosePanel();
    }
}
