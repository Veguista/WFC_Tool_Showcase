using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class ShootingRope : MonoBehaviour
{
    public Orientation myOrientation = Orientation.right;

    public RopeInfo myRopeInfo;


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


    PlayerInputActions InputActions { get { return StatesManager.instance.InputActions; } }
    EnergyController Energy_Controller { get { return EnergyController.instance; } }
    AnimationCurve RopeLenghtAnimationCurve { get { return MyRopeManager.ropeLenghtMultiplierBasedOnNumberOfWeapons; } }


    void SubscribeToInputEvents()
    {
        if (myOrientation == Orientation.left)
        {
            InputActions.FightingPosition.ShootLeftRope.performed += TryToCreatePulse;
            InputActions.FightingPosition.ReleaseLeftTrigger.performed += ResetTrigger;
        }
        else
        {
            InputActions.FightingPosition.ShootRightRope.performed += TryToCreatePulse;
            InputActions.FightingPosition.ReleaseRightTrigger.performed += ResetTrigger;
        }
    }


    private void Awake()
    {
        SubscribeToInputEvents();
        SubscribeToInventoryToggleEvent();
    }


    #region Toggling Inventory mode.

    // This function is called when we change from the drawing mode into the fighting mode.
    // It manages all actions that need to happen during this transition.
    void ChangeIntoFightingMode()
    {
        // We first copy the RopeInfo from the drawing rope.
        if (myOrientation == Orientation.left)
            myRopeInfo = MyRopeManager.leftDrawingRope.myRopeInfo;
        else
            myRopeInfo = MyRopeManager.rightDrawingRope.myRopeInfo;

        // We then paint our Rope.
        PaintRope();

        // We spawn the weapons prefabs needed and place them in our Rope.
        PlaceWeaponsInRope();
    }


    // This function is called when we change from the fighting mode into the drawing mode.
    void ChangeIntoDrawingMode()
    {
        ResetAllPulses();
        DestroyWeapons();
    }


    // This function is subscribed to and called by an event in StatesController.
    // The event activates when players press the inventory button.
    void ToggleInventory(bool ropeDrawingOn)
    {
        if (ropeDrawingOn)
            ChangeIntoDrawingMode();
        else
            ChangeIntoFightingMode();
    }


    void SubscribeToInventoryToggleEvent()
    {
        StatesManager.instance.OnRopeDrawToggle += ToggleInventory;
    }

    #endregion

    #region Painting the Rope

    LineRenderer _myLineRenderer;
    LineRenderer MyLineRenderer
    {
        get
        {
            if (_myLineRenderer == null)
            {
                if (gameObject.TryGetComponent<LineRenderer>(out LineRenderer rendererOut))
                {
                    _myLineRenderer = rendererOut;
                    _myLineRenderer.useWorldSpace = false;
                }

                else
                    Debug.LogError("All ropes must have a LineRenderer component attached to them.");
            }

            return _myLineRenderer;
        }
    }


    // Property used to reference our SplineContainer.
    SplineContainer _mySplines;
    public SplineContainer MySplines
    {
        get
        {
            if (_mySplines == null)
            {
                if (TryGetComponent<SplineContainer>(out SplineContainer container))
                    _mySplines = container;

                else
                    Debug.LogError("Could not find a Spline attached to " + transform.gameObject.name + ".");
            }
            
            return _mySplines;
        }
    }


    // Number of points to be found in the LineRenderer. Also the number of times the Spline will be evaluated.
    const int numberOfLineRendererPoints = 15;


    // This function sets the points in the rope's Line Renderer.
    void PaintRope()
    {
        // We first check whether we have weapons to place. If we don't, we reduce our line to nothing.
        if (myRopeInfo.state == RopeState.empty)
        {
            MyLineRenderer.positionCount = 0;
            return;
        }


        MyLineRenderer.positionCount = numberOfLineRendererPoints;

        Vector3[] linePoints = new Vector3[numberOfLineRendererPoints];

        for (int i = 0; i < numberOfLineRendererPoints; i++)
        {
            float percentagePositionOfSpline = (i + 1f) / numberOfLineRendererPoints;

            float correctedPercentage = 
                percentagePositionOfSpline * RopeLenghtAnimationCurve.Evaluate(myRopeInfo.WeaponsInRope.Count);

            Vector3 newPosition = MySplines.Spline.EvaluatePosition(correctedPercentage);

            linePoints[i] = newPosition;
        }


        MyLineRenderer.SetPositions(linePoints);
    }


    #endregion

    #region Managing Weapons

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

        set { _weaponsInRopeDictionary = value; }
    }
    List<float> MyWeaponLocations
    {
        get
        {
            if (_myWeaponLocations == null)
                _myWeaponLocations = new();

            return _myWeaponLocations;
        }

        set {  _myWeaponLocations = value; }
    }


    // Property to create and manage our GameObject folder for our rope Weapons.
    Transform _weaponFolder;
    Transform WeaponFolder
    {
        get
        {
            if (_weaponFolder == null)
            {
                _weaponFolder = Instantiate<GameObject>(new GameObject(), transform).transform;
                _weaponFolder.name = "Rope Weapons";
            }

            return _weaponFolder;
        }
    }


    // This function is called when we switch to fighting mode to evaluate where the Weapons should be placed in the rope.
    void PlaceWeaponsInRope()
    {
        float ropeSubdivisions = myRopeInfo.WeaponsInRope.Count;

        if (ropeSubdivisions == 0)
            return;

        for(int i = 1; i <= ropeSubdivisions; i++)
        {
            float percentagePositionInRope = i / ropeSubdivisions;


            // Clamping the value of percentagePositionInRope.
            if (percentagePositionInRope < 0 || percentagePositionInRope >= 1)
                percentagePositionInRope = Mathf.Clamp(percentagePositionInRope, 0, 0.999f);


            // We correct our percentage value using our Animation Curve.
            float correctedPercentage =
                    percentagePositionInRope * RopeLenghtAnimationCurve.Evaluate(myRopeInfo.WeaponsInRope.Count);


            // Instantiating a Weapon GameObject at the position.
            GameObject weaponGameObject = 
                GameObject.Instantiate(myRopeInfo.WeaponsInRope[i - 1].myWeaponStats.weaponPrefab, WeaponFolder);

            weaponGameObject.transform.localPosition = MySplines.Spline.EvaluatePosition(correctedPercentage);


            // Adding our Weapon to our Lists and Dictionaries.
            if (weaponGameObject.TryGetComponent<Weapon>(out Weapon weapon))
            {
                WeaponsInRopeDictionary.Add(correctedPercentage, weapon);
                MyWeaponLocations.Add(correctedPercentage);
            }
            else
                Debug.LogError("Trying to Add a Weapon prefab (" + weaponGameObject.name + ") but the prefab does not" +
                    "contain a Weapon script.");
        }
    }


    // This function is called when we switch to drawing mode.
    // It destroys all weapon GameObjects so that we can spawn them afterwars.
    void DestroyWeapons()
    {
        foreach(Weapon w in WeaponsInRopeDictionary.Values)
        {
            Destroy(w.gameObject);
        }

        // Resetting our Lists and Dictionaries.
        MyWeaponLocations = null;
        WeaponsInRopeDictionary = null;
    }


    // This function is used by pulses to determine if they should activate any Weapons during
    // their movement. It returns an array with any weapons found between the input percentages.
    public Weapon[] ActivatedWeaponsInPath
        (float initialPercentageCompletition, float finalPercentageCompletition)
    {
        // If our rope contains no weapons, we return an empty array inmediately.
        if (MyWeaponLocations.Count == 0)
            return new Weapon[0];


        // We set up corrected values for both our initial and final percentages.
        float correctedInitialPercentage = 
            initialPercentageCompletition * RopeLenghtAnimationCurve.Evaluate(myRopeInfo.WeaponsInRope.Count);
        float correctedFinalPercentage =
            finalPercentageCompletition * RopeLenghtAnimationCurve.Evaluate(myRopeInfo.WeaponsInRope.Count);


        int positionOfFirstWeaponInList = 0;
        bool notFoundThePosition = true;
        List<Weapon> activatedWeapons = new();


        // We determine which weapon is the first we have to check for activation.
        while (notFoundThePosition && positionOfFirstWeaponInList < MyWeaponLocations.Count)
        {
            // If the weapon's location is the first to be bigger than our initial position,
            // we want to check that weapon's activation.
            if (MyWeaponLocations[positionOfFirstWeaponInList] > correctedInitialPercentage)
                notFoundThePosition = false;

            else
                positionOfFirstWeaponInList++;
        }


        bool keepChecking = true;

        // We now check our first possible weapon. If it is activated, we also check the next weapon
        // until we check the last weapon in the List or we find a weapon that wasn't activated.
        while (keepChecking && positionOfFirstWeaponInList <= MyWeaponLocations.Count - 1)
        {
            if (MyWeaponLocations[positionOfFirstWeaponInList] < correctedFinalPercentage)
            {
                activatedWeapons.Add(WeaponsInRopeDictionary[MyWeaponLocations[positionOfFirstWeaponInList]]);
                positionOfFirstWeaponInList++;
            }
            else
                keepChecking = false;
        }


        return activatedWeapons.ToArray();
    }


    #endregion

    #region Shooting Pulses

    // Our references to the pulse GameObjects, active or inactive.
    // A pulse GameObject should be disabled when it stops being used.
    // Pulses deactivate automatically when they reach the end of a rope.
    // They also automatically add/remove themselves from active/inactive pulses lists.
    [HideInInspector] public List<GameObject> inactivePulses = new();
    [HideInInspector] public List<GameObject> activePulses = new();


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
            if (StatesManager.instance.InputActions.FightingPosition.ShootLeftRope.ReadValue<float>() >= 0.95f)
                return;
        }
        else
        {
            if (StatesManager.instance.InputActions.FightingPosition.ShootRightRope.ReadValue<float>() >= 0.95f)
                return;
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

    // This function is used to obtain pulses to be activated by our CreatePulse function.
    // It first checks if there are any inactive pulses availible.
    // If there aren't, it creates a new one, adding it to the pool of pulses.
    private GameObject ObtainPulseGameObject()
    {
        if (inactivePulses.Count == 0)
            return Instantiate<GameObject>(MyRopeManager.ropePulsePrefab, transform);

        else
            return inactivePulses[0];
    }


    // This method is called when players release the trigger.
    private void ResetTrigger(InputAction.CallbackContext context)
    {
        hasTriggeredBeenReleased = true;
    }


    // This function is used to disable all pulse GameObjects currently active.
    public void ResetAllPulses()
    {
        // If there are no active pulses at the moment, we return.
        if (activePulses.Count == 0)
            return;

        for (int i = 0; i < activePulses.Count; i++)
        {
            activePulses[i].SetActive(false);
        }
    }


    // This function is used by pulses to Evaluate our rope while accounting for a possible lenght alteration.
    public Vector3 EvaluateRopeCorrectedPosition(float percentage)
    {
        if(percentage < 0 || percentage > 1)
        {
            Debug.LogError("Percentage of completion value must be in the range [0,1]." +
                "\nCannot evaluate the rope's position at t = " + percentage + " .");
            return Vector3.zero;
        }

        float correctedPercentage = percentage * RopeLenghtAnimationCurve.Evaluate(myRopeInfo.WeaponsInRope.Count);

        Vector3 result = MySplines.Spline.EvaluatePosition(correctedPercentage);

        return result;
    }


    // This function is used by pulses to Evaluate our rope while accounting for a possible lenght alteration.
    public Quaternion EvaluateRopeCorrectedTangent(float percentage)
    {
        if (percentage < 0 || percentage > 1)
        {
            Debug.LogError("Percentage of completion value must be in the range [0,1]." +
                "\nCannot evaluate the rope's tangent at t = " + percentage + " .");
            return Quaternion.identity;
        }

        float correctedPercentage = percentage * RopeLenghtAnimationCurve.Evaluate(myRopeInfo.WeaponsInRope.Count);

        Quaternion result = Quaternion.Euler(MySplines.Spline.EvaluateTangent(correctedPercentage));

        return result;
    }

    #endregion
}
