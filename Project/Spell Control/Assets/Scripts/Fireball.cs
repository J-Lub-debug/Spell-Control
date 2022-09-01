using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//Colission between fireballs: Ignored

public class Fireball : NetworkBehaviour
{
    int damage, startingDamage;

    Vector2 startingPosition;

    Vector3 startingLocalScale;

    Vector3 scalingSize;
    float scalingRate; 

    float distanceTraveled;
    float range = 2f;

    int n = 1;

    Vector2 hitDir = Vector2.zero;

    private void Start()
    {
        startingPosition = transform.position;
        startingLocalScale = transform.localScale;

        scalingSize = new Vector3(0.02f, 0.02f, 1f);
        scalingRate = 0.2f;
        startingDamage = damage = 50;
    }

    [Command]
    void CmdDestroyObject()
    {
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    void CmdRefreshFireball()
    {
        RpcRefreshFireball();
    }

    [ClientRpc]
    void RpcRefreshFireball()
    {
        startingPosition = transform.position;
        n = 1;
        transform.localScale = startingLocalScale;
        damage = startingDamage;
        //GetComponent<SpriteRenderer>().flipX = !(GetComponent<SpriteRenderer>().flipX);
        float rotation = Vector2.Angle(GetComponent<Rigidbody2D>().velocity.normalized, hitDir.normalized);
        if (transform.rotation.z < 0)
            rotation = -rotation;
        transform.Rotate(0, 0, rotation);
        hitDir = Vector2.zero;
    }


    
    private void FixedUpdate()
    {
        Vector2 currentPosition = transform.position;
        distanceTraveled = (float)(currentPosition - startingPosition).magnitude;
        if(distanceTraveled > n * scalingRate)
        {

            n++;
            damage = damage - 5;
            transform.localScale = transform.localScale - scalingSize;
        }


        if(distanceTraveled > range)
        {
            CmdDestroyObject();
            
        }
    }

    [ServerCallback] 
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.transform.GetComponent<PlayerStatus>())
        {

            PlayerStatus playerStatus = collision.transform.GetComponent<PlayerStatus>();
            playerStatus.DamagePlayer(damage);
           
        }
        if(collision.gameObject.tag != "Deflect" || hitDir == Vector2.zero) //If it hits the shield it won't destroy on impact
        {
            CmdDestroyObject();
        }
        else if(hitDir != Vector2.zero) //If it hits the shield it will regain its power as if player casted a new fireball from the position of shield
        {

            CmdRefreshFireball();
        }
    }

    [ServerCallback]

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Deflect")
        {
            Vector2 velocity = GetComponent<Rigidbody2D>().velocity;
            hitDir = velocity;
        }
    }



}
