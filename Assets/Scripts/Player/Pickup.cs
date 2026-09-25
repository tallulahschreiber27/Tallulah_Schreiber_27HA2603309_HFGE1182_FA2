using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    [SerializeField] protected int value = 25;
    protected bool inRange = false;
    protected GameObject player;

    public void Collect()
    {
        Debug.Log("[DEBUG 6] Pickup Collect() method called. inRange = " + inRange + ", player = " + (player != null ? player.name : "NULL"));

        if (inRange && player != null)
        {
            ApplyEffect(player);
        }
    }

    protected virtual void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            inRange = true;
            player = col.gameObject;
            Debug.Log("[DEBUG 7] Pickup trigger entered by valid Player tag: " + col.gameObject.name);
        }
    }

    protected virtual void OnTriggerExit(Collider col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            inRange = false;
            player = null;
            Debug.Log("[DEBUG 8] Pickup trigger exited by Player tag: " + col.gameObject.name);
        }
    }

    protected abstract void ApplyEffect(GameObject player);

    protected virtual void DestroyPickup()
    {
        Destroy(gameObject);
    }
}
