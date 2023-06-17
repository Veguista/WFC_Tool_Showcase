using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeColourController : MonoBehaviour
{
    LineRenderer _myLineRenderer;
    LineRenderer MyLineRenderer
    {
        get
        {
            if (_myLineRenderer == null)
                _myLineRenderer = GetComponent<LineRenderer>();

            return _myLineRenderer;
        }
    }


    // Instantly changes this rope's material colour to the desired new colour.
    // This also cancels any unfinished colour transitions.
    public void ChangeRopeColour(Color newColor)
    {
        CancelActiveCoRoutine();

        ApplyNewColour(newColor);
    }

    // Transitions this rope's material colour to the desired new colour in the indicated time.
    // This also cancels any unfinished colour transitions.
    public void TransitionRopeColour(Color newColor, float durationOfTransition)
    {
        CancelActiveCoRoutine();

        StartCoroutine(TransitionColour(MyLineRenderer.startColor, newColor, durationOfTransition));
    }



    // CoRoutine used to transition between 2 colours.
    IEnumerator TransitionColour(Color initialColour, Color targetColour, float duration)
    {
        if(initialColour == targetColour)
            yield break;

        Timer transitionTimer = new Timer(duration); ;

        while(!transitionTimer.IsComplete)
        {
            transitionTimer.Update();

            ApplyNewColour(Color.Lerp(initialColour, targetColour, transitionTimer.PercentageComplete));

            yield return null;
        }

        activeCoRoutine = null;
    }


    // Cancelling the last CoRoutine.
    Coroutine activeCoRoutine;

    void CancelActiveCoRoutine()
    {
        if (activeCoRoutine == null)
            return;

        StopCoroutine(activeCoRoutine);
        activeCoRoutine = null;
    }


    // Changing the rope colour.
    void ApplyNewColour(Color newColour)
    {        
        Gradient temporaryGradient = new Gradient();

        GradientColorKey[] temporaryColorKeys = new GradientColorKey[2];

        temporaryColorKeys[0] = new GradientColorKey(newColour, 0);
        temporaryColorKeys[1] = new GradientColorKey(newColour, 1);

        temporaryGradient.colorKeys = temporaryColorKeys;

        MyLineRenderer.colorGradient = temporaryGradient;
    }


    // This function allows for more control in script, directly allowing other scripts to set the rope's gradient.
    public void ApplyNewColourGradient(Gradient newGradient)
    {
        MyLineRenderer.colorGradient = newGradient;
    }


    // Used to be able to access the current colorGradient from other scripts.
    public Gradient LineRendererGradient
    {
        get { return MyLineRenderer.colorGradient; }
    }
}
