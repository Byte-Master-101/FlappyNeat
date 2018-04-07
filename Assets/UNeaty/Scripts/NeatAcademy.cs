using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UNeaty
{
    public enum AcademyType
    {
        Internal,
        External,
        Player
    }

    [Serializable]
    public class PlayerNeuronInput
    {
        public double DefaultState = 0;
        public List<InputKey> InputKeys = new List<InputKey>();
    }

    [Serializable]
    public class InputKey
    {
        public KeyCode Key;
        public double State;
    }

    public class NeatAcademy : MonoBehaviour
    {
        [HideInInspector] public List<NeatAgent> Agents;

        [HideInInspector] public bool VisualizeNetwork;
        [HideInInspector] public NeatVisualiser TheNeatVisualiser;

        [HideInInspector] public AcademyType TheAcademyType;

        [HideInInspector] public List<PlayerNeuronInput> PlayerNeuronInputs;

        [HideInInspector] public int NeuralNetworkInputCount = 1;
        [HideInInspector] public int NeuralNetworkOutputCount = 1;

        [HideInInspector] public TextAsset ExternalNetworkData;
        [HideInInspector] public bool PreviewOnly;

        [HideInInspector] public bool SaveNetworkInfo;
        [HideInInspector] public string SaveFolderLocation = "";
        [HideInInspector] public string SaveFilePrefix = "NeuralNetworkData-";
        [HideInInspector] public int SaveIterationInterval = 1000;

        [HideInInspector] public bool RenderStateUpdates;

        [HideInInspector] public int CurrentIteration = 0;

        [HideInInspector] public NeatNeuralNetwork BestNeatNeuralNetwork;
        [HideInInspector] public double BestReward = double.MinValue;

        public bool ShouldPreviewOnly { get { return TheAcademyType == AcademyType.External && PreviewOnly; } }
        public bool ShouldRenderStateUpdates
        {
            get
            {
                return ShouldPreviewOnly || (TheAcademyType == AcademyType.External && RenderStateUpdates) ||
                   (TheAcademyType == AcademyType.Internal && RenderStateUpdates) || (TheAcademyType == AcademyType.Player);
            }
        }
        public bool ShouldSaveNetworkInfo
        {
            get
            {
                return (TheAcademyType == AcademyType.External && !PreviewOnly && SaveNetworkInfo) ||
                       (TheAcademyType == AcademyType.Internal && SaveNetworkInfo);
            }
        }

        Camera MainCamera = null;

        private void Awake()
        {
            //Application.targetFrameRate = -1;
            MainCamera = Camera.main;

            if (TheAcademyType == AcademyType.External)
            {
                using (MemoryStream TheMemoryStream = new MemoryStream(ExternalNetworkData.bytes))
                {
                    BestNeatNeuralNetwork = new NeatNeuralNetwork(TheMemoryStream);
                }
            }
            else
                BestNeatNeuralNetwork = new NeatNeuralNetwork(NeuralNetworkInputCount, NeuralNetworkOutputCount);

            if (TheNeatVisualiser)
                TheNeatVisualiser.VisualizeNetwork(BestNeatNeuralNetwork);
        }

        double[] CollectPlayerInput()
        {
            if (PlayerNeuronInputs.Count < NeuralNetworkOutputCount)
                throw new UnassignedReferenceException("The number of neurons in PlayerNeuronInputs does not correspond to the number of output neurons.");

            double[] Result = new double[NeuralNetworkOutputCount];
            for (int i = 0; i < NeuralNetworkOutputCount; i++)
            {
                Result[i] = PlayerNeuronInputs[i].DefaultState;
                foreach (InputKey AnInputKey in PlayerNeuronInputs[i].InputKeys)
                {
                    if (Input.GetKey(AnInputKey.Key))
                        Result[i] = AnInputKey.State;
                }
            }

            return Result;
        }

        public void AgentUpdate(NeatAgent TheNeatAgent)
        {
            if (TheAcademyType == AcademyType.Player)
            {
                TheNeatAgent.CollectState();
                TheNeatAgent.AgentStep(CollectPlayerInput());
            }
            else
                TheNeatAgent.AgentStep(TheNeatAgent.TheNeatNeuralNetwork.FeedForward(TheNeatAgent.CollectState()));
        }

        bool Quitting = false;
        private void OnApplicationQuit() { Quitting = true; }
        public void OnDestroy() { if (Quitting && ShouldSaveNetworkInfo) SaveNetwork(); }

        public void SaveNetwork()
        {
            using (FileStream TheFileStream = new FileStream(Path.Combine(SaveFolderLocation, SaveFilePrefix + CurrentIteration + ".bytes"), FileMode.Create))
                BestNeatNeuralNetwork.Serialize(TheFileStream);
        }

        public void SubmitAgentReward(double Reward, NeatAgent TheAgent)
        {
            if (MainCamera && RenderStateUpdates != MainCamera.enabled)
            {
                MainCamera.enabled = RenderStateUpdates;
            }

            CurrentIteration++;

            if (ShouldSaveNetworkInfo && CurrentIteration % SaveIterationInterval == 0)
                SaveNetwork();

            if (ShouldPreviewOnly)
            {
                TheAgent.TheNeatNeuralNetwork = new NeatNeuralNetwork(BestNeatNeuralNetwork);
            }
            else
            {
                if (Reward > double.MinValue && Reward > BestReward)
                {
                    BestReward = Reward;
                    BestNeatNeuralNetwork = TheAgent.TheNeatNeuralNetwork;

                    if (VisualizeNetwork)
                        TheNeatVisualiser.VisualizeNetwork(BestNeatNeuralNetwork);
                }

                NeatNeuralNetwork NewNetwork = new NeatNeuralNetwork(BestNeatNeuralNetwork);
                NewNetwork.Mutate(UsePointNodeMutation: false);
                TheAgent.TheNeatNeuralNetwork = NewNetwork;
            }
        }
    }
}