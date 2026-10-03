using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining dynamic world interaction label text update logic.
    /// </summary>
    public interface IDynamicLabelProvider
    {
        void UpdateDynamicLabel(GameObject interactor = null);
    }
}
