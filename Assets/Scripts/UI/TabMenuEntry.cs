using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TabMenuEntry : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI RoleText;
    public TextMeshProUGUI StatusText;
    [Tooltip("The background color of the row. Used in conjunction with player rolls.")]
    public Color BackgroundColor;
}
