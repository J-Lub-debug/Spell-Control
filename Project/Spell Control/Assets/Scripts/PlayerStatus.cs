using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerStatus : NetworkBehaviour {

    public GameObject StatuePrefab;

    public RectTransform healthBar;
    

    [SyncVar(hook = "OnChangeHealth")]
    public int _health = 100;


    public float walkingSpeed = 1.5f;
    public float walkingSpeedDecreaseRate;

    [SyncVar]
    public float petrificationProgression = 0;

    float petrificationProgressionRate;

    public void Start()
    {
        petrificationProgressionRate = 10.0f;
        walkingSpeedDecreaseRate = walkingSpeed * 1.0f / petrificationProgressionRate;

    }

    public void OnChangeHealth(int health)
    {
        if(isLocalPlayer)
        {
            healthBar.sizeDelta = new Vector2(health*2, healthBar.sizeDelta.y);
            _health = health;
        }
    }

    public void DamagePlayer(int dmg)
    {
        _health = _health - dmg;


        if (_health <= 0)
        {
            //Play some animation
            Die();
        }
    }

    public void Petrification()
    {

        petrificationProgression += petrificationProgressionRate;
        if (walkingSpeed > 0)
        {
            walkingSpeed -= walkingSpeedDecreaseRate;
        }
        else if (walkingSpeed < 0)
        {
            walkingSpeed = 0;
        }
        if (petrificationProgression >= 100)
        {
            Vector2 pos = transform.position;
            CmdSpawnStatue(pos);
            Die();

        }



    }

    void Die()
    {
        CmdDie();
    }

    [Command]
    void CmdDie()
    {
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    void CmdSpawnStatue(Vector2 pos)
    {
        GameObject Statue = Instantiate(StatuePrefab, pos, Quaternion.identity);
        NetworkServer.Spawn(Statue);
    }

}
