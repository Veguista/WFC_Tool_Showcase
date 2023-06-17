using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
    #region Static Reference

    // Singleton Reference. [Only place ONE manager in each scene]
    public static RumbleManager instance;
    void InitializeStaticInstance()
    {
        if(instance == null)
        {
            instance = this;
            return;
        }

        Debug.LogError("There can only be one RumbleManager at once in a scene. " +
            "Destroying this instance of the RumbleManager.");
        Destroy(this.gameObject);
    }

    #endregion

    public List<RumbleProfile> rumbleProfiles;

    // This is a reference to the current Rumble that is being executed.
    // If no rumble is being executed, this reference should be NULL.
    public Rumble activeRumble;


    private void Awake()
    {
        InitializeStaticInstance();
    }


    private void Update()
    {
        if (activeRumble != null)
            PerformRumble();
        else
            SetGamepadRumble(0, 0);
    }


    // Begins playing the Rumble Profile identified by its position in the rumbleProfiles List.
    public void StartRumble(int rumbleProfileID)
    {
        if(rumbleProfileID < 0)
        {
            Debug.LogError("rumbleProfileID needs to be bigger than 0.");
            return;
        }

        if(rumbleProfileID > rumbleProfiles.Count - 1)
        {
            Debug.LogWarning("rumbleProfileID (" + rumbleProfileID 
                + ") does not match with any RumbleProfile in rumbleProfiles.");
            return;
        }

        activeRumble = new Rumble(rumbleProfiles[rumbleProfileID]);
    }


    // Performs all Rumble associated methods during Update(). It also checks if the rumble being performed is finished.
    void PerformRumble()
    {
        SetGamepadRumble(activeRumble.LowFrequencyValue, activeRumble.HighFrequencyValue);

        if (activeRumble.IsTimerFinished())
            activeRumble = null;
    }


    #region Methods
    // This function sets the vibration values in our controller.
    void SetGamepadRumble(float lowFrequency, float highFrequency)
    {
        // Escape route in case there is no controller connected.
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
    }


    // Bool used to keep track of whether the haptics are paused (and can thus be resumed).
    bool hapticsArePaused = false;

    // This function resets the Rumbling Manager to its original state.
    public void ResetRumbleManager()
    {
        // Escape route in case there is no controller connected.
        if (Gamepad.current == null)
            return;

        activeRumble = null;

        InputSystem.ResetHaptics();

        hapticsArePaused = false;
    }


    // This function pauses any rumbling happening, which allows it to resume afterwards if needed.
    public void PauseRumble()
    {
        // Escape route in case there is no controller connected.
        if (Gamepad.current == null)
            return;

        // We don't pause if we already paused.
        if (hapticsArePaused)
            return;

        InputSystem.PauseHaptics();

        hapticsArePaused = true;
    }


    // This function resumes any rumbling that was paused before. We can only resume if we were paused before.
    public void ResumeRumble()
    {
        // Escape route in case there is no controller connected.
        if (Gamepad.current == null)
            return;

        // We don't pause if we already paused.
        if (!hapticsArePaused)
            return;

        InputSystem.ResumeHaptics();

        hapticsArePaused = false;
    }

    #endregion
}
