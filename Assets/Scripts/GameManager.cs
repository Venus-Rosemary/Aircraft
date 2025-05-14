using DG.Tweening;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    #region 字段和属性
    [Header("游戏核心设置")]
    public Transform player;                                  // 玩家对象
    public GameObject SpeedLine;                                //速度线对象
    public Vector3 playerStartPosition = new Vector3(0, 5, 0);// 玩家初始位置
    public float gameSpeed = 30f;                            // 游戏基础速度
    public float startBeltOffset = 200f;                     // 小行星带初始偏移量

    [Header("游戏时间设置")]
    public float gameDuration = 300f;                        // 游戏总持续时间
    private float currentGameTime;                           // 当前游戏时间
    private float[] taskTriggerTimes = { 30f, 90f, 150f, 210f, 270f }; // 任务触发时间点
    private int currentTaskIndex = 0;                        // 当前任务索引

    // 组件引用
    private bool isGameRunning = false;
    private PlayerControl playerControl;
    private EnergyGenerator itemGenerator;

    private bool isTaskCompleted = false;                   // 任务完成标志(看任务是否做过，做过为true)
    #endregion

    #region 生命周期方法
    private void Start()
    {
        InitializeComponents();
        SetInitialGameState();
    }
    private void Update()
    {
        if (!isGameRunning) return;

        UpdateGameTime();
        CheckTaskTriggers();
        CheckGameEnd();
    }
    #endregion

    #region 初始化方法
    private void InitializeComponents()
    {
        playerControl = player.GetComponent<PlayerControl>();
        itemGenerator = EnergyGenerator.Instance;
    }

    private void SetInitialGameState()
    {
        player.gameObject.SetActive(false);
        SpeedLine.gameObject.SetActive(false);
        itemGenerator.SetStartSpawn(false);
    }
    #endregion

    #region 辅助方法
    private void UpdateGameTime()
    {
        currentGameTime -= Time.deltaTime;
        UIController.Instance.UpdateTimeUI(currentGameTime);
    }
    private void CheckTaskTriggers()
    {
        if (currentTaskIndex < taskTriggerTimes.Length &&
            gameDuration - currentGameTime >= taskTriggerTimes[currentTaskIndex])
        {
            TriggerNewTask();
        }
        if (currentTaskIndex!=0 && !isTaskCompleted)
        {
            if (gameDuration - currentGameTime >= taskTriggerTimes[currentTaskIndex-1] + 29f)
            {
                Debug.Log("超时");
                itemGenerator.ResetTask();
                UIController.Instance.UpdateTaskUI($"任务已超时");

                isTaskCompleted = true;
                DOVirtual.DelayedCall(3F, () => UIController.Instance.UpdateTaskUI(""));
            }
        }
    }

    public void SetTaskCompletedState(bool state)
    {
        isTaskCompleted=state;
    }

    private void TriggerNewTask()
    {
        isTaskCompleted = false;
        string taskSequence = itemGenerator.GenerateNewTask();
        UIController.Instance.UpdateTaskUI($"新任务：{taskSequence}\n  时间：30秒");
        currentTaskIndex++;
    }
    private void CheckGameEnd()
    {
        if (currentGameTime <= 0)
        {
            EndAchievementJudgment();
            GameOver();
            ShowEndGameMessage();
        }
    }

    private void ShowEndGameMessage()
    {
        if (playerControl.GetCurrentScore() <= playerControl.GetTargetScore())
        {
            UIController.Instance.UpEndPanelHintUI("任务失败，任务物品收集不足");
        }
        else
        {
            UIController.Instance.UpEndPanelHintUI("任务成功，恭喜你");
        }
    }

    public bool IsGameRunning()
    {
        return isGameRunning;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    #endregion

    #region 游戏流程控制
    private void InitializeGameState()
    {
        isGameRunning = true;
        currentGameTime = gameDuration;
        currentTaskIndex = 0;
        UIController.Instance.UpdateTaskUI("");
        DifficultyController.Instance.OnResetStatsValueSet();

    }

    private void SetupPlayer()
    {
        player.gameObject.SetActive(true);
        player.position = playerStartPosition;
        player.rotation = Quaternion.Euler(0, 0, 0);
        playerControl.InitializePlayer();


        SpeedLine.gameObject.SetActive(true);
    }

    private void SetupAsteroidBelts()
    {
        var asteroidManager = AsteroidBeltPoolManager.Instance;
        foreach (var belt in asteroidManager.GetAsteroidBelts())
        {
            belt.gameObject.SetActive(true);

            belt.maxAsteroids = 20;

            belt.RegenerateAsteroids();
            belt.SetMoveSpeed(gameSpeed);
            belt.transform.position = new Vector3(
                belt.transform.position.x,
                belt.transform.position.y,
                belt.transform.position.z + startBeltOffset
            );
        }
    }

    private void SetupItemGenerator()
    {
        itemGenerator.SetTaskLength(3);
        itemGenerator.SetStartSpawn(true);
        itemGenerator.SetMoveSpeed(gameSpeed);
        itemGenerator.ResetTaskSystem();
    }

    private void EndGameState()
    {
        isGameRunning = false;
        MapGenerator.Instance.trackSpeed = gameSpeed;
    }

    private void CleanupGameObjects()
    {
        // 关闭玩家
        player.gameObject.SetActive(false);

        SpeedLine.gameObject.SetActive(false);
        // 重置相机
        if (playerControl.cameraTarget != null)
        {
            playerControl.cameraTarget.DOLocalMoveZ(playerControl.GetOriginalCameraPosition().z, 0.5f);
        }
        Camera.main.DOFieldOfView(60f, 0.5f);

        // 重置小行星带
        var asteroidManager = AsteroidBeltPoolManager.Instance;
        foreach (var belt in asteroidManager.GetAsteroidBelts())
        {
            belt.SetMoveSpeed(gameSpeed);
        }

        // 清理道具生成器
        itemGenerator.SetStartSpawn(false);
        itemGenerator.ClearAllItems();
        itemGenerator.ResetTask();
    }

    private void ShowGameResults()
    {
        UIController.Instance.SetGameEndUI();           //游戏结束UI面板
    }

    private void EndAchievementJudgment()
    {
        // 检查通关成就
        DifficultyController.Instance.CheckGameClearAchievement(playerControl.GetCurrentScore() >= playerControl.GetTargetScore());
    }

    public void StartGame()
    {
        if (isGameRunning) return;

        InitializeGameState();  //初始化游戏状态
        SetupPlayer();          //设置玩家
        SetupAsteroidBelts();   //设置小行星带
        SetupItemGenerator();   //设置能量块
    }

    public void GameOver()
    {
        if (!isGameRunning) return;

        EndGameState();         //结束游戏状态
        CleanupGameObjects();   //结束清理和重置
        ShowGameResults();      //展示游戏结果
    }

    public void RestartGame()
    {
        GameOver();
        StartGame();
    }
    #endregion
}