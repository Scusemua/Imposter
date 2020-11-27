using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DetectiveScannerUI : MonoBehaviour
{
    [Tooltip("Displays the nickname of the player whose body is being tracked/scanned.")]
    public TextMeshProUGUI CurrentBodyText;

    [Tooltip("Displays how much time is left (in seconds) until the next scan is performed.")]
    public TextMeshProUGUI NextUpdateText;

    /// <summary>
    /// Fires when the player clicks the close button.
    /// </summary>
    public event Action ClearClicked;

    public void CloseButtonClicked()
    {
        enabled = false;
    }

    public void ClearButtonClicked()
    {
        ClearClicked?.Invoke();
    }

    /// <summary>
    /// Update the text field which displays the name of the player whose body is currently being tracked/scanned.
    /// </summary>
    public void SetCurrentBodyText(string text)
    {
        CurrentBodyText.text = text;
    }

    /// <summary>
    /// Update the text field corresponding to the time until the next scan is performed.
    /// </summary>
    public void UpdateTimeUntilNextScan(float time)
    {
        // Don't display less than zero seconds.
        if (time < 0)
            time = 0f;

        NextUpdateText.text = time.ToString() + " seconds";
    }
}
