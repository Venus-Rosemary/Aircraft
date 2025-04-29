using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : Singleton<ObstacleGenerator>
{
    [Header("生成物设置")]
    public GameObject asteroidPrefab;     // 小行星预制体
    public GameObject energyPrefab;       // 能量块预制体
    public float spawnHeight = 5f;        // 生成高度
    public float spawnInterval = 2f;      // 生成间隔时间
    public float spawnWidth = 5f;         // 生成区域宽度
    public float moveSpeed;               // 移动速度
    public float destroyDistance = 20f;   // 销毁距离

    private float lastSpawnTime;
    [SerializeField] private List<GameObject> allSpawnedObject=new List<GameObject>();//存储所有生成的未被销毁的预制体

    void Update()
    {
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            SpawnObstacleOrCollectible();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnObstacleOrCollectible()
    {
        GameObject prefabToSpawn = Random.value > 0.5f ? asteroidPrefab : energyPrefab;

        float randomX = Random.Range(-spawnWidth, spawnWidth);
        float randomZ = transform.position.z;

        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, transform);

        // 设置标签
        spawnedObject.tag = prefabToSpawn == asteroidPrefab ? "Asteroid" : "Energy";
        spawnedObject.AddComponent<ObstacleMovement>().speed = moveSpeed;

        allSpawnedObject.Add(spawnedObject);
    }

    public void SetClearList(GameObject gameObject)
    {
        if (allSpawnedObject.Contains(gameObject))
        {
            allSpawnedObject.Remove(gameObject);
        }
    }

    public void SetListAllMoveSpeed(float mSpeed)
    {
        moveSpeed=mSpeed;
        foreach (var item in allSpawnedObject)
        {
            item.GetComponent<ObstacleMovement>().speed = mSpeed;
        }
    }

    private void OnDestroy()
    {

    }
}