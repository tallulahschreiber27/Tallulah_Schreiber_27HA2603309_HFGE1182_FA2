using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float damage;

    private void OnCollisionEnter(Collision col)
    {
        switch (col.gameObject.tag)
        {
            case "Player":
                col.gameObject.GetComponent<ShootHandler>().AddAmmo(1);
                Destroy(gameObject);
                break;
            case "Enemy":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;
            case "NPC":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;
            case "Crate":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Arrow", damage);
                Destroy(gameObject);
                break;
            default:

                break;
        }
    }
}