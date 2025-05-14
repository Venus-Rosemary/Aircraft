using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    #region 变量定义

    [Header("移动设置")]
    [SerializeField] private float maxMoveSpeed = 10f;          // 最大移动速度
    [SerializeField] private float tiltAngle = 36f;            // 倾斜角度
    [SerializeField] private float rotationSpeed = 2f;         // 旋转速度
    [SerializeField] private float xBoundary = 20f;            // 水平移动边界

    [SerializeField] private float xtiltAngle = 30f;            //x轴倾斜角度
    [SerializeField] private float yBoundary = 10f;             //垂直移动边界

    [Header("加速设置")]
    [SerializeField] private float boostZOffset = 6f;          // 加速时相机偏移
    [SerializeField] private float normalTrackSpeed = 30f;      // 正常速度
    [SerializeField] private float boostTrackSpeed = 60f;      // 加速速度
    [SerializeField] private float speedTransitionTime = 0.5f; // 速度过渡时间
    [SerializeField] private float boostMoveSpeedMultiplier = 2f; // 加速倍率

    [Header("状态设置")]
    [SerializeField] private float maxHealth = 100f;           // 最大护盾值
    [SerializeField] private float maxEnergy = 100f;           // 最大能量值
    [SerializeField] private float energyDecreaseInterval = 1f;// 能量消耗间隔
    [SerializeField] private float energyDecreaseAmount = 1f;  // 能量消耗量
    [SerializeField] private float energyGainAmount = 5f;      // 能量获得量
    [SerializeField] private float asteroidDamage = 10f;       // 小行星伤害
    [SerializeField] private float healthGainAmount = 5f;     // 护盾恢复量

    [Header("UI设置")]
    public Slider ShieldUI;                                    // 护盾UI
    public Slider FlightUI;                                    // 飞行能量UI
    public TMP_Text ScoreUI;                                   // 分数UI
    public TMP_Text Hint;                                      // 提示文本
    public Transform cameraTarget;                             // 相机目标

    [Header("护盾设置")]
    [SerializeField] private float invincibleDuration = 10f;    // 无敌持续时间
    [SerializeField] private GameObject shieldEffect;           // 护盾特效对象
    private bool isInvincible = false;                         // 是否处于无敌状态
    private Coroutine invincibleCoroutine;                     // 无敌状态协程

    [Header("特效设置")]
    [SerializeField] private GameObject hitEffect;              //撞击陨石特效


    // 私有变量
    private float currentMaxMoveSpeed;                         // 当前最大速度
    private float currentTrackSpeed;                           // 当前轨道速度
    private Vector3 originalCameraPosition;                    // 相机初始位置
    private bool isBoosting;                                   // 是否在加速
    private InputSystemActions inputActions;                   // 输入系统
    private float targetRotation;                             // 目标旋转角度Z
    private float targetXRotation;                             // 目标旋转角度X
    private float currentVelocity;                            // 速度平滑值
    private float currentH;
    private float lastEnergyDecreaseTime;                     // 上次能量减少时间
    private bool hintblue;                                    // 蓝色提示标志
    private bool hintyellow;                                  // 黄色提示标志

    [SerializeField] private float currentHealth;             // 当前护盾值
    [SerializeField] private float currentEnergy;             // 当前能量值
    [SerializeField] private int currentScore;                // 当前分数
    [SerializeField] private int targetScore=10;                 // 目标分数

    #endregion

    private void Awake()
    {
        inputActions=new InputSystemActions();
    }
    void Start()
    {
        InitializeTheGameToStart();
    }


    #region 初始化
    private void InitializeTheGameToStart()//start中的初始化
    {
        if (cameraTarget != null)
            originalCameraPosition = cameraTarget.localPosition;


        currentMaxMoveSpeed = maxMoveSpeed;
        currentTrackSpeed = normalTrackSpeed;

        InitializePlayer();
    }
    public void InitializePlayer()//正常初始化
    {

        hitEffect.SetActive(false);
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        currentScore = 0;
        ScoreUI.text = $"当前任务物品：{currentScore}/{targetScore}";
        Hint.text = "";
        hintblue = false;
        hintyellow = false;
        shieldEffect.SetActive(false);
        isInvincible = false;
    }
    #endregion


    void Update()
    {
        Movement();
        MovementY();
        HandleBoost();
        HandleEnergyDecrease();

        ShieldUI.value = currentHealth/ maxHealth;
        FlightUI.value = currentEnergy/ maxEnergy;

        if (currentHealth <= maxHealth * 0.3f && hintblue)
        {
            Hint.text = $"护盾不足，请优先收集蓝色能量块";
            Color blueColor;
            if (ColorUtility.TryParseHtmlString("#00FFFF", out blueColor))
            {
                Hint.color = blueColor; // 设置文本颜色
            }
            hintblue = false;
        }
        else if (currentHealth > maxHealth * 0.3f && !hintblue)
        {
            Hint.text = "";
            hintblue = true;
        }

        if (currentEnergy <= maxEnergy * 0.3f && hintyellow)
        {
            Hint.text = $"能源不足，请优先收集黄色能量块";
            Color yellowColor;
            if (ColorUtility.TryParseHtmlString("#FFFF00", out yellowColor))
            {
                Hint.color = yellowColor; // 设置文本颜色
            }
            hintyellow = false;
        }
        else if (currentEnergy > maxEnergy * 0.3f && !hintyellow)
        {
            Hint.text = "";
            hintyellow = true;
        }

    }


    private void OnEnable()
    {
        inputActions.PC.Enable();
    }

    private void OnDisable()
    {
        inputActions.PC.Disable();
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
        }
    }

    #region 加速系统
    private void HandleBoost()//加速
    {
        // 检测加速输入
        bool boostInput = inputActions.PC.Move.ReadValue<Vector2>().y > 0;

        if (boostInput && !isBoosting)
        {
            // 开始加速
            isBoosting = true;
            if (cameraTarget != null)
            {
                cameraTarget.DOLocalMoveZ(originalCameraPosition.z - boostZOffset, speedTransitionTime);
            }

            // 轨道平滑过渡到加速速度
            DOTween.To(() => currentTrackSpeed, x => {
                currentTrackSpeed = x;
                MapGenerator.Instance.trackSpeed = x;
            }, boostTrackSpeed, speedTransitionTime);

            // 左右平滑过渡到加速移动速度
            DOTween.To(() => currentMaxMoveSpeed, x => currentMaxMoveSpeed = x,
                maxMoveSpeed * boostMoveSpeedMultiplier, speedTransitionTime);

            EnergyGenerator.Instance.SetMoveSpeed(boostTrackSpeed);//设置物品速度
            AsteroidBeltPoolManager.Instance.SetAllAsteroidBelMoveSpeed(boostTrackSpeed);//设置小行星带速度
            Camera.main.DOFieldOfView(70f, speedTransitionTime);
        }
        else if (!boostInput && isBoosting)
        {
            // 结束加速
            isBoosting = false;
            if (cameraTarget != null)
            {
                cameraTarget.DOLocalMoveZ(originalCameraPosition.z, speedTransitionTime);
            }

            // 平滑过渡回正常速度
            DOTween.To(() => currentTrackSpeed, x => {
                currentTrackSpeed = x;
                MapGenerator.Instance.trackSpeed = x;
            }, normalTrackSpeed, speedTransitionTime);

            // 平滑过渡回正常移动速度
            DOTween.To(() => currentMaxMoveSpeed, x => currentMaxMoveSpeed = x,
                maxMoveSpeed, speedTransitionTime);

            EnergyGenerator.Instance.SetMoveSpeed(normalTrackSpeed);
            AsteroidBeltPoolManager.Instance.SetAllAsteroidBelMoveSpeed(normalTrackSpeed);

            Camera.main.DOFieldOfView(60f, speedTransitionTime);
        }
    }
    #endregion

    #region 移动系统
    private void Movement()//玩家左右移动
    {
        // 获取输入
        Vector2 moveVector2 = inputActions.PC.Move.ReadValue<Vector2>();
        float moveInput = moveVector2.x;

        bool upInput = inputActions.PC.MoveUp.IsPressed();
        bool downInpur = inputActions.PC.MoveDown.IsPressed();

        // 根据输入设置目标旋转角度
        if (moveInput < 0)
        {
            targetRotation = tiltAngle;
            //if (upInput)
            //    targetXRotation = xtiltAngle;
            //else if (downInpur)
            //    targetXRotation = -xtiltAngle;
            //else
            //    targetXRotation = 0;
        }
        else if (moveInput > 0)
        {
            targetRotation = -tiltAngle;
            //if (upInput)
            //    targetXRotation = xtiltAngle;
            //else if (downInpur)
            //    targetXRotation = -xtiltAngle;
            //else
            //    targetXRotation = 0;
        }
        else
        {
            targetRotation = 0;
            //if (upInput)
            //    targetXRotation = xtiltAngle;
            //else if (downInpur)
            //    targetXRotation = -xtiltAngle;
            //else
            //    targetXRotation = 0;
        }

        // 平滑旋转
        float currentRotation = transform.rotation.eulerAngles.z;
        if (currentRotation > 180) currentRotation -= 360;

        // 获取当前X轴旋转
        float currentXRotation = transform.rotation.eulerAngles.x;
        if (currentXRotation > 180) currentXRotation -= 360;

        float newRotation = Mathf.SmoothDamp(currentRotation, targetRotation, ref currentVelocity, 1f / rotationSpeed);//z轴不断转向targetRotation

        //float newXRotation = Mathf.SmoothDamp(currentXRotation, targetXRotation, ref currentH, 1f / rotationSpeed);//x轴不断转向targetXRotation
        transform.rotation = Quaternion.Euler(currentXRotation, 0, newRotation);

        // 根据当前旋转角度计算速度
        float currentSpeedMultiplier = Mathf.Abs(newRotation) / tiltAngle;//速度系数
        float currentMoveSpeed = currentMaxMoveSpeed * currentSpeedMultiplier;//当前速度

        // 根据当前角度决定移动方向
        float moveDirection = newRotation > 0 ? -1 : 1;
        
        // 应用移动（即使没有输入，只要有倾斜角度就会移动）
        if (Mathf.Abs(newRotation) > 0.1f)
        {
            Vector3 movement = new Vector3(moveDirection, 0f, 0f);
            transform.position += movement * Time.deltaTime * currentMoveSpeed;
        }

        // 确保在边界内
        float clampedX = Mathf.Clamp(transform.position.x, -xBoundary, xBoundary);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private void MovementY()//垂直方向的移动
    {

        bool upInput = inputActions.PC.MoveUp.IsPressed();
        bool downInput = inputActions.PC.MoveDown.IsPressed();

        if (transform.position.y <= -yBoundary + 12f)
        {
            downInput = false;
        }
        if (transform.position.y >= yBoundary + 9f)
        {
            upInput = false;
        }

        // 根据输入设置X轴目标旋转角度
        if (upInput)
            targetXRotation = -xtiltAngle;
        else if (downInput)
            targetXRotation = xtiltAngle;
        else
            targetXRotation = 0;

        // 平滑旋转X轴
        float currentXRotation = transform.rotation.eulerAngles.x;
        if (currentXRotation > 180) currentXRotation -= 360;

        float newXRotation = Mathf.SmoothDamp(currentXRotation, targetXRotation, ref currentH, 1f / rotationSpeed);

        // 获取当前Z轴旋转
        float currentZRotation = transform.rotation.eulerAngles.z;
        if (currentZRotation > 180) currentZRotation -= 360;

        // 分别应用X轴和Z轴的旋转
        transform.rotation = Quaternion.Euler(newXRotation, 0, currentZRotation);

        // 计算垂直移动速度
        float verticalSpeedMultiplier = Mathf.Abs(newXRotation) / xtiltAngle;
        float currentVerticalSpeed = currentMaxMoveSpeed * verticalSpeedMultiplier;

        // 根据倾斜方向决定移动方向
        float moveDirectionY = newXRotation > 0 ? -1 : 1;

        // 应用垂直移动
        if (Mathf.Abs(newXRotation) > 0.1f)
        {
            Vector3 movement = new Vector3(0f, moveDirectionY, 0f);
            transform.position += movement * Time.deltaTime * currentVerticalSpeed;
        }

        // 确保在垂直边界内，并在到达边界时回正
        float clampedY = Mathf.Clamp(transform.position.y, -yBoundary+11f, yBoundary*2);
        if (transform.position.y <= -yBoundary+12f|| transform.position.y >= yBoundary+9f)
        {
            targetXRotation = 0;
        }
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
    }

    #endregion

    private void HandleEnergyDecrease()//能量消耗
    {
        if (Time.time - lastEnergyDecreaseTime >= energyDecreaseInterval)
        {
            DecreaseEnergy(energyDecreaseAmount);
            lastEnergyDecreaseTime = Time.time;
        }
    }

    public void TakeDamage(float damage)//受到伤害
    {
        if (isInvincible) return;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth <= 0)
        {
            // 游戏结束逻辑
            GameManager.Instance.GameOver();

            UIController.Instance.UpEndPanelHintUI("粉身碎骨，你变成了太空垃圾");
            Debug.Log("粉身碎骨，你变成了太空垃圾");


            DifficultyController.Instance.CheckHealthDepletedAchievement();
        }
    }

    // 护盾激活方法
    private IEnumerator ActivateInvincible()
    {
        isInvincible = true;
        if (shieldEffect != null)
        {
            shieldEffect.SetActive(true);
        }

        yield return new WaitForSeconds(invincibleDuration);

        isInvincible = false;
        if (shieldEffect != null)
        {
            shieldEffect.SetActive(false);
        }
    }

    public void GainHealth(float damage)//增加血量
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + damage);
    }

    public void GainEnergy(float amount)//增加能量
    {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
    }

    public void AddScore(int score)//加分
    {
        currentScore += score;

        ScoreUI.text = $"当前任务物品：{currentScore}/{targetScore}";
    }

    private void DecreaseEnergy(float amount)//能量耗尽
    {
        currentEnergy = Mathf.Max(0, currentEnergy - amount);
        if (currentEnergy <= 0)
        {
            // 能量耗尽逻辑
            GameManager.Instance.GameOver();

            UIController.Instance.UpEndPanelHintUI("能量耗尽，你变成了太空垃圾");
            Debug.Log("能量耗尽，你变成了太空垃圾");

            DifficultyController.Instance.CheckEnergyDepletedAchievement();
        }
    }

    private void OnTriggerEnter(Collider other)
    {


        switch (other.tag)
        {
            case "Asteroid":

                hitEffect.SetActive(false);
                hitEffect.transform.position=other.transform.position;
                hitEffect.SetActive(true);

                TakeDamage(asteroidDamage);

                DifficultyController.Instance.OnAsteroidHit();

                EnergyGenerator.Instance.RemoveFromList(other.gameObject);
                Destroy(other.gameObject);
                break;
            case "Energy":
                GainEnergy(energyGainAmount);

                ItemType collectedType = EnergyGenerator.Instance.GetItemTypeFromGameObject(other.gameObject);
                EnergyGenerator.Instance.CheckCollection(collectedType);                // 检查收集顺序

                DifficultyController.Instance.OnEnergyCollected();

                EnergyGenerator.Instance.RemoveFromList(other.gameObject);
                Destroy(other.gameObject);
                break;
            case "Health":
                GainHealth(healthGainAmount);

                collectedType = EnergyGenerator.Instance.GetItemTypeFromGameObject(other.gameObject);
                EnergyGenerator.Instance.CheckCollection(collectedType);

                DifficultyController.Instance.OnEnergyCollected();

                EnergyGenerator.Instance.RemoveFromList(other.gameObject);
                Destroy(other.gameObject);
                break;
            case "Score":
                AddScore(1);

                collectedType = EnergyGenerator.Instance.GetItemTypeFromGameObject(other.gameObject);
                EnergyGenerator.Instance.CheckCollection(collectedType);

                DifficultyController.Instance.OnEnergyCollected();

                EnergyGenerator.Instance.RemoveFromList(other.gameObject);
                Destroy(other.gameObject);
                break;
            case "Shield":
                if (invincibleCoroutine != null)
                {
                    StopCoroutine(invincibleCoroutine);
                }
                invincibleCoroutine = StartCoroutine(ActivateInvincible());
                EnergyGenerator.Instance.RemoveFromList(other.gameObject);
                Destroy(other.gameObject);
                break;
        }

    }



    #region 外部获取数值
    public Vector3 GetOriginalCameraPosition()
    {
        return originalCameraPosition;
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    public int GetTargetScore()
    {
        return targetScore;
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 绘制移动边界
        Gizmos.color = Color.blue;
        Vector3 center = new Vector3(0, transform.position.y, transform.position.z);
        Vector3 size = new Vector3(xBoundary * 2, 0.1f, 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
