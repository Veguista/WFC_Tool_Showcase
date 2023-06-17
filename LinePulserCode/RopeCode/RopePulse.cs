using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class RopePulse: MonoBehaviour
{
    // A pulse's intensity affects the intensity of the weapon it triggers.
    [SerializeField] [Range(0.1f, 10)] float initialPulseIntensity = 1;
    [HideInInspector] public float pulseIntensity;

    // A weapon's speed determines how fast it travels through a rope.
    // The value is meassured in (Nodes triggered / second).
    [SerializeField] [Range(0.1f, 10)] float initialPulseVelocity = 1;
    [HideInInspector] public float pulseVelocity;


    // Information regarding the lenght of the rope this pulse travels, and the lenght travelled by the node so far.
    int ropeLenght = 0;             // (in nodes)
    float ropeLenghtTraversed = 0;  // (in nodes)


    // A property used to determine how far along the rope our pulse is.
    float PercentageCompletion { get { return ropeLenghtTraversed / ropeLenght; } }


    // Property used to initialize the myRope variable.
    ShootingRope _myRope;
    ShootingRope MyRope
    {
        get
        {
            if(_myRope == null)
            {
                if (transform.parent.TryGetComponent<ShootingRope>(out ShootingRope result))
                    _myRope = result;
                else
                {
                    Debug.LogError("Rope Pulse couldn't find a ShootingRope script in it's parent." +
                        "\nRope Pulse needs to find a ShootingRope script to be able to follow it's path.");
                    return null;
                }
            }

            return _myRope;
        }
    }


    private void OnEnable()
    {
        // Handling the pulses' pulling system.
        if (MyRope.inactivePulses.Contains(this.gameObject))
            MyRope.inactivePulses.Remove(this.gameObject);

        MyRope.activePulses.Add(this.gameObject);


        // Getting the rope's information.
        ropeLenght = MyRope.myRopeInfo.WeaponsInRope.Count;


        // Placing the pulse at the beginning of the rope.
        transform.localPosition = MyRope.MySplines.Spline.EvaluatePosition(0);
        pulseIntensity = initialPulseIntensity;
        pulseVelocity = initialPulseVelocity;
    }

    private void OnDisable()
    {
        // Handling the pulses' pulling system.
        MyRope.activePulses.Remove(this.gameObject);
        MyRope.inactivePulses.Add(this.gameObject);

        // Resetting values regarding the lenght of the rope.
        ropeLenght = 0;
        ropeLenghtTraversed = 0;
    }



    private void Update()
    {
        UpdatePulsePositionAndTriggerWeapons();
    }



    void UpdatePulsePositionAndTriggerWeapons()
    {
        // We first store our percentage completition before altering it
        // (to check for activated weapons later)
        float oldPercentageCompletition = PercentageCompletion;

        // We move our pulse.
        ropeLenghtTraversed += pulseVelocity * Time.deltaTime;

        if (ropeLenghtTraversed > ropeLenght)   // We clamp our completition value.
            ropeLenghtTraversed = ropeLenght;


        // We use the corrected Position and Tangent to account for the (possibly) altered lenght of the rope.
        transform.localPosition = MyRope.EvaluateRopeCorrectedPosition(PercentageCompletion);
        transform.localRotation = MyRope.EvaluateRopeCorrectedTangent(PercentageCompletion);


        // We then check if our pulse activated any weapons during it's movement.
        Weapon[] activatedWeapons =
            MyRope.ActivatedWeaponsInPath(oldPercentageCompletition, PercentageCompletion);

        if (activatedWeapons.Length > 0)
        {
            foreach (Weapon weapon in activatedWeapons)
            {
                weapon.ActivateWeapon(this);
            }
        }


        // Finally, we check if our pulse has finished travelling the rope.
        // If it has, it self-destroys.
        if (PercentageCompletion >= 1)
            this.gameObject.SetActive(false);
    }
}
