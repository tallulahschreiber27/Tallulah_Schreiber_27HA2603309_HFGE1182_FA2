using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class ShootHandler : MonoBehaviour
{
    [Header("UI ELEMENTS")]
    [SerializeField] private TMP_Text ammoText;

    [Header("WEAPON VISUALS")]
    [SerializeField] private GameObject bowVisual;
    [SerializeField] private GameObject fistVisual;

    [Header("ACTIONS")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileForce = 20f;
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private float maxChargeTime = 1f;
    private bool isCharging = false;
    private float chargeTime = 0f;

    [Header("PUNCH SYSTEM CONFIGURATION")]
    [SerializeField] private GameObject punchHitBox;
    [SerializeField] private float punchDamage = 15f;

    [Header("AMMO SYSTEM CONFIGURATION")]
    [SerializeField] private int currentAmmo = 10;
    [SerializeField] private int maxAmmo = 10;
    [SerializeField] private int totalAmmo = 30;
    [SerializeField] private int maxReserveAmmo = 50;

    [Header("CAMERA")]
    [SerializeField] private Camera cam;
    [SerializeField] private float defaultCamFOV = 60f;
    [SerializeField] private float zoomCamFOV = 40f;
    private bool isZooming;

    private bool isPunching = false;
    private Coroutine punchCoroutine; 

    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    private void Start()
    {
        if (punchHitBox != null) punchHitBox.SetActive(false);
        CheckWeaponAndUIState();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isCharging && !isPunching)
        {
            if (currentAmmo > 0)
            {
                OnBeginShootManual();
            }
            else
            {
                punchCoroutine = StartCoroutine(PunchObject());
            }
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            OnShootManual();
        }

        if (Input.GetMouseButton(1) && currentAmmo > 0)
        {
            isZooming = true;
        }
        else
        {
            isZooming = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            OnReloadManual();
        }

        ZoomIn();
        ChargeShot();
    }

    private void CheckWeaponAndUIState()
    {
        bool bowIsReady = currentAmmo > 0;

        if (bowVisual != null) bowVisual.SetActive(bowIsReady);
        if (fistVisual != null) fistVisual.SetActive(!bowIsReady);

        if (ammoText != null)
        {
            if (bowIsReady)
            {
                ammoText.text = $"ARROWS: {currentAmmo}";
            }
            else
            {
                ammoText.text = "WEAPON: FISTS";
            }
        }
    }

    public void ChargeShot()
    {
        if (isCharging)
        {
            chargeTime = Mathf.MoveTowards(chargeTime, maxChargeTime, Time.deltaTime);
        }
    }

    private void OnBeginShootManual()
    {
        isCharging = true;
        chargeTime = 0f;
    }

    public void OnShootManual()
    {
        isCharging = false;

        if (currentAmmo <= 0)
        {
            CheckWeaponAndUIState();
            return;
        }

        currentAmmo--;
        CheckWeaponAndUIState();

        if (projectilePrefab != null && projectileSpawn != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileSpawn.position, projectileSpawn.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();

            if (rb != null)
            {
                float combinedCharge = Mathf.Max(0.2f, chargeTime / maxChargeTime);
                rb.AddForce(projectileSpawn.forward * projectileForce * combinedCharge, ForceMode.Impulse);
            }
        }
    }

    private void OnReloadManual()
    {
        if (currentAmmo == maxAmmo || totalAmmo <= 0)
        {
            Debug.Log("[RELOAD DENIED] Clip already full or no arrows in reserve pool.");
            return;
        }

        Debug.Log("Reloading Weapon...");

        if (isPunching)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            if (punchHitBox != null) punchHitBox.SetActive(false);
            isPunching = false;
        }

        isCharging = false;

        int ammoNeeded = maxAmmo - currentAmmo;

        if (totalAmmo >= ammoNeeded)
        {
            currentAmmo += ammoNeeded;
            totalAmmo -= ammoNeeded;
        }
        else
        {
            currentAmmo += totalAmmo;
            totalAmmo = 0;
        }

        Debug.Log($"[RELOAD SUCCESS] Clip: {currentAmmo} | Reserves Remaining: {totalAmmo}");

        CheckWeaponAndUIState();
    }

    public void AddAmmo(int amount)
    {
        if (totalAmmo >= maxReserveAmmo) return;

        totalAmmo += amount;

        if (totalAmmo > maxReserveAmmo)
        {
            totalAmmo = maxReserveAmmo;
        }

        Debug.Log($"[AMMO PICKUP] Gathered arrows. Total Reserves: {totalAmmo}");

        if (currentAmmo <= 0)
        {
            Debug.Log("[AUTO-RELOAD] Active weapon empty! Automatically drawing bow from new pickup items.");
            OnReloadManual();
        }
        else
        {
            CheckWeaponAndUIState();
        }
    }

    private void ZoomIn()
    {
        if (cam == null) return;
        float targetFOV = isZooming ? zoomCamFOV : defaultCamFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 10f);
    }

    public IEnumerator PunchObject()
    {
        isPunching = true;
        if (punchHitBox != null) punchHitBox.SetActive(true);

        Vector3 boxCenter = punchHitBox != null ? punchHitBox.transform.position : transform.position;
        Vector3 boxHalfExtents = Vector3.one * 0.5f;

        if (punchHitBox != null)
        {
            BoxCollider boxCol = punchHitBox.GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                boxHalfExtents = Vector3.Scale(boxCol.size, punchHitBox.transform.lossyScale) * 0.5f;
                boxCenter = boxCol.bounds.center;
            }
        }

        Quaternion boxRotation = punchHitBox != null ? punchHitBox.transform.rotation : transform.rotation;
        Collider[] hitColliders = Physics.OverlapBox(boxCenter, boxHalfExtents, boxRotation);

        foreach (Collider other in hitColliders)
        {
            if (other.transform.root == transform.root) continue;

            if (other.CompareTag("Enemy") || other.CompareTag("NPC") || other.CompareTag("Crate"))
            {
                HealthHandler targetHealth = other.GetComponent<HealthHandler>();
                if (targetHealth != null)
                {
                    targetHealth.DamageHandler(punchDamage);
                }
            }
        }

        yield return new WaitForSecondsRealtime(0.3f);

        if (punchHitBox != null) punchHitBox.SetActive(false);
        isPunching = false;
    }

    private void OnDrawGizmos()
    {
        if (punchHitBox != null)
        {
            Gizmos.color = Color.red;
            Matrix4x4 originalMatrix = Gizmos.matrix;
            Gizmos.matrix = punchHitBox.transform.localToWorldMatrix;

            BoxCollider boxCol = punchHitBox.GetComponent<BoxCollider>();
            if (boxCol != null) Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            else Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = originalMatrix;
        }
    }
}
