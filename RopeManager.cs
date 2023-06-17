using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeManager : MonoBehaviour
{
    #region Static reference
    public static RopeManager instance;

    void InitializeStaticReference()
    {
        if (instance == null)
            instance = this;
        else
        {
            Debug.LogError("Two different Rope Managers scripts are active at the same time. " +
                "\nYou need to destroy one of them. Automatically destroying this one.");
            Destroy(this);
        }
    }

    #endregion

    private void Awake()
    {
        InitializeStaticReference();
    }


    #region Variables

    [Header("Drawing Variables")]
    [Range(0, 50)] public int maxNumberOfKnots = 20;
    [Range(0, 10)] public float distanceBetweenKnots = 2;
    [Range(0, 5)] public float ropeDissapearTime = 1;



    [Space]
    [Header("Prefabs")]
    public GameObject ropePulsePrefab;
    public GameObject ropeDissapearControllerPrefab;
    


    [Space]
    [Header("Movement Variables")]
    // The acceleration the target will experience (m/s^2).
    // [Range(0, 100)] public float targetAcceleration = 50; [DEPRECATED]

    // The maximum speed the target will have while moving.
    [Range(0, 50)] public float targetMaxSpeed = 10;

    // The way the target's speed will decay as it completes the rope.
    // Range[0, 1] - X is number of length of the rope. [0 ==  0 lenght; 1 == max Lenght]
    [SerializeField] public AnimationCurve targetDecelerationCurve;



    [Space]
    [Header("Input variables")]
    // Ammount of time player's need to hold the direction in the stick to start drawing a rope.
    [Range(0, 3)] public float holdTimeToDeleteRope = 0.5f;

    // Angle range (in the left or the right) which allows players to restart the rope.
    [Range(0, 180)] public int ropeStartAngleRange = 30;

    // Angle range (in the left or the right) which allows players to restart the rope.
    [Range(0, 1)] public float minimumStickMagnitudeWhileDrawing = 0.4f;

    // Ammount of time that needs to pass for the rope drawing tutorial to reappear.
    [Range(0, 10)] public float ropeDrawingTutorialReappearingTime = 5f;



    [Space]
    [Header("Colour variables")]
    public Color initialDrawingColour;
    public Color finalDrawingColour;
    public AnimationCurve colorVariationCurve;

    [Range(0f, 3f)] public float timeForDrawingRopeColourToDissapear = 0.5f;
    public Color deletedRopeColour;



    [Space]
    [Header("Sound variables")]
    [Range(1f, 5f)] public float finalSoundPitch;



    [Space]
    [Header("Rope References")]
    public DrawingRope leftDrawingRope;
    public DrawingRope rightDrawingRope;

    [Space]

    public ShootingRope leftShootingRope;
    public ShootingRope rightShootingRope;



    [Space]
    [Header("Shooting Mode Variables")]
    
    // We multiply the size of our Weapon prefabs by this ammount when displaying them in our inventory.
    [Range(0.1f, 10f)] public float weaponSizeInventoryMultiplier = 1;
    public AnimationCurve ropeLenghtMultiplierBasedOnNumberOfWeapons;

    #endregion
}
