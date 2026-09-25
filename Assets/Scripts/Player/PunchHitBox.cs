using System;
using UnityEngine;

public class PunchHitBox : MonoBehaviour
{
    [SerializeField] private float damage;
    private void OnTriggerEnter(Collider col)
    {
        switch (col.gameObject.tag)
        {
            case  "Enemy":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Punch", damage);
                break;
            case  "NPC":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Punch", damage);
                break;
            case "Crate":
                col.gameObject.GetComponent<HealthHandler>().DamageHandler("Punch", damage);
                break;
            default:
                break;
        }
    }
}
