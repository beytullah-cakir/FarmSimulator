using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int playerMoney;
    public string saveDate;
    public List<FruitSaveData> fruitStates = new List<FruitSaveData>();
    public List<HarvestZoneSaveData> harvestZones = new List<HarvestZoneSaveData>();
    public List<UnlockZoneSaveData> unlockZones = new List<UnlockZoneSaveData>();
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
