using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIController : Singleton<UIController>
{
    [Header("UI页面")]
    public GameObject StartUI;
    public GameObject GameUI;
    public GameObject EndUI;

    [Header("Button")]
    public Button StartGameBuotton;
    public Button RestartGameBuotton;
    public Button ReturnButton;
    public Button StartPanelExitButton;
    public Button EndPanelExitButton;

    [Header("UI引用")]
    public TMP_Text timeText;
    public TMP_Text EndHintText;
    public TMP_Text TaskHintText;


    [Header("成就UI设置")]
    public GameObject AchievementUI;
    public TMP_Text AchievementText;
    public Transform OriginalPos;
    public Transform TargetPos;
    private Sequence sequence;


    void Start()
    {
        SetUIActiveState(true, false, false);
        StartGameBuotton.onClick.AddListener(SetGameRunningUI);
        RestartGameBuotton.onClick.AddListener(SetRestartButton);
        ReturnButton.onClick.AddListener(SetReturnButton);
        StartPanelExitButton.onClick.AddListener(QuitGame);
        EndPanelExitButton.onClick.AddListener(QuitGame);

        TaskHintText.text = "";
    }

    
    void Update()
    {
        
    }

    public void UpdateTimeUI(float currentTime)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timeText.text = string.Format("剩余时间：{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void UpEndPanelHintUI(string hintText)//更新结束界面提示
    {
        EndHintText.text = $"{hintText}";
    }

    public void UpdateTaskUI(string taskTintText)//更新任务提示
    {
        TaskHintText.text = $"{taskTintText}";
    }

    public void SetGameEndUI()//结束界面
    {
        SetUIActiveState(false, false, true);
    }

    public void ShowAchievementUI(string achievement)
    {
        // 初始化UI状态
        HomingAchievementUI();
        AchievementUI.SetActive(true);
        AchievementUI.transform.position = OriginalPos.position;
        AchievementText.text = achievement;

        // 创建动画序列
        sequence = DOTween.Sequence();

        // 淡入并移动到目标位置
        sequence.Append(AchievementUI.transform.DOMove(TargetPos.position, 2f))
                .AppendInterval(3f) // 显示停留时间
                .Append(AchievementUI.transform.DOMove(OriginalPos.position, 2f))
                .OnComplete(() => AchievementUI.SetActive(false));
    }

    private void HomingAchievementUI()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }
    }

    private void SetUIActiveState(bool startUI,bool gameUI,bool endUI)
    {
        StartUI.SetActive(startUI);
        GameUI.SetActive(gameUI);
        EndUI.SetActive(endUI);
    }


    public void SetGameRunningUI()//游玩界面
    {
        GameManager.Instance.StartGame();
        SetUIActiveState(false, true, false);
    }

    private void SetRestartButton()//重玩本关
    {
        //重玩
        GameManager.Instance.RestartGame();
        SetGameRunningUI();
    }

    private void SetReturnButton()//返回主菜单
    {
        SetUIActiveState(true, false, false);
    }



    public void QuitGame()//退出游戏
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
