using UnityEngine;
using UnityEngine.UI;

/// <summary> 屏蔽界面 阻止玩家操作 退出游戏 </summary>
public class BlockPanel : BaseUIForms
{
    public Text InfoText;
    public Button QuitBtn;

    private void Start()
    {
        QuitBtn.onClick.AddListener(Application.Quit);
    }

    public void ShowInfo(string info)
    {
        InfoText.text = info;
    }
}
