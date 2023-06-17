using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRotator : MonoBehaviour
{
    // Reference to our body's transform.
    [SerializeField] Transform characterTransform;

    // This float determines the maximum radius the camera target can be at.
    [SerializeField][Range(0, 15)] float cameraRadiusMultiplier = 10f;

    // This animation curve is used to prevent magnitudes of 0 in the vectors
    // that determine which direction players are facing.
    // Range [0,1]; 0 == axis magnitude of 0; 1 == maximum axis magnitude.
    [SerializeField] AnimationCurve rotationVecMagnitude;

    // This float multiplies the inputs from the leftStick when it is the only one used
    // (this way, the camera moves further away for the right stick).
    [SerializeField] [Range(0, 1)] float leftStickMultiplier = 0.6f;

    // This float blends the right and left sticks directions in the axis when they are both active.
    [SerializeField] float movingBlendMultiplier = 0.7f;


    // Reference to the PlayerInputActions in the StatesController.
    PlayerInputActions PlayerActions
    {
        get
        {
            return StatesManager.instance.InputActions;
        }
    }


    Vector2 directionAxis = new Vector2(0, -1);
    float axisMagnitude = 1;


    private void Update()
    {
        // This extra precaution prevents us from rotating when we are not supposed to.
        if (StatesManager.instance.PlayerInput.currentActionMap.name != "FightingPosition")
            return;

        Vector2? axis = ObtainDirVector();

        // If none of our sticks returned a value.
        if(axis == null)
        {
            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.localPosition = 
                new Vector3(axis.Value.x, 0, axis.Value.y) * cameraRadiusMultiplier * axisMagnitude;
        }


        // Option used to direct the un-smoothed player.
        characterTransform.LookAt(characterTransform.position + new Vector3(directionAxis.x, 0, directionAxis.y));
    }


    Vector2? ObtainDirVector()
    {
        Vector2 axis = PlayerActions.FightingPosition.Aim.ReadValue<Vector2>();
        

        if (axis.magnitude != 0)
        {
            axis = axis * movingBlendMultiplier 
                + PlayerActions.FightingPosition.Movement.ReadValue<Vector2>() * (1 - movingBlendMultiplier);
            axis *= rotationVecMagnitude.Evaluate(axis.magnitude);
        }

        // If the right joystick does not have a value over the DeadZone, we check the left joystick. 
        else
        {
            axis = PlayerActions.FightingPosition.Movement.ReadValue<Vector2>();

            // If the left joystick does not have a value over the DeadZone either, we use our stored axis.
            if (axis.magnitude != 0)
            {
                axis *= rotationVecMagnitude.Evaluate(axis.magnitude) * leftStickMultiplier;
            }

            // If none of our sticks return a value, we return null (it will be interpreted in the other part of the script.)
            else
                return null;
        }

        directionAxis = Geometry2D.RotatingVector2D(axis.normalized, 45f);

        axisMagnitude = axis.magnitude;

        return Geometry2D.RotatingVector2D(axis, 45f);
    }
}
