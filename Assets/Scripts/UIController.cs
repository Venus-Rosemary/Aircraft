using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIController : Singleton<UIController>
{
    [Header("UIҳ��")]
    public GameObject StartUI;
    public GameObject GameUI;
    public GameObject EndUI;

    [Header("Button")]
    public Button StartGameBuotton;
    public Button RestartGameBuotton;
    public Button ReturnButton;
    public Button StartPanelExitButton;
    public Button EndPanelExitButton;

    [Header("UI����")]
    public TMP_Text timeText;
    public TMP_Text EndHintText;
    public TMP_Text TaskHintText;


    [Header("�ɾ�UI����")]
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
            timeText.text = string.Format("ʣ��ʱ�䣺{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void UpEndPanelHintUI(string hintText)//���½���������ʾ
    {
        EndHintText.text = $"{hintText}";
    }

    public void UpdateTaskUI(string taskTintText)//����������ʾ
    {
        TaskHintText.text = $"{taskTintText}";
    }

    public void SetGameEndUI()//��������
    {
        SetUIActiveState(false, false, true);
    }

    public void ShowAchievementUI(string achievement)
    {
        // ��ʼ��UI״̬
        HomingAchievementUI();
        AchievementUI.SetActive(true);
        AchievementUI.transform.position = OriginalPos.position;
        AchievementText.text = achievement;

        // ������������
        sequence = DOTween.Sequence();

        // ���벢�ƶ���Ŀ��λ��
        sequence.Append(AchievementUI.transform.DOMove(TargetPos.position, 2f))
                .AppendInterval(3f) // ��ʾͣ��ʱ��
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


    public void SetGameRunningUI()//�������
    {
        GameManager.Instance.StartGame();
        SetUIActiveState(false, true, false);
    }

    private void SetRestartButton()//���汾��
    {
        //����
        GameManager.Instance.RestartGame();
        SetGameRunningUI();
    }

    private void SetReturnButton()//�������˵�
    {
        SetUIActiveState(true, false, false);
    }



    public void QuitGame()//�˳���Ϸ
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
