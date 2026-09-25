using System;
using System.Collections;
using UnityEngine;

public class CampFire : MonoBehaviour
{
   [SerializeField] private float cookTime;
   
   private void OnTriggerEnter(Collider col)
   {
      if (col.GetComponent<AdvancedHealthPickup>() != null)
      {
         StartCoroutine(CookMeat(col.gameObject));
      }
   }

   private IEnumerator CookMeat(GameObject meat)
   {
      yield return new WaitForSeconds(cookTime);
      meat.GetComponent<AdvancedHealthPickup>().ChangeState();
   }
}
