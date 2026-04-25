using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    public float range = 3f;
    public Camera cam;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, range))
            {
                KeyObject key = hit.collider.GetComponent<KeyObject>();

                if (key != null)
                {
                    key.PickUp();
                }
            }
        }
    }
}