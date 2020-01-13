using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnLogic : MonoBehaviour
{
    public Player player;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponentInParent<Player>();
    }
    //Lock-on logic lives in OnTriggerEnter/Exit
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.layer == 10 && !player.lockableEnemies.Contains(other.transform))
        {
            player.lockableEnemies.Add(other.transform);
            player.GetComponent<Megaman_Rig>().ToggleLookAtRig(other.transform);
        }
        else if (other.gameObject.layer == 14)
        {
            player.pointOfInterest = other.transform;
            player.GetComponent<Megaman_Rig>().ToggleLookAtRig(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (player.lockableEnemies.Contains(other.transform))
        {
            player.GetComponent<Megaman_Rig>().ToggleLookAtRig(null);
            player.lockableEnemies.Remove(other.transform);
            
        }
            
    }
}
