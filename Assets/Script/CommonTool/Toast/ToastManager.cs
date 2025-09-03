using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToastManager : MonoSingleton<ToastManager>
{
    public void ShowToast(string info)
    {
        UIManager.GetInstance().ShowUIForms(nameof(Toast), info);
    }
}
