using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Static Reference

    public static PlayerMovement instance;

    void InitializeStaticReference()
    {
        instance = this;
    }

    #endregion


    [Header("Movement Variables")]
    [SerializeField] [Range(0.0001f, 100)] float acceleration;    // The m/s that get accelerated in a second just by walking.
    [SerializeField] [Range(0.0001f, 100)] float maxWalkingSpeed; // Maximum speed the character can reach by just walking.
    [SerializeField] [Range(1, 200)] float topSpeed;              // Maximum speed the character can reach by any means.


    // This curve calculates what speed players want to reach when the movement magnitude is smaller than 1.
    // Range(0, 1); 0 == Stopped; 1 == maxWalkingSpeed;
    [SerializeField] AnimationCurve walkingCurve;

    // Range(0, 2); 0 == Stopped; 1 == maxWalkingSpeed; 2 == topSpeed;
    [SerializeField] AnimationCurve deccelerationMultiplier;

    // Range(-1, 1); -1 == Opposite; 0 == Perpendicular; 1 == Same Direction;
    // (Uses the dot product of normalize vectors to evaluate the curve)
    [SerializeField] AnimationCurve oppositeMoveMultiplierCurve;



    // Reference to the PlayerInputActions in the StatesController.
    PlayerInputActions PlayerActions
    { get { return StatesManager.instance.InputActions; } }


    // Reference to our CharacterController.
    CharacterController characterController;
    CharacterController CharacterController
    {
        get
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            
            return characterController;
        }
        set
        {
            characterController = value;
        }
    }


    // Public property regarding whether the player is paused.
    // It automatically handles pausing or unpausing when setting its value.
    bool _paused = false;
    bool Paused
    {
        get
        {
            return _paused;
        }
        set
        {
            if (value == _paused)    // If we are not changing the value, we don't do anything.
                return;
            
            if (value)              // If we are pausing the character.
            {
                pausedVelocityHolder = accumulatedSpeed;
                accumulatedSpeed = Vector3.zero;
            }
            else                    // If we are resuming the character.
            {
                accumulatedSpeed = pausedVelocityHolder;
                pausedVelocityHolder = Vector3.zero;
            }

            _paused = value;
        }
    }

    // Hidden variables.
    Vector3 accumulatedSpeed = Vector3.zero;
    Vector3 pausedVelocityHolder = Vector3.zero;


    private void Awake()
    {
        InitializeStaticReference();

        // We subscribe to the OnRopeDrawToggle event.
        StatesManager.instance.OnRopeDrawToggle += PauseInRopeDrawingModeToggle;
    }



    private void Update()
    {
        // If we are paused, we don't need to do anything else.
        if (_paused == true)
            return;

        CalculateMovement();

        MoveCharacter();
    }


    #region Movement methods
    // This function returns a Vector2 of the left stick values after applying a correction to account for the camera rotation.
    Vector2 ObtainControlValues()
    {
        Vector2 axis = PlayerActions.FightingPosition.Movement.ReadValue<Vector2>();

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


    // This function calculates how much the character has to move before the Move function.
    void CalculateMovement()
    {
        // We first clamp the character's speed with the topSpeed Value.
        accumulatedSpeed = accumulatedSpeed.normalized * Mathf.Clamp(accumulatedSpeed.magnitude, 0, topSpeed);

        // We obtain our axis (with camera correction).
        Vector2 movementAxis = ObtainControlValues();


        // We create a variable to calculate how fast our character wants to go (m/s).
        Vector3 targetVelocity;

        if (movementAxis.magnitude == 0)
            targetVelocity = Vector3.zero;
        else
            targetVelocity = new Vector3(movementAxis.x, 0, movementAxis.y).normalized
                                 * maxWalkingSpeed * walkingCurve.Evaluate(movementAxis.magnitude);

        // An a variable to see how much our speed needs to change to achive our targetVelocity.
        Vector3 targetVelocityChange = targetVelocity - accumulatedSpeed;

        // If our changes ammount to 0, we don't do anything.
        if (targetVelocityChange.magnitude == 0)
            return;

        // We calculate the dot product of both vectors (accumulatedSpeed and tagetVelocity)
        float dotProductNormalize = Vector3.Dot(accumulatedSpeed.normalized, targetVelocityChange.normalized);
        float dotProduct = Vector3.Dot(accumulatedSpeed, targetVelocityChange);

        // We calculate the 2 composite vectors of the targetVelocity.
        Vector3 parallelVec = accumulatedSpeed * dotProduct / (accumulatedSpeed.magnitude * accumulatedSpeed.magnitude);


        // If the dot product is positive or 0, we don't need to add any modifiers to our acceleration.
        if (dotProductNormalize >= 0)
        {
            // If our desired acceleration is too big, we clamp it.
            if (targetVelocityChange.magnitude > acceleration)
                targetVelocityChange = targetVelocityChange.normalized * acceleration;
        }

        // If the dot product is less than 0, and our acceleration won't allow us to reach the desired velocity,
        // we apply modifiers to our decceleration.
        else if(targetVelocityChange.magnitude > acceleration)
        {
            // We calculate the perpendicular vector.
            Vector3 perpendicularVec = targetVelocityChange - parallelVec;

            Vector3 maxAccelerationVec = (parallelVec.normalized * oppositeMoveMultiplierCurve.Evaluate(dotProductNormalize) * acceleration
                                 + perpendicularVec.normalized * acceleration) * deccelerationMultiplier.Evaluate(movementAxis.magnitude);

            float maxParallelAcceleration = (parallelVec 
                                            * Vector3.Dot(parallelVec, maxAccelerationVec) 
                                            / (parallelVec.magnitude * parallelVec.magnitude)).magnitude;


            // If our multiplier will make the velocity go over the accumulated speed
            if (maxParallelAcceleration > accumulatedSpeed.magnitude)
            {   
                // Since we are only applying a part of our multiplier, we need to know how much of it we are applying.
                float specialMultiplier = accumulatedSpeed.magnitude / parallelVec.magnitude;
                
                maxAccelerationVec = (parallelVec.normalized * specialMultiplier * acceleration
                        + perpendicularVec * acceleration) * deccelerationMultiplier.Evaluate(movementAxis.magnitude);

                parallelVec = parallelVec.normalized * accumulatedSpeed.magnitude;
            }
            else
            {
                // We apply our modifier to our parallel vec.
                parallelVec *= oppositeMoveMultiplierCurve.Evaluate(dotProductNormalize);
            }


            // And we recombine the vectors in our targetVelocityChange.
            Vector3 newTargetVelocityChange = parallelVec + perpendicularVec;

            // And if our desired acceleration is too big, we clamp it.
            if (newTargetVelocityChange.magnitude > maxAccelerationVec.magnitude)
            {
                targetVelocityChange = newTargetVelocityChange.normalized * maxAccelerationVec.magnitude;
            }
        }

        accumulatedSpeed += targetVelocityChange;
    }


    // This function moves the character in the direction of the accumulated speed.
    void MoveCharacter()
    {
        CharacterController.Move(accumulatedSpeed * Time.deltaTime);
    }



    // This function adds a force to the character's accumulated speed.
    public void Addforce(Vector3 force)
    {
        accumulatedSpeed += force;
    }
    #endregion

    void PauseInRopeDrawingModeToggle(bool ropeOn)
    {
        Paused = ropeOn;
    }
}
