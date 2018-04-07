using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UNeaty
{
    public abstract class NeatAgent : MonoBehaviour
    {
        public NeatAcademy TheNeatAcademy;

        public NeatNeuralNetwork TheNeatNeuralNetwork;

        public double Reward = 0;

        private void Start()
        {
            TheNeatAcademy.SubmitAgentReward(double.MinValue, this);
            AgentStart();
            AgentReset();
        }

        private void Update() { TheNeatAcademy.AgentUpdate(this); }

        public abstract double[] CollectState();

        public abstract void AgentStep(double[] Action);

        public abstract void AgentReset();

        public abstract void AgentStart();

        private void OnApplicationQuit() { KillAgent(); }

        public void KillAgent()
        {
            TheNeatAcademy.SubmitAgentReward(Reward, this);
            AgentReset();
        }
    }
}