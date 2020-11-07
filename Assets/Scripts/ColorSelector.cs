using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using Lean.Gui;
using System.Linq;

public class ColorSelector : NetworkBehaviour
{
    public GameObject ColorSelectionButton;

    public CustomNetworkRoomPlayer RoomPlayer;

    /// <summary>
    /// Maintain a list of available colors on the server.
    /// </summary>
    private List<Color> availableColors = new List<Color>(GameColors.ALL_COLORS);

    /// <summary>
    /// Maintain a list of colors that have been claimed by players.
    /// </summary>
    private List<Color> unavailableColors = new List<Color>();

    public Dictionary<Color, LeanButton> ColorToButtonMap = new Dictionary<Color, LeanButton>();
    
    /// <summary>
    /// Print (via Debug.Log()) the available and unavailable colors lists.
    /// </summary>
    private void logAvailableAndUnavailableColors()
    {
        string s1 = "";
        string s2 = "";
        availableColors.ForEach(c => s1 += (c + " (" + GameColors.COLOR_NAMES[c] + "), "));
        unavailableColors.ForEach(c => s2 += (c + " (" + GameColors.COLOR_NAMES[c] + "), "));
        Debug.Log("Available Colors: " + s1);
        Debug.Log("Unavailable Colors: " + s2);
    }

    /// <summary>
    /// Attempts to claim the given newColor, returning the given oldColor (if oldColor is a valid color).
    /// </summary>
    /// <returns>Returns True if the new color has been claimed, otherwise returns False.</returns>
    [Server]
    public bool AttemptClaimColor(Color oldColor, Color newColor)
    {
        Debug.Log("CmdSelectColor(" + oldColor + ", " + newColor + ")");
        Debug.Log("User is requesting color " + newColor + " (" + GameColors.COLOR_NAMES[newColor] + "). Old color = " + oldColor + " (" + GameColors.COLOR_NAMES[oldColor] + ")");
        logAvailableAndUnavailableColors();

        // If the color is available, then make it unavailable and inform the player.
        if (availableColors.Contains(newColor))
        {
            availableColors.Remove(newColor);

            // Avoid adding duplicates. We don't use HashSet bc we want to be able to remove via index, though.
            if (!unavailableColors.Contains(newColor))
            {
                unavailableColors.Add(newColor);
                ColorToButtonMap[newColor].enabled = false;
            }
            else
                Debug.LogError("Color " + newColor + " (" + GameColors.COLOR_NAMES[newColor] + ") was contained within both availableColors and unavailableColors...");

            // If the user is switching colors (which should always be the case for now...), then
            // mark their old color as available now.
            if (oldColor != null)
            {
                // Make sure the color that the user is claiming to have is considered unavailable.
                if (!unavailableColors.Contains(oldColor))
                {
                    Debug.LogWarning("Player is attempting to switch from color " + oldColor + " (" + GameColors.COLOR_NAMES[oldColor] + "), but this color is currently available...");
                    ColorToButtonMap[oldColor].enabled = true; // Just ensure it is interactable... 
                }
                else
                {
                    unavailableColors.Remove(oldColor);

                    if (!availableColors.Contains(oldColor))
                        availableColors.Add(oldColor);

                    ColorToButtonMap[oldColor].enabled = true;
                }
            }

            return true;
        }

        return false;
    }

    //[Command(ignoreAuthority = true)]
    //public void CmdAssignInitialColor()
    //{
    //    if (!RoomPlayer.hasAuthority)
    //    {
    //        Debug.LogWarning("RoomPlayer " + RoomPlayer.DisplayName + " (netId = " + netId + ") is not local player. Returning from CmdAssignInitialColor() immediately.");
    //        return;
    //    }
    //    Debug.Log("Assigning initial color for player " + RoomPlayer.DisplayName + ", netId = " + netId);

    //    logAvailableAndUnavailableColors();

    //    // Randomly assign a color to the player.
    //    int index = (int)Mathf.Floor(Random.value * availableColors.Count);

    //    Color assignedColor = availableColors[index];
    //    availableColors.RemoveAt(index);

    //    if (!unavailableColors.Contains(assignedColor))
    //        unavailableColors.Add(assignedColor);
    //    else
    //        Debug.LogError("Color " + assignedColor + " (" + GameColors.COLOR_NAMES[assignedColor] + ") was contained within availableColors and unavailableColors simultaneously...");

    //    ColorToButtonMap[assignedColor].enabled = false;

    //    Debug.Log("Assigning color " + assignedColor + " (" + GameColors.COLOR_NAMES[assignedColor] + ") to player " + netId + ".");
    //    RoomPlayer.PlayerModelColor = assignedColor;
    //}

    [Server]
    public Color ClaimRandomAvailableColor()
    {
        logAvailableAndUnavailableColors();

        // Randomly assign a color to the player.
        int index = (int)Mathf.Floor(Random.value * availableColors.Count);

        Color randomColor = availableColors[index];
        availableColors.RemoveAt(index);

        if (!unavailableColors.Contains(randomColor))
            unavailableColors.Add(randomColor);
        else
            Debug.LogError("Color " + randomColor + " (" + GameColors.COLOR_NAMES[randomColor] + ") was contained within availableColors and unavailableColors simultaneously...");

        ColorToButtonMap[randomColor].enabled = false;

        Debug.Log("Assigning color " + randomColor + " (" + GameColors.COLOR_NAMES[randomColor] + ") to player " + netId + ".");

        return randomColor;
    }

    [Client]
    public void PopulateButtons(CustomNetworkRoomPlayer roomPlayer)
    {
        // Create a button for each color.
        foreach (Color color in GameColors.ALL_COLORS)
        {
            // First, instantiate the GameObject.
            GameObject colorButton = Instantiate(ColorSelectionButton, transform);

            colorButton.GetComponentsInChildren<Image>()[1].color = color;
            colorButton.GetComponent<LeanButton>().OnClick.AddListener(() =>
            {
                Color clickedColor = colorButton.GetComponentsInChildren<Image>()[1].color;
                Debug.Log("Clicked button with color " + clickedColor + " (" + GameColors.COLOR_NAMES[clickedColor] + ")");

                //if (RoomPlayer.isLocalPlayer) CmdSelectColor(RoomPlayer.PlayerModelColor, clickedColor);
                roomPlayer.OnClickColorButton(clickedColor);
            });

            ColorToButtonMap.Add(color, colorButton.GetComponent<LeanButton>());
        }
    }
}
