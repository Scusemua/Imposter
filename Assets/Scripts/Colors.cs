using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These are the colors that players can select from.
/// </summary>
public static class GameColors
{
    public static Color RED = new Color(255, 0, 127);
    public static Color ORANGE = new Color(255, 128, 0);
    public static Color YELLOW = new Color(255, 255, 0);
    public static Color GREEN = new Color(0, 204, 0);
    public static Color BLUE = new Color(0, 128, 255);
    public static Color CYAN = new Color(51, 255, 255);
    public static Color PURPLE = new Color(102, 0, 204);
    public static Color PINK = new Color(255, 0, 255);
    public static Color BROWN = new Color(102, 51, 0);
    public static Color BLACK = new Color(0, 0, 0);
    public static Color WHITE = new Color(255, 255, 255);
    public static Color BEIGE = new Color(255, 229, 204);
    public static Color GRAY = new Color(160, 160, 160);

    /// <summary>
    /// An array of all of the available colors.
    /// </summary>
    public static Color[] ALL_COLORS = new Color[]
    {
        RED, ORANGE, YELLOW, GREEN, BLUE, CYAN, PURPLE, PINK, BROWN, BLACK, WHITE, BEIGE, GRAY
    };
}
