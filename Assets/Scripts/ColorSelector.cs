using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using Lean.Gui;

public class ColorSelector : NetworkBehaviour
{
    public GameObject ColorSelectionButton;

    /// <summary>
    /// Maintain a list of available colors on the server.
    /// </summary>
    private HashSet<Color32> availableColors = new HashSet<Color32>(GameColors.ALL_COLORS);

    /// <summary>
    /// Maintain a list of colors that have been claimed by players.
    /// </summary>
    private HashSet<Color> unavailableColors = new HashSet<Color>();

    public CustomNetworkRoomPlayer RoomPlayer;

    [Command(ignoreAuthority = true)]
    public void CmdSelectColor(Color oldColor, Color newColor)
    {
        Debug.Log("User is requesting color " + newColor + ". Old color = " + oldColor);
        Debug.Log("Available Colors: " + string.Join("", availableColors));
        Debug.Log("Unavailable Colors: " + string.Join("", unavailableColors));

        // If the color is available, then make it unavailable and inform the player.
        if (availableColors.Contains(newColor))
        {
            availableColors.Remove(newColor);
            unavailableColors.Add(newColor);

            // If the user is switching colors (which should always be the case for now...), then
            // mark their old color as available now.
            if (oldColor != null)
            {
                // Make sure the color that the user is claiming to have is considered unavailable.
                if (!unavailableColors.Contains(oldColor))
                    Debug.LogError("Player is attempting to switch from color " + oldColor + ", but this color is currently available...");
                else
                {
                    unavailableColors.Remove(oldColor);
                    availableColors.Add(oldColor);
                }
            }

            RoomPlayer.PlayerModelColor = newColor;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Create a button for each color.
        foreach (Color32 color in GameColors.ALL_COLORS)
        {
            // First, instantiate the GameObject.
            GameObject colorButton = Instantiate(ColorSelectionButton, transform);

            colorButton.GetComponentsInChildren<Image>()[1].color = color;
            colorButton.GetComponent<LeanButton>().OnClick.AddListener(() =>
            {
                Color32 clickedColor = colorButton.GetComponentsInChildren<Image>()[1].color;
                Debug.Log("Clicked button with color " + clickedColor);

                if (RoomPlayer.isLocalPlayer) CmdSelectColor(RoomPlayer.PlayerModelColor, clickedColor);
            });
        }
    }
}
