using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VotingUI : MonoBehaviour
{
    public Timer CountdownTimer;
    public PlayerController PlayerController;
    public GameObject PlayerUI;

    // Start is called before the first frame update
    void Start()
    {
        CountdownTimer.OnTimerCompleted += OnTimerCompleted;
    }

    public void OnTimerCompleted(int arg0)
    {
        Debug.Log("Timer has completed.");

        PlayerUI.SetActive(true);
        PlayerController.MovementEnabled = true;

        // TODO: Handle votes?
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        CountdownTimer.OnTimerCompleted -= OnTimerCompleted;
    }
}
