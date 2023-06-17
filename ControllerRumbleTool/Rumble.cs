using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rumble
{
    public RumbleProfile profile;


    public float HighFrequencyValue
    {
        get
        {
            RumbleTimer.Update();

            return ObtainTimelineValueAtTime(profile.highFrequencyTimeline, RumbleTimer.Time);
        }
    }
    public float LowFrequencyValue
    {
        get
        {
            RumbleTimer.Update();

            return ObtainTimelineValueAtTime(profile.lowFrequencyTimeline, RumbleTimer.Time);
        }
    }


    // This function is used to Evaluate timelines outside of their TimeLenght
    // (which is needed when both Timelines don't have the same TimelineLenght).
    public float ObtainTimelineValueAtTime(Timeline timeline, float time)
    {
        if (time < 0)
        {
            Debug.LogError("Time cannot have a negative value. Cannot access Timeline's value.");
            return 0;
        }


        if (timeline == null)
        {
            Debug.LogError("timeline cannot be NULL.");
            return 0;
        }


        // If our time is longer than the individual TimelineLenght, we just return the last TimelinePoint's value.
        if (timeline.TimelineLenght < time)
            return timeline.EvaluateTimeline(timeline.TimelineLenght);

        return timeline.EvaluateTimeline(time);
    }


    #region Timer

    Timer _rumbleTimer;
    public Timer RumbleTimer
    {
        get
        {
            if (_rumbleTimer == null)
                _rumbleTimer = new Timer(profile.RumbleLenght);

            return _rumbleTimer;
        }

        set { _rumbleTimer = value; }
    }


    public void ResetTimer()
    {
        RumbleTimer = null;
    }

    public bool IsTimerFinished()
    {
        RumbleTimer.Update();

        if (RumbleTimer.IsComplete)
            return true;
        else
            return false;
    }

    #endregion


    // Constructor.
    public Rumble(RumbleProfile p)
    {
        profile = p;
    }
}
