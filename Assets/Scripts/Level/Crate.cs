using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Crate : MonoBehaviour
{
    [SerializeField] private List<GameObject> itemTable;
    [SerializeField] private List<float> chanceTable;
    private List<float> chance;

    private void Start()
    {
        chance = new List<float>();
        int i = 0;
        foreach (var percentile in chanceTable)
        {
            chance.Add((percentile/chanceTable.Sum()) * 100);
            i++;
        }
    }

    public void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            bool itemDropped = false;
            int i = 0;
            foreach (GameObject item in itemTable)
            {
                int rnd = UnityEngine.Random.Range(0, 100);
            
                if (chance[i] >= rnd)
                {
                    itemDropped = true;
                    Instantiate(item, new Vector3(transform.position.x, transform.position.y + 1, transform.position.z), Quaternion.identity);
                    break;
                }

                i++;
            }

            if (itemDropped)
            {
                Debug.Log("You found something");
            }
            else
            {
                Debug.Log("No items dropped");
            }
           
        }
        
    }
}
