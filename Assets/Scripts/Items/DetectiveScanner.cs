using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class DetectiveScanner : UsableItem
{
    [Tooltip("The current body identified by the scanner.")]
    public Player CurrentBody;

    [Tooltip("This prefab is instantiated wherever the scanner points.")]
    public GameObject ScannerIndicatorPrefab;
    public GameObject InstantiatedIndicator;

    [Tooltip("This sound plays when a scan is performed.")]
    public AudioClip ScanUpdated;

    /// <summary>
    /// Input is enabled when a player is holding this item.
    /// </summary>
    [HideInInspector]
    public bool InputEnabled;

    /// <summary>
    /// How long until this can be scanned again.
    /// </summary>
    private float scanCooldown;

    public string ItemName { get => "Detective Scanner"; }

    void Awake()
    {
        // Not exactly sure where to put this, so I am putting it in multiple places.
        Id = ItemDatabase.BodyScannerItemId;
    }

    void Alive()
    {
        // Not exactly sure where to put this, so I am putting it in multiple places.
        Id = ItemDatabase.BodyScannerItemId;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Not exactly sure where to put this, so I am putting it in multiple places.
        Id = ItemDatabase.BodyScannerItemId;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer || !InputEnabled) return;

        scanCooldown -= Time.deltaTime;
        
        
        if (HoldingPlayer != null)
        {
            // Update the UI of the holding player, if the holding player is non-null.
            HoldingPlayer.Player.PlayerUI.DetectiveScannerUI.UpdateTimeUntilNextScan(scanCooldown);

            // Is a scan available and do we have a body to point to?
            if (scanCooldown <= 0 && CurrentBody != null)
            {
                // If there already exists an indicator somewhere on the map, destroy it first.
                if (InstantiatedIndicator != null)
                    Destroy(InstantiatedIndicator);

                Player killer = (NetworkManager.singleton as NetworkGameManager).NetIdMap[CurrentBody.KillerId];
                InstantiatedIndicator = Instantiate(ScannerIndicatorPrefab, killer.transform.position, killer.transform.rotation);
                HoldingPlayer.AudioSource.PlayOneShot(ScanUpdated);
            }

            if (Input.GetKeyDown(KeyCode.T))
                HoldingPlayer.Player.PlayerUI.DetectiveScannerUI.enabled = true;
        }
    }

    public void OnGroundStateChanged(bool _, bool _New)
    {
        // If the scanner was dropped and there was a indicator spawned, then destroy it.
        if (!_New)
        {
            if (InstantiatedIndicator != null)
            {
                Destroy(InstantiatedIndicator);
                InstantiatedIndicator = null;
            }
            InputEnabled = false;
        }
        else if (_New)
        {
            InputEnabled = true;
            HoldingPlayer.Player.PlayerUI.DetectiveScannerUI.ClearClicked += ClearCurrentBody;
        }
    }

    /// <summary>
    /// Clears the current player body. This would stop scans from occuring. 
    /// </summary>
    public void ClearCurrentBody()
    {
        if (!isLocalPlayer) return;

        CurrentBody = null;
    }
}
