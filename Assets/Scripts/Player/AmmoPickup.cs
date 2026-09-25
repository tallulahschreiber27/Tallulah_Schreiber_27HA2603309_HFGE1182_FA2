using UnityEngine;

public class AmmoPickup:Pickup
{
    protected override void ApplyEffect(GameObject player)
    {
        player.GetComponent<ShootHandler>().AddAmmo(value);
        Debug.Log("Ammo");
        DestroyPickup();
    }
}
