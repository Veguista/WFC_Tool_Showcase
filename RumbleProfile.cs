using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewRumbleProfile", menuName = "Create new Rumble Profile")]
[System.Serializable]
public class RumbleProfile : ScriptableObject
{   
    public Timeline lowFrequencyTimeline;
    public Timeline highFrequencyTimeline;

    // Rumble Lenght provides the highest lenght value our of the two Timelines.
    public float RumbleLenght
    {
        get
        {
            if (lowFrequencyTimeline.TimelineLenght >= highFrequencyTimeline.TimelineLenght)
                return lowFrequencyTimeline.TimelineLenght;

            return highFrequencyTimeline.TimelineLenght;
        }
    }
}
