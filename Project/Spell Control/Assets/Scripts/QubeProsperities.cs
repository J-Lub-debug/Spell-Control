using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class QubeProsperities : NetworkBehaviour {

    int damage = 50;
    float curTime = 0;
    float nextDamage = 2;

    [Command]
    void CmdDestroyObject()
    {
        NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 9 && GetComponent<Rigidbody2D>().velocity.magnitude > 0.3f) 
        {
            if (curTime <= 0)
            {
                curTime = nextDamage;
                PlayerStatus playerStatus = collision.transform.GetComponent<PlayerStatus>();
                playerStatus.DamagePlayer(damage);
            }
            else
            {
                curTime -= Time.deltaTime;

            }

        }
        else if(collision.gameObject.layer == 10)
        {
            CmdDestroyObject();
        }
    }
}
