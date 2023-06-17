using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;


[CustomEditor(typeof(RumbleProfile))]
public class RumbleProfileEditor : Editor
{
    Color lowFrequencyColor = Color.cyan, highFrequencyColor = Color.magenta;

    public override void OnInspectorGUI()
    {
        RumbleProfile myProfile = target as RumbleProfile;

        // Methods used to test Rumbles.
        UpdateEditorDeltaTime();
        RumbleMethods();


        GUILayout.Space(10);

        // Title of the RumbleProfile at hand.
        GUIStyle headStyle = new GUIStyle();
        headStyle.fontSize = 25;
        headStyle.fontStyle = FontStyle.Bold;
        headStyle.normal.textColor = Color.white;
        headStyle.alignment = TextAnchor.MiddleCenter;



        GUILayout.Label("Rumble Profile", headStyle);

        GUILayout.Space(10);


        #region Graph
        TLP.Editor.EditorGraph graph;

        // Small precaution for those cases where we don't have more than 1 TimelinePoint.
        if (myProfile.RumbleLenght == 0)
        {
            graph = new TLP.Editor.EditorGraph(0, -0.1f, 1, 1.1f, "", 100);
            graph.AddFunction(x => 0, Color.red);
        }
        else
        {
            graph = new TLP.Editor.EditorGraph(0, -0.1f, myProfile.RumbleLenght, 1.1f, "", 100);
            graph.AddFunction(x => myProfile.lowFrequencyTimeline.EvaluateTimeline(x), lowFrequencyColor);
            graph.AddFunction(x => myProfile.highFrequencyTimeline.EvaluateTimeline(x), highFrequencyColor);
        }


        // Horizontal lines.
        graph.AddLineY(0, Color.white);
        graph.AddLineY(0.5f, new Color(0.2f, 0.2f, 0.2f));
        graph.AddLineY(1, new Color(0.4f, 0.4f, 0.4f));


        // Vertical lines.
        for (int i = 0; i < myProfile.RumbleLenght + 2; i++)
        {
            if (i == 0)
                graph.AddLineX(i, Color.grey);
            else
                graph.AddLineX(i, new Color(0.3f, 0.3f, 0.3f));
        }


        // Test Rumble timer.
        if (rumbleTestTime != 0)
            graph.AddLineX(rumbleTestTime, Color.red);


        graph.Draw();

        #endregion



        GUILayout.Space(10);

        using (new GUILayout.HorizontalScope())
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Vector2[] pointsArray = Timeline.ConvertTimelineToVector2Array(myProfile.lowFrequencyTimeline.Points);

                using (new GUILayout.HorizontalScope())
                {
                    // Text about lowFrequency List<>.
                    GUILayout.Label("Low Frequency");

                    GUILayout.Space(10);

                    Color newLowFreqColor = EditorGUILayout.ColorField(lowFrequencyColor);

                    if (newLowFreqColor != lowFrequencyColor)
                        lowFrequencyColor = newLowFreqColor;
                }
                

                GUILayout.Space(5);

                // Add Point button.
                if (GUILayout.Button("+"))
                {
                    myProfile.lowFrequencyTimeline.AddPoint();
                    SetScriptableObjectAsDirty();
                }

                // List of Points.
                foreach (Vector2 point in pointsArray)
                {
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label("T:", GUILayout.MinWidth(18), GUILayout.MaxWidth(20));

                        float newTime = EditorGUILayout.FloatField("", point.x, GUILayout.MaxWidth(60));

                        if(newTime != point.x)
                        {
                            //Undo.RecordObject(myProfile, "change TimelinePoint Time (Rumble Profile)");
                            myProfile.lowFrequencyTimeline.ChangePointTime(point.x, newTime);
                            SetScriptableObjectAsDirty();
                        }

                        GUILayout.Space(5);

                        GUILayout.Label("V:", GUILayout.MinWidth(18), GUILayout.MaxWidth(20));

                        float newValue = EditorGUILayout.FloatField("", point.y, GUILayout.MaxWidth(60));

                        if (newValue != point.y)
                        {
                            //Undo.RecordObject(myProfile, "change TimelinePoint Value (Rumble Profile)");
                            myProfile.lowFrequencyTimeline.ChangePointValue(point.x, newValue);
                            SetScriptableObjectAsDirty();
                        }


                        // Erase Point button.
                        if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
                        {
                            myProfile.lowFrequencyTimeline.RemovePoint(point.x);
                            SetScriptableObjectAsDirty();
                        }
                    }
                }
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Vector2[] pointsArray = Timeline.ConvertTimelineToVector2Array(myProfile.highFrequencyTimeline.Points);


                using (new GUILayout.HorizontalScope())
                {
                    // Text about highFrequency List<>.
                    GUILayout.Label("High Frequency");

                    GUILayout.Space(10);

                    Color newHighFreqColor = EditorGUILayout.ColorField(highFrequencyColor);

                    if (newHighFreqColor != highFrequencyColor)
                        highFrequencyColor = newHighFreqColor;
                }


                GUILayout.Space(5);


                // Add Point button.
                if (GUILayout.Button("+"))
                {
                    myProfile.highFrequencyTimeline.AddPoint();
                }

                // List of Points.
                foreach (Vector2 point in pointsArray)
                {
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label("T:", GUILayout.MinWidth(18), GUILayout.MaxWidth(20));

                        float newTime = EditorGUILayout.FloatField("", point.x, GUILayout.MaxWidth(60));

                        if (newTime != point.x)
                        {
                            //Undo.RecordObject(myProfile, "change TimelinePoint Time (Rumble Profile)");
                            myProfile.highFrequencyTimeline.ChangePointTime(point.x, newTime);
                            SetScriptableObjectAsDirty();
                        }

                        GUILayout.Space(5);

                        GUILayout.Label("V:", GUILayout.MinWidth(18), GUILayout.MaxWidth(20));

                        float newValue = EditorGUILayout.FloatField("", point.y, GUILayout.MaxWidth(60));

                        if (newValue != point.y)
                        {
                            //Undo.RecordObject(myProfile, "change TimelinePoint Value (Rumble Profile)");
                            myProfile.highFrequencyTimeline.ChangePointValue(point.x, newValue);
                            SetScriptableObjectAsDirty();
                        }


                        // Erase Point button.
                        if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
                        {
                            myProfile.highFrequencyTimeline.RemovePoint(point.x);
                            SetScriptableObjectAsDirty();
                        }
                    }
                }
            }
        }


        // Adding the ability to test the RumbleProfile.
        if(GUILayout.Button("Test RumbleProfile", GUILayout.MinHeight(50)))
        {
            editorRumble = new Rumble(myProfile);
        }
    }

    void SetScriptableObjectAsDirty()
    {
        EditorUtility.SetDirty(target as RumbleProfile);
    }


    Rumble editorRumble;
    float rumbleTestTime = 0;

    float timeSinceStart = 0;
    float deltaTime = 0;


    void UpdateEditorDeltaTime()
    {
        deltaTime = Time.realtimeSinceStartup - timeSinceStart;

        timeSinceStart = Time.realtimeSinceStartup;
    }


    void RumbleMethods()
    {
        if (editorRumble == null)
            return;

        // Updating our time.
        rumbleTestTime += deltaTime;


        // And checking it does not exceed our limits.
        if (rumbleTestTime > editorRumble.profile.RumbleLenght)
            rumbleTestTime = editorRumble.profile.RumbleLenght;


        float lowFreqValue = 
            editorRumble.ObtainTimelineValueAtTime(editorRumble.profile.lowFrequencyTimeline, rumbleTestTime);
        float highFreqValue =
            editorRumble.ObtainTimelineValueAtTime(editorRumble.profile.highFrequencyTimeline, rumbleTestTime);


        // Getting highFreq and lowFreq values.
        SetGamepadRumble(lowFreqValue, highFreqValue);


        if (rumbleTestTime == editorRumble.profile.RumbleLenght)
            ResetRumble();
    }

    void SetGamepadRumble(float lowFrequency, float highFrequency)
    {
        // Escape route in case there is no controller connected.
        if (Gamepad.current == null)
            return;

        Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
    }

    void ResetRumble()
    {
        editorRumble = null;
        rumbleTestTime = 0;
        SetGamepadRumble(0, 0);
    }

    private void OnDisable()
    {
        ResetRumble();
    }


    // This override forces the editor to be updated every frame.
    public override bool RequiresConstantRepaint()
    {
        if (editorRumble == null)
            return false;

        return true;
    }
}
