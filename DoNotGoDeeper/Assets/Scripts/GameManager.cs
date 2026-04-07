using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int keysCollected = 0;
    public int keysRequired = 1;

    void Awake()
    {
        instance = this;
    }

    public void CollectKey()
    {
        keysCollected++;
        Debug.Log("Keys: " + keysCollected + "/" + keysRequired);
    }

    public bool HasEnoughKeys()
    {
        return keysCollected >= keysRequired;
    }
}