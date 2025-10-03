using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public enum CharacterAction
{
    attack,
    freeze,
    specjalCharge
}


public class CharacterMove : MonoBehaviour
{

 
    [HideInInspector] public Character character;
  //  public string prefabName;
    [HideInInspector] public Animator animator;
    [SerializeField] int itemUseOnAttack = 10;
    [SerializeField] Transform graphicCharacter;
    public Transform weaponParent;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] GameObject damageText;
    public NavMeshAgent navMeshAgent;
    [SerializeField] float waitBeforeAttack = 0.4f;
    [SerializeField] float waitAfterAttack = 0.1f;
    [HideInInspector] public CharacterMenager characterMenager;
    [HideInInspector] [ReadOnly(true)] public bool isOnMove;
    public SphereCollider attackCollider;
    [HideInInspector] public List<Resource> resourcesInEq = new();
    [Tooltip("Distance after enemy will ignore path and attack building (less means enemy will go around buildings)")] 
    public float distanceToAttackBuildings;
    [HideInInspector] public bool attackBuildings;
    Rigidbody rb;
    [SerializeField] float knockbackToDamagePlus = 20;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public bool followPlayer;
    [SerializeField] VisualEffect bloodEffect;
    [Header("WEAPON")]
    public GameObject startMainWeapon;
    [SerializeField] float wandAttackDistance;
    public List<GameObject> RandomizeWeapons;
    public float chanceForDropWeapon = 0.05f;
    [HideInInspector] public Weapon mainWeapon;
    CharEffects charEffects;
    float defaultAttackRadius;

    [Tooltip("if null serach sphere collider")]
    public Collider bodyCollider;
    int navMeshDefaultArea;
    [HideInInspector] public Village myVillage;
    Interaction interaction;

    public struct Resource_
    {
        public ResourceType type;
        public int amount;
    }
    [Header("Drop")]
    [SerializeField] ItemDrop[] dropItems;
    [SerializeField] float resourceVelocity = 2;
    [SerializeField] float weaponVelocity = 3;
    [SerializeField] GameObject deadBody;
    [Serializable]
    public struct ItemDrop
    {
        [Range(0f, 1f)] public float dropChance;
        [Tooltip("multiple by chance")] public int maxCount;
        public GameObject item;
    }

    [Header("enemy settings")]
    PlayerMove playerMove; 
    public float distanceToAttackPlayer;
    Transform stuffMenager;

    [Header("character bechaviour: ")]

    [Tooltip("will charge specjal modifer")]
    public bool setOnSpecjalCharging;
    public bool setOnAttackCharging;


    private void Awake()
    {
        interaction = GetComponentInChildren<Interaction>();
        if (interaction != null && gameObject.CompareTag("enemy"))
        {
            interaction.gameObject.SetActive(false);
        }


        navMeshDefaultArea = navMeshAgent.areaMask;
        character = GetComponent<Character>();
        if(attackCollider == null) attackCollider = GetComponent<SphereCollider>();
        animator = graphicCharacter.GetComponentInChildren<Animator>();
        spriteRenderer = graphicCharacter.GetComponentInChildren<SpriteRenderer>();
        characterMenager = GameObject.FindGameObjectWithTag("GameController").GetComponent<CharacterMenager>();
        if(bodyCollider == null) bodyCollider = GetComponent<CapsuleCollider>();
        if(attackCollider != null) defaultAttackRadius = attackCollider.radius;

    }

    private void OnEnable()
    {
       if(setOnSpecjalCharging) StartCoroutine(SpecjalChargeLoop());
    }

    private void Start()
    {
        playerMove = characterMenager.playerMove;
        stuffMenager = characterMenager.stuffMenager;

        if (RandomizeWeapons.Count > 0)
        {
            startMainWeapon = RandomizeWeapons[Random.Range(0, RandomizeWeapons.Count - 1)];
        }

        if (startMainWeapon != null) AddWeapon(startMainWeapon);
        navMeshAgent.enabled = true;
        navMeshAgent.updateRotation = false;
        rb = GetComponent<Rigidbody>();
        charEffects = GetComponentInChildren<CharEffects>();

        transform.localScale = Vector3.one * (1 + character.characterStats.characterSizeMultiplayer);

        
        bloodEffect.SetVector2("ParticleSize", bloodEffect.GetVector2("ParticleSize") * (1 + (character.characterStats.characterSizeMultiplayer * 0.4f)));

        weaponParent.localScale = Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer);

        ResetCharacterStats();


        if (character.characterStats.fireCultist > 0 || character.characterStats.speed == 0) StartCoroutine(ActualizeMoveAnim());
        else
        {
            attackCollider.enabled = true;

            if (mainWeapon.GetComponent<Weapon>().isWand)
            {
                attackCollider.radius = wandAttackDistance;
            }

            if (waitForMoveTo != null) StopCoroutine(waitForMoveTo);
            waitForMoveTo = StartCoroutine(WaitForMoveTo());
        }

        if(character.spellParent != null)
             character.ResetSpells();

        

    }


    Vector3 movingTo;
    public void MoveTo(Vector3 pos)
    {
        movingTo = pos;

      


        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;
        if (navMeshAgent.enabled && knockback == null && characterAction == null)
        {

            

            navMeshAgent.isStopped = false;


            isOnMove = true;

            
            if (character.characterStats.fireCultist <= 0) // enemy
            {
                /*if (actualizePathForEnemy != null)
                {
                    StopCoroutine(actualizePathForEnemy);
                    actualizePathForEnemy = null;
                }*/

                // player attack (only if this is gate attack)
                if (pos == Vector3.zero && Vector3.Distance(transform.position, pos) + distanceToAttackPlayer > Vector3.Distance(transform.position, playerMove.transform.position) && cantAttackPlayer == false && attackPlayerLoop == null)
                {

                    attackPlayerLoop = StartCoroutine(AttackPlayerLoop());

                    return;

                }


               // navMeshAgent.SetDestination(pos);

                // normal move
                if (attackPlayerLoop == null && navMeshAgent.SetDestination(pos))
                {
                    if (navMeshAgent.hasPath)
                    {
                        navMeshAgent.areaMask = navMeshDefaultArea;
                        attackBuildings = false;

                        if (Vector3.Distance(navMeshAgent.destination, navMeshAgent.pathEndPosition) > 1f)
                        {
                           // Debug.Log("waitForAttackBuilding");
                            if (waitForAttackBuilding == null)
                            {
                                waitForAttackBuilding = StartCoroutine(WaitForAttackBuilding(navMeshAgent.pathEndPosition));
                            }


                        }

                    }
                    else
                    {

                        navMeshAgent.areaMask = NavMesh.AllAreas;


                        attackBuildings = false;

                        if (Vector3.Distance(Vector3.zero, navMeshAgent.pathEndPosition) < 10f)
                        {
                           // navMeshAgent.SetDestination(navMeshAgent.pathEndPosition);
                            if (waitForAttackBuilding == null)
                            {

                                waitForAttackBuilding = StartCoroutine(WaitForAttackBuilding(navMeshAgent.pathEndPosition));
                            }
                        }
                        else
                        {
                            navMeshAgent.SetDestination(pos);

                            if (Vector3.Distance(navMeshAgent.destination, navMeshAgent.pathEndPosition) > 1f)
                            {
                                if (waitForAttackBuilding == null)
                                {
                                    waitForAttackBuilding = StartCoroutine(WaitForAttackBuilding(navMeshAgent.pathEndPosition));
                                }
                            }
                        }




                    }
                }
               /* else
                {
                    Debug.Log("nie mozna dostac sie do wskazanego miejsca: " + pos);
                }*/

              //  Debug.Log(" 1: has path = " + navMeshAgent.hasPath + " des: " + navMeshAgent.destination + "  endPos = " + navMeshAgent.pathEndPosition + " reamingDistance = " + navMeshAgent.remainingDistance + " position = " + transform.position);
               


            }
            else // ally
            {
                navMeshAgent.SetDestination(pos);

            }


            ActualizeAnimation(pos - transform.position);
        }
        else
        {
            attackCollider.enabled = true;
            if (waitForMoveTo != null) StopCoroutine(waitForMoveTo);
            waitForMoveTo = StartCoroutine(WaitForMoveTo());
        }
    }

    Coroutine waitForMoveTo;
    IEnumerator WaitForMoveTo() // wait if characterd doing something
    {
        do
        {
            yield return new WaitForSeconds(0.5f);

            

        } while (!(navMeshAgent.enabled && knockback == null && characterAction == null));

        MoveTo(movingTo);

        if (attackCollider.enabled == true)
        {
            attackCollider.enabled = false;
            yield return new WaitForFixedUpdate();
            attackCollider.enabled = true;
        }
    }

    Coroutine waitForAttackBuilding;
    IEnumerator WaitForAttackBuilding(Vector3 endPos)
    {

        yield return new WaitForSeconds(0.3f);
        while (navMeshAgent.isOnNavMesh & Vector3.Distance(transform.position, endPos) > distanceToAttackBuildings && navMeshAgent.remainingDistance < 100 && navMeshAgent.hasPath)
        {
            yield return new WaitForSeconds(0.5f);
        }

        navMeshAgent.areaMask = NavMesh.AllAreas;
        attackBuildings = true;
        if (attackCollider.enabled == true)
        {
            attackCollider.enabled = false;
            yield return new WaitForFixedUpdate();
            attackCollider.enabled = true;
        }
        
        waitForAttackBuilding = null;

    }



    Coroutine attackPlayerLoop;
    bool cantAttackPlayer;
    IEnumerator AttackPlayerLoop()
    {
        float loopTime = Random.Range(0.3f, 0.7f);
        do
        {
            
            if (navMeshAgent.enabled)
            {
                navMeshAgent.SetDestination(playerMove.transform.position);
                ActualizeAnimation(navMeshAgent.velocity);
                attackBuildings = false;
                if (waitForAttackBuilding != null) StopCoroutine(waitForAttackBuilding);
            }
            yield return new WaitForSeconds(loopTime);
            
        } while (navMeshAgent.enabled && distanceToAttackPlayer * 2 > navMeshAgent.remainingDistance && navMeshAgent.remainingDistance != 0 && navMeshAgent.hasPath);
        attackPlayerLoop = null;
        cantAttackPlayer = true;

        MoveTo(Vector3.zero);

        cantAttackPlayer = false;

    }

    IEnumerator ActualizeMoveAnim()
    {
        while (true)
        {
            if (navMeshAgent.velocity != Vector3.zero || characterAction != null)
            {
                isOnMove = true;

                if (characterAction == null) ActualizeAnimation(navMeshAgent.velocity);


            }
            else
            {
                if (navMeshAgent.velocity == Vector3.zero) ActualizeAnimation(Vector3.zero);
                isOnMove = false;

            }
            yield return new WaitForSeconds(0.6f);

        }

    }

    private void OnTriggerEnter(Collider other) // is in attack distance
    {

        if (!other.isTrigger)
        {
            attackOnlyBuilding = false;

            if (character.characterStats.attackAnyone > 0) // attack anyone
            {
                if (other.CompareTag("ally") || other.CompareTag("enemy") || other.CompareTag("playerBody"))
                {
                    StartAction(CharacterAction.attack, (other.transform.position + Vector3.up) - transform.position);
                    attackCollider.enabled = false;
                }
            }
            else if (character.characterStats.fireCultist <= 0) // enemy
            {
                if (other.CompareTag("ally") || other.CompareTag("playerBody"))
                {
                    StartAction(CharacterAction.attack, (other.transform.position + Vector3.up) - transform.position);
                    attackCollider.enabled = false;

                }
                else if (other.gameObject.layer == 17) // build
                {
                    bool attackAnything = false;

                   

                    if (navMeshAgent.velocity.magnitude < 1f || (transform.position.magnitude > 2 && Vector3.Distance(navMeshAgent.pathEndPosition, other.transform.position) < distanceToAttackBuildings))
                    {
                        attackAnything = true;
                    }

                    if(((!navMeshAgent.hasPath || attackBuildings || attackAnything) && other.gameObject.tag is not "enemyBuild" && other.transform.position.magnitude < transform.position.magnitude) || (other.gameObject.tag is "gate"))
                    {
                        attackOnlyBuilding = true;
                        StartAction(CharacterAction.attack, (other.transform.position + Vector3.up) - transform.position);
                        attackCollider.enabled = false;
                    }
                    
                }
            }
            else if (character.characterStats.fireCultist > 0) // Ally
            {
                if (other.CompareTag("enemy"))
                {
                    StartAction(CharacterAction.attack, (other.transform.position + Vector3.up) - transform.position);
                    attackCollider.enabled = false;

                }
                else if (other.CompareTag("resourceSource") && characterMenager.enemyList.Count < 1 && !followPlayer)
                {
                    StartAction(CharacterAction.attack, (other.transform.position + Vector3.up) - transform.position);
                    attackCollider.enabled = false;

                }
                
            }
             
        }
        else if (character.characterStats.fireCultist > 0) // ally resource itp
        {
            if (other.CompareTag("Player") && resourcesInEq.Count > 0 && dropResource == null)
            {
                dropResource = StartCoroutine(DropAllResource(other.transform));

            }
            else if (other.gameObject.layer == 15 && dropResource == null && characterMenager.sendForResource != null)
            {
                StartCoroutine(TakeResource(other.gameObject.GetComponent<Resource>()));

            }

        }

    }

   

    void ActualizeAnimation(Vector3 move)
    {
        if (move == Vector3.zero)
        {
            animator.SetBool("walk", false);
        }
        else
        {
            animator.SetBool("walk", true);

            if (move.x != 0) // zmiana obrotu potaci
            {
                if (mainWeapon != null) mainWeapon.animator.SetBool("stopAttacking", true);
                RotateCharacter(move);
            }
            
        }
    }

    void RotateCharacter(Vector3 lookVector)
    {
        if (lookVector.x < 0 && graphicCharacter.transform.localScale.x > 0) // zmiana obrotu potaci
        {
            graphicCharacter.transform.localScale = new(-1, 1, 1);
            character.charEffects.transform.localScale = new(-1, 1, 1);
        }
        else if (lookVector.x > 0 && graphicCharacter.transform.localScale.x < 0)
        {
            graphicCharacter.transform.localScale = new(1, 1, 1);
            character.charEffects.transform.localScale = new(1, 1, 1);

        }
    }

    public Coroutine characterAction;
    void StartAction(CharacterAction actionType, Vector3 vector)
    {
        if (characterAction == null && knockback == null)
        {
            if (actionType == CharacterAction.attack)
            {
                characterAction = StartCoroutine(Attack(vector));
            }else if (actionType == CharacterAction.freeze)
            {
                characterAction = StartCoroutine(FreezeCharacter());
            }
            else if (actionType == CharacterAction.specjalCharge)
            {
                characterAction = StartCoroutine(ChargeModifier(true));
            }




        }
    }

    bool attackOnlyBuilding;
    IEnumerator Attack(Vector3 attackVector)
    {
        
        if(navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.isStopped = true;
        }

        RotateCharacter(attackVector);


        // Debug.Log(attackVector + "    " + CodeMonkeyDirection(attackVector));

        if (mainWeapon != null) // Attack
        {
            if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.velocity = character.characterStats.moveOnAttack * navMeshAgent.speed * attackVector.normalized;
                ActualizeAnimation(navMeshAgent.velocity);
            }

            float attackSpeed = (1 + character.characterStats.attackSpeedMultiplayer);

            animator.SetBool("prepareAttack", true);
            yield return new WaitForSeconds(waitBeforeAttack / attackSpeed);

            if (setOnAttackCharging && character.attackChargingModifer.isLoadedModiferReady && Random.Range(0f,1f) > 0.5f)
            {
                StartCoroutine(ChargeModifier(false));

                yield return new WaitForSeconds(character.attackChargingModifer.loadedModifierChargeTime);

            }

            


            animator.SetBool("prepareAttack", false);

            if (resetWeaponParent != null) StopCoroutine(resetWeaponParent);


            weaponParent.transform.eulerAngles = new Vector3(0, CodeMonkeyDirection(attackVector), 0);

            Weapon weapon = mainWeapon;
            if(attackOnlyBuilding)
            {
                weapon.attackOnlyBuildings = true;
                attackOnlyBuilding = false;
            }
            else
            {
               // Debug.Log(":not attack Build");
                weapon.attackOnlyBuildings = false;
            }

            weapon.animator.speed = 1 * attackSpeed;

            weapon.Attack((int)(itemUseOnAttack / (1 + character.characterStats.reduceWeaponUse)), attackVector);

            yield return new WaitForSeconds(weapon.weaponAttackTime / attackSpeed);
            weapon.animator.speed = 1;

            /*if (weapon.itemHp < 0)
            {
                RemoveWeapon(weapon);
            }*/

        }



        ActualizeAnimation(attackVector);
        resetWeaponParent = StartCoroutine(ResetWeaponParent());

        characterAction = null;



        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = false;
            ActualizeAnimation(navMeshAgent.velocity);
        }


        MoveTo(movingTo);

        yield return new WaitForSeconds(waitAfterAttack / (1 + character.characterStats.attackSpeedMultiplayer));

        if (knockback == null && !isDead)
            attackCollider.enabled = true;


    }
    Coroutine resetWeaponParent;
    IEnumerator ResetWeaponParent()
    {
        for (int i = 8; i >= 0; i--)
        {
            yield return new WaitForFixedUpdate();
            weaponParent.transform.eulerAngles = new(0, weaponParent.eulerAngles.y * (i / 8), 0);

        }
    }

   
    public float CodeMonkeyDirection(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if (n < 0f) n += 360;
        if (graphicCharacter.transform.localScale.x < 0) n -= 180;

        return -n;
    }
    public float CodeMonkeyDirectionNonScale(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        if (n < 0f) n += 360;

        return -n;
    }

    public void AddWeapon(GameObject weaponPrefab)
    {
       
        Weapon weapon = Instantiate(weaponPrefab, weaponParent).GetComponent<Weapon>();
        weapon.transform.localPosition = Vector3.zero;
        mainWeapon = weapon;
        weapon.characterMove = this;
        weapon.SetVisualEffectsColor();
       // ResetWeapons();
    }

   /* void RemoveWeapon(Weapon weapon)
    {
        if(mainWeapon != null)
        weapons.Remove(weapon);
        weapon.DestroyWeapon();
        ResetWeapons();
    }

    void ResetWeapons()
    {


        int priority = -10;
        Weapon toSetOn = null;
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].weaponPriorityForCharacters > priority)
            {
                toSetOn = weapons[i];
                priority = weapons[i].weaponPriorityForCharacters;
            }
            weapons[i].gameObject.SetActive(false);
        }

        if(toSetOn != null )
        {
            toSetOn.gameObject.SetActive(true);
        }

    }*/
    
    public void ResetCharacterStats()
    {
        if(!enabled || !gameObject.activeInHierarchy) return;
        // gdzie indziej - attackAnyone, fireCultist, reduceWeaponUse, knocbackResist, freezeResist defense; w weapon.sc - weaponDamageMultiplayer, knockbackMultiplayer
        if(character.characterStats.freezeTimeLeft <= 0)
        {
            if (navMeshAgent != null) navMeshAgent.speed = (character.characterStats.speed + (character.characterStats.speed * character.characterStats.speedMultiplayer)); // speed, speedMultiplayer
            if (navMeshAgent != null && navMeshAgent.speed < 0) navMeshAgent.speed = 0;
        }
        else // char is freeze
        {
            navMeshAgent.speed = 0;
            
        }

        if (transform.localScale != Vector3.one * (1 + character.characterStats.characterSizeMultiplayer)) // characterSizeMultiplayer
        {

            if (setCharSize == null) setCharSize = StartCoroutine(SetCharacterSize());
        }
        if (weaponParent.localScale != Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer)) // characterSizeMultiplayer
        {
            if (setWeaponSize == null) setWeaponSize = StartCoroutine(SetWeaponSize());
           /* Debug.Log(attackCollider);
            Debug.Log(character.characterStats.weaponSizeMultiplayer);*/
            attackCollider.radius = defaultAttackRadius * (1 + character.characterStats.weaponSizeMultiplayer);
        }



        if (character.characterStats.hpHealingPerSec > 0) // hpHealingPerSec
        {
            if(hpHealingPorSecond == null) hpHealingPorSecond = StartCoroutine(HpHealingPerSecond());

        } 

        if(character.characterStats.fireDamageLeft > 0 || character.characterStats.fireDamagePerSecond > 0)
        {
            if(setOnFire ==null) setOnFire = StartCoroutine(SetOnFire());
        }

    }

    Coroutine setCharSize;
    IEnumerator SetCharacterSize()
    {
        while (transform.localScale != Vector3.one * (1 + character.characterStats.characterSizeMultiplayer))
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.one * (1 + character.characterStats.characterSizeMultiplayer), 0.5f * Time.deltaTime);
            yield return null; // Czekaj do nastêpnej klatki
        }
        setCharSize = null;

    }
    Coroutine setWeaponSize;
    IEnumerator SetWeaponSize()
    {
        while (weaponParent.localScale != Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer))
        {
            weaponParent.localScale = Vector3.MoveTowards(weaponParent.localScale, Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer), 0.5f * Time.deltaTime);
            yield return null; // Czekaj do nastêpnej klatki
        }
        setWeaponSize = null;

    }

    // take resource

    public void CreateAndTakeResource(GameObject obj)
    {
        Resource resource = Instantiate(obj, stuffMenager).GetComponent<Resource>();
        resource.colider.enabled = false;
        resource.animator.SetBool("take", true);
        resourcesInEq.Add(resource);
        resource.gameObject.SetActive(false);

    }

    IEnumerator TakeResource(Resource resource)
    {
        resource.colider.enabled = false;
        resource.animator.SetBool("take", true);
        resourcesInEq.Add(resource);

        /*resource.rb.isKinematic = false;

        int loopCount = 0;
        do
        {
            resource.rb.AddForce((resource.transform.position - transform.position).normalized * 6, ForceMode.VelocityChange);
            yield return new WaitForFixedUpdate();
            loopCount++;
        } while ((transform.position - resource.transform.position).magnitude < 4 && loopCount < 30);
        loopCount = 0;
        do
        {
            resource.rb.AddForce((transform.position - resource.transform.position).normalized * 6, ForceMode.VelocityChange);
            yield return new WaitForFixedUpdate();
            loopCount++;

        } while ((transform.position - resource.transform.position).magnitude > 1 && loopCount < 30);*/

        yield return new WaitForSeconds(0.45f);

        resource.gameObject.SetActive(false);


    }


    Coroutine dropResource;
    [Tooltip("drop from eq")]
    IEnumerator DropAllResource(Transform dropTarget)
    {
        if(navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {

            navMeshAgent.isStopped = true;

            foreach (Resource resource in resourcesInEq)
            {
                resource.gameObject.SetActive(true);
                resource.ResourceCreate();
                resource.animator.SetBool("take", false);
                resource.animator.Play("create");


                // bardziej bociazajace
                resource.transform.position = transform.position;
                resource.rb.isKinematic = false;
                resource.rb.AddForce((dropTarget.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) - transform.position).normalized * 25, ForceMode.VelocityChange);
                // resource.rb.velocity = (dropTarget.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f)) - transform.position).normalized * 5;
                // resource.transform.position = dropTarget.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));

                yield return new WaitForSeconds(0.1f);

            }
            navMeshAgent.isStopped = false;

            resourcesInEq.Clear();
            yield return new WaitForSeconds(15f);
            dropResource = null;
        }

    }


    public void DealDamage(Vector3 attackVector, Vector2Int damage, float knockbackForce, DamageType type = DamageType.physics)
    {
        if(isDead) return;

        if(characterAction != null && character.characterStats.freezeTimeLeft <= 0)
        {
            StopCoroutine(characterAction);
            StopAllCharging();
            characterAction = null;
            animator.SetBool("prepareAttack", false);
            animator.SetBool("specjalCharge", false);

        }

        if (attackVector != Vector3.zero) ActualizeAnimation(-attackVector);

        GameObject txtObj = Instantiate(damageText, transform.position, transform.rotation, transform.parent);

        TextMeshProUGUI txt = txtObj.GetComponentInChildren<TextMeshProUGUI>(); 
        

        int realDamage = Random.Range(damage.x, damage.y+1);
        realDamage -= character.characterStats.defense;
        if(realDamage < 0) realDamage = 0;

        txt.text = realDamage.ToString();
        txt.transform.localScale = Vector3.one * (1 + (realDamage / 25f));


        character.characterStats.hp -= realDamage;
        if (character.characterStats.hp <= 0)
        {
            DropDeadBody(attackVector, knockbackForce);
           // bloodEffect.Play();
            Die();


        }
        else BloodEffect();

        if (character.characterStats.fireCultist <= 0) // enemy
        {
            txt.color = characterMenager.GetEnemyDamageColor(type);
        }
        Destroy(txtObj, 0.8f);


        charEffects.PlayDamageSound(type);

        if (type != DamageType.physics && !isDead) // DAMAGE TYPE
        {
            if (type == DamageType.fire)
            {

                character.characterStats.fireDamageLeft += realDamage;
                if (setOnFire == null) setOnFire = StartCoroutine(SetOnFire());
            }
            else if (type == DamageType.frozen)
            {

                character.characterStats.freezeTimeLeft += realDamage;
                if (characterAction == null) characterAction = StartCoroutine(FreezeCharacter());
            }
            else if (type == DamageType.electric)
            {
                if (electricShock == null) electricShock = StartCoroutine(ElectricShock());
                else
                {
                    StopCoroutine(electricShock);
                    charEffects.RemoveCharacterEffect(CharacterEffect.electricity);
                    electricShock = StartCoroutine(ElectricShock());
                }
            }
        }

        if (attackVector != Vector3.zero) // KNOCKBACK
        {
            if (knockback != null) StopCoroutine(knockback);
            knockback = StartCoroutine(Knockback(attackVector, knockbackForce));
        }
        else
        {
            attackCollider.enabled = true;
        }

        weaponParent.transform.eulerAngles = new Vector3(0, 0, 0);

        
    }

    Coroutine knockback;
    IEnumerator Knockback(Vector3 attackVector, float knockbackForce)
    {

        animator.SetBool("knockback", true);


        attackCollider.enabled = false;
        if(!isDead) navMeshAgent.isStopped = true;

        attackVector = new Vector3(attackVector.x, 0, attackVector.z);


        rb.velocity = attackVector.normalized * (knockbackForce / (character.characterStats.characterSizeMultiplayer + 1));
        if ((knockbackForce / (character.characterStats.characterSizeMultiplayer + 1)) > knockbackToDamagePlus) animator.SetBool("damage+", true);
        
        if(character.characterStats.freezeTimeLeft <= 0) animator.SetTrigger("damage");


        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);


        while (rb.velocity.magnitude > 0.04f)
        {
            yield return null;
        }

        animator.SetBool("knockback", false);


        yield return new WaitForSeconds(0.15f);
        if (!isDead)
        {
            animator.SetBool("damage+", false);
            if(navMeshAgent.enabled) navMeshAgent.isStopped = false;
            MoveTo(movingTo);
            attackCollider.enabled = true;
        }

        knockback = null;

    }

    [HideInInspector] public bool isDead;
    void Die()
    {
        animator.SetBool("knockback", false);

        characterMenager.RemoveCharacter(this);
        isDead = true;
        animator.enabled = true;
        animator.SetBool("die", true);
        StopAllCoroutines();
        navMeshAgent.enabled = false;
        attackCollider.enabled = false;

        bodyCollider.enabled = false;
        weaponParent.gameObject.SetActive(false);
        charEffects.soundComponent.PlaySound(GameSound.characterDeath);
        charEffects.RemoveAllCharacterEffect();
        

        DropResource();
        DropWeapon();
        characterMenager.KillCharacter(this);

        Destroy(gameObject, 1f);


    }

    void BloodEffect(bool bloodExplosion = true)
    {

        float x = 0;
        if (character.characterStats.hp < character.characterStats.maxHp)
        {
            x = (-((float)character.characterStats.hp / character.characterStats.maxHp) + 1.15f) * 0.6f;
        }

        spriteRenderer.material.SetFloat("_BloodEffect", x);


        if (bloodExplosion) bloodEffect.Play();

    }

    

    [Tooltip("drop after die")]
    void DropResource()
    {

        Transform parentToSpawn = GameObject.FindGameObjectWithTag("stuffMenager").transform;
        if (transform.parent != null) parentToSpawn = transform.parent;

        foreach (var item in dropItems)
        {
            float x = Random.Range(0f, 1f);


            if (x < item.dropChance)
            {

                int count = Random.Range(1, item.maxCount);

                if (item.item.name == "bone" || item.item.name == "bone Variant") // for bone drop
                {
                    count = Random.Range(1, item.maxCount + (int)(item.maxCount * characterMenager.mapMenager.GetStatsValue(PlayerStat.bonesFromEnemy)));
                }
                else
                {
                    count = Random.Range(1, item.maxCount);
                }

                for (int i = 0; i < count; i++)
                {
                    Vector3 randPos = new Vector3(Random.Range(-resourceVelocity, resourceVelocity), 0, Random.Range(-resourceVelocity, resourceVelocity)) + rb.velocity;

                    Resource res = Instantiate(item.item, transform.position, item.item.transform.rotation, parentToSpawn).GetComponent<Resource>();
                    res.rb.isKinematic = false;
                    res.rb.velocity = randPos;

                }


            }
        }

    }

    void DropWeapon()
    {
        Transform parentToSpawn = GameObject.FindGameObjectWithTag("stuffMenager").transform;
        if (transform.parent != null) parentToSpawn = transform.parent;


        float x = Random.Range(0f, 1f);

        if(x < chanceForDropWeapon + (chanceForDropWeapon * characterMenager.mapMenager.GetStatsValue(PlayerStat.chanceForEnemyDropWeapon)))
        {
            Vector3 rand = new(Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));

            Weapon weapon = Instantiate(startMainWeapon, transform.position, startMainWeapon.transform.rotation, parentToSpawn).GetComponent<Weapon>();


            weapon.Throw(rand, weaponVelocity, false, true);
        }

    }

    Coroutine hpHealingPorSecond;
    IEnumerator HpHealingPerSecond()
    {
        while (character.characterStats.hpHealingPerSec > 0)
        {
            yield return new WaitForSeconds(1);
            if(character.characterStats.hp < character.characterStats.maxHp)
            {
                character.characterStats.hp += character.characterStats.hpHealingPerSec;
                if(character.characterStats.hp > character.characterStats.maxHp) character.characterStats.hp = character.characterStats.maxHp;
            }
            BloodEffect(false);
        }
        hpHealingPorSecond = null;
    }

    Coroutine setOnFire;
    IEnumerator SetOnFire()
    {
        charEffects.AddCharacterEffect(CharacterEffect.fire);
        while (character.characterStats.fireDamageLeft > 0 || character.characterStats.fireDamagePerSecond > 0)
        {
            spriteRenderer.material.SetColor("_BloodColor", characterMenager.fireBloodColor);
            int dmg = character.characterStats.fireDamageLeft + character.characterStats.fireDamagePerSecond;
            character.characterStats.fireDamageLeft /= 2;
            GameObject txtObj = Instantiate(damageText, transform.position, transform.rotation, transform.parent);
            TextMeshProUGUI txt = txtObj.GetComponentInChildren<TextMeshProUGUI>();
            if (dmg < 0) dmg = 0;
            txt.text = dmg.ToString();
            if(character.characterStats.fireCultist <= 0)
            {
                txt.color = characterMenager.GetEnemyDamageColor(DamageType.fire);
            }
            Destroy(txtObj, 0.8f);

            if (character.characterStats.hp < 0) break;

            character.characterStats.hp -= dmg;

            if (character.characterStats.hp < 0) Die();
            
            yield return new WaitForSeconds(0.8f);
        }
        charEffects.RemoveCharacterEffect(CharacterEffect.fire);

        spriteRenderer.material.SetColor("_BloodColor", characterMenager.bloodColor);

        setOnFire = null;
    }


    IEnumerator FreezeCharacter()
    {
        attackCollider.enabled = false;
        navMeshAgent.speed = 0;
        ActualizeAnimation(Vector3.zero);
        charEffects.AddCharacterEffect(CharacterEffect.ice);
        yield return new WaitForSeconds(0.3f);
        animator.enabled = false;
        while (character.characterStats.freezeTimeLeft > 0)
        {
            if (character.characterStats.hp < 0) break;

            yield return new WaitForSeconds(1f / (character.characterStats.freezeResist + 1f));

            character.characterStats.freezeTimeLeft -= 1;
        }
        animator.enabled = true;
        charEffects.RemoveCharacterEffect(CharacterEffect.ice);
        characterAction = null;
        ResetCharacterStats();
        if(knockback == null && !isDead)
        {
            attackCollider.enabled = true;
            MoveTo(movingTo);
        }
    }

    Coroutine electricShock;
    IEnumerator ElectricShock()
    {
        charEffects.AddCharacterEffect(CharacterEffect.electricity);

        yield return new WaitForSeconds(0.8f);

        charEffects.RemoveCharacterEffect(CharacterEffect.electricity);

        electricShock = null;
    }

   

    [Tooltip("destroy characterMove, rigidbody, and navMesh for playerMove")]
    public void DestroyCharacterMove()
    {
        transform.localScale = Vector3.one * (1 + character.characterStats.characterSizeMultiplayer);
      //  Destroy(attackCollider);
      //  Destroy(GetComponent<Rigidbody>());
        if (navMeshAgent != null) Destroy(navMeshAgent);
        Destroy(this);
    }


    public void SetFireCultistLevel(int fireCultist)
    {

        

        character.characterStats.fireCultist = fireCultist;

        if(fireCultist > 0)
        {
            spriteRenderer.material = characterMenager.allyMaterial;
            gameObject.tag = "ally";

            if (interaction != null)
            {
                interaction.gameObject.SetActive(true);
            }

        }
        else
        {
            spriteRenderer.material = characterMenager.enemyMaterial;
            gameObject.tag = "enemy";

            if (interaction != null)
            {
                interaction.gameObject.SetActive(false);


            }

        }
        character.ResetSpells();
    }

    public void Slip()
    {
        if (isDead) return;
        if (characterAction != null && character.characterStats.freezeTimeLeft <= 0)
        {
            StopCoroutine(characterAction);
            StopAllCharging();

            characterAction = null;
            animator.SetBool("prepareAttack", false);
            animator.SetBool("specjalCharge", false);
        }
        if (knockback != null) StopCoroutine(knockback);
        knockback = StartCoroutine(Knockback(Vector3.zero, 10000));

        weaponParent.transform.eulerAngles = new Vector3(0, 0, 0);
    }
   
    public void DropDeadBody(Vector3 attackVector, float force)
    {
        DeadBody body = Instantiate(deadBody, transform.position, transform.rotation, stuffMenager).GetComponent<DeadBody>();
        body.transform.localScale = transform.localScale;
        body.CreateDeadBody(spriteRenderer.sprite, attackVector, force);
        spriteRenderer.enabled = false;


    }


    IEnumerator ChargeModifier(bool isSpecjal)
    {

        if (navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.isStopped = true;
        }

        character.StartChargingModiferUpgrade(isSpecjal);



        if (isSpecjal)
        {
            charEffects.soundComponent.PlaySound(GameSound.characterWalk);


            animator.SetBool("specjalCharge", true);

            yield return new WaitForSeconds(character.specjalChargingModifer.loadedModifierChargeTime);
            characterAction = null;

        }
        else
        {

            charEffects.soundComponent.PlaySound(GameSound.characterUpgrade);


            animator.SetBool("attackCharge", true);

            yield return new WaitForSeconds(character.attackChargingModifer.loadedModifierChargeTime);

        }

        animator.SetBool("specjalCharge", false);
        animator.SetBool("attackCharge", false);





        if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = false;
            ActualizeAnimation(navMeshAgent.velocity);
        }

        MoveTo(movingTo);

        if (knockback == null && !isDead)
            attackCollider.enabled = true;

    }

    public void StopAllCharging()
    {

        character.BreakAnyCharging();


        animator.SetBool("specjalCharge", false);
        animator.SetBool("attackCharge", false);

        characterAction = null;

    }

    IEnumerator SpecjalChargeLoop()
    {
        float chargeTime = character.specjalChargingModifer.loadedModifierChargeTime + character.specjalChargingModifer.loadedModifierCooldown + character.specjalChargingModifer.loadedModifierDuration + 1;

        yield return new WaitForSeconds(Random.Range(1, 12));

        while (enabled)
        {
            StartAction(CharacterAction.specjalCharge, movingTo);

            yield return new WaitForSeconds(chargeTime);

            while (!(characterAction == null && knockback == null) && !IsEnemyNearby())
            {
                yield return new WaitForSeconds(3);

            }
        }
    }

    public bool IsEnemyNearby()
    {
        // Ustalanie tagu przeciwnika – przyjmujemy, ¿e jeœli nasz tag to "enemy",
        // to przeciwnik ma tag "ally", a jeœli nasz tag to "ally", to przeciwnik ma tag "enemy".
        string enemyTag = "";
        if (gameObject.CompareTag("enemy"))
            enemyTag = "ally";
        else if (gameObject.CompareTag("ally"))
            enemyTag = "enemy";
        else
            return false; // Jeœli tag nie pasuje do znanych, nie wykrywamy przeciwników.

        // U¿ywamy promienia kolizji z komponentu attackCollider, jeœli jest dostêpny.
        // W przeciwnym razie ustawiamy domyœln¹ wartoœæ.
        

        float detectionRadius = attackCollider != null ? attackCollider.radius * transform.lossyScale.y * 1.7f : 10f;

        // Ograniczamy wyszukiwanie do tej samej warstwy, na której znajduje siê ten obiekt.
        int layerMask = 1 << gameObject.layer;

        // Znajdujemy wszystkie kolidery w zadanym promieniu, które znajduj¹ siê na tej samej warstwie.
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, layerMask);
        foreach (Collider col in colliders)
        {
            // Pomijamy sam obiekt i sprawdzamy, czy znaleziony obiekt ma tag przeciwnika.
            if (col.gameObject != gameObject && col.gameObject.CompareTag(enemyTag))
            {
                return true;
            }
        }
        return false;
    }




}
