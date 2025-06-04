using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class DifficultyController : Singleton<DifficultyController>
{
    [Header("难度基础数值")]
    public int baseAsteroidNumber = 20;    // 基础小行星数量(和linearAsteroid脚本中一样)
    public int baseTaskLength = 3;        // 基础任务长度
    public float checkInterval = 60f;     // 难度检查间隔（秒）

    [Header("难度调整参数")]
    [Range(0, 1)]
    //public float difficultyThreshold = 0.2f;  // 难度调整阈值
    public int asteroidAdjustment = 10;        // 小行星数量调整值(增减值)
    public int taskLengthAdjustment = 1;      // 任务长度调整值(增减值)
    public int maxHitCount = 10;            //击中次数阈值

    [Header("玩家表现数值")]
    public float evasionRate = 0f;        // 躲避率
    public float pickingRate = 0f;        // 拾取率
    public bool taskRightOrWrong = false; // 任务完成情况

    private float timer;
    private int asteroidEncountered;      // 遇到的小行星数量
    private int asteroidHit;              // 被击中的次数
    private int energySpawned;            // 生成的能量块数量
    private int energyCollected;          // 收集的能量块数量
    private int OriginallyAsteroidNumber; // 最初小行星带的小行星数量、
    private int OriginallyTaskLength;     // 最初任务长度

    [Header("成就内容")]
    public List<AchievementData> achievementDatas = new List<AchievementData>();
    private void Start()
    {
        // 初始化成就列表
        achievementDatas = new List<AchievementData>
        {
            new AchievementData("首次通关", false),
            new AchievementData("完美躲避", false),
            new AchievementData("血量耗尽", false),
            new AchievementData("能量耗尽", false),
            new AchievementData("任务达人", false),
            new AchievementData("极限挑战", false)
        };
        SaveListAchievementToJson();

        ResetStats();
        // 初始化难度
        //UpdateDifficulty();
        OriginallyAsteroidNumber = 20;

        OriginallyTaskLength= EnergyGenerator.Instance.taskLength;

        baseAsteroidNumber = OriginallyAsteroidNumber;
        baseTaskLength = OriginallyTaskLength;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGameRunning()) return;

        timer += Time.deltaTime;

        if (timer >= checkInterval)
        {
            // 计算玩家表现
            CalculatePerformance();
            // 更新难度
            UpdateDifficulty();
            // 重置统计
            ResetStats();
        }
    }

    #region 将配置好的列表中的成就先存入json

    public void SaveListAchievementToJson()
    {
        foreach (var item in achievementDatas)
        {
            AchievementData currentData = TaskDataManager.Instance.LoadAchievementData(item.AchievementName);
            if (currentData == null)
            {
            }
            TaskDataManager.Instance.SaveAchievementData(item.AchievementName, false);
        }

        achievementDatas=TaskDataManager.Instance.LoadAchievementToList();//初始化好的json再存回列表
    }

    #endregion

    private void CalculatePerformance()
    {
        // 计算躲避率
        evasionRate = asteroidEncountered > 0 ? 
            1f - ((float)asteroidHit / asteroidEncountered) : 0f;

        // 计算拾取率
        pickingRate = energySpawned > 0 ? 
            (float)energyCollected / energySpawned : 0f;
    }

    #region 成就判断
    private void CheckAchievement(string achievementName)
    {
        AchievementData data = TaskDataManager.Instance.LoadAchievementData(achievementName);
        if (data != null && !data.AchievementState)
        {
            UIController.Instance.ShowAchievementUI($"解锁成就：{achievementName}");
            TaskDataManager.Instance.SaveAchievementData(achievementName, true);
        }
    }

    // 检查通关成就
    public void CheckGameClearAchievement(bool taskSuccess)
    {
        if (taskSuccess)
        {
            CheckAchievement("首次通关");
        }
    }

    // 检查完美躲避成就
    private void CheckPerfectEvasionAchievement()
    {
        if (timer >= checkInterval && asteroidHit == 0 && asteroidEncountered > 0)
        {
            CheckAchievement("完美躲避");
        }
    }

    // 检查血量耗尽成就
    public void CheckHealthDepletedAchievement()
    {
        CheckAchievement("血量耗尽");
    }

    // 检查能量耗尽成就
    public void CheckEnergyDepletedAchievement()
    {
        CheckAchievement("能量耗尽");
    }

    // 检查任务额外加分成就
    public void CheckTaskBonusAchievement()
    {
        CheckAchievement("任务达人");
    }

    // 检查难度提升成就
    private void CheckDifficultyAchievement()
    {
        if (baseAsteroidNumber >= 60)
        {
            CheckAchievement("极限挑战");
        }
    }
    #endregion

    private void UpdateDifficulty()
    {
        // 根据躲避率调整小行星数量
        if (evasionRate >= 0.8f && asteroidHit<= maxHitCount)
        {
            baseAsteroidNumber += asteroidAdjustment;
        }
        else if (evasionRate < 0.8f || asteroidHit >= maxHitCount * 2)
        {
            baseAsteroidNumber = Mathf.Max(5, baseAsteroidNumber - asteroidAdjustment);//10以上
        }

        // 根据拾取率和任务完成情况调整任务长度
        if (taskRightOrWrong)
        {
            baseTaskLength += taskLengthAdjustment;
            baseTaskLength = Mathf.Clamp(baseTaskLength , 2 , 6);
        }
        else if (!taskRightOrWrong)
        {
            baseTaskLength = Mathf.Max(2, baseTaskLength - taskLengthAdjustment);
        }

        // 更新游戏系统
        UpdateGameSystems();

        // 检查相关成就
        CheckPerfectEvasionAchievement();
        CheckDifficultyAchievement();
    }

    private void UpdateGameSystems()
    {
        // 更新小行星生成器
        var asteroidBelts = AsteroidBeltPoolManager.Instance.GetAsteroidBelts();
        foreach (var belt in asteroidBelts)
        {
            belt.SetAsteroidCount(baseAsteroidNumber);
        }

        // 更新任务生成器
        EnergyGenerator.Instance.SetTaskLength(baseTaskLength);
    }

    private void ResetStats()
    {
        timer = 0f;
        asteroidEncountered = 0;
        asteroidHit = 0;
        energySpawned = 0;
        energyCollected = 0;
        taskRightOrWrong = false;
    }

    #region 公共接口
    public void OnAsteroidSpawned()
    {
        asteroidEncountered+= baseAsteroidNumber;
    }

    public void OnAsteroidHit()
    {
        asteroidHit++;
    }

    public void OnEnergySpawned()
    {
        energySpawned++;
    }

    public void OnEnergyCollected()
    {
        energyCollected++;
    }

    public void OnTaskCompleted(bool success)
    {
        taskRightOrWrong = success;
    }

    public void OnResetStatsValueSet()
    {
        baseAsteroidNumber = OriginallyAsteroidNumber;
        baseTaskLength = OriginallyTaskLength;
    }
    #endregion
}
