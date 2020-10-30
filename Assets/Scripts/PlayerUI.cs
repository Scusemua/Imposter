using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
   [SerializeField]
   Text nicknameText;

   [SerializeField]
   Text roleText;

   private Player player;
   private PlayerController playerController;

   void Alive()
   {

   }

   // Start is called before the first frame update
   void Start()
   {
      if (nicknameText == null)
      {
         AssignTextComponents();
      }
   }

   // Update is called once per frame
   void Update()
   {
        
   }

   public void SetPlayer(Player player)
   {
      this.player = player;
      this.playerController = player.GetComponent<PlayerController>();

      SetNickname(player.Nickname);
   }

   void AssignTextComponents()
   {
      Text[] textComponents = GetComponentsInChildren<Text>();
      nicknameText = textComponents[0];
      roleText = textComponents[1];
   }

    public void SetNickname(string nickname)
    {
        if (nicknameText == null)
        {
            AssignTextComponents();
        }
        nicknameText.text = nickname;
    }

    public void SetRole(string roleName)
    {
        roleText.text = roleName.ToUpper();
    }
}
