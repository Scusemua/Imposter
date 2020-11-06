using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These are the colors that players can select from.
/// </summary>
public static class GameColors
{
    public static Color32  RED = new Color32 (255, 0, 127, 255);
    public static Color32  ORANGE = new Color32 (255, 128, 0, 255);
    public static Color32  YELLOW = new Color32 (255, 255, 0, 255);
    public static Color32  GREEN = new Color32 (0, 204, 0, 255);
    public static Color32  BLUE = new Color32 (0, 128, 255, 255);
    public static Color32  CYAN = new Color32 (51, 255, 255, 255);
    public static Color32  PURPLE = new Color32 (102, 0, 204, 255);
    public static Color32  PINK = new Color32 (255, 0, 255, 255);
    public static Color32  BROWN = new Color32 (102, 51, 0, 255);
    //public static Color32  BLACK = new Color32 (0, 0, 0, 255);
    public static Color32  WHITE = new Color32 (255, 255, 255, 255);
    //public static Color32  BEIGE = new Color32 (255, 229, 204, 255);
    public static Color32  GRAY = new Color32 (87, 87, 87, 255);

    /// <summary>
    /// An array of all of the available colors.
    /// </summary>
    public static Color32 [] ALL_COLORS = new Color32 []
    {
        RED, ORANGE, YELLOW, GREEN, BLUE, CYAN, PURPLE, PINK, BROWN, WHITE, GRAY, //BLACK, BEIGE
    };
}
