using UnityEngine;
using System.Collections.Generic;

public class AsteroidBeltPoolManager : Singleton<AsteroidBeltPoolManager>
{
    [Header("生成设置")]
    public float beltSpacing = 100f;      // 小行星带之间的间距
    public float resetPositionOffset = 200f;  // 重置位置的偏移量

    private List<LinearAsteroidBeltGenerator> asteroidBelts;
    private float initialZPosition;

    void Start()
    {
        // 获取所有子物体的生成器组件
        asteroidBelts = new List<LinearAsteroidBeltGenerator>();
        foreach (Transform child in transform)
        {
            var generator = child.GetComponent<LinearAsteroidBeltGenerator>();
            if (generator != null)
            {
                asteroidBelts.Add(generator);
            }
        }

        initialZPosition = transform.position.z;
    }

    void Update()
    {
        CheckAndResetBelts();
    }

    void CheckAndResetBelts()//检查和复位小行星带
    {
        foreach (var belt in asteroidBelts)
        {
            if (!belt.gameObject.activeSelf)
            {
                ResetBelt(belt);
            }
        }
    }

    void ResetBelt(LinearAsteroidBeltGenerator belt)//将小行星带中没有小行星的小行星带放回前方
    {
        // 找到最前面的小行星带
        float maxZ = float.MinValue;
        foreach (var b in asteroidBelts)
        {
            if (b.gameObject.activeSelf && b.transform.position.z > maxZ)
            {
                maxZ = b.transform.position.z;
            }
        }

        // 将失活的小行星带放到最前面
        Vector3 newPosition = belt.transform.position;
        newPosition.z = maxZ + beltSpacing;
        belt.transform.position = newPosition;

        // 重新激活并生成新的小行星
        belt.gameObject.SetActive(true);
        belt.RegenerateAsteroids();
    }

    public void SetAllAsteroidBelMoveSpeed(float speed)//设置小行星带的速度
    {
        foreach(var bel in asteroidBelts)
        {
            bel.SetMoveSpeed(speed); 
        }
    }

    public List<LinearAsteroidBeltGenerator> GetAsteroidBelts()//外部获取小行星带列表
    {
        return asteroidBelts;
    }
}