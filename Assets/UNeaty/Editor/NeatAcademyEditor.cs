#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace UNeaty
{
    [CustomEditor(typeof(NeatAcademy), true)]
    public class NeatAcademyEditor : Editor
    {
        int CurrentNeuronInputIndex = 0;

        public override void OnInspectorGUI()
        {
            NeatAcademy TheNeatAcademy = target as NeatAcademy;
            DrawDefaultInspector();

            EditorGUILayout.Separator();

            if (TheNeatAcademy.SaveFolderLocation == "") TheNeatAcademy.SaveFolderLocation = Application.dataPath + "/UNeaty/TrainingData";

            TheNeatAcademy.VisualizeNetwork = EditorGUILayout.Toggle("Visualize Network", TheNeatAcademy.VisualizeNetwork);
            if (TheNeatAcademy.VisualizeNetwork)
                TheNeatAcademy.TheNeatVisualiser = (NeatVisualiser)EditorGUILayout.ObjectField("The Visualiser",
                                                     TheNeatAcademy.TheNeatVisualiser, typeof(NeatVisualiser), true);

            EditorGUILayout.Separator();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            TheNeatAcademy.TheAcademyType = (AcademyType)EditorGUILayout.EnumPopup("Academy Type", TheNeatAcademy.TheAcademyType);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (TheNeatAcademy.TheAcademyType == AcademyType.Internal)
            {
                TheNeatAcademy.NeuralNetworkInputCount = EditorGUILayout.DelayedIntField("Input Neurons Count", TheNeatAcademy.NeuralNetworkInputCount);
                TheNeatAcademy.NeuralNetworkOutputCount = EditorGUILayout.IntField("Output Neurons Count", TheNeatAcademy.NeuralNetworkOutputCount);

                EditorGUILayout.Separator();
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Separator();

                TheNeatAcademy.RenderStateUpdates = EditorGUILayout.Toggle("Render State Updates", TheNeatAcademy.RenderStateUpdates);

                EditorGUILayout.Separator();

                TheNeatAcademy.SaveNetworkInfo = EditorGUILayout.Toggle("Save Network Info", TheNeatAcademy.SaveNetworkInfo);

                if (TheNeatAcademy.SaveNetworkInfo)
                {
                    EditorGUILayout.BeginHorizontal();
                    TheNeatAcademy.SaveFolderLocation = EditorGUILayout.TextField("Save Location", TheNeatAcademy.SaveFolderLocation);
                    if (GUILayout.Button("Browse"))
                    {
                        string NewSaveLocation = EditorUtility.OpenFolderPanel("Load Network Data File", "", "");
                        if (NewSaveLocation != "")
                            TheNeatAcademy.SaveFolderLocation = NewSaveLocation;
                    }
                    EditorGUILayout.EndHorizontal();

                    TheNeatAcademy.SaveFilePrefix = EditorGUILayout.TextField("File Prefix", TheNeatAcademy.SaveFilePrefix);
                    TheNeatAcademy.SaveIterationInterval = EditorGUILayout.IntField("Iterations Per Save", TheNeatAcademy.SaveIterationInterval);
                }
            }
            else if (TheNeatAcademy.TheAcademyType == AcademyType.External)
            {
                TheNeatAcademy.ExternalNetworkData = (TextAsset)EditorGUILayout.ObjectField("Network Data File",
                                                     TheNeatAcademy.ExternalNetworkData, typeof(TextAsset), true);
                TheNeatAcademy.PreviewOnly = EditorGUILayout.Toggle("Preview Only", TheNeatAcademy.PreviewOnly);

                EditorGUILayout.Separator();
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Separator();

                if (!TheNeatAcademy.PreviewOnly)
                {
                    TheNeatAcademy.RenderStateUpdates = EditorGUILayout.Toggle("Render State Updates", TheNeatAcademy.RenderStateUpdates);
                    EditorGUILayout.Separator();

                    TheNeatAcademy.SaveNetworkInfo = EditorGUILayout.Toggle("Save Network Info", TheNeatAcademy.SaveNetworkInfo);

                    if (TheNeatAcademy.SaveNetworkInfo)
                    {
                        EditorGUILayout.BeginHorizontal();
                        TheNeatAcademy.SaveFolderLocation = EditorGUILayout.TextField("Save Location", TheNeatAcademy.SaveFolderLocation);
                        if (GUILayout.Button("Browse")) TheNeatAcademy.SaveFolderLocation = EditorUtility.OpenFolderPanel("Load Network Data File", "", "");
                        EditorGUILayout.EndHorizontal();

                        TheNeatAcademy.SaveFilePrefix = EditorGUILayout.TextField("File Prefix", TheNeatAcademy.SaveFilePrefix);
                        TheNeatAcademy.SaveIterationInterval = EditorGUILayout.IntField("Iterations Per Save", TheNeatAcademy.SaveIterationInterval);
                    }
                }
                else
                    TheNeatAcademy.RenderStateUpdates = true;
            }
            else if (TheNeatAcademy.TheAcademyType == AcademyType.Player)
            {
                TheNeatAcademy.RenderStateUpdates = true;

                TheNeatAcademy.NeuralNetworkInputCount = EditorGUILayout.IntField("Input Neurons Count", TheNeatAcademy.NeuralNetworkInputCount);
                TheNeatAcademy.NeuralNetworkOutputCount = EditorGUILayout.IntField("Output Neurons Count", TheNeatAcademy.NeuralNetworkOutputCount);

                while (TheNeatAcademy.PlayerNeuronInputs.Count < TheNeatAcademy.NeuralNetworkOutputCount)
                    TheNeatAcademy.PlayerNeuronInputs.Add(new PlayerNeuronInput());
                while (TheNeatAcademy.PlayerNeuronInputs.Count > TheNeatAcademy.NeuralNetworkOutputCount)
                    TheNeatAcademy.PlayerNeuronInputs.RemoveAt(TheNeatAcademy.PlayerNeuronInputs.Count - 1);

                if (CurrentNeuronInputIndex > TheNeatAcademy.PlayerNeuronInputs.Count - 1 || CurrentNeuronInputIndex < 0)
                    CurrentNeuronInputIndex = 0;

                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField("Neuron Index", CurrentNeuronInputIndex.ToString());

                EditorGUI.BeginDisabledGroup(CurrentNeuronInputIndex == TheNeatAcademy.PlayerNeuronInputs.Count - 1);
                if (GUILayout.Button("\u25B2")) CurrentNeuronInputIndex++;
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(CurrentNeuronInputIndex == 0);
                if (GUILayout.Button("\u25BC")) CurrentNeuronInputIndex--;
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical("box");
                PlayerNeuronInput CurrentNeuronInput = TheNeatAcademy.PlayerNeuronInputs[CurrentNeuronInputIndex];

                CurrentNeuronInput.DefaultState = EditorGUILayout.DoubleField("  Default State", CurrentNeuronInput.DefaultState);
                for (int j = 0; j < CurrentNeuronInput.InputKeys.Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();
                    CurrentNeuronInput.InputKeys[j].Key = (KeyCode)EditorGUILayout.EnumPopup("    Key", CurrentNeuronInput.InputKeys[j].Key);
                    CurrentNeuronInput.InputKeys[j].State = EditorGUILayout.DoubleField(CurrentNeuronInput.InputKeys[j].State);
                    if (GUILayout.Button("-"))
                        CurrentNeuronInput.InputKeys.RemoveAt(j);

                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Key"))
                    CurrentNeuronInput.InputKeys.Add(new InputKey());
                EditorGUILayout.EndVertical();
                EditorGUI.EndDisabledGroup();
            }

            if (GUI.changed && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.SetDirty(TheNeatAcademy);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    }
}
#endif