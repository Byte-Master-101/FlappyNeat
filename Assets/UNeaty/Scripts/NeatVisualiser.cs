using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using UnityEngine;
using UnityEngine.UI;


namespace UNeaty
{
    public class NeatVisualiser : MonoBehaviour
    {
        public static NeatVisualiser Singleton = null;

        [SerializeField] bool DebugInfo = false;

        [SerializeField] GameObject NeuronPrefab;
        [SerializeField] GameObject LinePrefab;
        [SerializeField] Transform NeuronsParent;
        [SerializeField] Transform LinesParent;
        [SerializeField] float MinLineWidth = 1;
        [SerializeField] float MaxLineWidth = 5;
        [SerializeField] float ConnectionWeightBounds = 5;
        [SerializeField]
        Gradient ConnectionColors = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
            new GradientColorKey(new Color(1, 0, 0), 0),
            new GradientColorKey(new Color(1, 150f/255f, 0), 0.45f),
            new GradientColorKey(new Color(1, 1, 1), 0.5f),
            new GradientColorKey(new Color(0, 1, 200/255), 0.55f),
            new GradientColorKey(new Color(0, 1, 0), 1f)
            },
            alphaKeys = new GradientAlphaKey[]
            {
            new GradientAlphaKey(1, 0),
            new GradientAlphaKey(200f/255f, 0.5f),
            new GradientAlphaKey(1, 1)
            }
        };

        [SerializeField] Color DisabledConnectionColor = Color.magenta;

        RectTransform RectT;

        private void Awake()
        {
            if (Singleton == null)
                Singleton = this;
            else
                gameObject.SetActive(false);
        }

        // Use this for initialization
        void Start()
        {
            RectT = GetComponent<RectTransform>();
        }

        public void CreateConnection(uint NeuronAUID, uint NeuronBUID, float ConnectionWeight, bool Disabled)
        {
            RectTransform NeuronA = NeuronsParent.Find(NeuronAUID.ToString()).GetComponent<RectTransform>();
            RectTransform NeuronB = NeuronsParent.Find(NeuronBUID.ToString()).GetComponent<RectTransform>();

            RectTransform Line = Instantiate(LinePrefab, LinesParent).GetComponent<RectTransform>();
            Vector2 Center = (NeuronA.anchoredPosition + NeuronB.anchoredPosition) / 2;
            Line.anchoredPosition = Center;

            Vector2 Direction = NeuronB.anchoredPosition - NeuronA.anchoredPosition;
            Direction.Normalize();
            Line.right = Direction;

            float RealMaxRatio = (Math.Min(Math.Abs(ConnectionWeight), ConnectionWeightBounds) / ConnectionWeightBounds);

            Line.sizeDelta = new Vector2(Vector2.Distance(NeuronA.anchoredPosition, NeuronB.anchoredPosition), RealMaxRatio * (MaxLineWidth - MinLineWidth) + MinLineWidth);

            if (Disabled)
                Line.GetComponent<Image>().color = DisabledConnectionColor;
            else
                Line.GetComponent<Image>().color = ConnectionColors.Evaluate((RealMaxRatio / 2) * Math.Sign(ConnectionWeight) + 0.5f);
        }

        public void VisualizeNetwork(NeatNeuralNetwork BestNeuralNetwork)
        {
            StartCoroutine(VisualizeNetworkEnum(BestNeuralNetwork));
        }

        IEnumerator VisualizeNetworkEnum(NeatNeuralNetwork BestNeuralNetwork)
        {
            foreach (Transform aNeuron in NeuronsParent)
                Destroy(aNeuron.gameObject);

            yield return null;

            OrderedDictionary Neurons = BestNeuralNetwork.NetworkNeurons;

            if (DebugInfo)
            {
                StringBuilder SB = new StringBuilder();
                foreach (DictionaryEntry aDictionaryEntry in Neurons)
                {
                    SB.Append("Neuron: ");
                    SB.Append((uint)aDictionaryEntry.Key);
                    foreach (NeatNeuralNetwork.INeatConnection anINeatConnection in (aDictionaryEntry.Value as NeatNeuralNetwork.INeatNeuron).NeuronIncomingConnections)
                    {
                        SB.Append("\n\tConnection: [Weight: ");
                        SB.Append(anINeatConnection.ConnectionWeight);
                        SB.Append(", OtherUID: ");
                        SB.Append(anINeatConnection.OtherNeuronUniqueID);
                        SB.Append("]");
                    }
                    SB.AppendLine();
                }
                print(SB);
            }

            int i = 0;
            for (i = 0; i < BestNeuralNetwork.InputNeuronCount + 1; i++)
            {
                uint NeuronUID = (Neurons[i] as NeatNeuralNetwork.INeatNeuron).UniqueID;

                RectTransform CurrentNeuron = Instantiate<GameObject>(NeuronPrefab, NeuronsParent).GetComponent<RectTransform>();
                CurrentNeuron.name = NeuronUID.ToString();
                CurrentNeuron.GetComponent<RectTransform>().anchoredPosition = new Vector2(-CurrentNeuron.rect.x - RectT.rect.width / 2,
                    (RectT.rect.height / 2) - (RectT.rect.height / (BestNeuralNetwork.InputNeuronCount + 1f)) * (i + 0.5f));

                if (i == BestNeuralNetwork.InputNeuronCount)
                    CurrentNeuron.Find("BiasText").gameObject.SetActive(true);
                else
                    CurrentNeuron.Find("InputText").gameObject.SetActive(true);
            }

            for (; i < BestNeuralNetwork.InputNeuronCount + BestNeuralNetwork.HiddenNeuronCount + 1; i++)
            {
                uint NeuronUID = (Neurons[i] as NeatNeuralNetwork.INeatNeuron).UniqueID;

                Transform CurrentNeuron = Instantiate<GameObject>(NeuronPrefab, NeuronsParent).transform;

                CurrentNeuron.name = NeuronUID.ToString();
                CurrentNeuron.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    (RectT.rect.width / (BestNeuralNetwork.HiddenNeuronCount + 1)) * (i - BestNeuralNetwork.InputNeuronCount) - (RectT.rect.width / 2),
                    (((float)(new System.Random((int)NeuronUID)).NextDouble() * 2) - 1) * (RectT.rect.height / 3));
            }

            for (; i < BestNeuralNetwork.TotalNeuronCount; i++)
            {
                uint NeuronUID = (Neurons[i] as NeatNeuralNetwork.INeatNeuron).UniqueID;

                RectTransform CurrentNeuron = Instantiate<GameObject>(NeuronPrefab, NeuronsParent).GetComponent<RectTransform>();
                CurrentNeuron.name = NeuronUID.ToString();
                CurrentNeuron.transform.Find("OutputText").gameObject.SetActive(true);

                CurrentNeuron.GetComponent<RectTransform>().anchoredPosition = new Vector2(CurrentNeuron.rect.x + RectT.rect.width / 2,
                    (RectT.rect.height / 2) - (RectT.rect.height / (BestNeuralNetwork.OutputNeuronCount + 1f)) *
                    (i - BestNeuralNetwork.TotalNeuronCount + BestNeuralNetwork.OutputNeuronCount + 1f));
            }

            foreach (Transform aConnection in LinesParent)
                Destroy(aConnection.gameObject);

            for (i = 0; i < BestNeuralNetwork.TotalNeuronCount; i++)
            {
                NeatNeuralNetwork.INeatNeuron TheNeatNeuron = Neurons[i] as NeatNeuralNetwork.INeatNeuron;
                uint NeuronUID = TheNeatNeuron.UniqueID;

                ReadOnlyCollection<NeatNeuralNetwork.INeatConnection> IncomingConnections = TheNeatNeuron.NeuronIncomingConnections;
                for (int j = 0; j < IncomingConnections.Count; j++)
                {
                    NeatNeuralNetwork.INeatConnection CurrentConnection = IncomingConnections[j];
                    CreateConnection(NeuronUID, CurrentConnection.OtherNeuronUniqueID,
                                     (float)CurrentConnection.ConnectionWeight,
                                     CurrentConnection.Disabled);
                }
            }
        }
    }
}