using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerIdentificationUI : MonoBehaviour
{
    public TextMeshProUGUI PlayerNameText;
    public TextMeshProUGUI PlayerRoleText;
    public TextMeshProUGUI PlayerCauseOfDeathText;

    public Outline PlayerModelOutline;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisplayUI(Player target)
    {
        PlayerNameText.text = target.Nickname;
        PlayerRoleText.text = target.Role.ToString();

        if (NetworkGameManager.IsImposterRole(target.Role.ToString()))
            PlayerRoleText.color = Color.red;
        else
            PlayerRoleText.color = Color.white;

        PlayerModelOutline.OutlineColor = target.PlayerColor;

        this.enabled = true;
    }

    public void OnCloseClicked()
    {
        enabled = false;
    }
}
