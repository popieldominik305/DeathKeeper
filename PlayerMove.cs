using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

public enum InteractionType
{
    takeItem,
    startDialog,
    openChest,
    getModifer,
    getArtifact,
    teleport
}
public enum PlayerAction
{
    attack,
    prepareThrow,
    dash,
        
}

public class PlayerMove : MonoBehaviour
{
    [HideInInspector] public PlayerSetting playerSetting;
    [HideInInspector] public float defaultMoveSpeed;
    public Transform graphicCharacter;
    [HideInInspector] public Character character;
    public LayerMask groundLayer;
    [SerializeField] float dashTime = 0.2f;
    [SerializeField] float dashCooldown = 0.1f;
    [SerializeField] float waitBeforeAttack = 0.1f;
    [Tooltip("1 = deafultSpeed")] [SerializeField] float slowsAfterDash = 1f;
    [SerializeField] float dashSpeedAdd;
    [SerializeField] float sprintSpeedCooldown;
    [SerializeField] float sprintAnimMultiply = 1.2f;
    [Tooltip("1 - speedMultiply dont effect on dash langht, 0 - full effect")]
    [SerializeField] [Range(0f,1f)] float dashAnimMultiply = 0.4f;
    [SerializeField] float throwLoadingTime;
    [SerializeField] float throwPower;
    LayerMask uiLayer = 5;
    [SerializeField] Transform throwArrow;
    [HideInInspector] public Transform weaponParent;
    [HideInInspector] public Animator animator;
    Animator throwArrowAnim;
    [HideInInspector] public Rigidbody rb;
    private PlayerInput controls;
    [Tooltip("Y as Z")] Vector2 movement;
    [HideInInspector] public PlayerEq playerEq;
    [HideInInspector] public CardEq cardEq;
    [HideInInspector] public GateComponnent gateComponnent;
    public Collider bodyCollider;
    [HideInInspector] public bool allyOrder = false;
    [SerializeField] VisualEffect playerStepsVFX;
    [HideInInspector] public Animator pauseMenu;
    [HideInInspector] public TimeMenager timeMenager;
    [HideInInspector] public CharEffects charEffects;
    [SerializeField] GameObject damageText;
    [HideInInspector] public VisualEffect bloodEffect;
    [Tooltip("Animator dla domyœlnej animacji")]
    public RuntimeAnimatorController defaultAnim;
    public Material playerSpriteMaterial;
    StoryMenager storyMenager;
    SpriteRenderer spriteRenderer;
    [Header("Sounds")]
    public SoundComponent playerSounds;
    public SoundComponent chargingModifersSounds;
    [HideInInspector] public MapMenager mapMenager;
    [Header("statistic: ")]
    [SerializeField] int itemUseOnAttack = 10;
    float moveSpeed = 5f;
    public float playerSpeedMultiplayer;
    float defaultDrag = 4f; // change in start() or in inspektor
    [Tooltip("rigidbody.drag when you are frozen")]
    public float iceDrag = 15f;
    [HideInInspector] public ResourceCollector resourceCollector;
    //   INTERAKCJE 
    List<Interactions> interactionsList = new();
    ArtifactsManager artifactsManager;
    public InteractionType[] eInteractionsTypes;
    [HideInInspector] public SphereCollider pickupColider;
    [HideInInspector] public float defaultPickupRange;
    [Header("animators: ")]
    public Animator winAnimator;
    public Animator dieAnimator;
    public Animator gateDestroyAnimator;

    [Header("artifacts effects")]
    public Animator electricOnDash;
    public Vector2 fireProjectileShootVector;
    public float fireProjectileForceMin;
    public float fireProjectileForceMax;
    [Tooltip("used on throw")] public GameObject fireProjectilePrefab;
    public GameObject iceExplosionPrefab;
    public GameObject resurectionSoul;
    public LayerMask resurectionMask;

    public float maxInteractionDistance = 12f;

    List<TrailRenderer> dashTrails;


    public class Interactions
    {
        public InteractionType interactionType;
        public GameObject objectToInteract;
        public Interaction interactionObj;
    }
    private static PlayerMove instance;

    Shooter playerShooter;

    [SerializeField] float minKnockbackVelocity = 0.1f;
    [Tooltip("if higher dissolve will move faster")]
    [SerializeField] float dissolveOffsetOnDash;
    [SerializeField] GameObject dashEffects;
    [SerializeField] Animator dashEffectsAnim;

    [Header("Player skills: ")]

    [SerializeField] Image attackChargeImage;
    [SerializeField] Image specjalChargeImage;
    [SerializeField] Color chargingColor;
    [SerializeField] Color activeColor;
    [SerializeField] Color cooldownColor;
    [SerializeField] Image attackChargeImageClock;
    [SerializeField] Image specjalChargeImageClock;
    [SerializeField] TextMeshProUGUI attackChargeTimeText;
    [SerializeField] TextMeshProUGUI specjalChargeTimeText;



    [Tooltip("float, player hp multiply")] [HideInInspector] public float playerHpMultiply;
    [Tooltip("float, player speed multiply")][HideInInspector] public float playerSpeedMultiply;
    [Tooltip("int, player defense bonus")][HideInInspector] public float playerDefenseBonus;
    [Tooltip("int, player healing per s bonus")] [HideInInspector] public float playerHealingPerSecBonus;
    [Tooltip("float, player freeze resist bonus")][HideInInspector] public float playerFreezeResistBonus;
    [Tooltip("float, player size multiply")][HideInInspector] public float playerSizeMultiply;
    [Tooltip("float, player attack speed multiply")][HideInInspector] public float playerAttackSpeedMultiply;
    [Tooltip("float, player melee multiply")][HideInInspector] public float playerMeleeAttackMultiply;
    [Tooltip("float, player wand multiply")][HideInInspector] public float playerWandAttackMultiply;
    [Tooltip("float, playerKnockbacResist")][HideInInspector] public float playerKnockbacResist;
    [Tooltip("float, player item use multiply")][HideInInspector] public float playerItemUse;
    [Tooltip("float, player speed on attack")][HideInInspector] public float playerSpeedOnAttack;
    [Tooltip("float, def = 0")][HideInInspector] public float dashSpeedMultiply;
    [Tooltip("float, def = 0")][HideInInspector] public float sprintSpeedMultiply;
    [Tooltip("float, def = 0")][HideInInspector] public float sprintCooldownMultiply;


    private void Awake()
    {
      

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        playerShooter = GetComponent<Shooter>();
        mapMenager = FindAnyObjectByType<MapMenager>();
        artifactsManager = mapMenager.GetComponent<ArtifactsManager>();
        pickupColider = GetComponent<SphereCollider>();
        defaultPickupRange = pickupColider.radius;
        /* if (SaveGame.GetKilledCharacters().Count > 0)
         {
             GameObject lastKilled;
             List<CharacterMenager.KilledCharacters> killed = SaveGame.GetKilledCharacters();
             if (killed[^1].isAlly)
             {
                 lastKilled = mapMenager.GetAllyByName(killed[^1].name);

             }
             else
             {
                 lastKilled = mapMenager.GetEnemyByName(killed[^1].name);

             }
            // SaveGame.ClearKilledCharacter();

            // if (lastKilled != null) Resurection(lastKilled);


         }*/


        controls = new PlayerInput();

        playerSetting = GetComponent<PlayerSetting>();

        controls.Gameplay.Move.performed += ctx => MoveCharacter(ctx.ReadValue<Vector2>());
        controls.Gameplay.Scroll.performed += ctx => playerEq.ScrollMove(ctx.ReadValue<float>());

        controls.Gameplay.Move.canceled += ctx => MoveCharacter(Vector2.zero);
        controls.Gameplay.Fclick.started += ctx => Interact();
        controls.Gameplay.Eclick.started += ctx => Interact(true);
        controls.Gameplay.SpaceClick.started += ctx => StartAction(PlayerAction.dash, Vector3.zero);
        controls.Gameplay.EscClick.started += ctx => EscClick();
        controls.Gameplay.IClick.started += ctx => SetOffOn();

        if (graphicCharacter.childCount > 0) GetBodyComponetnts();
    }

    void Start()
    {
        resourceCollector = GetComponent<ResourceCollector>();
        mapMenager = FindAnyObjectByType<MapMenager>();
        playerEq = GetComponent<PlayerEq>();
        cardEq = GetComponent<CardEq>();
        timeMenager = GetComponent<TimeMenager>();
        rb = GetComponent<Rigidbody>();
        defaultDrag = rb.drag;
        animator = graphicCharacter.GetComponentInChildren<Animator>();
        character = graphicCharacter.GetComponentInChildren<Character>();
        spriteRenderer = graphicCharacter.GetComponentInChildren<SpriteRenderer>();
        throwArrowAnim = throwArrow.GetComponent<Animator>();
        storyMenager = GetComponent<StoryMenager>();    
        allyOrder = false;
        spriteRenderer.material.SetFloat("_BloodEffect", 0.45f);

        bodyCollider.transform.localScale = Vector3.one * (1 + character.characterStats.characterSizeMultiplayer);
        weaponParent.localScale = Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer);

        ActualizeSkillsImage();

        ResetPlayerStats();
        playerStepsVFX.Stop();

        playerShooter.numberOfProjectiles = (int)mapMenager.GetStatsValue(PlayerStat.projectileOnAttack);

        if(mapMenager != null)
        {
            playerSpeedOnAttack = mapMenager.GetStatsValue(PlayerStat.playerMoveOnAttack);
            dashSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.dashSpeedMultiply);
            sprintSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.sprintSpeedMultiply);
            sprintCooldownMultiply = mapMenager.GetStatsValue(PlayerStat.sprintCooldownMultiply);


        }

        

    }
    [SerializeField] DeveloperConsole dev;
    void SetOffOn()
    {
        dev.SetOffOn();
    }
    private void Update()
    {

        if (!isDead)
        {
            if (knockback == null)
            {
                if (!dash)
                {
                    rb.velocity = new Vector3(movement.x, 0, movement.y) * moveSpeed * playerSpeedMultiplayer * (playerSpeedMultiply + 1) * activeSprintMultiply;
                    if (movement != Vector2.zero && moveSpeed > 0)
                    {
                        playerSounds.audioSource.pitch = ((rb.velocity.magnitude / 10f) / (bodyCollider.transform.localScale.y)) * Time.timeScale;
                        if (!playerSounds.audioSource.isPlaying) playerSounds.PlaySound(GameSound.characterWalk, true);

                        if(activeSprintMultiply > 1)
                        {
                            animator.speed = sprintAnimMultiply;
                        }
                        animator.speed = 1;

                    }
                    else playerSounds.StopSound(GameSound.characterWalk);
                }
                else
                {
                    rb.velocity = dashMovement * moveSpeed * playerSpeedMultiplayer * (playerSpeedMultiply + 1);
                }
            }


            if (Input.GetMouseButtonDown(0) && !mouseOnUi && !allyOrder)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
                {
                    StartAction(PlayerAction.attack, hit.point - weaponParent.transform.position);

                }



            }

            else if (Input.GetMouseButtonDown(1) && !mouseOnUi && !allyOrder)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
                {
                    StartAction(PlayerAction.prepareThrow, hit.point - weaponParent.transform.position);
                }


            }

            if (interactionsList.Count > 0)
            {
               
                

                ShowClosestInteraction();
            }
        }

    }
    public Coroutine playerAction;
    public Coroutine dashAttack;
    void StartAction(PlayerAction actionType, Vector3 vector)
    {
        if(playerAction == null && Time.timeScale != 0 && !cardEq.IsCardInUse() && !isDead && knockback == null)
        {
            if(actionType == PlayerAction.attack)
            {
                playerAction = StartCoroutine(Attack(vector));
            }
            else if(actionType == PlayerAction.prepareThrow)
            {
                playerAction = StartCoroutine(PrepareThrow());
            }
            else if(actionType == PlayerAction.dash && movement != Vector2.zero && !isDashCooldown)
            {
                playerAction = StartCoroutine(Dash());
            }
            else if (actionType == PlayerAction.dash && movement == Vector2.zero)
            {
                if(character != null && character.specjalChargingModifer.isLoadedModiferReady)
                {
                    playerAction = StartCoroutine(ChargeModifier(true));
                }
            }

            if (sprintCooldown != null) StopCoroutine(sprintCooldown);

            sprintCooldown = StartCoroutine(SprintCooldown());


        }
        else if(Time.timeScale != 0 && !cardEq.IsCardInUse() && dash && actionType == PlayerAction.attack)
        {
            if(dashAttack==null && playerEq.haveItemInHands) dashAttack = StartCoroutine(DashAttack(new Vector3(movement.x, 0, movement.y)));
        }
    }

    float activeSprintMultiply = 1;
    Coroutine sprintCooldown;
    IEnumerator SprintCooldown()
    {
        activeSprintMultiply = 1;

        yield return new WaitForSeconds(sprintSpeedCooldown * (1 + sprintCooldownMultiply));

        activeSprintMultiply = 1 + sprintSpeedMultiply;

    }


    public void SetMoveActive(bool isActive)
    {
        isDead = !isActive;
    }


    void MoveCharacter(Vector2 move)
    {
        if (move != Vector2.zero)
        {
            playerStepsVFX.Play();
        }else playerStepsVFX.Stop();

        movement = move;

        ActualizeAnimation(movement);

    }
    void ActualizeAnimation(Vector2 move)
    {
        if (playerAction == null && Time.timeScale != 0)
        {
            if (move == Vector2.zero)
            {
                animator.SetBool("walk", false);
            }
            else
            {
                animator.SetBool("walk", true);


                if (move.x != 0) // zmiana obrotu potaci
                {
                 
                    
                    RotateCharacter(move);
                }
                
            }
        }
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        if(controls != null) controls.Gameplay.Disable();
    }


    void RotateCharacter(Vector3 lookVector)
    {
        if (lookVector.x < 0 && graphicCharacter.transform.localScale.x > 0) // zmiana obrotu potaci
        {
            graphicCharacter.transform.localScale = new(-1, 1, 1);
            character.charEffects.transform.localScale = new(-1, 1, 1);

            Weapon activeWeapon = weaponParent.GetComponentInChildren<Weapon>();

            if (playerEq.haveItemInHands && activeWeapon!=null) activeWeapon.animator.SetBool("stopAttacking", true);
        }
        else if (lookVector.x > 0 && graphicCharacter.transform.localScale.x < 0)
        {
            graphicCharacter.transform.localScale = new(1, 1, 1);
            character.charEffects.transform.localScale = new(1, 1, 1);
            Weapon activeWeapon = weaponParent.GetComponentInChildren<Weapon>();

            if (playerEq.haveItemInHands && activeWeapon != null) weaponParent.GetComponentInChildren<Weapon>().animator.SetBool("stopAttacking", true);
        }
    }

    

    public void AddInteraction(InteractionType type, GameObject obj, Interaction interaction)
    {
        interactionsList.Add(new Interactions {interactionType = type, objectToInteract = obj, interactionObj = interaction });
    }
    public void RemoveInteraction(InteractionType type, GameObject obj)
    {
        foreach(Interactions interact in interactionsList)
        {
            if(obj == interact.objectToInteract)
            {
                interact.interactionObj.HideButton();
                interactionsList.Remove(interact);
                break;
            }
        }

    }

    void ShowClosestInteraction()
    {


        for (int i = 0; i < interactionsList.Count; i++)
        {
            if (interactionsList[i].interactionObj == null || interactionsList[i].objectToInteract == null)
            {
                if (interactionsList[i].interactionObj != null) interactionsList[i].interactionObj.HideButton();
                interactionsList.RemoveAt(i);
                i--;
            }
        }
        if (interactionsList.Count < 1) return;
        Interactions interactionF = new();
        Interactions interactionE = new();
        float x = 100;
        float y = 100;
        foreach (var element in interactionsList)
        {
            if(element.interactionObj != null && element.interactionObj.isActiveAndEnabled)
            {
                if (Vector3.Magnitude(element.interactionObj.transform.position - transform.position) < x && !eInteractionsTypes.Contains(element.interactionType))
                {
                    interactionF = element;
                    x = Vector3.Magnitude(element.interactionObj.transform.position - transform.position);
                }
                else if (Vector3.Magnitude(element.interactionObj.transform.position - transform.position) < y && eInteractionsTypes.Contains(element.interactionType))
                {
                    interactionE = element;
                    y = Vector3.Magnitude(element.interactionObj.transform.position - transform.position);
                }
            }
           

        }
        foreach (var element in interactionsList)
        {
            if(element.interactionObj == interactionF.interactionObj || element.interactionObj == interactionE.interactionObj)
            {
                element.interactionObj.ShowButton();
            }
            else
            {
                element.interactionObj.HideButton();
            }
        }
    }

    public void Interact(bool isE = false)
    {
        if(playerAction == null && Time.timeScale > 0)
        {
            if (interactionsList.Count > 0)
            {
                for (int i = 0; i < interactionsList.Count; i++)
                {
                    if (interactionsList[i].interactionObj == null || interactionsList[i].objectToInteract == null)
                    {
                        if (interactionsList[i].interactionObj != null) interactionsList[i].interactionObj.HideButton();
                        interactionsList.RemoveAt(i);
                        i--;
                    }
                }
                if (interactionsList.Count < 1) return;

                Interactions interaction = interactionsList[0];

                if (isE)
                {
                    float x = 100;
                    foreach (var element in interactionsList)
                    {
                        if (Vector3.Magnitude(element.interactionObj.transform.position - transform.position) < x)
                        {
                            if (eInteractionsTypes.Contains(element.interactionType))
                            {
                                interaction = element;
                                x = Vector3.Magnitude(element.interactionObj.transform.position - transform.position);
                            }
                        }
                    }
                    if (!eInteractionsTypes.Contains(interaction.interactionType))
                    {
                        return;
                    }

                }
                else
                {
                    float x = 100;
                    foreach (var element in interactionsList)
                    {
                        if (Vector3.Magnitude(element.interactionObj.transform.position - transform.position) < x)
                        {
                            if (!eInteractionsTypes.Contains(element.interactionType))
                            {
                                interaction = element;
                                x = Vector3.Magnitude(element.interactionObj.transform.position - transform.position);
                            }
                        }
                    }
                    if (eInteractionsTypes.Contains(interaction.interactionType))
                    {
                        return;
                    }
                }

                if (interaction.interactionType == InteractionType.takeItem)
                {
                    playerEq.AddItem(interaction.objectToInteract, true);
                    RemoveInteraction(InteractionType.takeItem, interaction.objectToInteract);
                }
                else if (interaction.interactionType == InteractionType.startDialog)
                {
                    interaction.objectToInteract.SetActive(true);
                    if (!interaction.interactionObj.doNotRemoveAfterInteract)
                    {
                        RemoveInteraction(InteractionType.startDialog, interaction.objectToInteract);

                    }
                }
                else if (interaction.interactionType == InteractionType.openChest)
                {
                    interaction.objectToInteract.GetComponent<Chest>().OpenChest();
                    RemoveInteraction(InteractionType.openChest, interaction.objectToInteract);
                    
                }else if(interaction.interactionType == InteractionType.getModifer)
                {
                    interaction.interactionObj.AddPlayerModifer(this);


                    RemoveInteraction(InteractionType.getModifer, interaction.objectToInteract);
                }
                else if (interaction.interactionType == InteractionType.getArtifact)
                {
                    interaction.objectToInteract.GetComponent<ArtifactOnGround>().GetArtifact();


                    RemoveInteraction(InteractionType.getArtifact, interaction.objectToInteract);
                }
                else if (interaction.interactionType == InteractionType.teleport)
                {
                    transform.position = new Vector3(0, 0, -3);
                    animator.Play("create");

                    GrassGenerator grassGenerator = FindAnyObjectByType<GrassGenerator>();
                    if(grassGenerator != null) grassGenerator.ResetGrass();

                    RemoveInteraction(InteractionType.teleport, interaction.objectToInteract);
                }

            }
                
        }

    }

    
    public void EscClick()
    {
        if (pauseMenu.gameObject.activeSelf)
        {
            timeMenager.TimeStart(this);
            pauseMenu.gameObject.SetActive(false);
        }
        else
        {
            timeMenager.TimeStop(this);
            pauseMenu.gameObject.SetActive(true);
            pauseMenu.transform.GetChild(0).gameObject.SetActive(true);
            pauseMenu.transform.GetChild(1).gameObject.SetActive(false);
        }
    }
    
    IEnumerator DashAttack(Vector3 attackVector)
    {
        Weapon weapon = weaponParent.GetComponentInChildren<Weapon>();

        float playerItemUseModifer = 1 + playerItemUse;
        if (playerItemUseModifer < 0) playerItemUseModifer = 0;
        weapon.Attack((int)((itemUseOnAttack / (1 + character.characterStats.reduceWeaponUse)) * (1 + playerItemUseModifer)), attackVector, true);

        yield return null;
        float animTime = weapon.animator.GetCurrentAnimatorStateInfo(0).length;
        float startTime = Time.time;
        do
        {
            yield return new WaitForEndOfFrame();
            
            weaponParent.transform.eulerAngles = new Vector3(0, CodeMonkeyDirection(attackVector), 0);
        } while (dash && Time.time - startTime < animTime);

        if (weapon.itemHp < 0) playerEq.RemoveItem(weapon, true);
        resetWeaponParent = StartCoroutine(ResetWeaponParent());

        dashAttack = null;
    }


    IEnumerator Attack(Vector3 attackVector)
    {

        throwArrowAnim.SetBool("celuj", false);

        moveSpeed = defaultMoveSpeed * (playerSpeedOnAttack + character.characterStats.moveOnAttack);
        RotateCharacter(attackVector);


        // Debug.Log(attackVector + "    " + CodeMonkeyDirection(attackVector));
        if (resetWeaponParent != null) StopCoroutine(resetWeaponParent);


        Weapon weapon = weaponParent.GetComponentInChildren<Weapon>();

        if (weapon == null) playerEq.haveItemInHands = false;

        if (playerEq.haveItemInHands) // Attack
        {


            float attackSpeed = ((1 + character.characterStats.attackSpeedMultiplayer) * (1 + playerAttackSpeedMultiply));


            animator.SetBool("prepareAttack", true);
            yield return new WaitForSeconds(waitBeforeAttack / attackSpeed);

            if (character.attackChargingModifer.isLoadedModiferReady)
            {
                if (Input.GetMouseButton(0))
                {
                    StartCoroutine(ChargeModifier(false));
                }
            }

            while (Input.GetMouseButton(0))
            {
                yield return null;
            }


            animator.SetBool("prepareAttack", false);

            weaponParent.transform.eulerAngles = new Vector3(0, CodeMonkeyDirection(attackVector), 0);
           // animator.SetTrigger("attack");
            
            yield return new WaitForFixedUpdate();
            

            weapon.animator.speed = 1 * attackSpeed;

            float playerItemUseModifer = 1 + playerItemUse;
            if (playerItemUseModifer < 0) playerItemUseModifer = 0;
            weapon.Attack((int)((itemUseOnAttack / (1 + character.characterStats.reduceWeaponUse)) * (1 + playerItemUseModifer)), attackVector);

            yield return new WaitForSeconds(weapon.weaponAttackTime / attackSpeed);



            weapon.animator.speed = 1;

            if (weapon.itemHp < 0) playerEq.RemoveItem(weapon, true);

            if (playerShooter.numberOfProjectiles > 0.5f)
            {
                playerShooter.FireProjectiles(attackVector);
            }

        }



        MoveCharacter(movement);

        resetWeaponParent = StartCoroutine(ResetWeaponParent());


        if (isDashCooldown) moveSpeed = defaultMoveSpeed * slowsAfterDash;
        else moveSpeed = defaultMoveSpeed;
        playerAction = null;
        ActualizeAnimation(movement);

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


    [HideInInspector] public int healingForThrow = 0;


    IEnumerator PrepareThrow()
    {
        moveSpeed = 0;

        throwArrowAnim.SetBool("celuj", true);
        animator.SetBool("prepareAttack", true);

        _ = ThrowBonusTime();
        float _throwPower = 0;
        Vector3 throwVector = Vector3.zero;
        do
        {
            yield return new WaitForSeconds(throwLoadingTime / 25);
            _throwPower += 0.04f;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                RotateCharacter(hit.point - weaponParent.transform.position);

                
                throwVector = hit.point - weaponParent.transform.position;
                throwArrow.eulerAngles = new(0, CodeMonkeyDirectionNonScale(throwVector), 0);
                
            }
            

        } while (Input.GetMouseButton(1));

        if (playerEq.haveItemInHands)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                if ((hit.point - weaponParent.transform.position).x < 0) { graphicCharacter.transform.localScale = new(-1, 1, 1); }
                else { graphicCharacter.transform.localScale = new(1, 1, 1); }
                throwVector = hit.point - weaponParent.transform.position;
                throwArrow.eulerAngles = new(0, CodeMonkeyDirectionNonScale(throwVector), 0);
            }

            

            if (_throwPower > 1) _throwPower = 1;
            Weapon weapon = weaponParent.GetComponentInChildren<Weapon>();
            weapon.Throw(throwVector, _throwPower * throwPower, throwBonus);

            playerEq.RemoveItem(weapon);


            if (healingForThrow > 0)
            {
                character.AddModifier(new Character.CharacterStats { hpHealingPerSec = healingForThrow, effectType = CharacterEffect.healing }, 1.5f);
            }
           

        }
        animator.SetBool("prepareAttack", false);
        throwArrowAnim.SetBool("celuj", false);
        MoveCharacter(movement);
        if (isDashCooldown) moveSpeed = defaultMoveSpeed * slowsAfterDash;
        else moveSpeed = defaultMoveSpeed;
        playerAction = null;
        ActualizeAnimation(movement);
    }

    bool throwBonus = false;
    async Task ThrowBonusTime()
    {
        throwBonus = false;
        await Task.Delay(500);
        throwBonus = true;
        await Task.Delay(200);
        throwBonus = false;
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

    bool dash = false;
    bool isDashCooldown = false;
    Vector3 dashMovement;

    [HideInInspector] public bool isElectricOnDash;
    [HideInInspector] public bool isIceExplosionEndDash;


    IEnumerator Dash()
    {
        
        playerSounds.PlaySound(GameSound.characterDash);
        dashMovement = new Vector3(movement.x, 0, movement.y);
        dash = true;
        animator.SetBool("dash", true);
        moveSpeed = (defaultMoveSpeed * (1 + dashSpeedMultiply)) + dashSpeedAdd;
        animator.speed = 1 + (character.characterStats.speedMultiplayer * dashAnimMultiply);

        if(dashEffectsAnim != null)
        {
            dashEffectsAnim.SetTrigger("dash");
            dashEffectsAnim.speed = 1 + (character.characterStats.speedMultiplayer * dashAnimMultiply);

        }

        if (isElectricOnDash)
        {
            electricOnDash.gameObject.SetActive(true);
            electricOnDash.speed = 1 + (character.characterStats.speedMultiplayer * dashAnimMultiply);
            electricOnDash.Play("ElectricOnDash");
        }

        bodyCollider.enabled = false;
        //  Vector3 pos = transform.position;

        float timeLeft = dashTime / animator.speed;
        Vector2 offset = new();

        Vector2 moveModifer = Vector2.one;

        if(movement.x < 0)
        {
            moveModifer = new Vector2(-1, 1);
        }


        while (timeLeft > 0)
        {
            yield return null;
            timeLeft -= Time.deltaTime;
            offset += dissolveOffsetOnDash * moveModifer * new Vector2(dashMovement.x, dashMovement.z).normalized * animator.speed * Time.deltaTime;

            

            spriteRenderer.material.SetVector("_dissolveOffset", offset);

        }

      //  float dis = Vector3.Distance(transform.position, pos);
      //  Debug.Log(" anim speed: " + animator.speed + " time after: " + dashTime / animator.speed + " time defult: " + dashTime + " dis: " + dis + " moveSpeed: " + moveSpeed);

        animator.speed = 1;
        animator.SetBool("dash", false);

        if (isElectricOnDash) electricOnDash.gameObject.SetActive(false);

        if (isIceExplosionEndDash)
        {
            GameObject explosion = Instantiate(iceExplosionPrefab, transform.position, transform.rotation, mapMenager.stuffMenager);
            Destroy(explosion, 1.5f);
        }


        moveSpeed = defaultMoveSpeed * slowsAfterDash;
        weaponParent.transform.localEulerAngles = new Vector3(0, 0, 0);

        bodyCollider.enabled = true;
        isDashCooldown = true;
        dash = false;
        playerAction = null;
        ActualizeAnimation(movement);

        if (dashCooldownRenevSpeed <= 0) dashCooldownRenevSpeed = 0.1f;

        yield return new WaitForSeconds(dashCooldown / dashCooldownRenevSpeed);
        isDashCooldown = false;
        moveSpeed = defaultMoveSpeed;
        
    }
    [HideInInspector] public float dashCooldownRenevSpeed = 1;
   
    


    List<MyUiElement> activeUiElements = new();
    public bool mouseOnUi;
    public void ActualizeUi(MyUiElement target)
    {
        if (target.isMouseOver)
        {
            activeUiElements.Add(target);
        }
        else
        {
            activeUiElements.Remove(target);
        }

        if(activeUiElements.Count > 0)
        {
            for (int i = 0; i < activeUiElements.Count; i++)
            {
                if(activeUiElements[i] == null)
                {
                    activeUiElements.RemoveAt(i);
                    i--;
                }
                else if (!activeUiElements[i].isMouseOver || !activeUiElements[i].gameObject.activeSelf)
                {
                    activeUiElements.RemoveAt(i);
                    i--;
                }
            }
        }
        
        if (activeUiElements.Count > 0)
            mouseOnUi = true;
        else mouseOnUi = false;

    }


    public void ResetPlayerStats()
    {
        // gdzie indziej - reduceWeaponUse (brakuje - knocbackResist, defense); w weapon.sc - weaponDamageMultiplayer, knockbackMultiplayer

        if (character == null || !gameObject.activeInHierarchy) return;

        defaultMoveSpeed = (character.characterStats.speed + (character.characterStats.speed * character.characterStats.speedMultiplayer)); // speed, speedMultiplayer
        if(!dash) moveSpeed = defaultMoveSpeed;

        if(bodyCollider.transform.localScale != Vector3.one * (1 + character.characterStats.characterSizeMultiplayer)) // characterSizeMultiplayer
        {
            if(setCharSize == null) setCharSize = StartCoroutine(SetCharacterSize());
        }
        if (weaponParent.localScale != Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer)) // characterSizeMultiplayer
        {
            if (setWeaponSize == null) setWeaponSize = StartCoroutine(SetWeaponSize());
        }
        

    }
    IEnumerator HpHealingPerSecond()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (character.characterStats.hp < character.characterStats.maxHp)
            {
                character.characterStats.hp += (character.characterStats.hpHealingPerSec + (int)playerHealingPerSecBonus);
                if (character.characterStats.hp > character.characterStats.maxHp) character.characterStats.hp = character.characterStats.maxHp;

                // BloodEffect

                float x = 0;

                x = (-((float)character.characterStats.hp / character.characterStats.maxHp) + 1.15f) * 0.6f;

                spriteRenderer.material.SetFloat("_BloodEffect", x);

            }

        }
    }

    Coroutine setCharSize;
    IEnumerator SetCharacterSize()
    {
        while (bodyCollider.transform.localScale != Vector3.one * ((1 + character.characterStats.characterSizeMultiplayer) * (1 + playerSizeMultiply)))
        {
            bodyCollider.transform.localScale = Vector3.MoveTowards(bodyCollider.transform.localScale, Vector3.one * ((1 + character.characterStats.characterSizeMultiplayer) * (1 + playerSizeMultiply)), 0.5f * Time.deltaTime);
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

    // Otrzymywanie obrazen


    [HideInInspector] public int fireProjectileOnDamage = 0;


    public void DealDamage(Vector3 attackVector, Vector2Int damage, float knockbackForce, DamageType type = DamageType.physics)
    {
        if (playerAction != null && character.characterStats.freezeTimeLeft <= 0)
        {
            if (playerEq.haveItemInHands)
            {
                Weapon weapon = weaponParent.GetComponentInChildren<Weapon>();
                if(weapon != null) weapon.SetOffSlashEffects();
            }
            StopCoroutine(playerAction);
            playerAction = null;
            StopAllCharging();
            animator.SetBool("prepareAttack", false);
        }

        if (attackVector != Vector3.zero) ActualizeAnimation(-attackVector);

        GameObject txtObj = Instantiate(damageText, transform.position, transform.rotation, transform.parent);
        TextMeshProUGUI txt = txtObj.GetComponentInChildren<TextMeshProUGUI>();
        int realDamage = Random.Range(damage.x, damage.y + 1);
        realDamage -= character.characterStats.defense;
        realDamage -= (int)playerDefenseBonus;
        if (realDamage < 0) realDamage = 0;

        txt.text = realDamage.ToString();
        txt.transform.localScale = Vector3.one * (1 + (realDamage / 25f));

        character.characterStats.hp -= realDamage;
        if (character.characterStats.hp <= 0)
        {
            Die();
            bloodEffect.Play();
        }
        else BloodEffect();


        Destroy(txtObj, 0.8f);


        if (character.characterStats.freezeTimeLeft > 0 && type != DamageType.frozen)
        {
            character.characterStats.freezeTimeLeft = 0;
            if(playerAction != null)
            {
                StopCoroutine(playerAction);
                playerAction = null;
                rb.drag = defaultDrag;
            }
            charEffects.RemoveCharacterEffect(CharacterEffect.ice);
            animator.enabled = true;
        }

        charEffects.PlayDamageSound(type);

        if (type != DamageType.physics && character.characterStats.hp >= 0) // DAMAGE TYPE
        {
            if (type == DamageType.fire)
            {
                character.characterStats.fireDamageLeft += realDamage;
                if (setOnFire == null) setOnFire = StartCoroutine(SetOnFire());
            }
            else if (type == DamageType.frozen)
            {
                character.characterStats.freezeTimeLeft += realDamage;
                if (playerAction == null) playerAction = StartCoroutine(FreezeCharacter());
                
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

        if(fireProjectileOnDamage > 0)
        {

            for (int i = 0; i < fireProjectileOnDamage; i++)
            {

                Rigidbody projectileRb = Instantiate(fireProjectilePrefab, transform.position + new Vector3(0,1,0), transform.rotation, mapMenager.stuffMenager).GetComponent<Rigidbody>();

                Vector3 randomVector = new(Random.Range(-fireProjectileShootVector.x, fireProjectileShootVector.x), fireProjectileShootVector.y, Random.Range(-fireProjectileShootVector.x, fireProjectileShootVector.x));

                projectileRb.velocity = randomVector * Random.Range(fireProjectileForceMin, fireProjectileForceMax);

            }


        }

        if (attackVector != Vector3.zero) // KNOCKBACK
        {
            if (knockback != null) StopCoroutine(knockback);
            knockback = StartCoroutine(Knockback(attackVector, knockbackForce));
        }

        weaponParent.transform.eulerAngles = new Vector3(0, 0, 0);


    }

    Coroutine knockback;
    IEnumerator Knockback(Vector3 attackVector, float knockbackForce)
    {

        animator.SetBool("knockback", true);


        attackVector = new Vector3(attackVector.x, 0, attackVector.z);


        float realKnockback = knockbackForce / ((character.characterStats.characterSizeMultiplayer + 1) * (playerKnockbacResist + 1));

        rb.velocity = attackVector.normalized * realKnockback;
        if (realKnockback > 25) animator.SetBool("damage+", true);

        if (character.characterStats.freezeTimeLeft <= 0) animator.SetTrigger("damage");

        while (rb.velocity.magnitude > minKnockbackVelocity)
        {

            yield return null;
        }

        animator.SetBool("knockback", false);


        if (!isDead)
        {
            animator.SetBool("damage+", false);
        }

        dash = false;
        isDashCooldown = false;
        moveSpeed = defaultMoveSpeed;


        knockback = null;
    }

   

    public void BloodEffect()
    {

        float x = 0;
        if (character.characterStats.hp < character.characterStats.maxHp)
        {
            x = (-((float)character.characterStats.hp / character.characterStats.maxHp) + 1.15f) * 0.6f;
        }

        spriteRenderer.material.SetFloat("_BloodEffect", x);
   
        bloodEffect.Play();

    }

 
  

    Coroutine setOnFire;
    IEnumerator SetOnFire()
    {
        charEffects.AddCharacterEffect(CharacterEffect.fire);
        while (character.characterStats.fireDamageLeft > 0 || character.characterStats.fireDamagePerSecond > 0)
        {
            int dmg = character.characterStats.fireDamageLeft + character.characterStats.fireDamagePerSecond;
            character.characterStats.fireDamageLeft /= 2;
            GameObject txtObj = Instantiate(damageText, transform.position, transform.rotation, transform.parent);
            TextMeshProUGUI txt = txtObj.GetComponentInChildren<TextMeshProUGUI>();
            if (dmg < 0) dmg = 0;
            txt.text = dmg.ToString();
            
            Destroy(txtObj, 0.8f);

            if (character.characterStats.hp < 0) break;

            character.characterStats.hp -= dmg;

            if (character.characterStats.hp < 0) Die();

            yield return new WaitForSeconds(0.8f);
        }
        charEffects.RemoveCharacterEffect(CharacterEffect.fire);


        setOnFire = null;
    }


    IEnumerator FreezeCharacter()
    {
        ActualizeAnimation(Vector3.zero);
        charEffects.AddCharacterEffect(CharacterEffect.ice);
        animator.enabled = false;
        rb.drag = iceDrag;

        while (character.characterStats.freezeTimeLeft > 0)
        {
            if (character.characterStats.hp < 0) break;

            yield return new WaitForSeconds(0.5f / (character.characterStats.freezeResist + playerFreezeResistBonus + 1f));

            character.characterStats.freezeTimeLeft -= 1;
        }
        animator.enabled = true;
        charEffects.RemoveCharacterEffect(CharacterEffect.ice);
        playerAction = null;
        rb.drag = defaultDrag;

        ResetPlayerStats();
    }

    Coroutine electricShock;
    IEnumerator ElectricShock()
    {
        charEffects.AddCharacterEffect(CharacterEffect.electricity);

        yield return new WaitForSeconds(0.8f);

        charEffects.RemoveCharacterEffect(CharacterEffect.electricity);

        electricShock = null;
    }

    bool isDead;

    [HideInInspector] public int resurectionUsed = 0;

    public void SetDieOnCharacterChoose()
    {
        isDead = true;
        if(character != null)
        {
            Transform obj = character.transform.Find("dash anim");
            if(obj != null) obj.gameObject.SetActive(false);

        }


        playerStepsVFX.enabled = false;

    }

    void Die()
    {
        if (!isDead)
        {

            animator.enabled = true;
            animator.SetBool("dash", false);

            animator.SetBool("die", true);

            animator.speed = 1;
            StopAllCoroutines();
            bodyCollider.enabled = false;
            weaponParent.gameObject.SetActive(false);
            charEffects.soundComponent.PlaySound(GameSound.characterDeath);
            charEffects.RemoveAllCharacterEffect();
            playerSounds.StopSound(GameSound.characterWalk, true);
            playerAction = null;
            StopAllCharging();

            if (mapMenager.GetStatsValue(PlayerStat.playerResurection) > resurectionUsed && !isGateDestroyed)
            {
                resurectionUsed++;

                StartCoroutine(SearchForSpecjalResurection());

                return;


            }

            isDead = true;

            resurectionUsed = 0;



            playerEq.RemoveAllItems();
            resourceCollector.RemoveResourceOnDie();
            Debug.Log("die");
            cardEq.RemoveAllCards();
            character.DestroyAllSpells();

            LoseGameAnimator();

        }

    }

    [HideInInspector] public bool isGateDestroyed;
    Coroutine waitForLose;
    public void LoseGameAnimator()
    {

        if (waitForLose == null)
        {
            if (isGateDestroyed)
            {
                gateDestroyAnimator.gameObject.SetActive(true);
                gateDestroyAnimator.SetTrigger("win");
            }
            else
            {
                dieAnimator.gameObject.SetActive(true);
                dieAnimator.SetTrigger("win");
            }

            mapMenager.characterMenager.SetWinAnimation(true);

            waitForLose = StartCoroutine(WaitForLose());
        }
    }

    IEnumerator WaitForLose()
    {
        yield return new WaitForSeconds(4);

        storyMenager.LoadScene(storyMenager.hubName);
        waitForLose = null;
    }
    public void WinGame()
    {
        if (!isDead)
        {
            mapMenager.WinMap();

            isDead = true;
            animator.enabled = true;
            animator.SetBool("dash", false);

            animator.SetTrigger("win");
            animator.speed = 1;
            StopAllCoroutines();
            bodyCollider.enabled = false;
            weaponParent.gameObject.SetActive(false);
            charEffects.RemoveAllCharacterEffect();
            playerSounds.StopSound(GameSound.characterWalk, true);

            winAnimator.gameObject.SetActive(true);
            winAnimator.SetTrigger("win");

            mapMenager.characterMenager.SetWinAnimation(false);
            StartCoroutine(WaitForWin());
        }
        resurectionUsed = 0;
    }

    IEnumerator WaitForWin()
    {
        yield return new WaitForSeconds(4);

        playerEq.RemoveAllItems();
        playerAction = null;
        resourceCollector.RemoveResourceOnDie();
        Debug.Log("win");
        cardEq.RemoveAllCards();
        storyMenager.LoadScene(storyMenager.hubName);
        character.DestroyAllSpells();

        
    }

    IEnumerator SearchForSpecjalResurection()
    {
        resurectionSoul.SetActive(true);

        GameObject resurectionBody = null;

        int loopCount = 0;
        while (resurectionBody == null &&  loopCount < 40)
        {
            loopCount++;
            Collider[] colliders = Physics.OverlapSphere(transform.position, loopCount * 4, resurectionMask);
            Debug.Log(colliders.Length);
            foreach (Collider collider in colliders)
            {
                if (!collider.GetComponent<CharacterMove>().isDead)
                {
                    resurectionBody = collider.gameObject;
                }
            }
            
            yield return null;

        }
        Debug.Log("search for body " +  resurectionBody);
        while (resurectionBody != null && Vector3.Distance(resurectionBody.transform.position, transform.position) > 2f)
        {
            Vector3 targetPos = resurectionBody.transform.position;

            Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, 9 * Time.deltaTime);
            transform.position = nextPos;
            moveSpeed = 0;
            yield return null;

        }
        moveSpeed = defaultMoveSpeed;

        resurectionSoul.SetActive(false);

        if (resurectionBody != null)
        {
            CharacterMove body = resurectionBody.GetComponent<CharacterMove>();
            
            
            Resurection(resurectionBody, false);
            if (body != null)
            {
                body.DealDamage(body.transform.position - transform.position, new Vector2Int(30, 50), 25, DamageType.physics);
            }
        }
        else
        {
            Die();
        }

    }






    [Tooltip("function create no body and add it to player, with characterMove")]
    public void Resurection(GameObject newBody = null, bool endGame = true)
    {
        
        if (newBody != null)
        {

            CharacterMove charMove = newBody.GetComponent<CharacterMove>();

            GameObject newStartWeapon = null;


            if(charMove != null)
            {
                if (charMove.RandomizeWeapons.Count > 0)
                {
                    newStartWeapon = charMove.RandomizeWeapons[Random.Range(0, charMove.RandomizeWeapons.Count)];
                }
                else
                {
                    newStartWeapon = charMove.startMainWeapon;
                }
            }
           
             



            if (playerEq.haveItemInHands)
            {
                playerEq.RemoveAllItems();
            }

            if (character != null) Destroy(character.gameObject);


            if (endGame)
            {
                artifactsManager.UnequipAllArtifactsAtResurection();
                mapMenager.RemoveMapEffects();
            }

           


            GameObject prefab = newBody.GetComponentInChildren<Animator>().gameObject;
            GameObject body = Instantiate(prefab, graphicCharacter.position + new Vector3(0,1,0), newBody.transform.rotation, graphicCharacter);
            body.transform.localPosition = new Vector3(0, 1, 0);

            Character charForStats = newBody.GetComponent<Character>();


            character = body.AddComponent<Character>();
            character.characterStats = charForStats.characterStats;
            character.race = charForStats.race;
            character.attackChargingModifer = charForStats.attackChargingModifer;
            character.specjalChargingModifer = charForStats.specjalChargingModifer;

            ActualizeSkillsImage();

            character.characterStats.hp = character.characterStats.maxHp;
            


            if (character != null)
            {
                SaveGame.SaveLastBody(character.name); // Zak³adaj¹c, ¿e klasa Character ma pole 'characterName'
            }

            charEffects = body.GetComponentInChildren<CharEffects>();
            animator = body.GetComponent<Animator>();
            bloodEffect = charEffects.bloodEffect;

            weaponParent = body.transform.GetChild(0).GetChild(0).GetChild(0);


            foreach (Transform weapons in weaponParent)
            {
                Destroy(weapons.gameObject);
            }


            spriteRenderer = body.GetComponentInChildren<SpriteRenderer>();

            animator.runtimeAnimatorController = defaultAnim;

            spriteRenderer.material = playerSpriteMaterial;

          //  Debug.Log(" " + animator + " " + character + " " + spriteRenderer);


            bodyCollider.transform.localScale = Vector3.one * (1 + character.characterStats.characterSizeMultiplayer);
            weaponParent.localScale = Vector3.one * (1 + character.characterStats.weaponSizeMultiplayer);

            if (endGame)
            {
                GameObject obj = Instantiate(dashEffects, character.transform.position, dashEffects.transform.rotation, character.transform);
                dashEffectsAnim = obj.GetComponent<Animator>();



            }

            if (dashEffectsAnim != null)
            {
                dashTrails = GetComponentsInChildren<TrailRenderer>().ToList<TrailRenderer>();


                foreach (TrailRenderer trail in dashTrails)
                {
                    trail.time *= (1 + character.characterStats.characterSizeMultiplayer);
                }

            }

            if(newStartWeapon != null)
            {
                
                playerEq.AddItem(newStartWeapon);
            }

        }

        isDead = false;
        playerStepsVFX.enabled = true;


        dash = false;
        isDashCooldown = false;
        knockback = null;
        animator.SetBool("knockback", false);

        animator.speed = 1;
        StopAllCoroutines();
        playerAction = null;

        winAnimator.gameObject.SetActive(false);
        gateDestroyAnimator.gameObject.SetActive(false);
        dieAnimator.gameObject.SetActive(false);
        isGateDestroyed = false;


        animator.enabled = true;
        animator.SetBool("die", false);
        animator.SetBool("dash", false);
        animator.Play("create");
        bodyCollider.enabled = true;
        weaponParent.gameObject.SetActive(true);

        

        // Map mneager player sats setting

        character.MultiplyMaxHp(playerHpMultiply + 1);
      
        character.characterStats.meleePowerMultiplayer += playerMeleeAttackMultiply;
        character.characterStats.wandsPowerMultiplayer += playerWandAttackMultiply;

        

        ResetPlayerStats();

        character.characterStats.hp = character.characterStats.maxHp;


        spriteRenderer.material.SetFloat("_BloodEffect", 0.45f);
        StartCoroutine(RemoveBloodFromSprite());
        StartCoroutine(HpHealingPerSecond());


        character.characterStats.fireCultist = 1000;
        character.ResetSpells();

        GetComponent<PlayerHp>().ResurectionInfoChange();


    }

    void GetBodyComponetnts()
    {
        charEffects = GetComponentInChildren<CharEffects>();
        animator = graphicCharacter.GetComponentInChildren<Animator>();
        bloodEffect = charEffects.bloodEffect;

        weaponParent = graphicCharacter.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        character = graphicCharacter.GetComponentInChildren<Character>();
        spriteRenderer = graphicCharacter.GetComponentInChildren<SpriteRenderer>();

        animator.runtimeAnimatorController = defaultAnim;

        spriteRenderer.material = playerSpriteMaterial;



    }

    IEnumerator RemoveBloodFromSprite()
    {
        yield return new WaitForSeconds(3);
        while (spriteRenderer.material.GetFloat("_BloodEffect") > 0.01f)
        {
            yield return new WaitForSeconds(0.2f);
            if (character.characterStats.hp < character.characterStats.maxHp) break;
            spriteRenderer.material.SetFloat("_BloodEffect", spriteRenderer.material.GetFloat("_BloodEffect") - 0.01f);
        }
    }

    [HideInInspector] public float growAfterKill = 0;
    [HideInInspector] public int gateHealAfterKill = 0;
    public void TrigerEnemyKilledByPlayer()
    {
        Debug.Log("you killed enemy");
        if(growAfterKill > 0)
        {
            character.AddModifier(new Character.CharacterStats { characterSizeMultiplayer = growAfterKill, effectType = CharacterEffect.sizeIncease }, 5);

        }
        if(gateHealAfterKill > 0)
        {
            if(gateComponnent != null) gateComponnent.buildingComponent.RepairBuild(gateHealAfterKill);
        }

    }


    IEnumerator ChargeModifier(bool isSpecjal)
    {

        moveSpeed = defaultMoveSpeed * (playerSpeedOnAttack + character.characterStats.moveOnAttack);



        character.StartChargingModiferUpgrade(isSpecjal);


        if (isSpecjal)
        {
            chargingModifersSounds.PlaySound(GameSound.characterWalk);


            animator.SetBool("specjalCharge", true);


            float timeLeft = character.specjalChargingModifer.loadedModifierChargeTime;

            specjalChargeImageClock.color = chargingColor;

            while (timeLeft > 0)
            {
                yield return null;
                timeLeft -= Time.deltaTime;

                if (!Input.GetKey(KeyCode.Space))
                {
                    StopAllCharging();
                    chargingModifersSounds.StopSound(GameSound.characterUpgrade);
                    break;
                }

                specjalChargeTimeText.text = ((int)timeLeft + 1).ToString();
                specjalChargeImageClock.fillAmount = timeLeft / character.specjalChargingModifer.loadedModifierChargeTime;



            }
            playerAction = null;

            specjalChargeTimeText.text = "";
            if (specjalChargeCooldown == null && !character.specjalChargingModifer.isLoadedModiferReady) specjalChargeCooldown = StartCoroutine(ChargeCooldown(isSpecjal));
        }
        else
        {

            chargingModifersSounds.PlaySound(GameSound.characterUpgrade);


            animator.SetBool("attackCharge", true);


            float timeLeft = character.attackChargingModifer.loadedModifierChargeTime;

            attackChargeImageClock.color = chargingColor;


            while (timeLeft > 0)
            {
                yield return null;
                timeLeft -= Time.deltaTime;

                if (!Input.GetMouseButton(0))
                {
                    StopAllCharging();
                    chargingModifersSounds.StopSound(GameSound.characterUpgrade);
                    break;
                }
                attackChargeTimeText.text = ((int)timeLeft + 1).ToString();
                attackChargeImageClock.fillAmount = timeLeft / character.attackChargingModifer.loadedModifierChargeTime;

            }

            attackChargeTimeText.text = "";
            if (attackChargeCooldown == null && !character.attackChargingModifer.isLoadedModiferReady) attackChargeCooldown = StartCoroutine(ChargeCooldown(isSpecjal));
        }

        animator.SetBool("specjalCharge", false);
        animator.SetBool("attackCharge", false);



        moveSpeed = defaultMoveSpeed;

    }

    public void StopAllCharging()
    {
        character.BreakAnyCharging();

        specjalChargeTimeText.text = "";
        attackChargeTimeText.text = "";

        specjalChargeImageClock.fillAmount = 0;
        attackChargeImageClock.fillAmount = 0;

        animator.SetBool("specjalCharge", false);
        animator.SetBool("attackCharge", false);
    }

    public void ActualizeSkillsImage()
    {
        if(character != null)
        {
            attackChargeImage.sprite = character.attackChargingModifer.skillIcon;
            attackChargeImageClock.sprite = character.attackChargingModifer.skillIcon;
            specjalChargeImage.sprite = character.specjalChargingModifer.skillIcon;
            specjalChargeImageClock.sprite = character.specjalChargingModifer.skillIcon;
        }
      
    }

    Coroutine attackChargeCooldown;
    Coroutine specjalChargeCooldown;

    public IEnumerator ChargeCooldown(bool isSpecjal)
    {


        if (isSpecjal)
        {
            float timeLeft = character.specjalChargingModifer.loadedModifierDuration;

            specjalChargeImageClock.color = activeColor;

            while (timeLeft > 0)
            {

                // Debug.Log("<color=red> " + timeLeft);

                yield return null;
                timeLeft -= Time.deltaTime;
                specjalChargeTimeText.text = ((int)timeLeft + 1).ToString();
                specjalChargeImageClock.fillAmount = timeLeft / character.specjalChargingModifer.loadedModifierDuration;
            }

            specjalChargeImageClock.color = cooldownColor;

            float cooldown = character.specjalChargingModifer.loadedModifierCooldown / (1 + mapMenager.GetStatsValue(PlayerStat.playerSkillCooldownSpeed));
            timeLeft = cooldown;

            while (timeLeft > 0)
            {
                yield return null;
                timeLeft -= Time.deltaTime;
                specjalChargeTimeText.text = ((int)timeLeft + 1).ToString();
                specjalChargeImageClock.fillAmount = timeLeft / cooldown;
            }

            specjalChargeTimeText.text = "";
            specjalChargeCooldown = null;
        }
        else
        {
            float timeLeft = character.attackChargingModifer.loadedModifierDuration;

            attackChargeImageClock.color = activeColor;


            while (timeLeft > 0)
            {
                yield return null;
                timeLeft -= Time.deltaTime;
                attackChargeTimeText.text = ((int)timeLeft + 1).ToString();
                attackChargeImageClock.fillAmount = timeLeft / character.attackChargingModifer.loadedModifierDuration;
            }

            attackChargeImageClock.color = cooldownColor;

            float cooldown = character.attackChargingModifer.loadedModifierCooldown / (1 + mapMenager.GetStatsValue(PlayerStat.playerSkillCooldownSpeed));
            timeLeft = cooldown;

            while (timeLeft > 0)
            {
                yield return null;
                timeLeft -= Time.deltaTime;
                attackChargeTimeText.text = ((int)timeLeft + 1).ToString();
                attackChargeImageClock.fillAmount = timeLeft / cooldown;
            }

            attackChargeTimeText.text = "";
            attackChargeCooldown = null;

        }


    }


}
