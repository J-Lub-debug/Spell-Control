using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class DuplicateMimic : NetworkBehaviour
{
    SpriteRenderer masterSprite;

    private Animator anim;
    public Animator masterAnim;

    public GameObject Master;
    float walkSpeed;
    Vector3 walkDirection;


    void flipSprite()
    {
        bool flipState = masterSprite.flipX;
        //GetComponent<SpriteRenderer>().flipX;
        CmdFlipSprite(flipState);
    }

    [Command]
    void CmdFlipSprite(bool state)
    {
        //GetComponent<SpriteRenderer>().flipX = state;
        RpcFlipSprite(state);
    }

    [ClientRpc]
    void RpcFlipSprite(bool state)
    {
        //if (isLocalPlayer) return;

        GetComponent<SpriteRenderer>().flipX = state;
    }

    public string getCurrentAnimationState(Animator animator)
    {
        AnimatorClipInfo[] clipInfo;
        clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        return clipInfo[0].clip.name;
    }

    private void Start()
    {
        masterSprite = Master.GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        masterAnim = Master.GetComponent<Animator>();
    }

    private void Update()
    {
  
        walkSpeed = GetComponent<PlayerStatus>().walkingSpeed;
        walkDirection = Master.GetComponent<PlayerMovement>().walkDirection;

        if (Master != null)
        {
            transform.GetComponent<Rigidbody2D>().MovePosition(transform.position + (walkDirection.normalized * walkSpeed) * Time.deltaTime);
            anim.Play(getCurrentAnimationState(masterAnim));
            flipSprite();
        }
    }
}
