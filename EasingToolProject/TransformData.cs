using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformData
{
    // World values.
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 lossyScale;

    // Local values.
    public Vector3 localPosition;
    public Quaternion localRotation;
    public Vector3 localScale;

    public TransformData(Transform t)
    {
        // World values.
        position = t.position;
        rotation = t.rotation;
        lossyScale = t.lossyScale;

        // Local values.
        localPosition = t.localPosition;
        localRotation = t.localRotation;
        localScale = t.localScale;
    }

    public void UpdateTransform(Transform t)
    {
        // World values.
        position = t.position;
        rotation = t.rotation;
        lossyScale = t.lossyScale;

        // Local values.
        localPosition = t.localPosition;
        localRotation = t.localRotation;
        localScale = t.localScale;
    }

    public void ApplyWorldDataTo(Transform targetTransform)
    {
        targetTransform.position = position;
        targetTransform.rotation = rotation;

        // World scale can never be changed.
        targetTransform.localScale = localScale;
    }

    public void ApplyLocalDataTo(Transform targetTransform)
    {
        targetTransform.localPosition = localPosition;
        targetTransform.localRotation = localRotation;
        targetTransform.localScale = localScale;
    }
}
