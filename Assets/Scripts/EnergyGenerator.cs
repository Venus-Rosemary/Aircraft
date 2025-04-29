using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public enum ItemType
{
    Energy,
    Health,
    Score,
}

[System.Serializable]
public class ItemPrefab
{
    public ItemType type;
    public GameObject prefab;
    public float spawnWeight = 1f; // 生成权重，权重越高越容易生成
}
public class EnergyGenerator : Singleton<EnergyGenerator>
{
    #region 字段和属性
    [Header("道具生成设置")]
    public ItemPrefab[] itemPrefabs;                         // 道具预制体数组

    [Header("生成参数")]
    public float spawnHeight = 5f;                           // 生成高度

    public float baseSpawnInterval = 2f;                     // 基础生成间隔
    public float minSpawnInterval = 0.5f;                    // 最小生成间隔
    private float spawnInterval = 2f;                             // 当前生成间隔

    public float spawnWidth = 5f;                            // 生成宽度
    public float moveSpeed;                                  // 移动速度
    public float destroyDistance = 20f;                      // 销毁距离

    [Header("任务系统")]
    public int taskLength = 3;                               // 任务序列长度
    [SerializeField] private List<ItemType> currentTask;     // 当前任务序列
    [SerializeField] private List<ItemType> playerCollection;// 玩家收集序列
    private bool isTaskActive = false;                       // 任务激活状态

    private float lastSpawnTime;                             // 上次生成时间
    private bool startSpawn = false;                         // 是否开始生成
    [SerializeField] private List<GameObject> allSpawnedEnergies = new List<GameObject>();
    #endregion


    #region 生命周期方法

    private void Start()
    {
        UpdateSpawnInterval();
    }
    void Update()
    {
        if (Time.time - lastSpawnTime >= spawnInterval && startSpawn)
        {
            SpawnEnergy();
            lastSpawnTime = Time.time;
        }
    }
    #endregion


    #region 道具生成

    private void UpdateSpawnInterval()
    {
        float t = (taskLength - 2f) / 4f; // 归一化任务长度
        t = Mathf.Clamp01(t); // 确保值在0-1之间
        spawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
    }

    // 在任务长度改变时更新生成间隔
    public void SetTaskLength(int length)
    {
        taskLength = length;
        UpdateSpawnInterval();
    }

    void SpawnEnergy()//能量块生成
    {
        if (itemPrefabs == null || itemPrefabs.Length == 0) return;

        // 计算总权重
        float totalWeight = 0;
        foreach (var items in itemPrefabs)
        {
            totalWeight += items.spawnWeight;
        }

        // 随机选择一个道具
        float randomWeight = Random.Range(0, totalWeight);
        ItemPrefab selectedItem = itemPrefabs[0];
        float currentWeight = 0;

        foreach (var items in itemPrefabs)
        {
            currentWeight += items.spawnWeight;
            if (randomWeight <= currentWeight)
            {
                selectedItem = items;
                break;
            }
        }

        float randomX = Random.Range(-spawnWidth, spawnWidth);
        float randomZ = transform.position.z;

        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);
        GameObject item = Instantiate(selectedItem.prefab, spawnPosition, Quaternion.identity, transform);
        
        // 设置标签
        switch (selectedItem.type)
        {
            case ItemType.Energy:
                item.tag = "Energy";
                break;
            case ItemType.Health:
                item.tag = "Health";
                break;
            case ItemType.Score:
                item.tag = "Score";
                break;
        }

        item.AddComponent<ObstacleMovement>().speed = moveSpeed;
        allSpawnedEnergies.Add(item);


        DifficultyController.Instance.OnEnergySpawned();
    }
    #endregion


    #region 公共接口
    public void SetStartSpawn(bool start)
    {
        startSpawn = start;
    }

    public void SetMoveSpeed(float mSpeed)//外部设置速度
    {
        moveSpeed = mSpeed;
        foreach (var energy in allSpawnedEnergies)
        {
            if(energy != null)
                energy.GetComponent<ObstacleMovement>().speed = mSpeed;
        }
    }

    public void RemoveFromList(GameObject energy)
    {
        allSpawnedEnergies.Remove(energy);
    }

    public void ClearAllItems()
    {
        foreach (var item in allSpawnedEnergies.ToArray())
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        allSpawnedEnergies.Clear();
    }
    #endregion


    #region 任务模块
    // 生成新任务
    public string GenerateNewTask()
    {
        isTaskActive = true;
        playerCollection.Clear();

        // 生成随机序列
        for (int i = 0; i < taskLength; i++)
        {
            ItemType randomType = (ItemType)Random.Range(0, System.Enum.GetValues(typeof(ItemType)).Length);
            currentTask.Add(randomType);
        }

        // 转换为显示字符串
        return string.Join("-", currentTask);
    }

    // 检查收集顺序
    public void CheckCollection(ItemType collectedType)
    {
        if (!isTaskActive) return;

        playerCollection.Add(collectedType);

        // 检查是否按正确顺序收集
        if (playerCollection.Count <= currentTask.Count)
        {
            // 检查当前收集的物品是否符合顺序
            if (playerCollection[playerCollection.Count - 1] != currentTask[playerCollection.Count - 1])
            {
                // 收集顺序错误，重置任务
                ResetTask();
                UIController.Instance.UpdateTaskUI("收集顺序错误，任务重置！");

                GameManager.Instance.SetTaskCompletedState(true);

                DifficultyController.Instance.OnTaskCompleted(false);

                DOVirtual.DelayedCall(3F, () => UIController.Instance.UpdateTaskUI(""));
                return;
            }

            // 检查是否完成整个序列
            if (playerCollection.Count == currentTask.Count)
            {
                // 完成任务，给予奖励
                PlayerControl player = FindObjectOfType<PlayerControl>();
                if (player != null)
                {
                    player.AddScore(taskLength); // 额外奖励分数

                    DifficultyController.Instance.CheckTaskBonusAchievement();

                    UIController.Instance.UpdateTaskUI("任务完成！获得额外分数：" + taskLength);

                    GameManager.Instance.SetTaskCompletedState(true);

                    DifficultyController.Instance.OnTaskCompleted(true);

                    DOVirtual.DelayedCall(3F, () => UIController.Instance.UpdateTaskUI(""));
                }
                ResetTask();
            }
        }
    }

    // 重置任务
    public void ResetTask()
    {
        currentTask.Clear();
        playerCollection.Clear();
        isTaskActive = false;
    }

    // 重置任务索引的方法
    public void ResetTaskSystem()
    {
        ResetTask();
    }

    // 修改 OnTriggerEnter 中的处理
    public ItemType GetItemTypeFromGameObject(GameObject obj)
    {
        switch (obj.tag)
        {
            case "Energy": 
                return ItemType.Energy;
            case "Health": 
                return ItemType.Health;
            case "Score": 
                return ItemType.Score;
            default: 
                return ItemType.Energy;
        }
    }
    #endregion
}