using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class RopeDissapearController : MonoBehaviour
{
    // Property used to reference our LineRenderer.
    LineRenderer _myLineRenderer;
    LineRenderer MyLineRenderer
    {
        get
        {
            if (_myLineRenderer == null)
            {
                if (gameObject.TryGetComponent<LineRenderer>(out LineRenderer myOut))
                {
                    _myLineRenderer = myOut;
                    _myLineRenderer.useWorldSpace = false;
                }
                else
                    Debug.LogError("Can't locate line renderer. Any RopeDissapearController prefab " +
                        "must always contain a LineRenderer.");
            }

            return _myLineRenderer;
        }
    }


    // Property used to reference our SplineContainer.
    SplineContainer mySplines;
    public SplineContainer MySplines
    {
        get
        {
            if (mySplines == null)
                mySplines = GetComponent<SplineContainer>();

            return mySplines;
        }
    }


    // Timer variables.
    float timer, totalTimer;

    float RopePercentageLeft { get { return timer / totalTimer; } }


    private void Update()
    {
        UpdateTimer();
        PaintRope();
    }


    // This function sets the point in the rope's Line Renderer.
    void PaintRope()
    {
        Vector3[] linePoints = new Vector3[MySplines.Spline.Count];
        BezierKnot[] myKnots = MySplines.Spline.ToArray();

        bool continuePainting = true;
        int i = 0;

        while (continuePainting && i < linePoints.Length - 1)
        {
            linePoints[i] = myKnots[i].Position;

            i++;

            float div = linePoints.Length - 1;

            if (i / div > RopePercentageLeft)
            {
                continuePainting = false;
            }
        }

        Vector3[] result = new Vector3[i];

        for(int x = 0; x < i; x++)
        {
            result[x] = linePoints[x];
        }

        MyLineRenderer.positionCount = result.Length;

        MyLineRenderer.SetPositions(result);
    }


    // This function reduces the lenght of the Spline using the timer values,
    void UpdateTimer()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            CloseRope();
        }
    }


    // This function is used when the animation is finished.
    // It disables this GameObject and resets the LineRenderer and SplineContainer.
    void CloseRope()
    {
        MySplines.Spline.Clear();
        MyLineRenderer.positionCount = 0;
        gameObject.SetActive(false);
    }


    // This function resets the totalTimer and Timer to the desired ammount of time.
    public void SetTimer(float timerLenght, float ropePercentageOfCompletion)
    {
        // The ropePercentageOfCompletion is the ammount of knots the rope has divided by
        // how many it could have (how many is the maximum in our RopeController).
        // This number can be bigger than expected if we expand the rope in other ways, so we check
        // that it is smaller than 1 first.

        if(ropePercentageOfCompletion < 1)
        {
            timerLenght *= ropePercentageOfCompletion;

            timer = timerLenght;
            totalTimer = timerLenght;
        }

        // If it is bigger than 1, we give the normal ammount of time for the rope to retrieve.
        timer = timerLenght;
        totalTimer = timerLenght;
    }


    // This function is used to set the dissapearing-rope's colour.
    public void SetRopeColour(Color newColour)
    {
        Gradient newGradient = new();
        GradientColorKey[] newColourKeys = new GradientColorKey[2];

        newColourKeys[0] = new GradientColorKey(newColour, 0);
        newColourKeys[1] = new GradientColorKey(newColour, 1);

        newGradient.colorKeys = newColourKeys;
        MyLineRenderer.colorGradient = newGradient;
    }


    string ropeDeletedSoundName = "Rope Deleted";
    AudioManager MyAudioManager
    {
        get 
        {
            if (_myAudioManager == null)
                _myAudioManager = AudioManager.instance;

            return _myAudioManager;
        }
    }
    AudioManager _myAudioManager;

    private void OnEnable()
    {
        // Activating the Deleted Line Sound.
        MyAudioManager.PlaySound(ropeDeletedSoundName);
    }
}
