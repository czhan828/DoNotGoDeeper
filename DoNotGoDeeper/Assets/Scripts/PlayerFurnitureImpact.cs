using UnityEngine;

/// <summary>
/// PlayerFurnitureImpact — Bridges CharacterController and NoiseObject.
///
/// CharacterController doesn't fire OnCollisionEnter on other objects,
/// so NoiseObject can't detect player contact directly. This script
/// listens to OnControllerColliderHit (which CharacterController DOES fire)
/// and forwards the hit to any NoiseObject on the touched object.
///
/// Attach to FPSController (same GameObject as CharacterController).
/// </summary>
public class PlayerFurnitureImpact : MonoBehaviour
{
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        NoiseObject noise = hit.gameObject.GetComponent<NoiseObject>();
        if (noise != null)
            noise.TriggerNoise();
    }
}
