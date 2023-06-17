using UnityEngine;
using System;

[Serializable]
public class TimelinePoint : IComparable
{
    [SerializeField] float _time;
    public float Time
    {
        get { return _time; }

        set
        {
            if (value < 0)
            {
                Debug.LogError("Time var in TimelinePoint has to be bigger than 0.");
                _time = 0;
                return;
            }

            _time = value;
        }
    }

    [SerializeField] float _value;
    public float Value
    {
        get { return _value; }

        set
        {
            if (value < 0)
            {
                Debug.LogError("Value var in TimelinePoint has to be in the Range[0,1].");
                _value = 0;
                return;
            }

            if (value > 1)
            {
                Debug.LogError("Value var in TimelinePoint has to be in the Range[0,1].");
                _value = 1;
                return;
            }

            _value = value;
        }
    }


    // Constructors.
    public TimelinePoint()
    {
        Value = 0;
        Time = 0;
    }

    public TimelinePoint(float time, float value)
    {
        Value = value;
        Time = time;
    }


    // Ordering TimelinePoints. It uses Time (from smaller Time value to biggest Time value)
    int IComparable.CompareTo(object obj)
    {
        TimelinePoint otherPoint = (TimelinePoint)obj;

        if (otherPoint.Time > Time)
            return -1;

        if (otherPoint.Time < Time)
            return 1;

        // Same time.
        return 0;
    }
}
