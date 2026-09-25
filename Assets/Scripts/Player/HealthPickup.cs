using UnityEngine;

public class HealthPickup : Pickup
{
    protected override void ApplyEffect(GameObject player)
    {
        Debug.Log("[DEBUG 9] Executing HealthPickup ApplyEffect on: " + player.name);

        HealthHandler health = player.GetComponent<HealthHandler>();

        if (health != null)
        {
            Debug.Log("[DEBUG 10] HealthHandler script found! Sending Heal value: " + value);
            health.HealHandler(value);
            DestroyPickup();
        }
        else
        {
            Debug.LogError("[DEBUG ERROR] The Player object has the 'Player' tag, but is completely missing the HealthHandler.cs script component!");
        }
    }
}
