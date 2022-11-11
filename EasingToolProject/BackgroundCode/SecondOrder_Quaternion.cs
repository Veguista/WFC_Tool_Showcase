using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


// This script attempts to be used for Quaternions (rotations).
public class SecondOrder_Quaternion
// Taken from the following video by t3ssel8r: https://www.youtube.com/watch?v=KPoeNZZ6H4s
{
    private Quaternion y;                   // We store the latest Quaternion.
    public Vector3 playerInputs;            // We store the player f, z and r inputs to
                                            // compare mid-runtime if they have changed.

    private SecondOrder_1D xValue, yValue, zValue, wValue;
    bool[] whichRotationToFollow = new bool[3];


    public SecondOrder_Quaternion(float f, float z, float r, Quaternion originalQuaternion)
    {
        // Initializing our 4 SecondOrder_1D values.
        xValue = new SecondOrder_1D(f, z, r, originalQuaternion.x);
        yValue = new SecondOrder_1D(f, z, r, originalQuaternion.y);
        zValue = new SecondOrder_1D(f, z, r, originalQuaternion.z);
        wValue = new SecondOrder_1D(f, z, r, originalQuaternion.w);


        // Initialize varibles.
        y = originalQuaternion;

        /*
        if (whichRotationToFollow.Length == followingRotations.Length)
            whichRotationToFollow = followingRotations;
        else
            Debug.LogError("followingRotations bool[] should hold 3 bools.");
        */


        // Store player input variables.
        playerInputs = new Vector3(f, z, r);
    }

    public Quaternion Update(float time, Quaternion newRotation)
    {
        /*
        if(followingRotations != whichRotationToFollow)
        {
            if(followingRotations[0] != whichRotationToFollow[0] && followingRotations[0] == true)
                    yValue = new SecondOrder_1D(playerInputs.x, playerInputs.y, 
                                                playerInputs.z, newRotation.y);

            if (followingRotations[1] != whichRotationToFollow[1] && followingRotations[1] == true)
                zValue = new SecondOrder_1D(playerInputs.x, playerInputs.y,
                                            playerInputs.z, newRotation.z);

            if (followingRotations[2] != whichRotationToFollow[2] && followingRotations[2] == true)
                wValue = new SecondOrder_1D(playerInputs.x, playerInputs.y,
                                            playerInputs.z, newRotation.w);
        }
        */


        // Check for a necessary inversion of the quaternions.
        if (Quaternion.Dot(y, newRotation) < 0)
        {
            // invert the signs on the components
            newRotation.x *= -1;
            newRotation.y *= -1;
            newRotation.z *= -1;
            newRotation.w *= -1;
        }


        // float x = xValue.Update(time, newRotation.x, 0, true);
        //float resultX = xValue.Update(time, newRotation.y, 0, true);
        float resultY = yValue.Update(time, newRotation.y, 0, true);
        float resultZ = zValue.Update(time, newRotation.z, 0, true);
        float resultW = wValue.Update(time, newRotation.w, 0, true);

        /*
        if (!followingRotations[0])
            resultY = 0;
        if (!followingRotations[1])
            resultZ = 0;
        if (!followingRotations[2])
            resultW = 0;
        */

        Quaternion result = new Quaternion(0, resultY, resultZ, resultW);
        result = result.normalized;

        y = result;
        // whichRotationToFollow = followingRotations;
        return result;
    }

    #region Unused initial code
    /*
    // Taken from the following page: https://gamedev.stackexchange.com/questions/108920/applying-angular-velocity-to-quaternion
    // Credits to Soonts for the code.
    public Quaternion rotate(Vector3 angularSpeed, float deltaTime)
    {
        Vector3 vec = angularSpeed * deltaTime;
        float length = vec.Length();
        if (length < 1E-6F)
            return this;    // Otherwise we'll have division by zero when trying to normalize it later on

        // Convert the rotation vector to quaternion. The following 4 lines are very similar to CreateFromAxisAngle method.
        float half = length * 0.5f;
        float sin = MathF.Sin(half);
        float cos = MathF.Cos(half);
        // Instead of normalizing the axis, we multiply W component by the length of it. This method normalizes result in the end.
        Quaternion q = new Quaternion(vec.X * sin, vec.Y * sin, vec.Z * sin, length * cos);

        q = Multiply(q, this);
        q.Normalize();
        // The following line is not required, only useful for people. Computers are fine with 2 different quaternion representations of each possible rotation.
        if (q.W < 0) q = Negate(q);
        return q;
    }
    */
    #endregion
}
