using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDash : MonoBehaviour
{
    [Header("Dash Variables")]
    [SerializeField] [Range(0, 20)] float dashDistance = 5f;
    [SerializeField] [Range(0, 2)] float dashDuration = 0.5f;
    [SerializeField] AnimationCurve dashDistanceVariation;
    // [SerializeField] [Range(0, 50)] float afterDashExtraSpeed = 5f;
    [SerializeField] [Range(0, 5)] float cooldown = 1f;


    // PlayerMovement reference.
    PlayerMovement _movementScript;
    PlayerMovement MovementScript
    {
        get
        {
            if (_movementScript == null)
                _movementScript = GetComponent<PlayerMovement>();

            return _movementScript;
        }
    }


    // CharacterController reference.
    CharacterController _characterController;
    CharacterController CharacterController
    {
        get
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();

            return _characterController;
        }
    }


    // RumbleManager static reference.
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

    const int dashRumbleID = 0;     // The ID used in the Rumble Manager to identify the dash Rumble.


    // SmoothedPlayer reference for rotation.
    Transform SmoothedPlayer { get { return SmoothedRotationReference.instance; } }


    // Dash cooldown timer.
    float timer = 0;    // If its value is > 0, the dash is on cooldown.
    void UpdateCooldownTimer()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
            timer = 0;
    }


    // Input system stuff.
    PlayerInputActions InputActions { get { return StatesManager.instance.InputActions; } }
    void SubscribeToInputEvents()
    {
        InputActions.FightingPosition.Dash.performed += TryToDash;
    }


    private void Awake()
    {
        SubscribeToInputEvents();
    }

    private void FixedUpdate()
    {
        UpdateCooldownTimer();
    }


    // Function called when players press the dash button.
    public void TryToDash(InputAction.CallbackContext context)
    {
        if (timer != 0)
            return;

        // Reseting our timer.
        timer = cooldown;

        StartCoroutine(DashCoRoutine());
    }


    public IEnumerator DashCoRoutine()
    {
        // Checking that our cooldown is ALWAYS LONGER than the dashDuration.
        if (cooldown < dashDuration)
        {
            Debug.LogError("Dash cooldown ALWAYS has to be LONGER than the dashDuration.");
            yield break;
        }


        // Activate the Dash Rumble.
        MyRumbleManager.StartRumble(dashRumbleID);


        Timer dashTimer = new Timer(dashDuration);
        float totalDistanceTravelled = 0;  // We store how much we have ALREADY TRAVELLED to discount it in our loop.


        while (!dashTimer.IsComplete)
        {
            dashTimer.Update();

            float distanceToTravelThisFrame = 
                dashDistance * dashDistanceVariation.Evaluate(dashTimer.PercentageComplete) - totalDistanceTravelled;

            totalDistanceTravelled = dashDistance * dashDistanceVariation.Evaluate(dashTimer.PercentageComplete);

            Vector2 direction = ObtainControlValues();
            Vector3 direction3D = new Vector3(direction.x, 0, direction.y);

            CharacterController.Move(direction3D.normalized * distanceToTravelThisFrame);

            yield return null;
        }


        // We transmit the "Extra-Speed" to our PlayerMovement script.
        // MovementScript.Addforce(SmoothedPlayer.forward * afterDashExtraSpeed);
    }


    // This function returns a Vector2 of the left stick values after applying a correction to account for the camera rotation.
    Vector2 ObtainControlValues()
    {
        Vector2 axis = InputActions.FightingPosition.Movement.ReadValue<Vector2>();

        // Applying a correction to account for the camera rotation:
        float magnitude = axis.magnitude;
        float angle = Mathf.Rad2Deg * Mathf.Asin(axis.y);

        if (axis.x < 0 & axis.y > 0)
            angle = 180 - angle;
        else if (axis.x == -1 & axis.y == 0)
            angle = 180;
        else if (axis.x < 0 & axis.y < 0)
            angle = 180 - angle;
        else if (axis.x == 0 & axis.y == -1)
            angle = 270;
        else if (axis.x > 0 & axis.y < 0)
            angle += 360;

        angle -= 45;

        axis = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle)).normalized * magnitude;

        return axis;
    }
}
