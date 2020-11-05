using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private enum Phase
    {
        DISCUSSION,
        TRANSITION, // Brief pause before beginning the voting phase. 
        VOTING 
    }

    /// <summary>
    /// Length of transition phase, which occurs briefly between DISCUSSION and VOTING.
    /// </summary>
    private const float transitionDuration = 1.0f;

    private Phase currentPhase;

    public float timeRemaining;

    public GameOptions gameOptions;

    public TextMeshProUGUI timerText;

    // Since apparently Color has no static orange property. 
    private Color orange = new Color(255, 140, 0);

    private float orangeThresholdPercent = 0.5f;
    private float redThresholdPercent = 0.25f;

    private float orangeThresholdVoting;
    private float redThresholdVoting;
    private float orangeThresholdDiscussion;
    private float redThresholdDiscussion;

    // Start is called before the first frame update
    void Start()
    {
        currentPhase = Phase.DISCUSSION;
        gameOptions = GameOptions.singleton;

        timeRemaining = gameOptions.discussionPeriod;

        // Calculate these once at the beginning so we don't have to recalculate them everytime.
        orangeThresholdVoting = gameOptions.votingPeriod * orangeThresholdPercent;
        redThresholdVoting = gameOptions.votingPeriod * redThresholdPercent;

        orangeThresholdDiscussion = gameOptions.discussionPeriod * orangeThresholdPercent;
        redThresholdDiscussion = gameOptions.discussionPeriod * redThresholdPercent;
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
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = ((int)timeRemaining).ToString();

            timerText.color = getColorFromTimeRemaining(timeRemaining);
        }

        // Timer has expired. Transition to next phase. 
        else if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            timerText.text = "0";
            timerText.color = Color.white;
            
            // Current phase is DISCUSSION. Need to transition to TRANSITION.
            if (currentPhase == Phase.DISCUSSION)
            {
                Debug.Log("Timer has finished period " + currentPhase + ". Transitioning to phase " + Phase.TRANSITION);
                currentPhase = Phase.TRANSITION;
                timeRemaining = 1.0f;
            }
            // Current phase is TRANSITION. Need to transition to VOTING.
            else if (currentPhase == Phase.TRANSITION)
            {
                Debug.Log("Timer has finished period " + currentPhase + ". Transitioning to phase " + Phase.VOTING);
                currentPhase = Phase.VOTING;
                timeRemaining = gameOptions.votingPeriod;
            }
            // Current phase is VOTING. We're done.
            else
            {
                Debug.Log("Timer has finished period " + currentPhase);
            }
        }
    }
}
