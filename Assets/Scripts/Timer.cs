using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{    public enum Phase
    {
        DISCUSSION,
        TRANSITION, // Brief pause before beginning the voting phase. 
        VOTING 
    }

    public float TimeRemaining;

    public GameOptions GameOptions;

    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI PhaseLabel;

    /// <summary>
    /// Length of transition phase, which occurs briefly between DISCUSSION and VOTING.
    /// </summary>
    private const float transitionDuration = 1.0f;

    private Phase currentPhase;

    // Since apparently Color has no static orange property. 
    private Color orange = new Color(255, 140, 0);

    private float orangeThresholdPercent = 0.525f; // A little above 50% to ensure color change is visible by 50%.
    private float redThresholdPercent = 0.275f;      // A little above 25% to ensure color change is visible by 25%.

    private float orangeThresholdVoting;
    private float redThresholdVoting;
    private float orangeThresholdDiscussion;
    private float redThresholdDiscussion;

    // Start is called before the first frame update
    void Start()
    {
        currentPhase = Phase.DISCUSSION;
        GameOptions = GameOptions.singleton;

        TimeRemaining = GameOptions.discussionPeriod;

        // Calculate these once at the beginning so we don't have to recalculate them everytime.
        orangeThresholdVoting = GameOptions.votingPeriod * orangeThresholdPercent;
        redThresholdVoting = GameOptions.votingPeriod * redThresholdPercent;

        orangeThresholdDiscussion = GameOptions.discussionPeriod * orangeThresholdPercent;
        redThresholdDiscussion = GameOptions.discussionPeriod * redThresholdPercent;

        PhaseLabel.text = "Discuss";
    }

    private Color getColorFromTimeRemaining(float timeRemaining)
    {
        switch (currentPhase)
        {
            case Phase.DISCUSSION:
                if (timeRemaining <= redThresholdDiscussion)
                    return Color.red;
                else if (timeRemaining <= orangeThresholdDiscussion)
                    return orange;
                else
                    return Color.white;
            case Phase.VOTING:
                if (timeRemaining <= redThresholdVoting)
                    return Color.red;
                else if (timeRemaining <= orangeThresholdVoting)
                    return orange;
                else
                    return Color.white;
            default:
                return Color.white;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // While the timer is still active, decrement by the amount of time that has passed.
        if (TimeRemaining > 0)
        {
            TimeRemaining -= Time.deltaTime;

            // Timer text is blank during transition phase.
            if (currentPhase != Phase.TRANSITION)
                TimerText.text = ((int)TimeRemaining).ToString();

            TimerText.color = getColorFromTimeRemaining(TimeRemaining);
        }

        // Timer has expired. Transition to next phase. 
        else if (TimeRemaining <= 0)
        {
            TimeRemaining = 0;
            TimerText.text = "0";
            TimerText.color = Color.white;
            
            // Current phase is DISCUSSION. Need to transition to TRANSITION.
            if (currentPhase == Phase.DISCUSSION)
            {
                Debug.Log("Timer has finished period " + currentPhase + ". Transitioning to phase " + Phase.TRANSITION);
                currentPhase = Phase.TRANSITION;
                PhaseLabel.text = "Vote";
                TimeRemaining = 1.0f;
                TimerText.text = "";
            }
            // Current phase is TRANSITION. Need to transition to VOTING.
            else if (currentPhase == Phase.TRANSITION)
            {
                Debug.Log("Timer has finished period " + currentPhase + ". Transitioning to phase " + Phase.VOTING);
                currentPhase = Phase.VOTING;
                PhaseLabel.text = "Vote";
                TimeRemaining = GameOptions.votingPeriod;
            }
            // Current phase is VOTING. We're done.
            else
            {
                Debug.Log("Timer has finished period " + currentPhase);
            }
        }
    }
}
