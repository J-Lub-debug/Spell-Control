using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//!!! Server Callback everywhere, how to spawn the nested PlayerObjecto on client and server
//so the Summon would see them on Clients

//??? Should summons chase each other AND/OR be able to attack each other


public class SummonFollow : NetworkBehaviour {

    public Animator anim;

    public GameObject Master;

    public List<GameObject> listOfTargets;
    GameObject currentTarget;

    int damage = 20;

    float curTime = 0; //remaining time left in seconds to apply dmg
    float nextDamage = 2; //number of second to next dmg apply


    [Client]
    public void FlipSprite(bool state)
    {
        GetComponent<SpriteRenderer>().flipX = state;

        if (!isLocalPlayer) return;

        CmdFlipSprite(state);
    }

    [Command]
    void CmdFlipSprite(bool state)
    {
        GetComponent<SpriteRenderer>().flipX = state;
        RpcFlipSprite(state);
    }

    [ClientRpc]
    void RpcFlipSprite(bool state)
    {
        if (isLocalPlayer) return;

        GetComponent<SpriteRenderer>().flipX = state;
    }


    void Start () {
        anim = GetComponent<Animator>();
        FindTargets();
        StartCoroutine(LookingForTarget());
    }

    public void FindTargets()
    {
        List<GameObject> tempListOfTargets = new List<GameObject>(GameObject.FindGameObjectsWithTag("Player"));
        tempListOfTargets.RemoveAll(item => item == Master || item.transform.IsChildOf(Master.transform));
        listOfTargets = new List<GameObject>(tempListOfTargets);


    }
    /* //After Split/(?) We would need to pass reference of GameObject to the command
    public void AddNewTarget(GameObject Target)
    {
        Debug.Log(Target.name);
        if(Master != Target)
        {
            Debug.Log("Not my master");
            listOfTargets.Add(Target);
        }
    }*/

    IEnumerator LookingForTarget()
    {
        while (listOfTargets.Count > 0)
        {

            currentTarget = FindClosestTarget(listOfTargets);
            while (currentTarget != null)
            {
                currentTarget = FindClosestTarget(listOfTargets);
                yield return new WaitForSeconds(1f);
            }
            if (currentTarget == null)
            {
                listOfTargets.Remove(currentTarget);
            }
        }
    }

    GameObject FindClosestTarget(List<GameObject> list)
    {
        GameObject target = null;
        if (list.Count > 0) //? Do we need it if we check already check it in LookingForTarget()
        {
            float minDistance = Vector3.Distance(transform.position, list[0].transform.position); ;
            target = list[0];
            for (int i = 1; i < list.Count; i++)
            {
                float distance = Vector3.Distance(transform.position, list[i].transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = list[i];
                }
            }
            return target;
        }
        else
        {
            return null;
        }
    }


    void Update () {
        if(currentTarget != null) //Check wheter player is still alive
        {
            Vector2 direction = currentTarget.transform.position - transform.position; //!!!
            float speed = 0.5f;
            transform.Translate(direction.normalized * speed * Time.deltaTime);

            if (Vector2.Dot(direction, Vector2.right) < 0)
            {
                FlipSprite(false);
            }

            if (Vector2.Dot(direction, Vector2.left) < 0)
            {
                FlipSprite(true);
            }
            
        }
        

    }

 
    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (collision.gameObject.layer == 8)
        {
            //! velocity may get reduced to zero because its after the collision
            if(collision.GetComponent<Rigidbody2D>().velocity.magnitude >= 0) //If the object is moving
            {
                Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.GetComponents<BoxCollider2D>()[0], true);
                Vector2 throwDirection;
                if (currentTarget != null)
                {
                    throwDirection = (currentTarget.transform.position - transform.position).normalized;
                }
                else
                {
                    throwDirection = (-(collision.GetComponent<Rigidbody2D>().velocity)).normalized;
                }
                float throwSpeed = 0.5f;
                collision.GetComponent<Rigidbody2D>().velocity = throwDirection * throwSpeed;
                transform.GetComponent<Rigidbody2D>().velocity = Vector2.zero; //! It still hits the monster before he catch it, to detect collision, and because of that we lose velocity.
                //Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.GetComponent<Collider2D>(),false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 8)
        {
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), collision.GetComponents<BoxCollider2D>()[0], false);
        }
    }

    /* ------------------------------------------------------------------- Attack enemy Player ------------------------------------------------- */
    [ServerCallback]
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 9 && collision.gameObject.transform.parent != transform.parent)
        {
            /*------------------------------------Timer---------------------------------------*/
            if (curTime <= 0)
            {
                curTime = nextDamage;
                PlayerStatus playerStatus = collision.transform.GetComponent<PlayerStatus>();
                playerStatus.DamagePlayer(damage);
                anim.Play("SummonAttack"); //(?) Apply damage once animation is finished or at the very end 
            }
            else 
            {
                curTime -= Time.deltaTime;

            }
        }
    }
}
