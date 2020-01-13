using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackCollision : MonoBehaviour
{
    public string Attack;
    private Enemy m_Enemy;


    private void Awake()
    {
        m_Enemy = transform.root.GetComponent<Enemy>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.name == "MegamanTrigger")
        {
            m_Enemy.GetComponent<Rig_Actions>().ToggleMove(other.transform, Attack);
            m_Enemy.ChangeSpeed(Attack);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.name == "MegamanTrigger")
        {
            m_Enemy.GetComponent<Rig_Actions>().ToggleMove(other.transform, Attack);
        }
    }
}
