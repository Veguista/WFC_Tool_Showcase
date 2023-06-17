using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

public class DrawingRope : MonoBehaviour
{
    public Orientation myOrientation = Orientation.right;

    public RopeInfo myRopeInfo;


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


    // Property used to reference our RopeManager.
    RopeManager _myRopeManager;
    public RopeManager MyRopeManager
    {
        get
        {
            if (_myRopeManager == null)
                _myRopeManager = RopeManager.instance;

            return _myRopeManager;
        }
    }


    // Property used to reference our target, which should be a child to this object.
    RopeTarget target;
    public RopeTarget Target
    {
        get
        {
            if (target == null)
                target = GetComponentInChildren<RopeTarget>();

            return target;
        }
    }


    PlayerInputActions InputActions { get { return StatesManager.instance.InputActions; } }
    EnergyController Energy_Controller { get { return EnergyController.instance; } }



    private void Update()
    {
        if (myRopeInfo.state == RopeState.drawing) // || state == RopeState.full)  // If we are drawing our rope.
        {
            PaintRope();
            ConvertRopeLenghtToColour();
            ConvertRopeLenghtToSound();
        }
    }


    #region Basic functionality
    // This function adds a BezierKnot to our Spline in the desired position.
    // It returns true if that know completed our rope.
    public bool AddBezierKnot(Vector3 position)
    {
        if(myRopeInfo.state != RopeState.drawing)
        {
            Debug.LogError("Trying to add knots to a Spline that is not in drawing State.");
            return false;
        }

        // We first add the dot to our position.
        MySplines.Spline.Add(new BezierKnot(position));

        // We then check if our rope lenght has been reached.
        // If it has, we change our rope State and return true.
        if(MySplines.Spline.Count >= Target.CompoundMaxRopeLenghtInKnots)
        {
            myRopeInfo.state = RopeState.full;
            return true;
        }

        return false;
    }


    // This function empties the Spline, eliminating all knots but our original one.
    public void EmptySpline()
    {
        myRopeInfo.state = RopeState.empty;

        EmptyLineRenderer();

        // We empty the Spline and leave only one knot inside.
        MySplines.Spline.Clear();
        MySplines.Spline.Add(new BezierKnot(Vector3.zero));
    }


    /* Deprecated / unfinished feature
    public bool CheckForIntersection(Vector3 start, Vector3 end)
    {
        IEnumerable<BezierKnot> myKnots = Splines.Spline.Knots;

        int ID = 0;

        foreach(BezierKnot b in myKnots)
        {

        }
    }
    */
    #endregion


    #region Line Painting

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
                {
                    Debug.LogError("All ropes must have a LineRenderer component attached to them.");
                }
            }

            return _myLineRenderer;
        }
    }


    // This function sets the points in the rope's Line Renderer.
    void PaintRope()
    {
        MyLineRenderer.positionCount = MySplines.Spline.Count;

        Vector3[] linePoints = new Vector3[MySplines.Spline.Count + 1];
        BezierKnot[] myKnots = MySplines.Spline.ToArray();

        for (int i = 0; i < linePoints.Length - 1; i++)
        {
            linePoints[i] = myKnots[i].Position;
        }

        linePoints[linePoints.Length - 1] = Target.transform.localPosition;

        MyLineRenderer.SetPositions(linePoints);
    }

    // This function activates the colouring of the rope as feedback on its remaining lenght.
    void ConvertRopeLenghtToColour()
    {
        float numberOfExtraKnots = Target.extraKnots;
        float maxRopeLenght = Target.CompoundMaxRopeLenghtInKnots - numberOfExtraKnots;
        float currentRopeLenght = Target.CurrentRopeLenghtInKnots;

        float percentage = (currentRopeLenght - numberOfExtraKnots) / maxRopeLenght;
        float clampedPercentage = Mathf.Clamp(percentage, 0, 1);

        Color initialColor = MyRopeManager.initialDrawingColour;
        Color finalColor = MyRopeManager.finalDrawingColour;


        ColourRope(Color.Lerp(initialColor, finalColor, MyRopeManager.colorVariationCurve.Evaluate(clampedPercentage)));


        /* [OLD WAY OF COLOURING ROPE]
        
        float easedPercentage = EasingFunctions.ApplyEase(percentage, EasingFunctions.Functions.InQuint);

        ColourRope(Color.Lerp(initialColor, finalColor, easedPercentage));
        */
    }


    // Pointer to access and initialize our RopeColourController reference.
    RopeColourController _myColourController;
    RopeColourController MyColourController
    {
        get
        {
            if (_myColourController == null)
                _myColourController = GetComponent<RopeColourController>();

            return _myColourController;
        }
    }


    // This function is used to colour our rope using a single colour.
    public void ColourRope(Color newColour)
    {
        MyColourController.ChangeRopeColour(newColour);
    }

    
    // Called while players delete a rope. This method alters the appearance of set rope.
    public void DeleteRopeDisplay(float percentageDelete)
    {
        if(percentageDelete < 0 || percentageDelete > 1)
        {
            Debug.LogError("The input var 'percentageDelete' (= " + percentageDelete + ") must be in the range [0,1].");
            return;
        }
        
        Gradient newGradient = new();
        newGradient.mode = GradientMode.Fixed;
        GradientColorKey[] newColourKeys;

        Color originalColour = MyRopeManager.initialDrawingColour;
        Color deletedColour = MyRopeManager.deletedRopeColour;


        if (percentageDelete == 0)          // Complete rope.
        {
            newColourKeys = new GradientColorKey[2];

            newColourKeys[0] = new GradientColorKey(originalColour, 0);
            newColourKeys[1] = new GradientColorKey(originalColour, 1);
        }

        else if (percentageDelete == 1)     // Fully deleted rope.
        {
            newColourKeys = new GradientColorKey[2];

            newColourKeys[0] = new GradientColorKey(deletedColour, 0);
            newColourKeys[1] = new GradientColorKey(deletedColour, 1);
        }

        else                                // Partially deleted rope.
        {
            newColourKeys = new GradientColorKey[3];
            
            float colourKeyPosition = EasingFunctions.ApplyEase(1 - percentageDelete, EasingFunctions.Functions.OutQuad);


            newColourKeys[0] = new GradientColorKey(originalColour, 0);
            newColourKeys[1] = new GradientColorKey(originalColour, colourKeyPosition);
            newColourKeys[2] = new GradientColorKey(deletedColour, 1);
        }


        // Applying our changes.
        newGradient.colorKeys = newColourKeys;

        MyColourController.ApplyNewColourGradient(newGradient);
    }
    
    
    // This function is used to return the rope to its original colour after drawing.
    public void ReturnRopeOriginalColour()
    {
        MyColourController.TransitionRopeColour(MyRopeManager.initialDrawingColour, MyRopeManager.timeForDrawingRopeColourToDissapear);
    }


    // Property used to instantiate and reference our GameObject with the RopeDissapearScript script.
    RopeDissapearController _myRopeDisposal;
    RopeDissapearController MyRopeDisposal
    {
        get
        {
            if(_myRopeDisposal == null)
            {
                GameObject obj = Instantiate<GameObject>(MyRopeManager.ropeDissapearControllerPrefab, transform);
                _myRopeDisposal = obj.GetComponent<RopeDissapearController>();
                obj.SetActive(false);
            }

            return _myRopeDisposal;
        }
    }


    // Property to obtain the RopeDissapearTimer from our RopeController
    float RopeDissapearTime { get { return MyRopeManager.ropeDissapearTime; } }


    // Reference to our Rumble Manager.
    RumbleManager _myRumbleManager;
    RumbleManager MyRumbleManager
    {
        get
        {
            if (_myRumbleManager == null)
                _myRumbleManager = RumbleManager.instance;

            return _myRumbleManager;
        }
    }

    const int deleteRopeRumbleID = 3;



    // This function activates an animation that slowly makes the old Line dissapear.
    void EmptyLineRenderer()
    {
        // We activate the GameObject to which we transfer our old Rope Spline.
        if (MyRopeDisposal.gameObject.activeInHierarchy == false)
            MyRopeDisposal.gameObject.SetActive(true);

        // We clear any left over splines in that GameObject.
        MyRopeDisposal.MySplines.Spline.Clear();

        // We duplicate our Spline into our disposable GameObject.
        foreach (BezierKnot knot in MySplines.Spline.Knots)
        {
            MyRopeDisposal.MySplines.Spline.Add(knot);
        }

        // We set a timer for the dissapearance of the rope.
        MyRopeDisposal.SetTimer(RopeDissapearTime, (float) MySplines.Spline.Count / MyRopeManager.maxNumberOfKnots);

        // Finally, we set the dissapearing-rope's colour.
        MyRopeDisposal.SetRopeColour(MyLineRenderer.endColor);

        // We perform the Rumble that indicates the rope has been deleted.
        MyRumbleManager.StartRumble(deleteRopeRumbleID);

        // And we clean our main rope.
        ResetLineRenderer();
    }


    // This function empties the player's line without an animation.
    void ResetLineRenderer()
    {
        MyLineRenderer.positionCount = 0;
    }

    #endregion


    #region Line Sound
    AudioManager MyAudioManager { get { return AudioManager.instance; } }

    Sound ropeDrawingSound = null;  // The Sound class allows this script to access its AudioSource as well.

    // This function initializes the sound of the rope drawing.
    void PlayRopeDrawingSound()
    {
        if(myOrientation == Orientation.left)
            ropeDrawingSound = MyAudioManager.PlaySound("Rope Drawing Left", true);
        else
            ropeDrawingSound = MyAudioManager.PlaySound("Rope Drawing Right", true);

        ropeDrawingSound.pitch = 1;
    } 


    // This function manages the sound of the rope as feedback on its remaining lenght.
    void ConvertRopeLenghtToSound()
    {
        if(ropeDrawingSound == null)
        {
            PlayRopeDrawingSound();
        }
        
        float percentage = Target.PercentageOfRopeCompleted;
        float easedPercentage = EasingFunctions.ApplyEase(percentage, EasingFunctions.Functions.InQuad);

        float targetPitch = MyRopeManager.finalSoundPitch;

        ropeDrawingSound.source.pitch = Mathf.Lerp(1, targetPitch, easedPercentage);
    }


    // Used to stop the Rope Drawing Audio. The sound reference is also restarted.
    public void StopRopeDrawingSound()
    {
        MyAudioManager.StopSound(ropeDrawingSound.name);
        
        ropeDrawingSound = null;
    }


    // Used to activate the sound of the complete rope.
    public void PlayCompletedRopeAudio()
    {
        MyAudioManager.PlaySound("Rope Complete");
    }

    #endregion


    #region Weapons placement and activation

    // This dictionary stores all of the weapons currently active in our rope together
    // with their position (calculated in % of completion) in that same rope.
    Dictionary<float, Weapon> _weaponsInRopeDictionary;

    // A list with all the weapon locations in our rope.
    List<float> _myWeaponLocations;


    // Properties used to initialize our Dictionaries and Lists.
    Dictionary<float, Weapon> WeaponsInRopeDictionary
    {
        get
        {
            if (_weaponsInRopeDictionary == null)
                _weaponsInRopeDictionary = new();

            return _weaponsInRopeDictionary;
        }
        set
        {
            _weaponsInRopeDictionary = value;
        }
    }
    List<float> MyWeaponLocations
    {
        get
        {
            if (_myWeaponLocations == null)
                _myWeaponLocations = new();

            return _myWeaponLocations;
        }
        set
        {
            _myWeaponLocations = value;
        }
    }



    // This function is used by pulses to determine if they should activate any Weapons during
    // their movement. It returns an array with any weapons found between the input percentages.

        // This function should not be called to shoot any weapons.
        // That function is located in the ShootingRope script.
    public Weapon[] ActivatedWeaponsInPath
        (float initialPercentageCompletition, float finalPercentageCompletition)
    {
        // If our rope contains no weapons, we return an empty array inmediately.
        if (MyWeaponLocations.Count == 0)
            return new Weapon[0];


        int positionOfFirstWeaponInList = 0;
        bool notFoundThePosition = true;
        List<Weapon> activatedWeapons = new();


        // We determine which weapon is the first we have to check for activation.
        while (notFoundThePosition && positionOfFirstWeaponInList < MyWeaponLocations.Count)
        {
            // If the weapon's location is the first to be bigger than our initial position,
            // we want to check that weapon's activation.
            if(MyWeaponLocations[positionOfFirstWeaponInList] > initialPercentageCompletition)
                notFoundThePosition = false;

            else
                positionOfFirstWeaponInList++;
        }


        bool keepChecking = true;

        // We now check our first possible weapon. If it is activated, we also check the next weapon
        // until we check the last weapon in the List or we find a weapon that wasn't activated.
        while(keepChecking && positionOfFirstWeaponInList <= MyWeaponLocations.Count - 1)
        {
            if (MyWeaponLocations[positionOfFirstWeaponInList] < finalPercentageCompletition)
            {
                activatedWeapons.Add(WeaponsInRopeDictionary[MyWeaponLocations[positionOfFirstWeaponInList]]);
                positionOfFirstWeaponInList++;
            }
            else
                keepChecking = false;
        }


        return activatedWeapons.ToArray();
    }


    // This list is called the validation que. Every time the rope is completed, it needs to
    // validate the position of all of the rope's weapons whithin the que.
    List<Weapon> validationQue = new();


    // This function adds a weapon to our rope at a concrete point. It does NOT validate them.
    public void AddWeaponToValidationQue(Weapon weapon)
    {
        validationQue.Add(weapon);
    }


    // This function takes our weapon positions and evaluates where they are placed within the Rope.
    // This function should be called EVERY TIME the rope is completed, as it's lenght might change.
    public void ValidateNewWeapons()
    {
        foreach(Weapon weapon in validationQue)
        {
            SplineUtility.GetNearestPoint<Spline>(MySplines.Spline,
                transform.InverseTransformPoint(weapon.transform.position),
                out Unity.Mathematics.float3 nearest, out float t);

            // Clamping the value of t.
            if (t < 0 || t >= 1)
                t = Mathf.Clamp(t, 0, 0.999f);

            myRopeInfo.WeaponsInRope.Add(weapon);
            WeaponsInRopeDictionary.Add(t, weapon);
            MyWeaponLocations.Add(t);
            weapon.MyWeaponNode.State = WeaponNode.NodeState.readyToFight;
        }

        validationQue.Clear();
    }


    // This function is called when our rope is destroyed.
    // It disables all of our current active weapons.
    // It also resets all of our lists and dictionaries.
    public void ResetRopeWeapons()
    {
        if (myRopeInfo.state == RopeState.drawing)
        {
            // Disabling all of our weapons waiting for validation.
            foreach (Weapon weapon in validationQue)
            {
                weapon.MyWeaponNode.State = WeaponNode.NodeState.drawWaitForTarget;
            }
        }
        else
        {
            // Disabling all of our active weapons.
            foreach (Weapon weapon in WeaponsInRopeDictionary.Values)
            {
                weapon.MyWeaponNode.State = WeaponNode.NodeState.drawWaitForTarget;
            }
        }



        // Reseting all of our lists and dictionaries.
        validationQue.Clear();
        myRopeInfo.WeaponsInRope.Clear();
        WeaponsInRopeDictionary.Clear();
        MyWeaponLocations.Clear();
    }

    #endregion


    #region Pulse Methods

    // Our references to the pulse GameObjects, active or inactive.
    // A pulse GameObject should be disabled when it stops being used.
    // Pulses deactivate automatically when they reach the end of a rope.
    // They also automatically add/remove themselves from active/inactive pulses lists.
    [HideInInspector] public List<GameObject> inactivePulses = new();
    [HideInInspector] public List<GameObject> activePulses = new();


    // This function is used to obtain pulses to be acivated by our CreatePulse function.
    // It first checks if there are any inactive pulses availible.
    // If there aren't, it creates a new one, adding it to the pool of pulses.
    private GameObject ObtainPulseGameObject()
    {
        if (inactivePulses.Count == 0)
            return Instantiate<GameObject>(MyRopeManager.ropePulsePrefab, transform);

        else
            return inactivePulses[0];
    }


    // This function is called by an input event when players press the trigger.
    // It calls ObtainPulseGameObject and enables that pulse if the trigger is fully pressed.
    public void TryToCreatePulse(InputAction.CallbackContext context)
    {
        // We prevent multiple callings of the same function prior to releasing the trigger.
        // We also prevent pulses from being created in any state other than full.
        if (hasTriggeredBeenReleased == false || myRopeInfo.state != RopeState.full)
            return;


        // We first check that our trigger is pushed to the maximum.
        if (myOrientation == Orientation.left)
        {
            if (StatesManager.instance.InputActions.FightingPosition.
                ShootLeftRope.ReadValue<float>() != 1)
            {
                return;
            }
        }
        else
        {
            if (StatesManager.instance.InputActions.FightingPosition.
                ShootRightRope.ReadValue<float>() != 1)
            {
                return;
            }
        }


        // We check if there is enough energy to perform the action.
        if (!Energy_Controller.CanAffordEnergySpend(EnergyController.energyExpense.normalPulse))
            return;

        Energy_Controller.SpendEnergy(EnergyController.energyExpense.normalPulse);


        hasTriggeredBeenReleased = false;

        ObtainPulseGameObject().SetActive(true);
    }


    // This bool is used to prevent a CreatePulse action from been called twice before players
    // release that trigger.
    bool hasTriggeredBeenReleased = true;

    // This method is called when players release the trigger.
    private void ResetTrigger(InputAction.CallbackContext context)
    {
        hasTriggeredBeenReleased = true;
    }


    // This function is used to disable all pulse GameObjects currently active.
    // It is used when the rope state changes while pulses are still active.
    public void ResetAllPulses()
    {
        // If there are no active pulses at the moment, we return.
        if (activePulses.Count == 0)
            return;

        for(int i = 0; i < activePulses.Count; i++)
        {
            activePulses[i].SetActive(false);
        }
    }

    #endregion
}
