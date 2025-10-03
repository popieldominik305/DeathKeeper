using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.UI;

public enum ArtifactClass
{
    nomralArtifact,
    statueArtifact,
    permanent,
    drug,
    sunriseUpgrade,
    food,

}

[Serializable]
public class Artifact
{
    [HideInInspector] public string name;
    public NameByLang[] artifactName;
    public DescriptionByLang[] artifactDescription;
    public List<ArtifactEffect> effects;
    public List<ArtifactEffect> bonusEffects;
    public int soulsCost;
    public bool isOwned;
    public int equipped;
    public int maxEquipped;
    public Sprite artifactSprite;
    [HideInInspector] public bool isGenerated;
    [HideInInspector] public bool isOnlyPositiveEffect;
    [HideInInspector] public bool equipedBeforeGate;
    public ArtifactClass artifactClass;
    public bool cantBeEquippedInHub;


}

[Serializable]
public class ArtifactEffect
{
    public PlayerStat stat;
    public float value;
    public bool isPositive = true; // Domyœlnie efekty artefaktów s¹ pozytywne

    // Nowe zmienne dla efektów czasowych
    [Tooltip ("is activate on start")]
    public bool isEffectActivate = true; // Domyœlnie aktywny
    public int activateCount = 0;
    public int dayToSwitchActivate = 0; // Domyœlnie 0 – standardowe dzia³anie
}

[Serializable]
public class ArtifactTimeDesscribe
{
    // Konfiguracja komunikatu – opis pobieramy z tej klasy NameByLang,
    // a kolor jako prosty string (np. "red" lub "#FF0000")
    public NameByLang describe;
    public string color;
}

public class ArtifactsManager : MonoBehaviour
{
    public TextMeshProUGUI inGameConsole;

    public List<Artifact> artifacts = new();

    [Tooltip("auto assing (not used by artifacts)")]
    public List<PlayerStat> notUsedStats;

    MapMenager mapMenager;
    public ArtifactChoose artifactChoose;
    public Transform equippedArtifactsParent;
    public GameObject equippedArtifactsPrefab;
    PlayerMove playerMove;

    [Header("settings for specjal artifacts")]
    public Shooter OnAttackShooter;
    public RandomObjectSpawn randomObjectOnStart;
    public List<ArtifactClassColor> classesColors;
    public GameObject breakfastArtifact;

    [Header ("messeges ")]

    public DescriptionByLang[] artifactEffectChanges;

    public Animator messegeAnimator;

    [Serializable]
    public class ArtifactClassColor
    {
        public ArtifactClass clas;
        public string color;
    }

    // <<< NOWE: Publiczne zmienne konfiguruj¹ce komunikaty czasowe >>>
    public ArtifactTimeDesscribe durationTimeDescription;
    public ArtifactTimeDesscribe delayTimeDescription;
    public ArtifactTimeDesscribe permanentTimeDescription;
    // <<< Koniec nowych zmiennych >>>

    [Serializable]
    public class ArtifactTimeDesscribe
    {
       
        public List<NameByLang> timeEffectDescriptions = new List<NameByLang>();
        public string color;
    }

    private void Awake()
    {
        playerMove = FindAnyObjectByType<PlayerMove>();
        mapMenager = GetComponent<MapMenager>();
        LoadArtifacts();
        EquipPermanentUpgrades();

    }

   
    void EquipPermanentUpgrades()
    {
        for (int id = 0; id < artifacts.Count; id++)
        {
            int toEquip = artifacts[id].equipped;
            artifacts[id].equipped = 0;
            for (var i = 0; i < toEquip; i++)
            {
                EquipArtifact(id, false);
            }
        }
    }

    [Tooltip("if you want to let more that one artifact (max is defined in inspector) you should check beforeGate = false; beforeGate = true is for artifacts equipped before gate")]
    public void EquipArtifact(int id, bool beforeGate = true)
    {
        if (mapMenager == null) mapMenager = GetComponent<MapMenager>();
        Artifact artifact = artifacts[id];

        // Dla czasowych artefaktów zawsze u¿ywamy beforeGate = false!
        if (artifact != null && (artifact.equipped < 1 || beforeGate == false))
        {
            artifact.equipped++;

            if (beforeGate)
            {
                artifact.equipedBeforeGate = true;
                foreach (ArtifactEffect bonusEffect in artifact.bonusEffects)
                {
                    if (bonusEffect.dayToSwitchActivate > 0)
                    {
                        if (bonusEffect.isEffectActivate)
                        {
                            mapMenager.SetStatValue(bonusEffect.stat, mapMenager.GetStatsValue(bonusEffect.stat) + bonusEffect.value);
                            bonusEffect.activateCount++;
                            StartCoroutine(ToggleArtifactEffectAfterDelay(bonusEffect, artifact));
                        }
                        else
                        {
                            StartCoroutine(ToggleArtifactEffectAfterDelay(bonusEffect, artifact));
                        }
                        
                    }
                    else
                    {
                        if (bonusEffect.isEffectActivate)
                        {
                            mapMenager.SetStatValue(bonusEffect.stat, mapMenager.GetStatsValue(bonusEffect.stat) + bonusEffect.value);
                            bonusEffect.activateCount++;

                        }
                    }
                }
            }

            foreach (ArtifactEffect effect in artifact.effects)
            {
                if (effect.dayToSwitchActivate > 0)
                {
                    if (effect.isEffectActivate)
                    {
                        mapMenager.SetStatValue(effect.stat, mapMenager.GetStatsValue(effect.stat) + effect.value);
                        effect.activateCount++;

                        StartCoroutine(ToggleArtifactEffectAfterDelay(effect, artifact));
                    }
                    else
                    {
                        StartCoroutine(ToggleArtifactEffectAfterDelay(effect, artifact));
                    }
                }
                else
                {
                    if (effect.isEffectActivate)
                    {
                        mapMenager.SetStatValue(effect.stat, mapMenager.GetStatsValue(effect.stat) + effect.value);
                        effect.activateCount++;

                    }
                }
            }

            AddArtifactOwned(id);

            AddArtifactEffects(artifact);
            Debug.Log("Equipped artifact: " + artifact.name + " / actual equipped count of this type: " + artifact.equipped + " / beforeGate: " + artifact.equipedBeforeGate);
            AddVisualArtifactToEq(artifact);
        }
    }

    [Tooltip("if you want to let more that one artifact (max is defined in inspector) you should check beforeGate = false; beforeGate = true is for artifacts equipped before gate")]
    public void UnequipArtifact(int id, bool beforeGate = true)
    {
        if (mapMenager == null) mapMenager = GetComponent<MapMenager>();
        Artifact artifact = artifacts[id];

        if (artifact != null && (artifact.equipped > 0 || beforeGate == false))
        {
            artifact.equipped--;

            if (beforeGate)
            {
                Debug.Log("Before gate artifact: bonus eff count: " + artifact.bonusEffects.Count);
                artifact.equipedBeforeGate = false;

                foreach (ArtifactEffect bonusEffect in artifact.bonusEffects)
                {
                    if (/*bonusEffect.isEffectActivate && */bonusEffect.activateCount > artifact.equipped)
                    {
                        mapMenager.SetStatValue(bonusEffect.stat, mapMenager.GetStatsValue(bonusEffect.stat) - bonusEffect.value);
                        bonusEffect.activateCount--;
                    }
                }
            }

            foreach (ArtifactEffect effect in artifact.effects)
            {


                if (/*effect.isEffectActivate && */effect.activateCount > artifact.equipped)
                {
                    mapMenager.SetStatValue(effect.stat, mapMenager.GetStatsValue(effect.stat) - effect.value);
                    effect.activateCount--;
                }
            }

            RemoveArtifactEffects(artifact);
            Debug.Log("Unequip artifact: " + artifact.name + " / actual equipped count of this type: " + artifact.equipped + " / beforeGate: " + artifact.equipedBeforeGate);
            RemoveVisualArtifactFromEq(artifact);
        }
    }

    [Tooltip("change isOwned and save in saveGame")]
    public void AddArtifactOwned(int id)
    {
        artifacts[id].isOwned = true;
       
        SaveArtifacts();
    }

    public int GetNotOwnedArtifactCount()
    {
        int count = 0;
        foreach (var item in artifacts)
        {
            if (item.isOwned)
                continue;
            else
                count++;
        }
        return count;
    }
    public int GetOwnedArtifactCount(ArtifactClass artifactClass)
    {
        int count = 0;
        foreach (var item in artifacts)
        {
            if (item.isOwned && item.artifactClass == artifactClass)
            {
                count++;
            }
        }
        return count;
    }
    public int GetEquippedArtifactCount(ArtifactClass artifactClass)
    {
        int count = 0;
        foreach (var item in artifacts)
        {
            if (item.equipped > 0 && item.artifactClass == artifactClass)
            {
                count += item.equipped;
            }
        }
        return count;
    }
    public void SaveArtifacts()
    {
        SaveGame.SaveArtifactsStatus(artifacts);
    }

    public void LoadArtifacts()
    {
        artifacts = SaveGame.LoadArtifactsStatus(artifacts);
        for (int id = 0; id < artifacts.Count; id++)
        {
            if (artifacts[id].artifactClass != ArtifactClass.permanent)
            {
                artifacts[id].equipped = 0;
            }
        }
    }

    [Tooltip("all, but not 'permanent'")]
    public void UnequipAllArtifactsAtResurection()
    {
        for (int id = 0; id < artifacts.Count; id++)
        {
            if (artifacts[id].artifactClass != ArtifactClass.permanent)
            {
                if (artifacts[id].equipped > 0 && artifacts[id].equipedBeforeGate)
                {
                    UnequipArtifact(id, true);
                    artifacts[id].equipedBeforeGate = true;
                }

                while (artifacts[id].equipped > 0)
                {
                    UnequipArtifact(id, false);
                }
            }
            artifacts[id].bonusEffects.Clear();
        }
        ResetArtifactsEquipTotems();
    }

    public bool CheckSpriteIndywiduality(Sprite sprite, Artifact artifact)
    {
        foreach (var item in artifacts)
        {
            if (item.artifactSprite == sprite && artifact != item)
            {
                return false;
            }
        }
        return true;
    }

    public void OnValidate()
    {
        List<PlayerStat> stats = new();
        mapMenager = GetComponent<MapMenager>();

        foreach (var item in mapMenager.stats)
        {
            stats.Add(item.stat);
        }

        int nameID = 0;

        foreach (var item in artifacts)
        {
            if (item.artifactName.Length > 0)
            {
                string effectsTxt = "";

                if (!CheckSpriteIndywiduality(item.artifactSprite, item))
                {
                    effectsTxt += "  <color=red> { Error : the same sprite } </color>";
                }
                

                foreach (var eff in item.effects)
                {
                    string colorTag = eff.isPositive ? "green" : "orange";
                    effectsTxt += $" <color={colorTag}> ({FindInspectorStatId(eff.stat)}) " + eff.stat + "</color> [" + eff.value + "], ";
                }

                string color = "white";
                foreach (var clasColor in classesColors)
                {
                    if (clasColor.clas == item.artifactClass)
                    {
                        color = clasColor.color;
                    }
                }
                
                item.name = $"[ {nameID} ] <color={color}>" + item.artifactClass + "</color> | " + item.artifactName[0].name + " { " + effectsTxt + " }";
            }
            else if (item.artifactSprite != null)
            {
                item.name = item.artifactSprite.name;
            }

            foreach (var eff in item.effects)
            {
                if (stats.Contains(eff.stat)) stats.Remove(eff.stat);
            }

            notUsedStats = stats;

            nameID++;
        }

        for (int id = 0; id < artifacts.Count; id++)
        {
            bool positive = true;
            foreach (var item in artifacts[id].effects)
            {
                if (!item.isPositive)
                {
                    positive = false;
                }
            }
            artifacts[id].isOnlyPositiveEffect = positive;

            if (artifacts[id].isOnlyPositiveEffect)
                artifacts[id].name += " (pos) ";
            else
                artifacts[id].name += " (neg) ";
        }

        int x = 0;
        foreach (var obj in randomObjectOnStart.objectToGenerate)
        {
            obj.name = x + "  " + obj.prefabs[0].name + " " + obj.count;
            x++;
        }
        
        
        


        if (mapMenager != null && randomObjectOnStart.objectToGenerate.Length > 3)
        {
            randomObjectOnStart.objectToGenerate[3].count = (int)mapMenager.GetStatsValue(PlayerStat.showWayToDiamondChest);

            randomObjectOnStart.objectToGenerate[1].count = (int)mapMenager.GetStatsValue(PlayerStat.showWayToArtifact);

            randomObjectOnStart.objectToGenerate[2].count = (int)mapMenager.GetStatsValue(PlayerStat.showWayToArtifactStatue);

            randomObjectOnStart.objectToGenerate[4].count = (int)mapMenager.GetStatsValue(PlayerStat.dragonEggs);

        }

    }

    int FindInspectorStatId(PlayerStat stat)
    {
        MapMenager map = GetComponent<MapMenager>();

        if(map != null)
        {
            for (int i = 0; i < map.stats.Count ;i++)
            {
                if(stat == map.stats[i].stat)
                {
                    return i;
                }
            }
        }
        return 0;
    }

    [Tooltip("only reset values on scene, not changing mapMenager.playerStats")]
    void AddArtifactEffects(Artifact artifact)
    {
        // characters 
        CharacterMenager charMenager = GetComponent<CharacterMenager>();

        Character.CharacterStats addStats = new();
        float charHpMultiply = 1;

        bool actualizeCharacters = false;
        foreach (var eff in artifact.effects)
        {
            if (eff.stat == PlayerStat.alliesSpeedMultiply)
            {
                addStats.speedMultiplayer += eff.value;
                actualizeCharacters = true;
            }
            else if (eff.stat == PlayerStat.alliesDamageMultiply)
            {
                addStats.meleePowerMultiplayer += eff.value;
                addStats.wandsPowerMultiplayer += eff.value;
                actualizeCharacters = true;
            }
            else if (eff.stat == PlayerStat.alliesSizeMultiply)
            {
                addStats.characterSizeMultiplayer += eff.value;
                actualizeCharacters = true;
            }
            else if (eff.stat == PlayerStat.alliesHpMultiply)
            {
                charHpMultiply += eff.value;
                actualizeCharacters = true;
            }
            // player
            else if (eff.stat == PlayerStat.playerSizeForKill)
            {
                playerMove.growAfterKill = mapMenager.GetStatsValue(PlayerStat.playerSizeForKill);
            }
            else if (eff.stat == PlayerStat.playerHealingForWeaponThrow)
            {
                playerMove.healingForThrow = (int)mapMenager.GetStatsValue(PlayerStat.playerHealingForWeaponThrow);
            }
            else if (eff.stat == PlayerStat.gateHealingForPlayerKill)
            {
                playerMove.gateHealAfterKill = (int)mapMenager.GetStatsValue(PlayerStat.gateHealingForPlayerKill);
            }
            else if (eff.stat == PlayerStat.fireProjectileOnDamage)
            {
                playerMove.fireProjectileOnDamage = (int)mapMenager.GetStatsValue(PlayerStat.fireProjectileOnDamage);
            }
            else if (eff.stat == PlayerStat.iceDashEnd)
            {
                if (mapMenager.GetStatsValue(PlayerStat.iceDashEnd) > 0.5f)
                {
                    playerMove.isIceExplosionEndDash = true;
                }
                else
                {
                    playerMove.isIceExplosionEndDash = false;
                }
            }
            else if (eff.stat == PlayerStat.electricityOnDash)
            {
                if (mapMenager.GetStatsValue(PlayerStat.electricityOnDash) > 0.5f)
                {
                    playerMove.isElectricOnDash = true;
                }
                else
                {
                    playerMove.isElectricOnDash = false;
                }
            }
            else if (eff.stat == PlayerStat.projectileOnAttack)
            {
                OnAttackShooter.numberOfProjectiles = (int)mapMenager.GetStatsValue(PlayerStat.projectileOnAttack);
            }
            else if (eff.stat == PlayerStat.playerPickupRange)
            {
                playerMove.pickupColider.radius = playerMove.defaultPickupRange * (1f + mapMenager.GetStatsValue(PlayerStat.playerPickupRange));
            }
            else if (eff.stat == PlayerStat.bonusChestsOnStart)
            {
                randomObjectOnStart.objectToGenerate[0].count = (int)mapMenager.GetStatsValue(PlayerStat.bonusChestsOnStart);
            }
            else if (eff.stat == PlayerStat.playerHpMultiply)
            {
                playerMove.playerHpMultiply = mapMenager.GetStatsValue(PlayerStat.playerHpMultiply);
                if (playerMove.character != null) playerMove.character.MultiplyMaxHp(1 + eff.value);
            }
            else if (eff.stat == PlayerStat.playerSpeedMultiply)
            {
                playerMove.playerSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.playerSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.playerFreezeResistBonus)
            {
                playerMove.playerFreezeResistBonus = mapMenager.GetStatsValue(PlayerStat.playerFreezeResistBonus);
            }
            else if (eff.stat == PlayerStat.playerDefenseBonus)
            {
                playerMove.playerDefenseBonus = mapMenager.GetStatsValue(PlayerStat.playerDefenseBonus);
            }
            else if (eff.stat == PlayerStat.playerHealingPerSecBonus)
            {
                playerMove.playerHealingPerSecBonus = mapMenager.GetStatsValue(PlayerStat.playerHealingPerSecBonus);
            }
            else if (eff.stat == PlayerStat.playerSizeMultiply)
            {
                playerMove.playerSizeMultiply = mapMenager.GetStatsValue(PlayerStat.playerSizeMultiply);
                playerMove.ResetPlayerStats();
            }
            else if (eff.stat == PlayerStat.playerAttackSpeedMultiply)
            {
                playerMove.playerAttackSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.playerAttackSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.playerMeleeAttackMultiply)
            {
                playerMove.playerMeleeAttackMultiply = mapMenager.GetStatsValue(PlayerStat.playerMeleeAttackMultiply);
                if (playerMove.character != null) playerMove.character.characterStats.meleePowerMultiplayer += (int)eff.value;
            }
            else if (eff.stat == PlayerStat.playerWandAttackMultiply)
            {
                playerMove.playerWandAttackMultiply = mapMenager.GetStatsValue(PlayerStat.playerWandAttackMultiply);
                if (playerMove.character != null) playerMove.character.characterStats.wandsPowerMultiplayer += (int)eff.value;
            }
            else if (eff.stat == PlayerStat.playerKnockbacResist)
            {
                playerMove.playerKnockbacResist = mapMenager.GetStatsValue(PlayerStat.playerKnockbacResist);
            }
            else if (eff.stat == PlayerStat.showWayToDiamondChest)
            {
                randomObjectOnStart.objectToGenerate[3].count = (int)mapMenager.GetStatsValue(PlayerStat.showWayToDiamondChest);
            }
            else if (eff.stat == PlayerStat.showWayToArtifact)
            {
                randomObjectOnStart.objectToGenerate[1].count = (int)mapMenager.GetStatsValue(PlayerStat.showWayToArtifact);
            }
            else if (eff.stat == PlayerStat.showWayToArtifactStatue)
            {
                randomObjectOnStart.objectToGenerate[2].count = (int)mapMenager.GetStatsValue(PlayerStat.showWayToArtifactStatue);
            }
            else if (eff.stat == PlayerStat.showWayToArtifactStatue)
            {
                playerMove.dashCooldownRenevSpeed = 1 + mapMenager.GetStatsValue(PlayerStat.dashRenevSpeed);
            }
            else if (eff.stat == PlayerStat.playerItemUse)
            {
                playerMove.playerItemUse = mapMenager.GetStatsValue(PlayerStat.playerItemUse);
            }
            else if (eff.stat == PlayerStat.playerMoveOnAttack)
            {
                playerMove.playerSpeedOnAttack = mapMenager.GetStatsValue(PlayerStat.playerMoveOnAttack);
            }
            else if (eff.stat == PlayerStat.dragonEggs)
            {
                randomObjectOnStart.objectToGenerate[4].count = (int)mapMenager.GetStatsValue(PlayerStat.dragonEggs);
            }
            else if (eff.stat == PlayerStat.dashSpeedMultiply)
            {
                playerMove.dashSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.dashSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.sprintCooldownMultiply)
            {
                playerMove.sprintCooldownMultiply = mapMenager.GetStatsValue(PlayerStat.sprintCooldownMultiply);
            }
            else if (eff.stat == PlayerStat.sprintSpeedMultiply)
            {
                playerMove.sprintSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.sprintSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.getBonusBones)
            {
                playerMove.resourceCollector.CreateResourceAroundPlayer(ResourceType.bone, (int)eff.value);
            }
            else if (eff.stat == PlayerStat.getBonusWood)
            {
                playerMove.resourceCollector.CreateResourceAroundPlayer(ResourceType.wood, (int)eff.value);
            }
            else if (eff.stat == PlayerStat.getBonusIron)
            {
                playerMove.resourceCollector.CreateResourceAroundPlayer(ResourceType.iron, (int)eff.value);
            }
            else if (eff.stat == PlayerStat.getBonusSouls)
            {
                playerMove.resourceCollector.AddResource(ResourceType.souls, (int)eff.value);
            }
            else if (eff.stat == PlayerStat.getBonusFlowers)
            {
                playerMove.resourceCollector.CreateResourceAroundPlayer(ResourceType.flower, (int)eff.value);
            }
            else if(eff.stat == PlayerStat.playerSkillCooldownSpeed)
            {
                if (playerMove.character != null) playerMove.character.cooldownSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.playerSkillCooldownSpeed);
            }
            
        }

        if (charMenager != null && actualizeCharacters)
        {
            foreach (CharacterMenager.Ally ally in charMenager.allyList)
            {
                if (ally.characterMove != null && !ally.characterMove.isDead)
                {
                    ally.characterMove.character.AddModifier(addStats);
                    ally.characterMove.character.MultiplyMaxHp(charHpMultiply);
                }
            }
        }

        GateComponnent gateComponnent = FindAnyObjectByType<GateComponnent>();
        if (gateComponnent != null)
        {
            gateComponnent.ActualizeGateStats();
        }
    }

    void RemoveArtifactEffects(Artifact artifact)
    {
        GateComponnent gateComponnent = FindAnyObjectByType<GateComponnent>();
        if (gateComponnent != null)
        {
            gateComponnent.ActualizeGateStats();
        }

        foreach (var eff in artifact.effects)
        {
            if (eff.stat == PlayerStat.playerSizeForKill)
            {
                playerMove.growAfterKill = mapMenager.GetStatsValue(PlayerStat.playerSizeForKill);
            }
            else if (eff.stat == PlayerStat.playerHealingForWeaponThrow)
            {
                playerMove.healingForThrow = (int)mapMenager.GetStatsValue(PlayerStat.playerHealingForWeaponThrow);
            }
            else if (eff.stat == PlayerStat.gateHealingForPlayerKill)
            {
                playerMove.gateHealAfterKill = (int)mapMenager.GetStatsValue(PlayerStat.gateHealingForPlayerKill);
            }
            else if (eff.stat == PlayerStat.fireProjectileOnDamage)
            {
                playerMove.fireProjectileOnDamage = (int)mapMenager.GetStatsValue(PlayerStat.fireProjectileOnDamage);
            }
            else if (eff.stat == PlayerStat.iceDashEnd)
            {
                if (mapMenager.GetStatsValue(PlayerStat.iceDashEnd) > 0.5f)
                {
                    playerMove.isIceExplosionEndDash = true;
                }
                else
                {
                    playerMove.isIceExplosionEndDash = false;
                }
            }
            else if (eff.stat == PlayerStat.electricityOnDash)
            {
                if (mapMenager.GetStatsValue(PlayerStat.electricityOnDash) > 0.5f)
                {
                    playerMove.isElectricOnDash = true;
                }
                else
                {
                    playerMove.isElectricOnDash = false;
                }
            }
            else if (eff.stat == PlayerStat.projectileOnAttack)
            {
                OnAttackShooter.numberOfProjectiles = (int)mapMenager.GetStatsValue(PlayerStat.projectileOnAttack);
            }
            else if (eff.stat == PlayerStat.playerPickupRange)
            {
                playerMove.pickupColider.radius = playerMove.defaultPickupRange * (1f + mapMenager.GetStatsValue(PlayerStat.playerPickupRange));
            }
            else if (eff.stat == PlayerStat.bonusChestsOnStart)
            {
                randomObjectOnStart.objectToGenerate[0].count = (int)mapMenager.GetStatsValue(PlayerStat.bonusChestsOnStart);
            }
            else if (eff.stat == PlayerStat.playerHpMultiply)
            {
                playerMove.playerHpMultiply = mapMenager.GetStatsValue(PlayerStat.playerHpMultiply);
                if(playerMove.character != null) playerMove.character.MultiplyMaxHp(1 + eff.value);

            }
            else if (eff.stat == PlayerStat.playerSpeedMultiply)
            {
                playerMove.playerSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.playerSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.playerFreezeResistBonus)
            {
                playerMove.playerFreezeResistBonus = mapMenager.GetStatsValue(PlayerStat.playerFreezeResistBonus);
            }
            else if (eff.stat == PlayerStat.playerDefenseBonus)
            {
                playerMove.playerDefenseBonus = mapMenager.GetStatsValue(PlayerStat.playerDefenseBonus);
            }
            else if (eff.stat == PlayerStat.playerHealingPerSecBonus)
            {
                playerMove.playerHealingPerSecBonus = mapMenager.GetStatsValue(PlayerStat.playerHealingPerSecBonus);
            }
            else if (eff.stat == PlayerStat.playerSizeMultiply)
            {
                playerMove.playerSizeMultiply = mapMenager.GetStatsValue(PlayerStat.playerSizeMultiply);
                playerMove.ResetPlayerStats();
            }
            else if (eff.stat == PlayerStat.playerAttackSpeedMultiply)
            {
                playerMove.playerAttackSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.playerAttackSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.playerMeleeAttackMultiply)
            {
                playerMove.playerMeleeAttackMultiply = mapMenager.GetStatsValue(PlayerStat.playerMeleeAttackMultiply);
                if (playerMove.character != null) playerMove.character.characterStats.meleePowerMultiplayer += (int)eff.value;
            }
            else if (eff.stat == PlayerStat.playerWandAttackMultiply)
            {
                playerMove.playerWandAttackMultiply = mapMenager.GetStatsValue(PlayerStat.playerWandAttackMultiply);
                if (playerMove.character != null) playerMove.character.characterStats.wandsPowerMultiplayer += (int)eff.value;
            }
            else if (eff.stat == PlayerStat.playerKnockbacResist)
            {
                playerMove.playerKnockbacResist = mapMenager.GetStatsValue(PlayerStat.playerKnockbacResist);
            }
            else if (eff.stat == PlayerStat.showWayToArtifact)
            {
                randomObjectOnStart.objectToGenerate[1].count = (int)mapMenager.GetStatsValue(PlayerStat.bonusChestsOnStart);
            }
            else if (eff.stat == PlayerStat.showWayToArtifactStatue)
            {
                randomObjectOnStart.objectToGenerate[2].count = (int)mapMenager.GetStatsValue(PlayerStat.bonusChestsOnStart);
            }
            else if (eff.stat == PlayerStat.showWayToDiamondChest)
            {
                randomObjectOnStart.objectToGenerate[3].count = (int)mapMenager.GetStatsValue(PlayerStat.bonusChestsOnStart);
            }
            else if (eff.stat == PlayerStat.showWayToArtifactStatue)
            {
                playerMove.dashCooldownRenevSpeed = 1 + mapMenager.GetStatsValue(PlayerStat.dashRenevSpeed);
            }
            else if (eff.stat == PlayerStat.playerItemUse)
            {
                playerMove.playerItemUse = mapMenager.GetStatsValue(PlayerStat.playerItemUse);
            }
            else if (eff.stat == PlayerStat.playerMoveOnAttack)
            {
                playerMove.playerSpeedOnAttack = mapMenager.GetStatsValue(PlayerStat.playerMoveOnAttack);
            }
            else if (eff.stat == PlayerStat.dragonEggs)
            {
                randomObjectOnStart.objectToGenerate[4].count = (int)mapMenager.GetStatsValue(PlayerStat.dragonEggs);
            }
            else if (eff.stat == PlayerStat.dashSpeedMultiply)
            {
                playerMove.dashSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.dashSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.sprintCooldownMultiply)
            {
                playerMove.sprintCooldownMultiply = mapMenager.GetStatsValue(PlayerStat.sprintCooldownMultiply);
            }
            else if (eff.stat == PlayerStat.sprintSpeedMultiply)
            {
                playerMove.sprintSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.sprintSpeedMultiply);
            }
            else if (eff.stat == PlayerStat.playerSkillCooldownSpeed)
            {
                if (playerMove.character != null) playerMove.character.cooldownSpeedMultiply = mapMenager.GetStatsValue(PlayerStat.playerSkillCooldownSpeed);
            }
        }
    }

    public void AddVisualArtifactToEq(Artifact artifact)
    {
        if (artifact.artifactClass == ArtifactClass.permanent) return;


        if (artifact.equipped > 1)
        {
            foreach (Transform inEq in equippedArtifactsParent)
            {
                if (inEq.Find("Icon").GetComponent<Image>().sprite == artifact.artifactSprite)
                {
                    inEq.Find("count").GetComponent<TextMeshProUGUI>().text = "X" + artifact.equipped;
                    inEq.Find("pop up").Find("describe").GetComponent<TextMeshProUGUI>().text = FormatEffectsWithMultiply(artifact);
                    break;
                }
            }
        }
        else if (artifact.equipped == 1)
        {
            GameObject artifactGO = Instantiate(equippedArtifactsPrefab, equippedArtifactsParent);
            artifactGO.transform.Find("Icon").GetComponent<Image>().sprite = artifact.artifactSprite;
            artifactGO.transform.Find("pop up").Find("describe").GetComponent<TextMeshProUGUI>().text = FormatEffectsWithMultiply(artifact);
            artifactGO.transform.Find("count").GetComponent<TextMeshProUGUI>().text = "";
        }
    }
    public void RemoveVisualArtifactFromEq(Artifact artifact)
    {
        if (artifact.equipped > 1)
        {
            foreach (Transform inEq in equippedArtifactsParent)
            {
                if (inEq.Find("Icon").GetComponent<Image>().sprite == artifact.artifactSprite)
                {
                    inEq.Find("count").GetComponent<TextMeshProUGUI>().text = "X" + artifact.equipped;
                    inEq.Find("pop up").Find("describe").GetComponent<TextMeshProUGUI>().text = FormatEffectsWithMultiply(artifact);
                    break;
                }
            }
        }
        else if (artifact.equipped == 1)
        {
            foreach (Transform inEq in equippedArtifactsParent)
            {
                if (inEq.Find("Icon").GetComponent<Image>().sprite == artifact.artifactSprite)
                {
                    inEq.Find("count").GetComponent<TextMeshProUGUI>().text = "";
                    inEq.Find("pop up").Find("describe").GetComponent<TextMeshProUGUI>().text = FormatEffectsWithMultiply(artifact);
                    break;
                }
            }
        }
        else
        {
            foreach (Transform inEq in equippedArtifactsParent)
            {
                if (inEq.Find("Icon").GetComponent<Image>().sprite == artifact.artifactSprite)
                {
                    Destroy(inEq.gameObject);
                    break;
                }
            }
        }
    }

    


    [Tooltip("affects multiply by count of artifact equipped")]
    public string FormatEffectsWithMultiply(Artifact artifact)
    {
        
        
        
        string description = "";
        MapMenager mapMenager = FindAnyObjectByType<MapMenager>();





        foreach (ArtifactEffect effect in artifact.effects)
        {
           // Debug.Log("stat: " + effect.stat + "active: " + effect.activateCount);


            if (effect.activateCount <= 0)
            {

                continue;
            }


            string effectText = mapMenager.GetStatDescription(effect.stat)
                .Replace("?%", (effect.value * effect.activateCount * 100).ToString("0") + "%")
                .Replace("?", (effect.value * effect.activateCount).ToString("0"));

            if (effect.value * effect.activateCount < 0)
            {
                effectText = effectText.Replace("+", "");
                effectText = effectText.Replace("- ", "-");
            }

            string colorTag = effect.isPositive ? "green" : "red";
            description += $"<color={colorTag}>- {effectText}</color>\n";



            

        }



        
        return description;
    }

    private string GetNameByLang(NameByLang[] names)
    {
        PlayerSetting playerSetting = FindAnyObjectByType<PlayerSetting>();
        foreach (var name in names)
        {
            if (name.lang == playerSetting.activeSetting.gameLang)
            {
                return name.name;
            }
        }
        return "Unknown Artifact"; // Fallback name
    }

    [SerializeField] Animator artifactsEqAnimator;
    [SerializeField] StatsDescriber playerStats;
    public void ShowHideArtifactsEQ()
    {
        

        TimeMenager timeManager = FindAnyObjectByType<TimeMenager>();

        if (timeManager != null)
        {
            if (artifactsEqAnimator.GetBool("open"))
            {
                artifactsEqAnimator.SetBool("open", false);
                timeManager.TimeStart(this);
                if (playerStats != null)
                {
                    playerStats.gameObject.SetActive(false);
             
                }
            }
            else
            {
                artifactsEqAnimator.SetBool("open", true);
                timeManager.TimeStop(this);

                if(playerStats != null)
                {


                    playerStats.gameObject.SetActive(true);
                    Animator animator = playerStats.GetComponent<Animator>();


                    if (playerMove != null && playerMove.character != null)
                    {
                        playerStats.DescribeCharacterStats(playerMove.character);
                    }

                    if (animator != null) animator.SetTrigger("change");


                }


            }
        }
    }

    public void ResetArtifactsEquipTotems()
    {
        ArtifactEquipTotem[] totems = FindObjectsOfType<ArtifactEquipTotem>();

        foreach (ArtifactEquipTotem item in totems)
        {
            if (item.artifactEquip != null)
            {
                item.artifactEquip.CreateArtifactSlots();
            }
        }
    }

    // ====================
    // Coroutine – prze³¹czanie stanu efektu po zadanym czasie
    // ====================
    private IEnumerator ToggleArtifactEffectAfterDelay(ArtifactEffect effect, Artifact artifact)
    {
        // Wyliczamy d³ugoœæ "dnia" jako sumê dayDuration i nightDuration
        float totalDayTime = mapMenager.GetStatsValue(PlayerStat.dayDuration) +
                             mapMenager.GetStatsValue(PlayerStat.nightDuration);

        // Czekamy: (totalDayTime * dayToSwitchActivate)
        yield return new WaitForSeconds(totalDayTime * effect.dayToSwitchActivate);

        // Po up³ywie czasu prze³¹czamy efekt:
        // Jeœli by³ aktywny – odejmujemy wartoœæ, a nastêpnie ustawiamy isEffectActivate na false.
        // Jeœli nie by³ aktywny – dodajemy wartoœæ, ustawiaj¹c isEffectActivate na true.
        bool showMessege = true;

        if (effect.isEffectActivate)
        {
            if(effect.activateCount > 0)
            {

                foreach (Artifact art in artifacts)
                {
                    if (art.artifactSprite == artifact.artifactSprite)
                    {
                        foreach (ArtifactEffect eff in art.effects)
                        {
                            if (eff.value == effect.value && eff.stat == effect.stat)
                            {

                                if(eff.activateCount > 0)
                                {
                                    mapMenager.SetStatValue(effect.stat, mapMenager.GetStatsValue(effect.stat) - effect.value);
                                    RemoveArtifactEffects(art);


                                    eff.activateCount--;
                                   // effect.activateCount--;
                                }
                               
                                break;

                            }
                        }
                        break;
                    }
                }

               
            }else
            {
                showMessege = false;
            }
            Debug.Log("Toggled OFF effect for artifact: " + artifact.artifactName[0].name + " | Stat: " + effect.stat);
            inGameConsole.text += "<color=red> Toggled OFF " + artifact.artifactName[0].name + " | Stat: " + effect.stat + "| Activate: " + effect.activateCount + " | active stat value: " + mapMenager.GetStatsValue(effect.stat) + " | change value: " + showMessege +  "\n";


        }
        else
        {
            if(effect.activateCount < artifact.equipped)
            {
                foreach (Artifact art in artifacts)
                {
                    if (art.artifactSprite == artifact.artifactSprite)
                    {
                        foreach (ArtifactEffect eff in art.effects)
                        {
                            if (eff.value == effect.value && eff.stat == effect.stat)
                            {
                                mapMenager.SetStatValue(effect.stat, mapMenager.GetStatsValue(effect.stat) + effect.value);
                                AddArtifactEffects(art);

                                eff.activateCount++;
                                // effect.activateCount++;
                                break;

                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                showMessege = false;
            }
            
            Debug.Log("Toggled ON effect for artifact: " + artifact.artifactName[0].name + " | Stat: " + effect.stat);
            inGameConsole.text += "<color=green> Toggled ON " + artifact.artifactName[0].name + " | Stat: " + effect.stat + "| Activate: " + effect.activateCount + " | active stat value: " + mapMenager.GetStatsValue(effect.stat)+ " | change value: " + showMessege + "\n";
        }
        if (showMessege)
        {
            ShowPlayerMessege(artifactEffectChanges);
            RemoveVisualArtifactFromEq(artifact);
        }
      


    }

    public void ShowPlayerMessege(DescriptionByLang[] description)
    {

        CardEq cardEq = FindAnyObjectByType<CardEq>();

        messegeAnimator.gameObject.SetActive(true);
        messegeAnimator.Play(0);
        messegeAnimator.GetComponentInChildren<TextMeshProUGUI>().text = cardEq.GetDescriptionByLang(description);

    }

    
}
