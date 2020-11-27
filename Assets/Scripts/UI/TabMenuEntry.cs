using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabMenuEntry : MonoBehaviour
{
    public static Color32 SheriffColor = new Color32(85, 149, 255, 255);
    public static Color32 ImposterColor = new Color32(255, 117, 86, 255);
    public static Color32 CrewmateColor = new Color32(157, 157, 157, 255);

    public TextMeshProUGUI NameText;
    public TextMeshProUGUI RoleText;
    public TextMeshProUGUI StatusText;

    public Color BackgroundColor
    {
        set
        {
            GetComponent<Image>().color = value;
        }
    }
}
