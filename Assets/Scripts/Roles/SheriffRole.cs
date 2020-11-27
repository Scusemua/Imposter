using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheriffRole : CrewmateRole {
    public override string Name { get { return "SHERIFF"; } }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // The sheriff gets a body scanner.
        player.GetComponent<PlayerController>().GivePlayerItem(ItemDatabase.BodyScannerItemId, false);
    }

    void Start() {
        base.Start();
    }

    void Update() {
        base.Update();
    }
}