using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

//(*) Odwolan do GameObject nie mozna przesylac do komend, ale mozna do RPC z komend (W komendach mozna za to uzyc zbiorow zeby trzymac odniesienia)

public class PlayerMovement : NetworkBehaviour
{
    public Animator anim;

    public Canvas canvas;
    List<Button> buttons = new List<Button>();
    Button previousButton;
    public RectTransform manaBar;

    public int Mana
    {
        get
        {
            return mana;
        }

        set
        {
            mana = value;
            manaBar.sizeDelta = new Vector2(mana * 2, manaBar.sizeDelta.y);
        }
    }

    bool controlEnabled = true;

    public GameObject playerObjects;

    GameObject mainCamera;
    float cameraOffset = 0.5f;

    public GameObject PlayerDuplicate;

    public GameObject CrosshairPrefab;
    GameObject Crosshair;


    public GameObject QubePrefab;

    public GameObject FireballPrefab;

    public GameObject SummonPrefab;
    public GameObject ShieldPrefab;

    int spellChoice = 0;

    float walkSpeed;
    public Vector3 walkDirection;

    float fireballSpeed = 1.5f; //> Test-Default: 0.5f
    float lastPetrificationTime = 0;


    Vector2 PlayerPosition;
    Vector2 CursorPosition;
    Vector2 direction;

    Vector2 crosshairSpawnPoint = new Vector2(0.3f, 0.3f);

    Vector2 CursorStartPosition;

    List<Spell> Spells;
    public int maxMana;
    public int mana;
    public bool isRegenMana;



    private void Awake()
    {

        GetComponent<FieldOfView>().enabled = false;
        InitalizeSpellsCooldowns();
        maxMana = 100;
        mana = maxMana;



    }

    public override void OnStartLocalPlayer()
    {
        InstantiateLocalObjects();
        anim = GetComponent<Animator>();

        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);


    }


    void InstantiateLocalObjects()
    {
        Vector2 crossHairStartPos = transform.position + (Vector3)crosshairSpawnPoint;
        Crosshair = Instantiate(CrosshairPrefab, crossHairStartPos, Quaternion.identity);

        Canvas can = Instantiate(canvas);
        can.transform.SetParent(transform.parent);
        GetComponent<PlayerStatus>().healthBar = can.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
        manaBar = can.transform.GetChild(1).GetChild(0).GetComponent<RectTransform>();


        List<GameObject> buttonObjs = new List<GameObject>(GameObject.FindGameObjectsWithTag("Button"));

        //FindGameObject doesn't return objects in any specific order
        buttonObjs.Sort((e1, e2) => e1.transform.GetSiblingIndex().CompareTo(e2.transform.GetSiblingIndex()));


        foreach (var buttonObj in buttonObjs)
        {
            buttons.Add(buttonObj.GetComponent<Button>());
        }

        ColorBlock cb = buttons[0].colors;
        cb.disabledColor = Color.white;
        buttons[0].colors = cb;

        previousButton = buttons[0];



    }




    /*private void OnDestroy()
    {
        Destroy(transform.parent.gameObject);
        Destroy(Crosshair); 
    }*/



    IEnumerator ManaRegen()
    {
        isRegenMana = true;
        float delay = 0.5f;
        while (mana < maxMana)
        {
            yield return new WaitForSeconds(delay);
            if (mana + 5f <= maxMana)
            {
                Mana = mana + 5;
            }
            else
            {
                Mana = maxMana;
            }
        }
        isRegenMana = false;
    }



    void Start()
    {
        Cursor.visible = true;
    }

    class Spell
    {
        public int id;
        public int manaCost;
        public float castingTime;


        public Spell(int id, int manaCost, float castingTime)
        {
            this.id = id;
            this.manaCost = manaCost;
            this.castingTime = castingTime;
        }
    }

    void InitalizeSpellsCooldowns()
    {
        Spells = new List<Spell>() {
            new Spell(0, 10, 0.5f),
            new Spell(1, 10, 1f),
            new Spell(2, 10, 0.5f),
            new Spell(3, 10, 1),
            new Spell(4, 10, 1.75f),
            new Spell(5, 10, 0),
            new Spell(6, 10, 3),
            new Spell(7, 10, 1),
            new Spell(8, 10, 2)
        };


    }

    public void ToggleHudGui()
    {
        NetworkManagerHUD hud = FindObjectOfType<NetworkManagerHUD>();
        if (hud != null)
            hud.showGUI = !hud.showGUI;
    }

    public float SpriteAngle(Vector2 crosshairDir)
    {
        float angle = Vector2.Angle(crosshairDir, transform.right);
        if ((Vector2.Dot(crosshairDir, transform.up)) < 0) //Dot product between two vectors gives -1 if they are in opposite direction, 1 if in the same and some number between in other cases
        {
            angle = -angle;
        }
        return angle;
    }

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



    IEnumerator SplitEnd(GameObject ObjectA, GameObject ObjectB, Vector2 splitVelocityA, Vector2 splitVelocityB, bool isPlayer)
    {
        //Splitting:
        //Player&Duplicate - use move position function 
        //Objects - use velocity 

        Vector3 positionA = ObjectA.transform.position;
        Vector3 positionB = ObjectB.transform.position;

        //Player
        float splitSpeed = 0.5f;
        float splitTimePostOverlap = 1f;
        float time = 0;

        //Split Overlap
        while ((ObjectA.GetComponent<Collider2D>().Distance(ObjectB.GetComponent<Collider2D>()).isOverlapped)) //ColliderDistance2D colliderDistance
        {
            if(!isPlayer)
            {
                yield return new WaitForSeconds(.2f);
            }
            if (isPlayer) //MovePosition(transform.position + (walkDirection.normalized * walkSpeed) * Time.deltaTime);
            {
                yield return new WaitForEndOfFrame();
                ObjectA.GetComponent<Rigidbody2D>().MovePosition((Vector2)ObjectA.transform.position + (splitVelocityA.normalized * splitSpeed) *Time.deltaTime);
                ObjectB.GetComponent<Rigidbody2D>().MovePosition((Vector2)ObjectB.transform.position + (splitVelocityB.normalized * splitSpeed) * Time.deltaTime);

            }

        }
        positionA = ObjectA.transform.position;
        positionB = ObjectB.transform.position;


        Debug.Log("Done");


        Physics2D.IgnoreCollision(ObjectA.GetComponent<Collider2D>(), ObjectB.GetComponent<Collider2D>(), false);

        //Split Move

        if(isPlayer)
        {
            while (time < splitTimePostOverlap)
            {
                time += Time.deltaTime;
                yield return new WaitForEndOfFrame();
                ObjectA.GetComponent<Rigidbody2D>().MovePosition((Vector2)ObjectA.transform.position + (splitVelocityA.normalized * splitSpeed) * Time.deltaTime);
                ObjectB.GetComponent<Rigidbody2D>().MovePosition((Vector2)ObjectB.transform.position + (splitVelocityB.normalized * splitSpeed) * Time.deltaTime);
            }
        }

        yield return new WaitForSeconds(2f);

        if(!isPlayer)
        {
            if (Vector2.Dot((ObjectA.GetComponent<Rigidbody2D>().velocity - splitVelocityA).normalized, -splitVelocityA.normalized) <= 0)
            {
                ObjectA.GetComponent<Rigidbody2D>().velocity -= splitVelocityA;
            }
            else
            {
                ObjectA.GetComponent<Rigidbody2D>().velocity -= (Vector2)Vector3.Project(ObjectA.GetComponent<Rigidbody2D>().velocity, splitVelocityA.normalized);
            }

            if (Vector2.Dot((ObjectB.GetComponent<Rigidbody2D>().velocity - splitVelocityB).normalized, -splitVelocityB.normalized) <= 0)
            {
                ObjectB.GetComponent<Rigidbody2D>().velocity -= splitVelocityB;
            }
            else
            {
                ObjectB.GetComponent<Rigidbody2D>().velocity -= (Vector2)Vector3.Project(ObjectB.GetComponent<Rigidbody2D>().velocity, splitVelocityB.normalized);
            }
        }
    }

    [Command]
    void CmdSpawnQube(Vector3 spawnPos)
    {
        GameObject Qube = Instantiate(QubePrefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(Qube);
    }

    [Command]
    void CmdSpawnFireball(Vector3 spawnPos, Vector2 dir, float speed, float angle, Quaternion prefabRotation)
    {
        GameObject Fireball = Instantiate(FireballPrefab, spawnPos, Quaternion.identity);
        Fireball.transform.Rotate(0, 0, prefabRotation.z + angle);
        Fireball.GetComponent<Rigidbody2D>().velocity = dir * speed;
        NetworkServer.Spawn(Fireball);
    }

    [Command]
    void CmdRaycastHitReference(GameObject gameObject, Vector2 dir)
    {
        float pushSpeed = 0.5f;
        gameObject.GetComponent<Rigidbody2D>().velocity = pushSpeed * dir;
    }

    [Command]
    void CmdSetTag(GameObject gameObject)
    {
        RpcSetTag(gameObject, "SwapAble");
    }

    [Command]
    void CmdSwap(GameObject gameObject)
    {
        Vector2 tempPosition = transform.position;

        RpcMoveObject(transform.gameObject, gameObject.transform.position);
        RpcMoveObject(gameObject, tempPosition);
        RpcSetTag(gameObject, "Untagged");
    }


    [ClientRpc]
    void RpcSetTag(GameObject gameObject, string tag)
    {
        gameObject.tag = tag;
    }

    [ClientRpc]
    void RpcMoveObject(GameObject gameObject, Vector3 pos)
    {
        if (gameObject.GetComponent<Rigidbody2D>())
        {
            gameObject.GetComponent<Rigidbody2D>().isKinematic = true;
            gameObject.transform.position = pos;
            gameObject.GetComponent<Rigidbody2D>().isKinematic = false;
            gameObject.GetComponent<Rigidbody2D>().velocity = Vector2.zero; //stop the movement if it's moving
        }
        else
        {
            gameObject.transform.position = pos;
        }
    }

    [Command]
    void CmdSpawnSummon(Vector3 spawnPos)
    {
        //SummonPrefab.GetComponent<SummonFollow>().Master = gameObject;
        GameObject Monster = Instantiate(SummonPrefab, spawnPos, Quaternion.identity, transform.parent);
        NetworkServer.Spawn(Monster);
        RpcSummonSetMaster(Monster, gameObject);
    }

    [ClientRpc]
    void RpcSummonSetMaster(GameObject Monster, GameObject master)
    {
        Monster.GetComponent<SummonFollow>().Master = master;
    }

    [Command]
    void CmdSummonUpdateTarget()
    {
        RpcSummonUpdateTarget(gameObject);
    }

    [ClientRpc]
    void RpcSummonUpdateTarget(GameObject master)
    {
        List<GameObject> Summons = new List<GameObject>(GameObject.FindGameObjectsWithTag("Summon"));
        Summons.RemoveAll(item => item.GetComponent<SummonFollow>().Master == master);


        for (int i = 0; i < Summons.Count; i++)
        {
            Summons[i].GetComponent<SummonFollow>().FindTargets();
        }
    }

    [Command]
    void CmdSplit(GameObject original, Vector2 splitVelocityDuplicate, Vector2 splitVelocityOriginal, bool isPlayer)
    {
        GameObject duplicate;
        if (isPlayer)
        {
            duplicate = original;

            duplicate = Instantiate(PlayerDuplicate, original.transform.position, Quaternion.identity);
        }
        else
        {
            duplicate = original;

            duplicate = Instantiate(QubePrefab, original.transform.position, Quaternion.identity);
        }

        NetworkServer.Spawn(duplicate);

        RpcSplit(duplicate, original, splitVelocityDuplicate, splitVelocityOriginal, isPlayer);
    }


    [ClientRpc]
    void RpcSplit(GameObject duplicate, GameObject original, Vector2 splitVelocityDuplicate, Vector2 splitVelocityOriginal, bool isPlayer)
    {
        if (isPlayer)
        {
            duplicate.transform.SetParent(transform);
            duplicate.GetComponent<DuplicateMimic>().Master = original;
        }

        Physics2D.IgnoreCollision(original.GetComponent<Collider2D>(), duplicate.GetComponent<Collider2D>());

        if (!isPlayer)
        {
            duplicate.GetComponent<Rigidbody2D>().velocity = original.GetComponent<Rigidbody2D>().velocity + splitVelocityDuplicate;
            original.GetComponent<Rigidbody2D>().velocity = original.GetComponent<Rigidbody2D>().velocity + splitVelocityOriginal;
        }

        StartCoroutine(SplitEnd(duplicate, original, splitVelocityDuplicate, splitVelocityOriginal, isPlayer));

    }

    [Command]
    void CmdSpawnShield(Vector3 spawnPos, float angle, Quaternion prefabRotation)
    {
        GameObject Shield = Instantiate(ShieldPrefab, spawnPos, prefabRotation);
        Shield.transform.Rotate(0, 0, prefabRotation.z + angle);
        NetworkServer.Spawn(Shield);
    }

    [Command]
    void CmdDamagePlayer(GameObject gameObject, int damage)
    {
        RpcDamagePlayer(gameObject, damage);
    }

    [ClientRpc]
    void RpcDamagePlayer(GameObject gameObject, int damage)
    {
        gameObject.GetComponent<PlayerStatus>().DamagePlayer(damage);
    }

    [Command]
    void CmdDestroyQube(GameObject gameObject)
    {
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    void CmdSetColliderToTrigger(GameObject gameObject)
    {
        RpcSetColliderToTrigger(gameObject);
    }

    [ClientRpc]
    void RpcSetColliderToTrigger(GameObject gameObject)
    {
        gameObject.GetComponent<CapsuleCollider2D>().isTrigger = true;
    }

    [Command]
    void CmdUnsetColliderToTrigger(GameObject gameObject)
    {
        RpcUnsetColliderToTrigger(gameObject);
    }

    [ClientRpc]
    void RpcUnsetColliderToTrigger(GameObject gameObject)
    {
        gameObject.GetComponent<CapsuleCollider2D>().isTrigger = false;
    }





    IEnumerator CastSpellPrimary(int spellID, float castingTime, Vector3 crosshairPos, Vector2 crosshairDir) //! castingTime should be same as animationDuration
    {
        switch (spellID)
        {
            case 0:
                if (mana - Spells[spellID].manaCost > 0)
                {
                    float angle = SpriteAngle(crosshairDir);
                    //Play Animation
                    anim.Play("Cast");


                    controlEnabled = false;
                    yield return new WaitForSeconds(castingTime);
                    controlEnabled = true;
                    Mana = mana - Spells[spellID].manaCost;
                    CmdSpawnFireball(crosshairPos, crosshairDir.normalized, fireballSpeed, angle, FireballPrefab.transform.rotation);
                }

                break;
            case 1:

                if (mana - Spells[spellID].manaCost > 0)
                {
                    //Play Animation
                    anim.Play("CastQube");
                    mana = mana - Spells[spellID].manaCost;
                    CmdSpawnQube(crosshairPos);

                    controlEnabled = false;
                    yield return new WaitForSeconds(castingTime);
                    controlEnabled = true;

                    //mana = mana - Spells[spellID].manaCost;
                    //CmdSpawnQube(crosshairPos);
                }

                break;
            case 2:
                {
                    Vector2 CursorEndPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 dir = (CursorEndPosition - CursorStartPosition).normalized;
                    int layerMask = 1 << 8;
                    RaycastHit2D hit = Physics2D.Raycast(CursorStartPosition, dir, (CursorEndPosition - CursorStartPosition).magnitude, layerMask);

                    if (hit.collider != null)
                    {
                        if (mana - Spells[spellID].manaCost > 0)
                        {
                            //Play Animation
                            anim.Play("Telekinesis");
                            controlEnabled = false;
                            yield return new WaitForSeconds(castingTime);
                            controlEnabled = true;

                            Mana = mana - Spells[spellID].manaCost;
                            CmdRaycastHitReference(hit.collider.gameObject, dir);
                        }
                    }

                    break;
                }

            case 3:
                {

                    int layerMask = 1 << 8;
                    RaycastHit2D hit = Physics2D.Raycast(crosshairPos, Vector2.right, 0.0000001f, layerMask);
                    if (hit.collider != null)
                    {
                        if (mana - Spells[spellID].manaCost > 0)
                        {
                            //Play Animation
                            controlEnabled = false;
                            yield return new WaitForSeconds(castingTime);
                            controlEnabled = true;

                            Mana = mana - Spells[spellID].manaCost;
                            CmdSetTag(hit.collider.gameObject);

                        }
                    }

                    break;
                }
            case 4:
                {
                    if (mana - Spells[spellID].manaCost > 0)
                    {
                        //Play Animation
                        anim.Play("SummonMonster");

                        controlEnabled = false;
                        yield return new WaitForSeconds(castingTime);
                        controlEnabled = true;

                        Mana = mana - Spells[spellID].manaCost;
                        CmdSpawnSummon(crosshairPos);
                    }
                    break;
                }
            case 5:
                {
                    int layerMask1 = 1 << 8;
                    RaycastHit2D hit1 = Physics2D.Raycast(crosshairPos, Vector2.right, 0.0000001f, layerMask1);
                    if (hit1.collider != null)
                    {
                        if (mana - Spells[spellID].manaCost > 0)
                        {
                            Vector2 offsetDir;

                            //If it's moving: SplitOffset it in the perpendicular direction to the direction it's moving
                            float offsetSpeed = 0.25f;
                            if (hit1.rigidbody.velocity != Vector2.zero)
                            {
                                offsetDir = new Vector2(hit1.rigidbody.velocity.y, -(hit1.rigidbody.velocity.x)).normalized * offsetSpeed;
                            }
                            //If it's not moving: position of cursor relative to player, perpencidular to that vector
                            else
                            {
                                offsetDir = new Vector2(crosshairDir.y, -(crosshairDir.x)).normalized * offsetSpeed;
                            }
                            int randomSplitSide = Random.Range(0, 2) * 2 - 1;
                            Vector2 splitVelocityDuplicate = randomSplitSide * offsetDir;
                            Vector2 splitVelocityHit = -randomSplitSide * offsetDir;

                            //Play Animation
                            controlEnabled = false;
                            yield return new WaitForSeconds(castingTime);
                            controlEnabled = true;

                            Mana = mana - Spells[spellID].manaCost;
                            CmdSplit(hit1.collider.gameObject, splitVelocityDuplicate, splitVelocityHit, false);
                        }
                    }

                    break;
                }
            case 6:
                {
                    if (mana - Spells[spellID].manaCost > 0)
                    {

                        float angle = SpriteAngle(crosshairDir);

                        //Play Animation
                        Mana = mana - Spells[spellID].manaCost;
                        CmdSpawnShield(crosshairPos, angle, ShieldPrefab.transform.rotation);
                        anim.Play("CastShield");
                        controlEnabled = false;
                        yield return new WaitForSeconds(castingTime);
                        controlEnabled = true;

                        //Mana = mana - Spells[spellID].manaCost;
                        //CmdSpawnShield(crosshairPos, angle, ShieldPrefab.transform.rotation);

                    }

                    break;
                }
            case 8:
                {
                    if (mana - Spells[spellID].manaCost > 0)
                    {
                        int damage = 40;
                        float offset = 0.22f;

                        FieldOfView fieldOfView = GetComponent<FieldOfView>();
                        Vector2 dir = fieldOfView.DirFromAngle(fieldOfView.angle, true);
                        Vector2 hitStart = (Vector2)transform.position + dir * offset;
                        float rayLength = fieldOfView.viewRadius - offset;
                        Lightning lightning = GetComponent<Lightning>();
                        float lightningLength;
                        RaycastHit2D hit = Physics2D.Raycast(hitStart, dir, rayLength);
                        if (hit.collider != null)
                        {
                            lightningLength = Vector2.Distance(hit.transform.position, hitStart);

                            //Play Animation
                            anim.Play("Cast");
                            controlEnabled = false;
                            yield return new WaitForSeconds(castingTime);
                            controlEnabled = true;

                            Mana = mana - Spells[spellID].manaCost;
                            lightning.DrawLightning(hitStart, hit.point, hit.transform.position, dir, lightningLength);


                            if (hit.collider.gameObject.tag == "Deflect" && hit.collider.isTrigger)
                            {
                                dir = Vector2.Reflect(dir, hit.normal);
                                hitStart = (Vector2)hit.transform.position + dir.normalized * offset;
                                hit = Physics2D.Raycast(hitStart, dir, fieldOfView.viewRadius - offset);
                                if (hit.collider != null)
                                {
                                    lightningLength = Vector2.Distance(hit.transform.position, hitStart);
                                    lightning.DrawLightning(hitStart, hit.point, hit.transform.position, dir, lightningLength);
                                }
                                else
                                {
                                    lightningLength = fieldOfView.viewRadius;
                                    Vector3 endPoint = hitStart + dir.normalized * lightningLength;
                                    lightning.DrawLightning(hitStart, endPoint, endPoint, dir, lightningLength);

                                }
                            }
                            if (hit.collider != null)
                            {
                                if (hit.collider.gameObject.tag == "Player")
                                {
                                    CmdDamagePlayer(hit.collider.gameObject, damage);
                                }
                                else if(hit.collider.gameObject.layer == 8)
                                {
                                    CmdDestroyQube(hit.collider.gameObject);
                                }
                            }
                        }
                        else
                        {
                            lightningLength = fieldOfView.viewRadius;
                            Vector3 endPoint = hitStart + dir.normalized * lightningLength;


                            //Play Animation
                            anim.Play("Cast");
                            controlEnabled = false;
                            yield return new WaitForSeconds(castingTime);
                            controlEnabled = true;

                            Mana = mana - Spells[spellID].manaCost;
                            lightning.DrawLightning(hitStart, endPoint, endPoint, dir, lightningLength);
                        }
                    }
                    break;
                }
        }
        yield return null;
    }

    IEnumerator CastSpellSecondary(int spellID, float castingTime, Vector3 crosshairPos, Vector2 crosshairDir)
    {
        switch (spellID)
        {
            case 2:
                {

                    Vector2 CursorEndPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    int layerMask = 1 << 8;
                    Vector2 dir1 = (CursorEndPosition - CursorStartPosition).normalized;
                    RaycastHit2D hit = Physics2D.Raycast(CursorStartPosition, dir1, (CursorEndPosition - CursorStartPosition).magnitude, layerMask);
                    Vector2 dir2 = (CursorStartPosition - CursorEndPosition).normalized;
                    RaycastHit2D hit2 = Physics2D.Raycast(CursorEndPosition, dir2, (CursorStartPosition - CursorEndPosition).magnitude, layerMask);
                    if (hit.collider != null && hit2.collider != null)
                    {
                        if (mana - Spells[spellID].manaCost > 0)
                        {
                            //Play Animation
                            anim.Play("Telekinesis");
                            controlEnabled = false;
                            yield return new WaitForSeconds(castingTime);
                            controlEnabled = true;


                            Mana = mana - Spells[spellID].manaCost;
                            CmdRaycastHitReference(hit.collider.gameObject, dir1);
                            CmdRaycastHitReference(hit2.collider.gameObject, dir2);
                        }
                    }

                }

                break;
            case 3:
                {
                    //Swap with the object
                    int layerMask = 1 << 8;
                    RaycastHit2D hit = Physics2D.Raycast(crosshairPos, Vector2.right, 0.0000001f, layerMask);
                    if (hit.collider != null)
                    {
                        if (hit.rigidbody.tag == "SwapAble")
                        {
                            if (mana - Spells[spellID].manaCost > 0)
                            {

                                CmdSetColliderToTrigger(gameObject);

                                //>Play Animation
                                anim.Play("Vanish");

                                controlEnabled = false;
                                yield return new WaitForSeconds(castingTime);
                                controlEnabled = true;

                                Mana = mana - Spells[spellID].manaCost;
                                CmdSwap(hit.collider.gameObject);

                                CmdUnsetColliderToTrigger(gameObject);

                            }
                        }
                    }
                    break;
                }
            case 5:
                {
                    if (mana - Spells[spellID].manaCost > 0)
                    {
                        Mana = mana - Spells[spellID].manaCost;

                        Vector2 offsetDir;

                        float offsetSpeed = 0.25f;
                        if (GetComponent<Rigidbody2D>().velocity != Vector2.zero)
                        {
                            offsetDir = walkDirection.normalized * offsetSpeed;
                        }
                        else
                        {
                            offsetDir = new Vector2(0, 1) * offsetSpeed;
                        }
                        int randomSplitSide = Random.Range(0, 2) * 2 - 1;
                        Vector2 splitVelocityDuplicate = randomSplitSide * offsetDir;
                        Vector2 splitVelocityHit = -randomSplitSide * offsetDir;


                        //Play Animation
                        controlEnabled = false;
                        yield return new WaitForSeconds(castingTime);
                        controlEnabled = true;


                        CmdSplit(gameObject, splitVelocityDuplicate, splitVelocityHit, true);

                        //>UpdateSummonTargetList
                        CmdSummonUpdateTarget();

                    }
                    break;
                }

        }
    }

    private void CastSpellHold(int spellID)
    {
        switch (spellID)
        {
            case 7:
                {
                    float petrificationDelay = 0.3f;
                    
                    if (mana - Spells[spellID].manaCost > 0)
                    {
                        if (Time.time > lastPetrificationTime + petrificationDelay)
                        {
                            Mana = mana - Spells[spellID].manaCost;

                            FieldOfView fieldOfView = GetComponent<FieldOfView>();

                            //Play Animation ~ ?Change walking sprite
                            anim.Play("Petrification");

                            fieldOfView.FindVisibleTargets();
                            lastPetrificationTime = Time.time;
                        }
                    }
                    break;
                }

        }
    }

    void ToggleButton(Button currentButton)
    {
        if(currentButton != previousButton)
        {
            ColorBlock cb = currentButton.colors;
            cb.disabledColor = Color.white;
            ColorBlock pb = previousButton.colors;
            pb.disabledColor = new Color32(200, 200, 200, 128);
            previousButton.colors = pb;
            currentButton.colors = cb;

            previousButton = currentButton;
        }
    }





    void Update()
    {
        if (controlEnabled)
        {

            if (!isLocalPlayer)
            {
                return;
            }

            walkSpeed = GetComponent<PlayerStatus>().walkingSpeed;

            walkDirection = Vector3.zero;
            //--------------------------Movement-----------------------------
            if (Input.GetKey(KeyCode.D))
            {
                walkDirection += Vector3.right;
            }
            if (Input.GetKey(KeyCode.A))
            {
                walkDirection += Vector3.left;
            }
            if (Input.GetKey(KeyCode.W))
            {
                walkDirection += Vector3.up;
            }
            if (Input.GetKey(KeyCode.S))
            {
                walkDirection += Vector3.down;
            }

            transform.GetComponent<Rigidbody2D>().MovePosition(transform.position + (walkDirection.normalized * walkSpeed) * Time.deltaTime);

            //--------------------------Spell Choice-----------------------------
            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                spellChoice = 0;  //*Fireball
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                spellChoice = 1; //*Qube
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                spellChoice = 2; //Telekinesis/Attraction
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                spellChoice = 3; //Place swap
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha5))
            {
                spellChoice = 4; //*Summon Monster
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha6))
            {
                spellChoice = 5; //Duplicity
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha7))
            {
                spellChoice = 6; // Shield
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha8))
            {
                spellChoice = 7; //Stone gaze
                ToggleButton(buttons[spellChoice]);
            }
            if (Input.GetKeyUp(KeyCode.Alpha9))
            {
                spellChoice = 8; //Lightning
                ToggleButton(buttons[spellChoice]);
            }

            if (Input.GetKeyUp(KeyCode.Escape))
            {
                ToggleHudGui();
            }




            //--------------------------Crosshair-----------------------------
            //>>> Calculating mouse position in world game and aiming

            PlayerPosition = transform.position;

            CursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            direction = CursorPosition - PlayerPosition;


            //-------------------------Sprite Facing Direction ----------------------------
            if (Vector2.Dot(direction, Vector2.right) < 0)
            {

                FlipSprite(false);
            }

            if (Vector2.Dot(direction, Vector2.left) < 0)
            {
                FlipSprite(true);
            }

            //--------------------- Crosshair type ----------------------
            if (spellChoice < 7)
            {
                Crosshair.SetActive(true);
                FieldOfView fieldOfView = GetComponent<FieldOfView>();
                fieldOfView.enabled = false;
            }
            else if (spellChoice >= 7)
            {
                Crosshair.SetActive(false);
                GetComponent<FieldOfView>().enabled = true;
            }
            //------------------ Crosshair pos ---------------------------
            switch (spellChoice)
            {
                case 0:
                    Crosshair.transform.position = PlayerPosition + Vector2.Scale(crosshairSpawnPoint, direction.normalized);
                    break;
                case 1:
                    {
                        Vector2 spawnDistance = direction.normalized * 0.2f;
                        Crosshair.transform.position = PlayerPosition + Vector2.Scale(crosshairSpawnPoint, direction.normalized) + spawnDistance;
                        break;
                    }
                case 2:
                    Crosshair.transform.position = PlayerPosition + direction;
                    break;
                case 3:
                    Crosshair.transform.position = PlayerPosition + direction;
                    break;
                case 4:
                    {
                        Vector2 maxDistance = direction.normalized * 0.3f;
                        if (direction.magnitude > maxDistance.magnitude)
                        {
                            Crosshair.transform.position = PlayerPosition + maxDistance;
                        }
                        else
                        {
                            Crosshair.transform.position = PlayerPosition + direction;
                        }
                        break;
                    }
                case 5:
                    {
                        Crosshair.transform.position = PlayerPosition + direction;
                        break;
                    }
                case 6:
                    {
                        Vector2 spawnDistance = direction.normalized * 0.1f;
                        Crosshair.transform.position = PlayerPosition + Vector2.Scale(crosshairSpawnPoint, direction.normalized) + spawnDistance;
                        break;
                    }
                case 7:
                    {
                        FieldOfView fieldOfView = GetComponent<FieldOfView>();
                        fieldOfView.viewRadius = 1.0f;
                        fieldOfView.viewAngle = 20.0f;
                        break;
                    }
                case 8:
                    {
                        FieldOfView fieldOfView = GetComponent<FieldOfView>();
                        fieldOfView.viewRadius = 1.6f;
                        fieldOfView.viewAngle = 1.0f;
                        break;
                    }

            }

            //----------------------Camera pos--------------------------------
            mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z) + (Vector3)(direction.normalized * cameraOffset);

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                CursorStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }


            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                StartCoroutine(CastSpellPrimary(spellChoice, Spells[spellChoice].castingTime, Crosshair.transform.position, direction)); //Spell Mode 1
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                CursorStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                StartCoroutine(CastSpellSecondary(spellChoice, Spells[spellChoice].castingTime, Crosshair.transform.position, direction)); //Spell Mode 2
            }

            if (Input.GetKey(KeyCode.Mouse0)) // Hold spells (petrification)
            {
                CastSpellHold(spellChoice);
            }

            if (mana != maxMana && !isRegenMana)
            {
                StartCoroutine(ManaRegen());
            }

            //if(Input.GetKeyUp(KeyCode.Mouse0))
            //{
            //    anim.Play("Idle")
            //}



        }
    }
}