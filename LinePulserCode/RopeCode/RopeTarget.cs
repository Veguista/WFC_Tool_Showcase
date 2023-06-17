using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class RopeTarget : MonoBehaviour
{
    // The transform to which we are sending our rotations to then follow with a SecondOrderTransform.
    [SerializeField] Transform unsmooothedTarget;

    [SerializeField] TutorialAlphaController myTutorialAlpha;


    // Property to get and set the state of the target.
    public enum TargetState { readyToDraw, drawing, complete }
    TargetState state = TargetState.readyToDraw;
    public TargetState State
    {
        get { return state; }

        set
        {
            // If we are not changing the value of our state, we return.
            if (state == value)
                return;

            // The state that we are leaving.
            switch (state)
            {
                case TargetState.readyToDraw:
                    {

                        break;
                    }
                case TargetState.drawing:
                    {
                        // We disable all aspects of our target if we stop drawing.
                        MyRb.velocity = Vector3.zero;
                        MyRenderer.enabled = false;
                        MySecondOrderTransform.enabled = false;

                        // Reset rope timer.


                        // We return the Rope to its original colour.
                        MyRope.ReturnRopeOriginalColour();

                        // We stop the Rope Drawing Sound.
                        MyRope.StopRopeDrawingSound();

                        break;
                    }
                case TargetState.complete:
                    {
                        // We remove any Pulses from our rope.
                        MyRope.ResetAllPulses();
                        break;
                    }
            }

            // The state that we are entering.
            switch (value)
            {
                case TargetState.readyToDraw:
                    {
                        MyRope.ResetRopeWeapons();
                        MyRope.EmptySpline();

                        // Reseting our power ups.
                        if(PowerUpManager.instance != null) // This check is in case that the player has not unlocked the PowerUps yet.
                        {
                            if (MyRope.myOrientation == Orientation.left)
                                PowerUpManager.instance.ResetLeftRopePowerUps();
                            else
                                PowerUpManager.instance.ResetRightRopePowerUps();
                        }


                        extraKnots = 0;         // Reseting the number of extraKnots our rope can have.

                        transform.localPosition = Vector3.zero;

                        break;
                    }
                case TargetState.drawing:
                    {
                        MyRope.myRopeInfo.state = RopeState.drawing;

                        MyRenderer.enabled = true;
                        MySecondOrderTransform.enabled = true;

                        if (MyRope.myOrientation == Orientation.left)
                            MySecondOrderTransform.followTransform.localRotation
                                = Quaternion.Euler(0, -90, 0);
                        else
                            MySecondOrderTransform.followTransform.localRotation
                                = Quaternion.Euler(0, 90, 0);

                        break;
                    }
                case TargetState.complete:
                    {
                        MyRope.myRopeInfo.state = RopeState.full;

                        // We validate all weapons in our rope.
                        MyRope.ValidateNewWeapons();


                        // We play a cute sound (SFX).
                        MyRope.PlayCompletedRopeAudio();

                        break;
                    }
            }

            state = value;
        }
    }


    #region Properties

    MeshRenderer _myRenderer;
    MeshRenderer MyRenderer
    {
        get
        {
            if (_myRenderer == null)
                _myRenderer = GetComponent<MeshRenderer>();

            return _myRenderer;
        }
    }


    DrawingRope _myRope;
    public DrawingRope MyRope
    {
        get
        {
            if (_myRope == null)
                _myRope = transform.parent.GetComponent<DrawingRope>();

            return _myRope;
        }
    }


    RopeManager _myRopeManager;
    RopeManager MyRopeManager
    {
        get
        {
            if (_myRopeManager == null)
                _myRopeManager = MyRope.MyRopeManager;

            return _myRopeManager;
        }
    }


    Rigidbody _myRb;
    Rigidbody MyRb
    {
        get
        {
            if (_myRb == null)
                _myRb = GetComponent<Rigidbody>(); ;

            return _myRb;
        }
    }


    SecondOrderTransform _mySecondOrderTransform;
    SecondOrderTransform MySecondOrderTransform
    {
        get 
        {
            if (_mySecondOrderTransform == null)
                _mySecondOrderTransform = GetComponent<SecondOrderTransform>();

            return _mySecondOrderTransform;
        }
    }


    Timer _myTutorialTimer;
    Timer MyTutorialTimer
    {
        get
        {
            if (_myTutorialTimer == null)
                _myTutorialTimer = new Timer(MyRopeManager.ropeDrawingTutorialReappearingTime);

            return _myTutorialTimer;
        }
    }

    #endregion


    // Property to reduce repeated oversized code when referencing InputActions in StatesController.
    PlayerInputActions InputActions { get { return StatesManager.instance.InputActions; } }

    void SubscribeToInputEvents()
    {
        if (MyRope.myOrientation == Orientation.left)
            InputActions.RopeDrawing.ReleaseLeftJoystick.performed += ReleaseJoystick;
        else
            InputActions.RopeDrawing.ReleaseRightJoystick.performed += ReleaseJoystick;
    }


    // Property used to reference our Last Knot in our rope.
    Vector3 LastKnotPosition
    {
        get 
        {
            Vector3 lastKnotPositionInParent = MyRope.MySplines.Spline[MyRope.MySplines.Spline.Count - 1].Position;
            return lastKnotPositionInParent; 
        }
    }


    private void Awake()
    {
        SubscribeToInputEvents();
    }


    private void Update()
    {
        // Precaution to avoid doing anything if we are not in the correct Action Map.
        if (StatesManager.instance.PlayerInput.currentActionMap.name != "RopeDrawing")
            return;
        

        if (State == TargetState.complete)
        {
            CheckDeletingRope();
            return;
        }

        // Updating our tutorial timer and activating it if needed. It resets if we ever start drawing.
        MyTutorialTimer.Update();

        if (MyTutorialTimer.IsComplete && !myTutorialAlpha.isVisible)
            myTutorialAlpha.Appear();


        if (State == TargetState.readyToDraw && CanBeginToDrawRope())
        {
            State = TargetState.drawing;

            hasJoystickBeenReleased = false;

            Move(Time.deltaTime);

            return;
        }


        if (State == TargetState.drawing)
        {
            if (IsStickMagnitudeMoreThanMinimum())
                MoveTarget();

            else
                State = TargetState.readyToDraw;
        }
    }


    #region Movement Properties
    // Variable values located in the RopeController.
    float TargetMaxSpeed { get { return MyRopeManager.targetMaxSpeed; } }
    AnimationCurve TargetDeccelerationCurve { get { return MyRopeManager.targetDecelerationCurve; } }



        // Properties and variables related to the ropes lenght.
    // We call it compound because it contains the extraKnots.
    public float CompoundMaxRopeLenghtInKnots
    {
        get
        {
            int normalMaxNumberKnots = MyRopeManager.maxNumberOfKnots;

            if (normalMaxNumberKnots <= 1)
                Debug.LogError("maxNumberOfKnots must always be bigger than 1.");
            
            int maxNumberOfKnots = normalMaxNumberKnots - 1 + extraKnots;
            return maxNumberOfKnots;
        }
    }

    public float CurrentRopeLenghtInKnots
    {
        get
        {
            int numberOfKnots = MyRope.MySplines.Spline.Count;
            float lastKnotToTargetDistance = (transform.localPosition - LastKnotPosition).magnitude;
            return numberOfKnots + lastKnotToTargetDistance / MyRopeManager.distanceBetweenKnots;
        }
    }

    public float PercentageOfRopeCompleted
    {
        get
        {
            return CurrentRopeLenghtInKnots / CompoundMaxRopeLenghtInKnots;
        }
    }

    public int extraKnots = 0;  // ExtraKnots is used by power-ups to amplify how long our rope can be.

    public void IncreaseExtraKnots(int numberOfExtraKnots)
    {
        extraKnots += numberOfExtraKnots;
    }
    #endregion


    #region Movement Methods
    // This function performs all of the movement functions.
    void MoveTarget()
    {
        Vector2 forceDirection = ObtainStickValues();

        // Setting the rotation of our character.
        unsmooothedTarget.LookAt(transform.parent.TransformPoint(
            new Vector3(forceDirection.x, 0, forceDirection.y)), transform.parent.up);

        float deccelerationMultiplier =
            TargetDeccelerationCurve.Evaluate(CurrentRopeLenghtInKnots / CompoundMaxRopeLenghtInKnots);

        MyRb.velocity = transform.forward * TargetMaxSpeed * deccelerationMultiplier;


        CheckForKnotAddition();
    }

    void Move(float time)
    {
        Vector2 forceDirection = ObtainStickValues();

        // Setting the rotation of our character.
        unsmooothedTarget.LookAt(transform.parent.TransformPoint(
            new Vector3(forceDirection.x, 0, forceDirection.y)), transform.parent.up);

        float deccelerationMultiplier =
            TargetDeccelerationCurve.Evaluate(CurrentRopeLenghtInKnots / CompoundMaxRopeLenghtInKnots);

        MyRb.velocity = transform.forward * TargetMaxSpeed * deccelerationMultiplier;


        CheckForKnotAddition();
    }


    // This function returns a Vector2 with the correct (left / right) stick's values.
    Vector2 ObtainStickValues()
    {
                // (Left Rope).
        if (MyRope.myOrientation == Orientation.left)
            return RopeActions.DrawLeftRope.ReadValue<Vector2>();

        else    // (Right Rope).
            return RopeActions.DrawRightRope.ReadValue<Vector2>();
    }


    // This function checks if we are far away enough from the LastKnot to have to create a new knot.
    // It also checks if our rope has surpased its maxLenght. If it has, we change the TargetState.
    void CheckForKnotAddition()
    {
        Vector3 distanceFromLastKnot = transform.localPosition - LastKnotPosition;
        float maxKnotSeparation = MyRopeManager.distanceBetweenKnots;

        // If we are too far away from the Last Knot.
        if (distanceFromLastKnot.magnitude >= maxKnotSeparation)
        {
            Vector3 localKnotPosition = LastKnotPosition + distanceFromLastKnot.normalized * maxKnotSeparation;


            bool isRopeComplete = MyRope.AddBezierKnot(localKnotPosition);

            if (isRopeComplete)
            {
                State = TargetState.complete;
                return;
            }
        }
    }
    #endregion


    #region Check Methods
    // It checks that the sticks magnitude doesn't drop below the minimum magnitude.
    bool IsStickMagnitudeMoreThanMinimum()
    {
        float magnitude;
        

        if (MyRope.myOrientation == Orientation.left)
            magnitude = RopeActions.DrawLeftRope.ReadValue<Vector2>().magnitude;

        else    // Right Rope.
            magnitude = RopeActions.DrawRightRope.ReadValue<Vector2>().magnitude;


        if (magnitude >= MinimumStickMagnitudeWhileDrawing)
            return true;
        else
            return false;
    }


        // Checking if and deleting the rope.

    float HoldTimeToDeleteRope { get { return MyRopeManager.holdTimeToDeleteRope; } }

    // Timer used to keep track of how much time players need to hold a button to delete a rope.
    Timer _deleteRopeTimer;
    Timer DeleteRopeTimer
    {
        get
        {
            if (_deleteRopeTimer == null)
                _deleteRopeTimer = new Timer(HoldTimeToDeleteRope);

            return _deleteRopeTimer;
        }
    } 


    // This function checks if players are trying to delete a rope.
    void CheckDeletingRope()
    {
        // This function should be called when the rope is complete.
        if (State != TargetState.complete)
            return;

        string deletingRopeSoundName;
        float shoulderButtonValue;

        if (MyRope.myOrientation == Orientation.left)
        {
            deletingRopeSoundName = "Rope Deleting Left";
            shoulderButtonValue = InputActions.RopeDrawing.DeleteLeftRope.ReadValue<float>();
        }
        else // Right rope.
        {
            deletingRopeSoundName = "Rope Deleting Right";
            shoulderButtonValue = InputActions.RopeDrawing.DeleteRightRope.ReadValue<float>();
        }


        bool tryingToDeleteRope = false;

        if (shoulderButtonValue == 1)
            tryingToDeleteRope = true;


        // We don't need to do anything if the rope-delete process wasn't and isn't started.
        if (tryingToDeleteRope == false && DeleteRopeTimer.Time == 0)
            return;

        // If we were trying to delete the rope and we JUST STOPPED.
        if (tryingToDeleteRope == false) // IMPLIES (DeleteRopeTimer.Time != 0);
        {
            // Stop the Audio for deleting the rope.
            AudioManager.instance.StopSound(deletingRopeSoundName);

            DeleteRopeTimer.Reset();

            MyRope.DeleteRopeDisplay(DeleteRopeTimer.PercentageComplete);

            return;
        }


            // We are DELETING THE ROPE.
        
        // If we are JUST STARTING to delete the rope, we play the sound.
        if(DeleteRopeTimer.Time == 0)
            AudioManager.instance.PlaySound(deletingRopeSoundName);


        // Updating the DeleteRopeTimer.
        DeleteRopeTimer.Update();            

        // Setting the rope's colour.
        MyRope.DeleteRopeDisplay(DeleteRopeTimer.PercentageComplete);


        // Checking if the rope has been completely deleted.
        if(DeleteRopeTimer.IsComplete)
        {
            State = TargetState.readyToDraw;
            DeleteRopeTimer.Reset();
        }
    }


    // This function checks that players are doing all the correct actions to draw the rope.
    bool CanBeginToDrawRope()
    {
        // This function should not be called at any other moment.
        if (State != TargetState.readyToDraw)
            return false;

        // If our Joystick wasn't released after drawing. [CURRENTLY NOT AT USE, BUT COULD BE USEFUL]
        if (!hasJoystickBeenReleased)
            return false;

        // If our stick's magnitude is bigger than the minimum to start drawing.
        if (ObtainStickValues().magnitude <= MinimumStickMagnitudeWhileDrawing)
            return false;

        // If our joystick is not in the correct angle space.
        if (!IsAngleInRangeForDrawing())
            return false;


        // We reset the ammount of time necessary for the Tutorial to reappear.
        MyTutorialTimer.Reset();


        return true;
    }

    #endregion


    #region Collision with Nodes
    private void OnTriggerEnter(Collider collider)
    {
        // Checking that our collision happened with an object on layer 11 (WeaponNodes).
        if (collider.gameObject.layer == 11)
        {
            Weapon newActiveWeapon = 
                collider.gameObject.GetComponent<WeaponNode>().EnableWeapon(MyRope);
            MyRope.AddWeaponToValidationQue(newActiveWeapon);
        }

        // Checking that our collision happened with an object on layer 12 (PowerUpNodes).
        else if (collider.gameObject.layer == 12)
        {
            // We store the gameObject to reactivate it in case that we reset this rope.
            PowerUp collidedPowerUp = null;

            if (collider.gameObject.TryGetComponent<PowerUp>(out PowerUp myOut))
                collidedPowerUp = myOut;
            else
                Debug.LogError("Collider " + collider.name + " is located in layer 12 " +
                    "(PowerUpNodes) but does not contain a PowerUpNode child script.");


            // Adding our PowerUp to our PowerUpManager list.
            if (MyRope.myOrientation == Orientation.left)
                PowerUpManager.instance.AddPowerUpToLeftRope(collidedPowerUp);
            else
                PowerUpManager.instance.AddPowerUpToRightRope(collidedPowerUp);


            // We activate the effects of the power up.
            myOut.ActivatePowerUp(this);

            // And deactivate it temporarily.
            collidedPowerUp.gameObject.SetActive(false);
        }
    }

    #endregion


    #region Input

    // Variable values located in the RopeController.
    int RopeStartAngleRange { get { return MyRopeManager.ropeStartAngleRange; }}
    float MinimumStickMagnitudeWhileDrawing { get { return MyRopeManager.minimumStickMagnitudeWhileDrawing; }}

    // This property is used to access the RopeDrawing action map in our StatesController.
    PlayerInputActions.RopeDrawingActions RopeActions { get { return StatesManager.instance.InputActions.RopeDrawing; }}


    // This function checks that the player stick is inside the accepted angle range.
    bool IsAngleInRangeForDrawing()
    {
        if (MyRope.myOrientation == Orientation.left)
        {
            Vector2 axis = RopeActions.DrawLeftRope.ReadValue<Vector2>();

                                                // Our angle range is twice the size of the max valid angle from (-1,0) to our axis.
            if (Vector2.Angle(axis, Vector2.left) <= RopeStartAngleRange / 2)
                return true;
            else
                return false;
        }

        else    // Right rope.
        {
            Vector2 axis = RopeActions.DrawRightRope.ReadValue<Vector2>();

                                                // Our angle range is twice the size of the max valid angle from (1,0) to our axis.
            if (Vector2.Angle(axis, Vector2.right) <= RopeStartAngleRange / 2)
                return true;
            else
                return false;
        }
    }


    // This bool is used to prevent CheckInputStateBeforeDrawing from resetting the rope
    // before players have at least released the joystick for a frame.
    bool hasJoystickBeenReleased = true;

    private void ReleaseJoystick(InputAction.CallbackContext context)
    {
        hasJoystickBeenReleased = true;
    }

    #endregion


    #region Deprecated functions

    //Unsmoothed version of Movement (DEPRECATED)


    /*
    // Alternative version of the Move() function used by the Input section to move the target a defined ammount of time.
    void Move(float time)
    {
        if (time == 0)
            return;

        Vector2 forceDirection = ObtainStickValues();

        MyRb.AddRelativeForce(new Vector3(forceDirection.x, 0, forceDirection.y)
            * TargetAcceleration * 10 * time, ForceMode.Acceleration);

        float deccelerationMultiplier =
            TargetDeccelerationCurve.Evaluate(CurrentRopeLenghtInKnots / MaxRopeLenghtInKnots);

        // We limit the velocity of the target.
        if (MyRb.velocity.magnitude > TargetMaxSpeed * deccelerationMultiplier)
        {
            Vector3 velocityDirection = MyRb.velocity.normalized;
            MyRb.velocity = velocityDirection * TargetMaxSpeed * deccelerationMultiplier;
        }

        CheckForKnotAddition();
    }


        // This function returns the speed at which the target should move this frame.
    float ObtainTargetSpeed()
    {
        float ropeLenghtInKnots = CurrentRopeLenghtInKnots;

        if (ropeLenghtInKnots < NumberOfAccelerationKnots)  // Returning an accelerating speed.
            return TargetSpeed * TargetAccelerationCurve.Evaluate(ropeLenghtInKnots / NumberOfAccelerationKnots);

        // Returning a speed that is deccelerating.
        return TargetSpeed * TargetDecelerationCurve.Evaluate
            ((MaxRopeLenghtInKnots - NumberOfAccelerationKnots) 
            / (CurrentRopeLenghtInKnots - NumberOfAccelerationKnots));
        // We substract the acceleration Knots from both ends to avoid overlap of the acceleration and decceleration curves.
    }

    */
    #endregion
}
