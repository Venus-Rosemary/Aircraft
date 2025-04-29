using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

[Serializable]
public class AchievementData
{
    public string AchievementName;
    public bool AchievementState;

    public AchievementData(string name, bool state)
    {
        AchievementName = name;
        AchievementState = state;
    }
}

[Serializable]
public class TaskDataCollection
{
    public List<AchievementData> tasks = new List<AchievementData>();
}

public class TaskDataManager : Singleton<TaskDataManager>
{
    private string SavePath => Path.Combine(Application.persistentDataPath, "taskData.json");
    private TaskDataCollection allTaskData;

    private void LoadAllData()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            allTaskData = JsonUtility.FromJson<TaskDataCollection>(json);
        }
        
        if (allTaskData == null)
        {
            allTaskData = new TaskDataCollection();
        }
    }

    public void SaveAchievementData(string name, bool state)
    {
        if (allTaskData == null) LoadAllData();

        AchievementData currentData = allTaskData.tasks.Find(t => t.AchievementName == name);
        bool shouldUpdate = false;

        if (currentData == null)
        {
            currentData = new AchievementData(name, state);
            allTaskData.tasks.Add(currentData);
            shouldUpdate = true;
        }
        else
        {
            currentData.AchievementName = name;
            currentData.AchievementState = state;
            shouldUpdate = true; 
        }

        if (shouldUpdate)
        {
            string json = JsonUtility.ToJson(allTaskData, true);
            Debug.Log("Persistent Data Path: " + Application.persistentDataPath);
            File.WriteAllText(SavePath, json);
            Debug.Log($"所有任务数据已保存: {json}");
        }
    }

    public AchievementData LoadAchievementData(string name)//根据name查找AchievementData
    {
        if (allTaskData == null) LoadAllData();
        return allTaskData.tasks.Find(t => t.AchievementName == name);
    }

    public List<AchievementData> GetAllAchievementData()
    {
        if (allTaskData == null) LoadAllData();
        return allTaskData.tasks;
    }

    //读取json，并存入列表
    public List<AchievementData> LoadAchievementToList()
    {
        if (allTaskData == null) LoadAllData();

        List<AchievementData> dataList = new List<AchievementData>();

        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            TaskDataCollection loadedData = JsonUtility.FromJson<TaskDataCollection>(json);

            if (loadedData != null && loadedData.tasks != null)
            {
                foreach (var task in loadedData.tasks)
                {
                    dataList.Add(new AchievementData(
                        task.AchievementName,
                        task.AchievementState
                    ));
                }
            }
        }

        return dataList;
    }
}