using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 道具颜色枚举
public enum ItemColor
{
    Blue,   
    Yellow, // 能量
    Green,  
    Cyan,   // 血量
    Purple, // 任务触发
    Red,    // 护盾
}

// 道具形状枚举
public enum ItemShape
{
    Cube,
    Sphere,
    Diamond,
    Star
}

// 颜色配置
[System.Serializable]
public class ColorConfig
{
    public string colorName;           // 颜色名称
    public Color color = Color.white;  // 颜色值
    public ItemColor effectType;       // 效果类型
    [Range(0f, 1f)]
    public float spawnWeight = 1f;     // 生成权重
}

[System.Serializable]
public class ItemShapePrefab
{
    public ItemShape shape;
    public GameObject prefab;
}

public class EnergyGenerator : Singleton<EnergyGenerator>
{
    #region 字段和属性
    [Header("道具预制体设置")]
    public ItemShapePrefab[] shapePrefabs;                   // 形状预制体数组

    [Header("道具颜色设置")]
    public ColorConfig[] colorConfigs;                       // 颜色配置数组
    private float totalSpawnWeight;                          // 总生成权重

    [Header("生成参数")]
    public float spawnHeightMin = 3f;                        // 最小生成高度
    public float spawnHeightMax = 7f;                        // 最大生成高度
    public float baseSpawnInterval = 2f;                     // 基础生成间隔
    public float minSpawnInterval = 0.5f;                    // 最小生成间隔
    public float boostSpawnMultiplier = 0.5f;               // 加速时生成间隔倍率
    private float spawnInterval = 2f;                        // 当前生成间隔
    private bool isBoostMode = false;                        // 是否处于加速状态

    public float spawnWidth = 5f;                            // 生成宽度
    public float moveSpeed;                                  // 移动速度
    public float destroyDistance = 20f;                      // 销毁距离

    [Header("任务系统")]
    public int taskLength = 3;                               // 任务序列长度
    [SerializeField] private List<ItemColor> currentTask;    // 当前任务序列
    [SerializeField] private List<ItemColor> playerCollection;// 玩家收集序列
    private bool isTaskActive = false;                       // 任务激活状态

    private float lastSpawnTime;                             // 上次生成时间
    private bool startSpawn = false;                         // 是否开始生成
    [SerializeField] private List<GameObject> allSpawnedItems = new List<GameObject>();
    #endregion

    #region 生命周期方法
    private void Start()
    {
        currentTask = new List<ItemColor>();
        playerCollection = new List<ItemColor>();
        UpdateSpawnInterval();
        CalculateTotalWeight();
    }

    private void CalculateTotalWeight()
    {
        totalSpawnWeight = 0f;
        if (colorConfigs != null)
        {
            foreach (var config in colorConfigs)
            {
                totalSpawnWeight += config.spawnWeight;
            }
        }
    }

    void Update()
    {
        if (Time.time - lastSpawnTime >= (isBoostMode ? spawnInterval * boostSpawnMultiplier : spawnInterval) && startSpawn)
        {
            SpawnItem();
            lastSpawnTime = Time.time;
        }
    }
    #endregion

    #region 道具生成
    private void UpdateSpawnInterval()
    {
        float t = (taskLength - 2f) / 4f;
        t = Mathf.Clamp01(t);
        spawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
    }

    public void SetTaskLength(int length)
    {
        taskLength = length;
        UpdateSpawnInterval();
    }

    void SpawnItem()
    {
        if (shapePrefabs == null || shapePrefabs.Length == 0 || colorConfigs == null || colorConfigs.Length == 0) return;

        // 随机选择形状
        int randomShapeIndex = Random.Range(0, shapePrefabs.Length);
        ItemShapePrefab selectedShape = shapePrefabs[randomShapeIndex];

        // 根据权重随机选择颜色配置
        ColorConfig selectedColorConfig = GetRandomColorConfig();
        if (selectedColorConfig == null) return;

        // 随机生成位置
        float randomX = Random.Range(-spawnWidth, spawnWidth);
        float randomY = Random.Range(spawnHeightMin, spawnHeightMax);
        float randomZ = transform.position.z;

        Vector3 spawnPosition = new Vector3(randomX, randomY, randomZ);
        GameObject item = Instantiate(selectedShape.prefab, spawnPosition, Quaternion.identity, transform);

        // 设置Outline颜色
        var outline = item.GetComponent<Outline>();
        if (outline != null)
        {
            outline.OutlineColor = selectedColorConfig.color;
        }

        // 设置标签和类型
        switch (selectedColorConfig.effectType)
        {
            case ItemColor.Cyan:
                item.tag = "Health";
                break;
            case ItemColor.Yellow:
                item.tag = "Energy";
                break;
            case ItemColor.Green:
                item.tag = "Score";
                break;
        }

        // 添加移动组件
        item.AddComponent<ObstacleMovement>().speed = moveSpeed;
        allSpawnedItems.Add(item);

        DifficultyController.Instance.OnEnergySpawned();
    }

    // 根据权重随机选择颜色配置
    private ColorConfig GetRandomColorConfig()
    {
        if (colorConfigs == null || colorConfigs.Length == 0) return null;

        float randomValue = Random.Range(0f, totalSpawnWeight);
        float currentWeight = 0f;

        foreach (var config in colorConfigs)
        {
            currentWeight += config.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return config;
            }
        }

        return colorConfigs[0];
    }
    #endregion

    #region 公共接口
    public void SetStartSpawn(bool start)
    {
        startSpawn = start;
    }

    public void SetBoostMode(bool boost)
    {
        isBoostMode = boost;
    }

    public void SetMoveSpeed(float mSpeed)
    {
        moveSpeed = mSpeed;
        foreach (var item in allSpawnedItems)
        {
            if(item != null)
                item.GetComponent<ObstacleMovement>().speed = mSpeed;
        }
    }

    public void RemoveFromList(GameObject item)
    {
        allSpawnedItems.Remove(item);
    }

    public void ClearAllItems()
    {
        foreach (var item in allSpawnedItems.ToArray())
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        allSpawnedItems.Clear();
    }
    #endregion

    #region 任务模块
    public string GenerateNewTask()
    {
        isTaskActive = true;
        playerCollection.Clear();
        currentTask.Clear();

        // 生成随机序列
        for (int i = 0; i < taskLength; i++)
        {
            ItemColor randomColor = (ItemColor)Random.Range(0, System.Enum.GetValues(typeof(ItemColor)).Length);
            currentTask.Add(randomColor);
        }

        // 转换为显示字符串
        return string.Join("-", currentTask);
    }

    public void CheckCollection(ItemColor collectedColor)
    {
        if (!isTaskActive) return;

        playerCollection.Add(collectedColor);

        // 检查是否按正确顺序收集
        if (playerCollection.Count <= currentTask.Count)
        {
            for (int i = 0; i < playerCollection.Count; i++)
            {
                if (playerCollection[i] != currentTask[i])
                {
                    ResetTask();
                    return;
                }
            }

            // 如果收集完成
            if (playerCollection.Count == currentTask.Count)
            {
                GameManager.Instance.SetTaskCompletedState(true);
                PlayerControl player = FindObjectOfType<PlayerControl>();
                if (player != null)
                {
                    player.AddScore(1);
                }
                ResetTask();
            }
        }
    }

    public void ResetTask()
    {
        isTaskActive = false;
        currentTask.Clear();
        playerCollection.Clear();
    }

    public void ResetTaskSystem()
    {
        ResetTask();
        ClearAllItems();
    }

    public ItemColor GetItemColorFromGameObject(GameObject obj)
    {
        if (obj.CompareTag("Health")) return ItemColor.Cyan;
        if (obj.CompareTag("Energy")) return ItemColor.Yellow;
        if (obj.CompareTag("Score")) return ItemColor.Green;
        return ItemColor.Blue; // 默认返回蓝色
    }
    #endregion
}