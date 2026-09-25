using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedHealthPickup : Pickup
{
    public enum HealthPickupState {Minor, Lesser, Greater}
    
    [Header("MECHANICS")]
    [SerializeField] private HealthPickupState state;
    [SerializeField] private float minorHealthMultiplier;
    [SerializeField] private float greaterHealthMultiplier;
    private bool recentStateChange = false;
    
    [Header("VISUALS")]
    [SerializeField] private GameObject minorVisuals;
    [SerializeField] private GameObject lesserVisuals;
    [SerializeField] private GameObject greaterVisuals;
    
    private void Update()
    {
        switch (state)
        {
            case HealthPickupState.Minor:
                minorVisuals.SetActive(true);
                lesserVisuals.SetActive(false);
                greaterVisuals.SetActive(false);
                break;
            case HealthPickupState.Lesser:
                minorVisuals.SetActive(false);
                lesserVisuals.SetActive(true);
                greaterVisuals.SetActive(false);
                break;
            case HealthPickupState.Greater:
                minorVisuals.SetActive(false);
                lesserVisuals.SetActive(false);
                greaterVisuals.SetActive(true);
                break;
        }
    }

    public void ChangeState()
    {
        if (recentStateChange)
        {
            return;
        }
        
        switch (state)
        {
            case HealthPickupState.Minor:
                break;
            case HealthPickupState.Lesser:
                state = HealthPickupState.Greater;
                StartCoroutine(StateChangeDelay());
                break;
            case HealthPickupState.Greater:
                state = HealthPickupState.Minor;
                StartCoroutine(StateChangeDelay());
                break;
            default:
                break;
        }
    }

    private IEnumerator StateChangeDelay()
    {
        recentStateChange = true;
        yield return new WaitForSecondsRealtime(1f);
        recentStateChange = false;
    }

    protected override void ApplyEffect(GameObject player)
    {
        switch (state)
        {
            case HealthPickupState.Minor:
                player.GetComponent<HealthHandler>().HealHandler(value * minorHealthMultiplier);
                break;
            case HealthPickupState.Lesser:
                player.GetComponent<HealthHandler>().HealHandler(value);
                break;
            case HealthPickupState.Greater:
                player.GetComponent<HealthHandler>().HealHandler(value * greaterHealthMultiplier);
                break;
        }
        
        DestroyPickup();
        Debug.Log("Heal");
    }
}
