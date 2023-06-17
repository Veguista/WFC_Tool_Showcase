using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Timeline
{
    [SerializeField] List<TimelinePoint> _timelinePoints;
    List<TimelinePoint> TimelinePoints
    {
        get
        {
            if (_timelinePoints == null)
            {
                _timelinePoints = new();
                _timelinePoints.Add(new TimelinePoint());
            }

            return _timelinePoints;
        }

        set { _timelinePoints = value; }
    }

    public void OrderTimeline()
    {
        TimelinePoints.Sort();
    }

    public TimelinePoint[] Points { get { return TimelinePoints.ToArray(); } }

    public float TimelineLenght
    {
        get
        {
            OrderTimeline();

            // Just in case our Timeline is empty.
            if (TimelinePoints.Count == 0)
                return 0;

            return TimelinePoints[TimelinePoints.Count - 1].Time;
        }
    }


    // Returns the Value of our Timeline at a given time.
    // If the evaluationTime is before the first TimelinePoint, we return the first point's value.
    // If the evaluationTime is after the last TimelinePoint, we return the last point's value.
    public float EvaluateTimeline(float evaluationTime)
    {
        // Checking that our time value is not under 0.
        if (evaluationTime < 0)
        {
            Debug.LogError("A Timeline cannot be evaluated at a negative time (time = " + evaluationTime + ").");
            return 0;
        }


        // With only 1 point, our Timeline is always worth the same.
        if (TimelinePoints.Count == 1)
            return TimelinePoints[0].Value;



        // Checking if the evaluationTime matches any of our points' time.
        TimelinePoint point = FindPointAtTime(evaluationTime);

        if (point != null)
            return point.Value;


        // Checking that our evaluationTime isn't smaller than our first point.
        // If it is, we return the same value as our first point.
        if (evaluationTime < TimelinePoints[0].Time)
            return TimelinePoints[0].Value;


        // At this point we assume that our point is an interpolation of 2 other points.
        TimelinePoint firstPoint = new(), secondPoint = new();

        // Finding the firstPoint:
        for (int i = 0; i < TimelinePoints.Count; i++)
        {
            if (TimelinePoints[i].Time < evaluationTime)
                firstPoint = TimelinePoints[i];
        }

        // Finding the secondPoint:
        for (int i = TimelinePoints.Count - 1; i >= 0; i--)
        {
            if (TimelinePoints[i].Time > evaluationTime)
                secondPoint = TimelinePoints[i];
        }


        // We calculate the distance in time between both points.
        float timeDistance = secondPoint.Time - firstPoint.Time;

        // And we calculate the percentage of Lerp between both times.
        float percentageLerp = (evaluationTime - firstPoint.Time) / timeDistance;

        return Mathf.Lerp(firstPoint.Value, secondPoint.Value, percentageLerp);
    }


    /// <summary>
    /// Looks in our Timeline for a Point with a concrete Time value.
    /// </summary>
    /// <param name="time"></param>
    /// <returns>A point if there is one with the input Time. Null if there isn't.</returns>
    public TimelinePoint FindPointAtTime(float time)
    {
        // We look for a point already in the Timeline with a similar Time value.
        foreach (TimelinePoint point in TimelinePoints)
        {
            if (point.Time == time)
            {
                return point;
            }
        }

        return null;
    }


    // If the Timeline is empty, it adds a TimelinePoint (0,0).
    // Otherwise, it adds a new point after the last point by adding 0.01f to its time and copying its value.
    public void AddPoint()
    {
        if (TimelinePoints.Count == 0)
        {
            TimelinePoints.Add(new TimelinePoint(0, 0));
            return;
        }

        OrderTimeline();

        TimelinePoint lastPoint = TimelinePoints[TimelinePoints.Count - 1];

        TimelinePoints.Add(new TimelinePoint(lastPoint.Time + 0.01f, lastPoint.Value));
    }

    // Adds a concrete point to our Timeline.
    public void AddPoint(TimelinePoint newPoint)
    {
        if (FindPointAtTime(newPoint.Time) != null)
        {
            Debug.LogWarning("Trying to add a Point to a Timeline with a duplicated Time.");
            return;
        }

        TimelinePoints.Add(newPoint);

        OrderTimeline();
    }


    public void RemovePoint(TimelinePoint pointToRemove)
    {
        // We look for a similar point in the Timeline.
        if (TimelinePoints.Contains(pointToRemove))
        {
            TimelinePoints.Remove(pointToRemove);

            // If our Timeline empties, we add a TimelinePoint (0,0) to avoid errors.
            if (TimelinePoints.Count == 0)
                AddPoint();

            return;
        }

        Debug.LogError("Couldn't find point (Time: " + pointToRemove.Time + "; Value: " + pointToRemove.Value + ")." +
            "\nThe point couldn't be removed.");
    }

    public void RemovePoint(float timeOfPoint)
    {
        TimelinePoint point = FindPointAtTime(timeOfPoint);

        if (point != null)
        {
            TimelinePoints.Remove(point);

            // If our Timeline empties, we add a TimelinePoint (0,0) to avoid errors.
            if (TimelinePoints.Count == 0)
                AddPoint();

            return;
        }

        Debug.LogError("Couldn't find a point at time: " + timeOfPoint + " secs." +
            "\nNo point was removed.");
    }


    public void ChangePointTime(float timeOfPoint, float newTime)
    {
        if (newTime < 0)
        {
            Debug.LogError("All points must have a Time higher than 0. Trying to set point time to " + newTime + ".");
            return;
        }


        // We check that no other point already in the Timeline already has the target newTime.
        if (FindPointAtTime(newTime) != null)
        {
            if (newTime == 0)   // This reduces the ammount of unnecessary messages.
                return;

            Debug.LogWarning("Trying to set time of TimelinePoint to an already occupied Time (" + newTime + ")." +
                "\nTry to set the Time to a different value.");
            return;
        }


        // And now we look for our point to set its Time.
        TimelinePoint point = FindPointAtTime(timeOfPoint);

        if (point != null)
        {
            point.Time = newTime;
            return;
        }


        Debug.LogError("Couldn't find a point at time: " + timeOfPoint + " secs.");
    }

    public void ChangePointValue(float timeOfPoint, float newValue)
    {
        if (newValue < 0 || newValue > 1)
        {
            Debug.LogWarning("All points must have a Value in the range [0, 1]. " +
                "Trying to set point time to " + newValue + ".");
            return;
        }


        // We look for our point to set its Value.
        TimelinePoint point = FindPointAtTime(timeOfPoint);

        if (point != null)
        {
            point.Value = newValue;
            return;
        }

        Debug.LogError("Couldn't find a point at time: " + timeOfPoint + " secs.");
    }


    /// <summary>
    /// Returns a Vector2 array containing the (Time; Value) of each of the points in the input array.
    /// </summary>
    /// <param name="pointsArray"></param>
    /// <returns>Array with (Time; Value) points in Vector2 form.</returns>
    public static Vector2[] ConvertTimelineToVector2Array(TimelinePoint[] pointsArray)
    {
        List<Vector2> result = new();

        foreach (TimelinePoint point in pointsArray)
        {
            result.Add(new Vector2(point.Time, point.Value));
        }

        return result.ToArray();
    }
}
