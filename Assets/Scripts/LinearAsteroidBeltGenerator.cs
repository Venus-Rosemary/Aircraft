using UnityEngine;
using System.Collections.Generic;

public class LinearAsteroidBeltGenerator : MonoBehaviour
{
    [Header("预制体")]
    public GameObject[] asteroidPrefabs;//小行星带中小行星预制体

    [Header("生成范围")]
    public float length = 100f;
    public float width = 20f;
    public float height = 10f;

    [Header("生成设置")]
    public int maxAsteroids = 20;                          //小行星带中小行星个数
    public Vector2 minMaxScale = new Vector2(0.5f, 2.5f);   //尺寸缩放范围

    public bool enableRandomSpin = true;                    //随机旋转

    [Tooltip("小行星间隔最小距离")] 
    public float minSpacing = 2f;

    [Tooltip("生成尝试次数")] 
    public int maxAttemptsPerAsteroid = 30;

    private List<Vector3> occupiedPositions = new List<Vector3>();
    private List<float> occupiedRadii = new List<float>();

    [Header("移动设置")]
    public float moveSpeed = 5f;            //小行星带移动速度
    public float destroyDistance = 20f;     //超出摄像机多远销毁

    private List<GameObject> activeAsteroids = new List<GameObject>();
    private bool isActive = true;

    void Start()
    {
        GenerateAsteroidsWithSpacing();
    }

    void Update()
    {
        if (!isActive) return;

        // 移动小行星带
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // 检查并销毁超出范围的小行星
        CheckAndDestroyAsteroids();
    }

    void CheckAndDestroyAsteroids()// 检查并销毁超出范围的小行星
    {
        for (int i = activeAsteroids.Count - 1; i >= 0; i--)
        {
            if (activeAsteroids[i] == null) 
            {
                activeAsteroids.RemoveAt(i);
                continue;
            }

            if (activeAsteroids[i].transform.position.z < Camera.main.transform.position.z - destroyDistance)
            {
                Destroy(activeAsteroids[i]);
                activeAsteroids.RemoveAt(i);
            }
        }

        // 检查是否所有小行星都已销毁
        if (activeAsteroids.Count == 0 && isActive)
        {
            DeactivateAsteroidBelt();
        }
    }

    void DeactivateAsteroidBelt()//停止移动，关闭小行星带
    {
        isActive = false;
        gameObject.SetActive(false);
    }

    public void SetAsteroidCount(int count)
    {
        maxAsteroids = count;
    }

    void GenerateAsteroidsWithSpacing()//小行星带中小行星生成
    {
        int successfullyPlaced = 0;
        int attempts = 0;

        while (successfullyPlaced < maxAsteroids && attempts < maxAsteroids * maxAttemptsPerAsteroid)
        {
            // 生成候选位置
            Vector3 candidatePos = transform.TransformPoint(new Vector3(
                Random.Range(-length/2, length/2),
                Random.Range(-height/2, height/2),
                Random.Range(-width/2, width/2)
            ));

            // 计算候选小行星尺寸
            float candidateScale = Random.Range(minMaxScale.x, minMaxScale.y);
            float candidateRadius = candidateScale * 0.5f; // 假设预制体原始半径为0.5单位

            // 碰撞检测
            bool isValidPosition = true;
            for (int i = 0; i < occupiedPositions.Count; i++)
            {
                float requiredDistance = candidateRadius + occupiedRadii[i] + minSpacing;
                if (Vector3.Distance(candidatePos, occupiedPositions[i]) < requiredDistance)
                {
                    isValidPosition = false;
                    break;
                }
            }

            // 通过检测则生成
            if (isValidPosition)
            {
                GameObject prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
                GameObject asteroid = Instantiate(prefab, candidatePos, Random.rotation, transform);
                asteroid.transform.localScale = Vector3.one * candidateScale;

                asteroid.tag = "Asteroid";

                // 旋转选中
                if (enableRandomSpin) asteroid.AddComponent<RotateSelf>();

                // 记录占位信息
                occupiedPositions.Add(candidatePos);
                occupiedRadii.Add(candidateRadius);
                activeAsteroids.Add(asteroid); // 添加到活动小行星列表
                successfullyPlaced++;
            }

            attempts++;
        }

        DifficultyController.Instance.OnAsteroidSpawned();
        //Debug.Log($"成功生成 {successfullyPlaced} 个小行星，尝试次数 {attempts}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()//绘制小行星带范围
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(length, height, width));
    }
#endif

    public void SetMoveSpeed(float speed)//外部设置小行星带速度
    {
        moveSpeed = speed;
    }

    public void RegenerateAsteroids()//重置
    {
        // 清理现有小行星
        foreach (var asteroid in activeAsteroids)
        {
            if (asteroid != null)
            {
                Destroy(asteroid);
            }
        }
        
        activeAsteroids.Clear();
        occupiedPositions.Clear();
        occupiedRadii.Clear();
        
        // 重置状态
        isActive = true;
        
        // 重新生成小行星
        GenerateAsteroidsWithSpacing();
    }
}