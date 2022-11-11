using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SecondOrderTransform))]
public class SecondOrderTransformEditor : Editor
{
    SecondOrderTransform myScript;      // Holds our target script.
    SecondOrderTransform MyScript       // Provides and initializes our target script.
    {
        get
        {
            if(myScript == null)
                myScript = target as SecondOrderTransform;

            return myScript;
        }
    }


    float leftX, rightX, lowY, highY;   // Variables that hold the size of the graph being drawn.

    private void Awake()
    {
        CalculateBounds();
    }


    public override void OnInspectorGUI()
    {
        // Setting up colors for our inspector.
        GUIStyle activeButtonStyle = new GUIStyle(EditorStyles.helpBox);
        GUIStyle inactiveButtonStyle = new GUIStyle(EditorStyles.helpBox);

        Texture2D activeTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        Texture2D inactiveTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        // Font styles.
        GUIStyle activeStyle = new GUIStyle(EditorStyles.boldLabel);
        GUIStyle inactiveStyle = new GUIStyle(EditorStyles.boldLabel);
        GUIStyle redTextStyle = new GUIStyle(EditorStyles.boldLabel);
        activeStyle.normal.textColor = new Color(.1f, .1f, .1f, .9f);
        inactiveStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
        redTextStyle.normal.textColor = new Color(.9f, .1f, .1f, .9f);

        activeTex.SetPixel(0, 0, new Color(.4f, .8f, .1f, .6f));
        inactiveTex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 0.4f));

        activeButtonStyle.normal.background = activeTex;
        inactiveButtonStyle.normal.background = inactiveTex;

        activeTex.Apply();
        inactiveTex.Apply();

        #region Header
        GUILayout.Space(13);

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontSize = 20;
        headerStyle.normal.textColor = Color.white;

        GUILayout.Label("Second Order Transform", headerStyle);
        #endregion

        GUILayout.Space(13);

        #region Run in-editor
        GUIStyle myStyle = new GUIStyle(EditorStyles.helpBox);
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);

        if (MyScript.runInEditor)
            tex.SetPixel(0, 0, new Color(.4f, .7f, 0f, .8f));
        else
            tex.SetPixel(0, 0, new Color(.3f, .3f, .3f, .5f));

        tex.Apply();
        myStyle.normal.background = tex;


        using (new GUILayout.VerticalScope(myStyle))
        {
            GUILayout.Space(3);

            using (new GUILayout.HorizontalScope())
            {
                GUIStyle newLabelStyle = new GUIStyle(EditorStyles.boldLabel);

                if (MyScript.runInEditor)
                    newLabelStyle.normal.textColor = new Color(0f, 0f, 0f);
                else
                    newLabelStyle.normal.textColor = new Color(.8f, .8f, .8f);

                GUILayout.Label("Operate during editor", newLabelStyle, GUILayout.MinWidth(200));

                bool newRunInEditor = EditorGUILayout.Toggle(MyScript.runInEditor);
                if (newRunInEditor != MyScript.runInEditor)
                {
                    Undo.RecordObject(MyScript, "change SecondOrderTransform operate during editor");
                    MyScript.runInEditor = newRunInEditor;
                }
            }

            GUILayout.Space(3);
        }
        #endregion

        GUILayout.Space(10);

        #region Follow transform and options
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Space(3);

            GUILayout.Label("Follow transform and options", EditorStyles.boldLabel);

            if (MyScript.whichSecondOrder != SecondOrderTransform.typeOfFollow.rotationDegrees)
            {
                GUILayout.Space(5);

                // Transform to follow
                Transform followTransform = (Transform)
                    EditorGUILayout.ObjectField("Transform to follow",
                    MyScript.followTransform, typeof(Transform), true);


                if (followTransform != MyScript.followTransform
                    && MyScript.whichSecondOrder != SecondOrderTransform.typeOfFollow.rotationDegrees)
                {
                    Undo.RecordObject(MyScript, "change Transform to follow in SecondOrderTransform");
                    MyScript.followTransform = followTransform;
                }

                if (followTransform == null)
                {
                    GUILayout.Space(5);

                    GUILayout.Label("A Transform to follow is required.", redTextStyle);
                }
            }


            GUILayout.Space(5);

            // Transform to follow
            SecondOrderTransform.typeOfFollow typeOfFollow = (SecondOrderTransform.typeOfFollow)
                EditorGUILayout.EnumPopup("Type of follow", MyScript.whichSecondOrder);

            if (typeOfFollow != MyScript.whichSecondOrder)
            {
                Undo.RecordObject(MyScript, "change type of follow in SecondOrderTransform");
                MyScript.whichSecondOrder = typeOfFollow;
            }

            #region Local or world space

            // Euler rotation does not allow to obtain values from world.
            if (MyScript.whichSecondOrder != SecondOrderTransform.typeOfFollow.rotationDegrees)
            {
                GUILayout.Space(5);

                SecondOrderTransform.typeOfSpace obtainLocalOrWorld = (SecondOrderTransform.typeOfSpace)
                    EditorGUILayout.EnumPopup("OBTAIN local/world values", MyScript.ObtainFromLocalOrWorld);

                if (obtainLocalOrWorld != MyScript.ObtainFromLocalOrWorld)
                {
                    Undo.RecordObject(MyScript, "change OBTAIN local - world values in SecondOrderTransform");
                    MyScript.ObtainFromLocalOrWorld = obtainLocalOrWorld;
                }
            }
            else
                MyScript.ApplyToLocalOrWorld = SecondOrderTransform.typeOfSpace.localSpace;


            // Only position and rotationQuaternion allow local apply.
            if (MyScript.whichSecondOrder == SecondOrderTransform.typeOfFollow.position
                || MyScript.whichSecondOrder == SecondOrderTransform.typeOfFollow.rotationQuaternion)
            {
                GUILayout.Space(5);

                SecondOrderTransform.typeOfSpace applyLocalOrWorld = (SecondOrderTransform.typeOfSpace)
                    EditorGUILayout.EnumPopup("APPLY local/world values", MyScript.ApplyToLocalOrWorld);

                if (applyLocalOrWorld != MyScript.ApplyToLocalOrWorld)
                {
                    Undo.RecordObject(MyScript, "change APPLY local - world values in SecondOrderTransform");
                    MyScript.ApplyToLocalOrWorld = applyLocalOrWorld;
                }
            }
            else
                MyScript.ApplyToLocalOrWorld = SecondOrderTransform.typeOfSpace.localSpace;


            #endregion



            if (MyScript.whichSecondOrder != SecondOrderTransform.typeOfFollow.rotationQuaternion)
            {
                GUILayout.Space(5);

                using (new GUILayout.HorizontalScope())
                {
                    bool newApplyX, newApplyY, newApplyZ;

                    if (MyScript.applyX)
                    {
                        using (new GUILayout.HorizontalScope(activeButtonStyle))
                        {
                            GUILayout.Label("Apply X", activeStyle, GUILayout.MinWidth(55));
                            newApplyX = EditorGUILayout.Toggle(MyScript.applyX);
                        }
                    }
                    else
                    {
                        using (new GUILayout.HorizontalScope(inactiveButtonStyle))
                        {
                            GUILayout.Label("Apply X", inactiveStyle, GUILayout.MinWidth(55));
                            newApplyX = EditorGUILayout.Toggle(MyScript.applyX);
                        }
                    }


                    if (MyScript.applyY)
                    {
                        using (new GUILayout.HorizontalScope(activeButtonStyle))
                        {
                            GUILayout.Label("Apply Y", activeStyle, GUILayout.MinWidth(55));
                            newApplyY = EditorGUILayout.Toggle(MyScript.applyY);
                        }
                    }
                    else
                    {
                        using (new GUILayout.HorizontalScope(inactiveButtonStyle))
                        {
                            GUILayout.Label("Apply Y", inactiveStyle, GUILayout.MinWidth(55));
                            newApplyY = EditorGUILayout.Toggle(MyScript.applyY);
                        }
                    }


                    if (MyScript.applyZ)
                    {
                        using (new GUILayout.HorizontalScope(activeButtonStyle))
                        {
                            GUILayout.Label("Apply Z", activeStyle, GUILayout.MinWidth(55));
                            newApplyZ = EditorGUILayout.Toggle(MyScript.applyZ);
                        }
                    }
                    else
                    {
                        using (new GUILayout.HorizontalScope(inactiveButtonStyle))
                        {
                            GUILayout.Label("Apply Z", inactiveStyle, GUILayout.MinWidth(55));
                            newApplyZ = EditorGUILayout.Toggle(MyScript.applyZ);
                        }
                    }


                    if (newApplyX != MyScript.applyX)
                    {
                        Undo.RecordObject(MyScript, "change applyX in SecondOrderTransform");
                        MyScript.applyX = newApplyX;
                    }

                    if (newApplyY != MyScript.applyY)
                    {
                        Undo.RecordObject(MyScript, "change applyY in SecondOrderTransform");
                        MyScript.applyY = newApplyY;
                    }

                    if (newApplyZ != MyScript.applyZ)
                    {
                        Undo.RecordObject(MyScript, "change applyZ in SecondOrderTransform");
                        MyScript.applyZ = newApplyZ;
                    }
                }
            }


            if(MyScript.whichSecondOrder == SecondOrderTransform.typeOfFollow.rotationDegrees)
            {
                GUILayout.Space(5);

                // Euler Rotation Script, which gives our script the rotation value.
                GameObject rotationScript = (GameObject)
                    EditorGUILayout.ObjectField("Rotation Script", 
                    MyScript.eulerRotationScript, typeof(GameObject), true);

                if (rotationScript != MyScript.eulerRotationScript)
                {
                    Undo.RecordObject(MyScript, "change Rotation Script in SecondOrderTransform");
                    MyScript.eulerRotationScript = rotationScript;
                }

                if (rotationScript == null)
                {
                    GUILayout.Space(5);

                    GUILayout.Label("A GameObject with a IEulerRotable interface is required.", redTextStyle);
                }


                if (MyScript.HowManySelectedApplies() > 1)
                {
                    GUILayout.Space(5);

                    GUILayout.Label("You can't follow more than 1 axis using Rotation degrees.", redTextStyle);
                }

                GUILayout.Space(3);
            }


            if(MyScript.HowManySelectedApplies() == 0)
            {
                GUILayout.Space(5);

                GUILayout.Label("You need to follow at least 1 axis.", redTextStyle);

                GUILayout.Space(5);
            }
        }
        #endregion

        GUILayout.Space(10);

        #region Second Order Variables
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Second Order variables", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Frequency
            float newFrequency = EditorGUILayout.Slider("Frequency", MyScript.frequency, 0.0001f, 8);
            if(newFrequency != MyScript.frequency)
            {
                Undo.RecordObject(MyScript, "change SecondOrderTransform frequency");
                MyScript.frequency = newFrequency;
            }

            // Damping
            float newDamping = EditorGUILayout.Slider("Damping", MyScript.damping, 0, 5);
            if (newDamping != MyScript.damping)
            {
                Undo.RecordObject(MyScript, "change SecondOrderTransform damping");
                MyScript.damping = newDamping;
            }

            // Damping
            float newInitialResponse = EditorGUILayout.Slider("InitialResponse", MyScript.initialResponse, -6, 6);
            if (newInitialResponse != MyScript.initialResponse)
            {
                Undo.RecordObject(MyScript, "change SecondOrderTransform initial response");
                MyScript.initialResponse = newInitialResponse;
            }

            GUILayout.Space(5);
        }
        #endregion

        GUILayout.Space(10);

        #region Function pre-visualization
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Function pre-visualization", EditorStyles.boldLabel);
            GUILayout.Space(5);
            DrawGraph();
            GUILayout.Space(5);
        }
        #endregion
    }


    #region Graph functions
    void DrawGraph()
    {
        CalculateBounds();

        // We initialize our graph.
        SecondOrder_1D myDynamics = new SecondOrder_1D(MyScript.frequency, MyScript.damping, MyScript.initialResponse, 0);


        TLP.Editor.EditorGraph graph = new TLP.Editor.EditorGraph(leftX, lowY, rightX, highY, "", 100);

        graph.AddFunction(x => myDynamics.Update(0.02f, 1, 0, true), Color.cyan);
        graph.AddLineX(0);
        graph.AddLineY(0, Color.white);
        graph.AddLineY(1, new Color(.5f, .8f, .3f, 1));

        for (int i = 1; i < 11; i++)
        {
            if (i <= rightX)
                graph.AddLineX(i, new Color(.3f + .05f * i, .3f - .05f * i, .3f, 1));
        }
        graph.Draw();
    }

    void CalculateBounds()
    {
        // We re-write our Dynamics class.
        SecondOrder_1D myBondsCalculation =
            new SecondOrder_1D(MyScript.frequency, MyScript.damping, MyScript.initialResponse, 0);

        // Size of the graph in the X Axis.
        leftX = Mathf.Clamp(-10 / Mathf.Sqrt(MyScript.frequency), -.3f, -.8f);
        rightX = Mathf.Min(7 / MyScript.frequency + 1f, 9.5f);

        // We calculate a sample of values for our function. We store them in a List<float>.
        List<float> myResults = new List<float>();
        for (int i = 0; i < 100; i++)
            myResults.Add(myBondsCalculation.Update(0.02f, 1, 0, true));

        lowY = Mathf.Min(Mathf.Min(myResults.ToArray()) - 0.3f, -.4f);
        highY = Mathf.Max(Mathf.Max(myResults.ToArray()) + 0.3f, 1.4f);
    }
    #endregion
}
