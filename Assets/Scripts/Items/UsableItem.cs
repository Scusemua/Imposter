using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class UsableItem : NetworkBehaviour
{
    [Tooltip("Unique identifier of the item.")]
    public int Id;                  // Unique identifier of the item.

    [Tooltip("Name of the item.")]
    public string Name;             // Name of the item.

    [Header("Speed Modifiers")]
    [Tooltip("If this is true, then you can specify an explicit speed modifier for the weapon. Otherwise it defaults to its class speed modifier.")]
    public bool UseCustomSpeedModifier;
    [Tooltip("Changing this value will not have an effect unless 'UseCustomSpeedModifier' is toggled (i.e., set to true).")]
    public float SpeedModifier;

    /// <summary>
    /// The item's unique ID.
    /// </summary>
    public int ItemId { get; }

    /// <summary>
    /// The player currently holding this item.
    /// </summary>
    [HideInInspector] public PlayerController HoldingPlayer;

    /// <summary>
    /// Indicates whether or not this item is on the ground.
    /// </summary>
    [SyncVar(hook = nameof(OnGroundStatusChanged))] public bool OnGround;

    #region Client Functions

    public void OnGroundStatusChanged(bool _, bool _New)
    {
        if (_New)
            HoldingPlayer = null;
    }

    #endregion 
}