using UnityEngine;

public class KeyObject : MonoBehaviour
{
    public void PickUp()
    {
        GameManager.instance.CollectKey();
        Destroy(gameObject);
    }
}