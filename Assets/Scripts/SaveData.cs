using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int playerMoney;
    public string saveDate;
    public System.Collections.Generic.List<FruitSaveData> fruitStates = new System.Collections.Generic.List<FruitSaveData>();
    public System.Collections.Generic.List<HarvestZoneSaveData> harvestZones = new System.Collections.Generic.List<HarvestZoneSaveData>();
    public System.Collections.Generic.List<UnlockZoneSaveData> unlockZones = new System.Collections.Generic.List<UnlockZoneSaveData>();
    public System.Collections.Generic.List<UpgradeEntrySaveData> upgradeEntries = new System.Collections.Generic.List<UpgradeEntrySaveData>();
}

[System.Serializable]
public class FruitSaveData
{
    public string fruitName;
    public bool isActive;
    public int basePrice;
    public float regrowthDuration;
}

[System.Serializable]
public class HarvestZoneSaveData
{
    public string saveID;
    public int incomeLevel;
    public int speedLevel;
    public int incomeUpgradeCost;
    public int speedUpgradeCost;
    public int treePurchaseCost;
    public int activeTreeCount;
}

[System.Serializable]
public class UnlockZoneSaveData
{
    public string saveID;
    public int currentInvestedMoney;
    public bool isUnlocked;
}

[System.Serializable]
public class UpgradeEntrySaveData
{
    public string entryName;
    public int incomeLevel;
    public int speedLevel;
    public int incomeUpgradeCost;
    public int speedUpgradeCost;
    public int treePurchaseCost;
    public int activeTreeCount;
}
