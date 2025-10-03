using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CharacterMenager;
using Random = UnityEngine.Random;

public class CharacterMenager : MonoBehaviour
{
    public Transform enemyParent;
    [HideInInspector] public List<CharacterMove> enemyList = new();
    public int minCharacterChooseCount = 6;
    public List<KilledCharacters> killedCharacters = new();
    public Animator dayNightAnimator;
    public GameObject characterButton;
    public GameObject guardSpot;
    public Transform characterButtonParent;
    [HideInInspector] public PlayerMove playerMove;
    [Header("Enemy Settings: ")]
    [HideInInspector] public MapMenager mapMenager;
    [HideInInspector] public int nightCount;
    [SerializeField] float enemySpawnDistance = 10;
    [SerializeField] float spawnDisFromPlayer = 10;
    [HideInInspector] public Transform stuffMenager;
    public Animator nightBarAnimator;
    [SerializeField] TextMeshProUGUI nightCountText; 
    [Header("colors Setting:")]
    public Color32 colorAlly;
    public Color32 colorEnemy;
    public Color32 colorAttackAnyone;
    public Color32 bloodColor;
    public Color32 fireBloodColor;
    public Material enemyMaterial;
    public Material allyMaterial;
    public List<DamageColor> enemyDamageColor;
    
    [HideInInspector] public MusicManager musicManager;

    [Header("ally settings")]
    public Transform allyParent;
    [HideInInspector] public List<Ally> allyList = new();
    List<Resource_> resources = new();
    public float playerFollowDistance = 7f;

    public Character.CharacterStats followPlayerStatsChange;

    public List<GameObject> alliesPrefabs = new();
    [Tooltip("distance ally will search resource")]
    public float maxResourceSerchDistance;
    public int maxNightCount = 5;

    GateComponnent gateComponnent;



    [Serializable]
    public struct DamageColor
    {
        public Color color;
        public DamageType type;

    }
    [Serializable]
    public struct Resource_
    {
        public float magnitude;
        public ResourceCreator resourceCreator;

    }
    [Serializable]
    public struct Ally
    {
        public CharacterMove characterMove;
        public Button button;
        public CharGuardSpot guardSpot;

    }
    [Serializable]
    public class KilledCharacters
    {
        public string name;
        public int countOfThisType;
        public bool isAlly;

    }
    private void Awake()
    {
        playerMove = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMove>();
        dayNightAnimator = GameObject.FindGameObjectWithTag("dayCycle").GetComponent<Animator>();
        musicManager = GetComponent<MusicManager>();
        stuffMenager = GameObject.FindGameObjectWithTag("stuffMenager").transform;
        mapMenager = GetComponent<MapMenager>();
        
    }

    

    private void OnEnable()
    {
        // zaczyna nowa rozgrywke
        dayNightAnimator.Play("day");
        StartCoroutine(DayNightTimer());
        killedCharacters.Clear();
        gateComponnent = FindAnyObjectByType<GateComponnent>();
    }

    private void OnDisable()
    {
        // wylaczany jest po przegranej

        if (dayNightAnimator != null) dayNightAnimator.SetTrigger("deathWorld");

        StopAllCoroutines();
        SaveKilledCharacters();


    }


    IEnumerator DayNightTimer()
    {
        while (true)
        {
            day = StartCoroutine(Day());
            Debug.Log("day");

            yield return new WaitForSeconds(1);
            
            do
            {
                yield return new WaitForSeconds(1);
            } while (dayNightAnimator.GetCurrentAnimatorStateInfo(0).IsTag("day"));


            if (sendForResource != null)
            {
                StopCoroutine(sendForResource);
                sendForResource = null;
            }
            if (day != null)
            {
                StopCoroutine(day);
                day = null;
            }

            night = StartCoroutine(Night());
            Debug.Log("night: " + nightCount);

            do
            {
                yield return new WaitForSeconds(1);
            } while (dayNightAnimator.GetCurrentAnimatorStateInfo(0).IsTag("night"));
        }
    }

    Coroutine day;
    IEnumerator Day()
    {
        if(gateComponnent == null) gateComponnent = FindAnyObjectByType<GateComponnent>();
        if(gateComponnent != null) 
        {

            if(Random.Range(0f,1f) < mapMenager.GetStatsValue(PlayerStat.chanceForBreakfast))
            {
                mapMenager.artifactsManager.randomObjectOnStart.CreateObjectInRadius(mapMenager.artifactsManager.breakfastArtifact, 90);

            }


            gateComponnent.MorningGateReset();

            musicManager.PlayMusic(MusicEvent.Day);

            yield return new WaitForSeconds(2);

            while (enemyList.Count > 0)
            {
                Debug.Log("dealing Day Damage");
                StartCoroutine(DayDamage());

                yield return new WaitForSeconds(1);
            }

            if(mapMenager != null && nightCount > 0) // sunriseArifact morningUpgrade
            {
                ArtifactChoose artifact = mapMenager.artifactsManager.artifactChoose;

                artifact.gameObject.SetActive(true);


                artifact.StartChoosingArtifact(true, ArtifactClass.sunriseUpgrade);
            }

            if (maxNightCount <= nightCount)
            {
                mapMenager.EndGame();
                playerMove.WinGame();
            }

            mapMenager.CreateDayNPC();

            ReloadResourceCreators();
            yield return null;

            sendForResource = StartCoroutine(SendAllyForResources());
            day = null;
        }



        
    }

    Coroutine night;
    IEnumerator Night()
    {

        musicManager.PlayMusic(MusicEvent.Night);

        yield return null;

        StartCoroutine(ReturnToBase());

        yield return null;

        mapMenager.DestroyDayNPC();

        nightCount++;

        nightBarAnimator.SetTrigger("show");
        if (maxNightCount <= nightCount)
        {
            nightCountText.text = "Final night";
        }
        else
        {
            nightCountText.text = "Night - " + nightCount;
        }

        yield return new WaitForSeconds(2);


        StartCoroutine(SpawnEnemies());



        


        do
        {
            yield return new WaitForSeconds(1);

            for (int i = 0; i < enemyList.Count; i++)
            {
              //  if(enemyList[i] != null) enemyList[i].MoveTo(Vector3.zero);
                yield return new WaitForSeconds(1);

            }

        } while (enemyList.Count > 0);
        night = null;
    }


    public void AddResourceCreator(ResourceCreator resCreator)
    {
        resources.Add(new() {magnitude = resCreator.transform.position.magnitude, resourceCreator = resCreator } );
        resSorted = false;
    }

  

    void ReloadResourceCreators()
    {
        foreach (Resource_ resCreator in resources)
        {
            resCreator.resourceCreator.ReadyToCreateNewResource();
        }
        
    }

    [HideInInspector] public Coroutine sendForResource;
    bool resSorted;
    List<Resource_> resForCharacters = new();

    IEnumerator SendAllyForResources()
    {
        if (!resSorted)
        {
            resources = resources.OrderBy(x => x.magnitude).ToList();

            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].resourceCreator == null)
                {
                    resources.RemoveAt(i);
                    i--;
                }
                else if (resources[i].magnitude < maxResourceSerchDistance)
                {
                    if (resources[i].resourceCreator.isActiveAndEnabled) resForCharacters.Add(resources[i]);
                }
                else
                {
                    break;
                }

            }

            resSorted = true;
        }

        List<Resource_> resList = resForCharacters;



        for (int i = 0; i < resList.Count; i++)
        {
            if (resList[i].resourceCreator == null || resList[i].resourceCreator.resourceObject == null)
            {
                resList.RemoveAt(i);
                i--;
            }
           

        }




        while (resList.Count > 0)
        {

            for (int j = 0; j < allyList.Count; j++)
            {
                if (!allyList[j].characterMove.isOnMove && !playerFollowers.Contains(allyList[j]))
                {
                    
                    for (int i = 0; i < resList.Count; i++)
                    {
                        if (resList[i].resourceCreator.resourceObject != null)
                        {
                            allyList[j].characterMove.MoveTo(resList[i].resourceCreator.transform.position);
                            resList.RemoveAt(i);
                            break;
                        }
                        else
                        {
                            resList.RemoveAt(i);
                            i--;
                        }
                    }

                }
                yield return new WaitForSeconds(0.1f);
            }

            yield return new WaitForSeconds(1);
        }
        StartCoroutine(ReturnToBase());
        sendForResource = null;
    }

    IEnumerator ReturnToBase()
    {
        
        for (int i = 0; i < allyList.Count; i++)
        {
            if (allyList[i].characterMove == null)
            {
                allyList.RemoveAt(i);
                i--;
            }
            else
            {
                allyList[i].characterMove.MoveTo(allyList[i].guardSpot.transform.position);
                yield return null;
            }
        }

        
    }

    

    IEnumerator SpawnEnemies()
    {
        List<GameObject> enemiesToSpawn = mapMenager.GetEnemiesToSpawn();
        yield return null;
        List<GameObject> bossToSpawn = mapMenager.GetBossToSpawn();
        yield return null;

        float enemySpawnRate = (dayNightAnimator.GetCurrentAnimatorStateInfo(0).length * 0.55f) / enemiesToSpawn.Count;

        float hpMultiply = 1 + mapMenager.GetStatsValue(PlayerStat.nightEnemiesHpMultiply);
        Character.CharacterStats addStats = new()
        {
            characterSizeMultiplayer = mapMenager.GetStatsValue(PlayerStat.nightEnemiesSizeMultiply),
            speedMultiplayer = mapMenager.GetStatsValue(PlayerStat.nightEnemiesSpeedMultiply),
            meleePowerMultiplayer = mapMenager.GetStatsValue(PlayerStat.nightEnemiesDamageMultiply),
            wandsPowerMultiplayer = mapMenager.GetStatsValue(PlayerStat.nightEnemiesDamageMultiply),
        };
        yield return null;

        int spawnedEnemyCount = 0;

        foreach (var enemy in enemiesToSpawn)
        {

            // Vector3 spawnPos = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length - 1)].position + new Vector3(Random.Range(0, enemySpread), 0, Random.Range(0, enemySpread));
            Vector3 spawnPos = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * enemySpawnDistance;
            while (Vector3.Magnitude(spawnPos - playerMove.transform.position) < spawnDisFromPlayer)
            {
                spawnPos = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * enemySpawnDistance;
            }
            
            GameObject obj = Instantiate(enemy, spawnPos, transform.rotation, enemyParent);
            CharacterMove charMove = obj.GetComponent<CharacterMove>();

            
            charMove.character.AddModifier(addStats);
            charMove.character.MultiplyMaxHp(hpMultiply);
            charMove.ResetCharacterStats();


            enemyList.Add(charMove);

            StartCoroutine(WaitToAttackGate(charMove));

            spawnedEnemyCount++;
            yield return new WaitForSeconds(enemySpawnRate);

            if(spawnedEnemyCount == (int)(enemiesToSpawn.Count / 2))
            {
                foreach (var boss in bossToSpawn)
                {

                    // Vector3 spawnPos = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length - 1)].position + new Vector3(Random.Range(0, enemySpread), 0, Random.Range(0, enemySpread));
                    Vector3 spawnPosBoss = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * enemySpawnDistance;
                    while (Vector3.Magnitude(spawnPos - playerMove.transform.position) < spawnDisFromPlayer)
                    {
                        spawnPosBoss = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * enemySpawnDistance;
                    }

                    GameObject objBoss = Instantiate(boss, spawnPosBoss, transform.rotation, enemyParent);
                    CharacterMove charMoveBoss = objBoss.GetComponent<CharacterMove>();


                    charMoveBoss.character.AddModifier(addStats);
                    charMoveBoss.character.AddModifier(mapMenager.bossUpgrade);
                    charMoveBoss.character.MultiplyMaxHp(hpMultiply);
                    charMoveBoss.ResetCharacterStats();


                    enemyList.Add(charMoveBoss);

                    StartCoroutine(WaitToAttackGate(charMoveBoss));

                    spawnedEnemyCount++;
                    yield return new WaitForSeconds(enemySpawnRate);
                }
            }

            
        }


        yield return new WaitForSeconds(1);
    }

    IEnumerator WaitToAttackGate(CharacterMove charMove)
    {
        yield return new WaitForSeconds(0.3f);
        charMove.MoveTo(Vector3.zero);

    }

    public void SetWinAnimation(bool enemyWin)
    {
        StartCoroutine(WaitForWinAnimation(enemyWin));
    }
    IEnumerator WaitForWinAnimation(bool enemyWin)
    {
        if (enemyWin)
        {
            for (int i = 0; i < enemyList.Count; i++)
            {
                if (enemyList[i] != null && !enemyList[i].isDead)
                {
                    enemyList[i].animator.SetBool("dash", false);
                    enemyList[i].animator.SetTrigger("win");
                    enemyList[i].StopAllCoroutines();
                    enemyList[i].isDead = true;
                    yield return null;
                }
            }
           
        }
        else
        {
            for (int i = 0; i < allyList.Count; i++)
            {
                if (allyList[i].characterMove != null && !allyList[i].characterMove.isDead)
                {
                    allyList[i].characterMove.animator.SetBool("dash", false);
                    allyList[i].characterMove.animator.SetTrigger("win");
                    allyList[i].characterMove.StopAllCoroutines();
                    allyList[i].characterMove.isDead = true;
                    yield return null;
                }
            }
           
        }
    }


    public void RemoveCharacter(CharacterMove character)
    {
        if(character.character.characterStats.fireCultist > 0)
        {
            for (int i = 0; i < allyList.Count; i++)
            {
                if (allyList[i].characterMove == character)
                {
                    allyList.RemoveAt(i);
                    break;
                }
            }
        }else
        {
            enemyList.Remove(character);
        }
    }

    public void AddAlly(GameObject allyPrefab, Vector3 GuardPos)
    {
        Debug.Log(allyPrefab);
        GameObject allyObj = Instantiate(allyPrefab, GuardPos, allyPrefab.transform.rotation, allyParent);
        CharacterMove a = allyObj.GetComponent<CharacterMove>();

        if(a == null)
        {
            Debug.Log("brak character move");
            return;
        }

       // Button b = Instantiate(characterButton, characterButtonParent).GetComponent<Button>();
        CharGuardSpot gs = Instantiate(guardSpot, GuardPos, guardSpot.transform.rotation, allyParent).GetComponent<CharGuardSpot>();
        gs.characterMove = a;
        
        gs.SetVisibility(false);

        float hpMultiply = 1 + mapMenager.GetStatsValue(PlayerStat.alliesHpMultiply);

        Character.CharacterStats addStats = new()
        {
            characterSizeMultiplayer = mapMenager.GetStatsValue(PlayerStat.alliesSizeMultiply),
            speedMultiplayer = mapMenager.GetStatsValue(PlayerStat.alliesSpeedMultiply),
            attackSpeedMultiplayer = mapMenager.GetStatsValue(PlayerStat.alliesSpeedMultiply),
            meleePowerMultiplayer = mapMenager.GetStatsValue(PlayerStat.alliesDamageMultiply),
            
        };

        

        a.character.AddModifier(addStats);
        a.character.MultiplyMaxHp(hpMultiply);
        a.ResetCharacterStats();

        Ally ally = new() { characterMove = a, button = null, guardSpot = gs };
       // ally.button.onClick.AddListener(() => SelectAlly(ally.characterMove));


        allyList.Add(ally);
        
    }

    public void FollowPlayerOrder(CharacterMove character)
    {
        foreach (var fol in playerFollowers)
        {
            if (fol.characterMove == character)
            {
                return;
            }
        }
        character.followPlayer = true;
        Ally ally = GetAllyInfo(character);
        ally.guardSpot.transform.SetParent(playerMove.transform, true);
        Vector3 pos = new(Random.Range(-1, 1), 0, Random.Range(-1, 1));
        ally.guardSpot.transform.position = playerMove.transform.position + pos.normalized * Random.Range(playerFollowDistance / 3, playerFollowDistance);
        character.MoveTo(ally.guardSpot.transform.position);

        playerFollowers.Add(ally);
        ally.characterMove.character.AddModifier(followPlayerStatsChange);


        if (followPlayer == null) followPlayer = StartCoroutine(FollowPlayer());

    }

    List<Ally> playerFollowers = new();

    Coroutine followPlayer;
    IEnumerator FollowPlayer()
    {
        while (playerFollowers.Count > 0)
        {
            for (var i = 0; i < playerFollowers.Count; i++)
            {
                if (playerFollowers[i].characterMove == null)
                {
                    if(playerFollowers[i].guardSpot != null) Destroy(playerFollowers[i].guardSpot);

                    playerFollowers.RemoveAt(i);
                    i--;
                    continue;
                }

                Vector3 x = new(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
                playerFollowers[i].characterMove.MoveTo(playerFollowers[i].guardSpot.transform.position + x);
                yield return new WaitForSeconds(1f);
            }
            
        }
        followPlayer = null;
    }


    public void SelectAlly(CharacterMove character)
    {
        if(allyOrders != null)
        {
            StopCoroutine(allyOrders);
            foreach (Ally ally in allyList)
            {
                ally.guardSpot.SetVisibility(false);
            }
        }
        allyOrders = StartCoroutine(WaitForOrders(character));
    }

    Coroutine allyOrders;
    IEnumerator WaitForOrders(CharacterMove characterMove)
    {

        playerMove.allyOrder = true;

        Ally ally = GetAllyInfo(characterMove);
        Vector3 previousPos = ally.guardSpot.transform.position;

        ally.guardSpot.gameObject.SetActive(true);
        ally.guardSpot.SetVisibility(true);

        while (true)
        {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, playerMove.groundLayer))
            {
                ally.guardSpot.transform.position = hit.point;

            }
            if(Input.GetMouseButtonDown(0) && !playerMove.mouseOnUi)
            {
                foreach (Ally al in playerFollowers)
                {
                    if(al.characterMove == ally.characterMove)
                    {
                        al.characterMove.followPlayer = false;

                        playerFollowers.Remove(al);
                        ally.characterMove.character.RemoveModifier(followPlayerStatsChange);

                        break;
                    }
                }


                ally.guardSpot.transform.SetParent(allyParent, true);
                characterMove.MoveTo(ally.guardSpot.transform.position);
                ally.guardSpot.animator.SetTrigger("go");
                yield return new WaitForSeconds(0.2f);
                ally.guardSpot.gameObject.SetActive(false);


                break;

            }
            else if (Input.GetMouseButtonDown(1) && !playerMove.mouseOnUi)
            {
                ally.guardSpot.animator.SetTrigger("stop");
                yield return new WaitForSeconds(0.2f);

                ally.guardSpot.transform.position = previousPos;

                ally.guardSpot.gameObject.SetActive(false);
                break;
            }
            yield return null;
        }
        allyOrders = null;
        playerMove.allyOrder = false;
    }

    Ally GetAllyInfo(CharacterMove characterMove)
    {
        foreach (Ally ally in allyList)
        {
            if(ally.characterMove == characterMove) return ally;
        }
        return new Ally();
    }

    public Color GetEnemyDamageColor(DamageType damageType)
    {
        foreach(DamageColor dmg in enemyDamageColor)
        {
            if (dmg.type == damageType) return dmg.color;
        }
        return enemyDamageColor[0].color;
    }

    public void RemoveAllCharacters()
    {
        

        for (int i = 0; i < allyParent.childCount; i++)
        {
            Destroy(allyParent.GetChild(i).gameObject);    
        }
        allyList.Clear();


        for (int i = 0; i < enemyParent.childCount; i++)
        {
            Destroy(enemyParent.GetChild(i).gameObject);
        }
        enemyList.Clear();
        
    }

    public void KillCharacter(CharacterMove charMove)
    {
        bool isAdded = false;
        foreach(KilledCharacters character in killedCharacters)
        {
            if(character.name == charMove.gameObject.name)
            {
                character.countOfThisType++;
                isAdded = true;
                break;
            }
        }
        if (!isAdded)
        {
            bool ally = false;
            if (charMove.character.characterStats.fireCultist > 0) ally = true;
            KilledCharacters newChar = new KilledCharacters() { countOfThisType = 1, name = charMove.gameObject.name, isAlly = ally};
            killedCharacters.Add(newChar);
        }
        

    }

   

    public void SaveKilledCharacters()
    {
        if(killedCharacters != null)
        {

            SaveGame.SaveKilledCharacters(killedCharacters);

            List<KilledCharacters> ownedBodies = new();
            ownedBodies.AddRange(killedCharacters);

            if (killedCharacters.Count < minCharacterChooseCount)
            {
                List<KilledCharacters> killedCharactersEarlier = SaveGame.GetOwnedBoides();
                
                for(int i = 0; i < killedCharactersEarlier.Count; i++)
                {
                    foreach (KilledCharacters killedNow in killedCharacters)
                    {
                        if (killedNow.name == killedCharactersEarlier[i].name)
                        {
                            killedCharactersEarlier.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
                ownedBodies.AddRange(killedCharactersEarlier);

            }
            SaveGame.SaveOwnedBoides(ownedBodies);

        }
    }

    [Tooltip("one of each type, from this who you kills less times in previous round")]
    public List<GameObject> GetKilledCharactersObjects()
    {
        List<GameObject> list = new List<GameObject>();
        List<int> killed = new List<int>();
        List<KilledCharacters> killedCharacters = SaveGame.GetOwnedBoides();

        // Tworzenie list obiektów GameObject i liczby zabitych
        foreach (KilledCharacters character in killedCharacters)
        {
            if (character.isAlly)
            {
                GameObject obj = mapMenager.GetAllyByName(character.name);
                if(obj == null) obj = mapMenager.GetEnemyByName(character.name);

                if (obj != null)
                {
                    list.Add(obj);
                }
                else
                {
                    Debug.LogWarning("cant find this name " + character.name);
                }
                
                killed.Add(character.countOfThisType);
            }
            else
            {
                GameObject obj = mapMenager.GetEnemyByName(character.name);
                if (obj == null) obj = mapMenager.GetAllyByName(character.name);

                if (obj != null)
                {
                    list.Add(obj);
                }
                else
                {
                    Debug.LogWarning("cant find this name " + character.name);
                }
                killed.Add(character.countOfThisType);
            }
        }

        // Tworzenie listy par (GameObject, int) dla sortowania
        List<KeyValuePair<GameObject, int>> combinedList = new List<KeyValuePair<GameObject, int>>();
        for (int i = 0; i < list.Count; i++)
        {
            combinedList.Add(new KeyValuePair<GameObject, int>(list[i], killed[i]));
        }

        // Sortowanie listy na podstawie liczby zabitych (rosn¹co)
        combinedList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

        // Tworzenie posortowanej listy GameObject
        List<GameObject> sortedList = new List<GameObject>();
        foreach (var pair in combinedList)
        {
            sortedList.Add(pair.Key);
        }

        return sortedList;
    }

    private void OnApplicationQuit()
    {
        if(enabled && killedCharacters != null) SaveKilledCharacters();
    }

    IEnumerator DayDamage()
    {
        Vector2Int damage = new((int)mapMenager.GetStatsValue(PlayerStat.dayDamage), (int)mapMenager.GetStatsValue(PlayerStat.dayDamage));
        for (int i = 0; i < enemyList.Count; i++)
        {
            if (enemyList[i] != null)
            {
                enemyList[i].DealDamage(Vector3.zero, damage, 0f);
                yield return null;
            }
            else
            {
                enemyList.RemoveAt(i);
                i--;
            }
        }
    }


}
