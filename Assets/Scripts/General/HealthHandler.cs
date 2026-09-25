using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI; 

public class HealthHandler : MonoBehaviour
{
    [Header("HEALTH PARAMETERS")]
    [Tooltip("Add tags here to define what this character is vulnerable to. Leave empty to accept ALL damage.")]
    [SerializeField] private string[] damageTags;
    [SerializeField] private float health;
    [SerializeField] private float healthMax = 100f;

    [Header("UI ELEMENTS")]
    [Tooltip("Drag and drop your health bar Slider here!")]
    [SerializeField] private UnityEngine.UI.Slider healthSlider;

    [Header("HEALTHBAR POSITIONING")]
    [Tooltip("How high above the character's pivot point should the health bar float?")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.2f, 0f);
    private Transform canvasTransform;
    private Transform mainCameraTransform;

    [Header("DECAY PARAMETERS")]
    [SerializeField] private bool doesDecay = false;
    [SerializeField] private float decayTickRate = 1f;
    [SerializeField] private float decayDamage = 2f;
    private float decayTimer;

    [Header("DEATH PARAMETERS")]
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyOnDeathDelay = 0f;

    private bool isDead = false;

    void Start()
    {
        health = healthMax;

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<UnityEngine.UI.Slider>();
        }

        if (healthSlider != null)
        {
            Canvas parentCanvas = healthSlider.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                canvasTransform = parentCanvas.transform;
                canvasTransform.SetParent(null); 
            }
        }

        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        UpdateHealthBarUI();
    }

    void Update()
    {
        if (isDead) return;

        if (doesDecay)
        {
            decayTimer += Time.deltaTime;
            if (decayTimer >= decayTickRate)
            {
                DamageHandler(decayDamage);
                decayTimer = 0f;
            }
        }
    }

    void LateUpdate()
    {
        if (isDead || canvasTransform == null) return;

        canvasTransform.position = transform.position + healthBarOffset;

        if (mainCameraTransform != null)
        {
            canvasTransform.LookAt(canvasTransform.position + mainCameraTransform.rotation * Vector3.forward,
                                   mainCameraTransform.rotation * Vector3.up);
        }
    }

    private void UpdateHealthBarUI()
    {
        if (healthSlider != null)
        {
            
            healthSlider.value = Mathf.Clamp01(health / healthMax);
        }
    }

    public void DamageHandler(string damageTag, float damageAmount)
    {
        if (isDead) return;

        
        if (damageTag == "Arrow" || damageTag == "Enemy" || (damageTags != null && damageTags.Contains(damageTag)))
        {
            ApplyDamage(damageAmount);
        }
        else
        {
            Debug.Log($"[{gameObject.name}] blocked damage from tag: {damageTag} (Not in accepted list).");
        }
    }

    public void DamageHandler(float damageAmount)
    {
        if (isDead) return;

        ApplyDamage(damageAmount);
    }

    private void ApplyDamage(float amount)
    {
        if (amount <= health)
        {
            health -= amount;
        }
        else
        {
            health = 0;
        }

        UpdateHealthBarUI();

        if (health <= 0)
        {
            DeathHandler();
        }
    }

    public void HealHandler(float healAmount)
    {
        if (isDead) return;

        if (health + healAmount >= healthMax)
        {
            health = healthMax;
        }
        else
        {
            health += healAmount;
        }

        UpdateHealthBarUI();
    }

    public void DeathHandler()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[{gameObject.name}] has died.");

        
        if (canvasTransform != null)
        {
            Destroy(canvasTransform.gameObject);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyOnDeathDelay);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
