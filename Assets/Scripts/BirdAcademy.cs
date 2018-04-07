using System.Collections;
using System.Collections.Generic;
using UNeaty;
using UnityEngine;
using UnityEngine.UI;

public class BirdAcademy : NeatAcademy
{
    [SerializeField] Text IterationNumberText;
    [SerializeField] Text BestRewardText;

    private void Start()
    {
        print("Seed: " + BestNeatNeuralNetwork.TopAncestorSeed);
    }

    // Update is called once per frame
    private void Update ()
    {
        if(IterationNumberText) IterationNumberText.text = "Iteration: " + CurrentIteration;
        if(BestRewardText) BestRewardText.text = "Best Reward: " + BestReward;
    }
}
