using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These are the colors that players can select from.
/// </summary>
public static class GameColors
{
    public static Color  RED = new Color (255 / 255.0f, 0, 0, 255 / 255.0f);
    public static Color  ORANGE = new Color (255 / 255.0f, 128 / 255.0f, 0, 255 / 255.0f);
    public static Color  YELLOW = new Color (255 / 255.0f, 255 / 255.0f, 0, 255 / 255.0f);
    public static Color  GREEN = new Color (0, 167 / 255.0f, 0, 255 / 255.0f);
    public static Color  BLUE = new Color (0, 172 / 255.0f, 255 / 255.0f, 255 / 255.0f);
    public static Color  CYAN = new Color (51 / 255.0f, 255 / 255.0f, 255 / 255.0f, 255 / 255.0f);
    public static Color  PURPLE = new Color (102 / 255.0f, 0, 204 / 255.0f, 255 / 255.0f);
    public static Color  PINK = new Color (255 / 255.0f, 0, 255 / 255.0f, 255 / 255.0f);
    public static Color  BROWN = new Color (102 / 255.0f, 51 / 255.0f, 0, 255 / 255.0f);
    //public static Color  BLACK = new Color (0, 0, 0, 255);
    public static Color  WHITE = new Color (255 / 255.0f, 255 / 255.0f, 255 / 255.0f, 255 / 255.0f);
    //public static Color  BEIGE = new Color (255, 229, 204, 255);
    public static Color  GRAY = new Color (87 / 255.0f, 87 / 255.0f, 87 / 255.0f, 255 / 255.0f);

    public static Color START_COLOR = new Color(0, 0, 0, 0);

    /// <summary>
    /// An array of all of the available colors.
    /// </summary>
    public static Color [] ALL_COLORS = new Color []
    {
        RED, ORANGE, YELLOW, GREEN, BLUE, CYAN, PURPLE, PINK, BROWN, WHITE, GRAY, //BLACK, BEIGE
    };

    public static Dictionary<Color, string> COLOR_NAMES = new Dictionary<Color, string>
    {
        {RED, "RED" },
        {ORANGE, "ORANGE" },
        {YELLOW, "YELLOW" },
        {GREEN, "GREEN" },
        {BLUE, "BLUE" },
        {CYAN, "CYAN" },
        {PURPLE, "PURPLE" },
        {PINK, "PINK" },
        {BROWN, "BROWN" },
        {WHITE, "WHITE" },
        {GRAY, "GRAY" },
        {START_COLOR, "START_COLOR" }
    };
}
