using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ShieldDeflect : NetworkBehaviour {

    Vector2 velocityReflectDir;
    Vector2 velocityDir;
    float velocitySpeed;


    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Vector2 velocity = collision.GetComponent<Rigidbody2D>().velocity;
        velocityDir = velocity.normalized;
        velocitySpeed = velocity.magnitude;


    }
    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        velocityReflectDir = Vector2.Reflect(velocityDir, collision.contacts[0].normal);
        collision.rigidbody.velocity = velocityReflectDir * velocitySpeed;
    }
}
