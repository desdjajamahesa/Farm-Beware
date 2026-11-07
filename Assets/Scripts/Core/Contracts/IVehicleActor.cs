using UnityEngine;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining vehicle kinematic manipulation, passenger seating,
    /// and exterior lighting control across asmdef boundaries.
    /// </summary>
    public interface IVehicleActor
    {
        bool IsDriverOccupied { get; }
        void EnterVehicle(GameObject player);
        void ExitVehicle();
        void Teleport(Vector3 position, Quaternion rotation);
        void SetHeadlights(bool enable);
    }
}
