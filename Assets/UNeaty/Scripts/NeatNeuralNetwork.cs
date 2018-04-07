using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;


namespace UNeaty
{
    public class NeatNeuralNetwork
    {
        enum NeatNeuronType
        {
            Input,
            Hidden,
            Output,
            Bias
        }

        public interface INeatNeuron
        {
            ReadOnlyCollection<INeatConnection> NeuronIncomingConnections { get; }
            uint UniqueID { get; }
        }

        public interface INeatConnection
        {
            double ConnectionWeight { get; }
            uint OtherNeuronUniqueID { get; }
            bool Disabled { get; }
        }

        class NeatNeuron : INeatNeuron
        {
            public ReadOnlyCollection<INeatConnection> NeuronIncomingConnections { get { return IncomingConnections.AsReadOnly(); } }
            public uint UniqueID { get { return Key; } }

            public uint Key { get; private set; }
            public double Value = 0;
            public List<INeatConnection> IncomingConnections = new List<INeatConnection>();
            public NeatNeuronType TheNeuronType;

            public NeatNeuron(uint Key, NeatNeuronType TheNeuronType)
            {
                this.Key = Key;
                this.TheNeuronType = TheNeuronType;
            }

            public NeatNeuron(NeatNeuron Main)
            {
                this.Key = Main.Key;
                this.Value = Main.Value;

                foreach (NeatConnection aNeatConnection in Main.IncomingConnections)
                    this.IncomingConnections.Add(new NeatConnection(aNeatConnection));

                this.TheNeuronType = Main.TheNeuronType;
            }
        }

        public struct NeatGpuConnection
        {
            public int FromIndex;
            public int ToIndex;
            public double Weight;

            public NeatGpuConnection(int FromIndex, int ToIndex, double Weight)
            {
                this.FromIndex = FromIndex;
                this.ToIndex = ToIndex;
                this.Weight = Weight;
            }
        };

        public struct NeatGpuDimensions
        {
            public int InputNeuronsCount;
            public int OutputNeuronsCount;
            public int TotalNeuronsCount;
            public int ConnectionCount;

            public NeatGpuDimensions(int inputNeuronsCount, int outputNeuronsCount, int totalNeuronsCount, int connectionCount)
            {
                InputNeuronsCount = inputNeuronsCount;
                OutputNeuronsCount = outputNeuronsCount;
                TotalNeuronsCount = totalNeuronsCount;
                ConnectionCount = connectionCount;
            }
        };

        public struct NeatGpuIndicies
        {
            public int ConnectionsStartIndex;
            public int InputStartIndex;
            public int NeuronValuesStartIndex;
            public int OutputStartIndex;

            public NeatGpuIndicies(int connectionsStartIndex, int inputStartIndex, int neuronValuesStartIndex, int outputStartIndex)
            {
                ConnectionsStartIndex = connectionsStartIndex;
                InputStartIndex = inputStartIndex;
                NeuronValuesStartIndex = neuronValuesStartIndex;
                OutputStartIndex = outputStartIndex;
            }
        };

        class NeatConnection : INeatConnection
        {
            public uint OtherNeuronUniqueID { get { return OtherNeuronKey; } }
            public double ConnectionWeight { get { return Weight; } }
            public bool Disabled { get { return !Enabled; } }

            public double Weight;
            public uint OtherNeuronKey;
            public bool Enabled;

            public NeatConnection(double Weight, uint OtherNeuronKey, bool Enabled = true)
            {
                this.Weight = Weight;
                this.OtherNeuronKey = OtherNeuronKey;
                this.Enabled = Enabled;
            }

            public NeatConnection(NeatConnection Main)
            {
                this.Weight = Main.Weight;
                this.OtherNeuronKey = Main.OtherNeuronKey;
                this.Enabled = Main.Enabled;
            }
        }

        public OrderedDictionary NetworkNeurons { get { return AllNeurons.AsReadOnly(); } }
        public int TotalNeuronCount { get { return AllNeurons.Count; } }
        OrderedDictionary AllNeurons;

        public Int32 InputNeuronCount { get; private set; }
        public Int32 HiddenNeuronCount { get { return TotalNeuronCount - InputNeuronCount - OutputNeuronCount - 1; } }
        public Int32 OutputNeuronCount { get; private set; }

        public Int32 TopAncestorSeed { get; private set; }

        public System.Random TheRandomizer { get; private set; }

        public NeatNeuralNetwork(Int32 InputNeuronCount, Int32 OutputNeuronCount) :
            this(InputNeuronCount, OutputNeuronCount, Environment.TickCount)
        { }

        public NeatNeuralNetwork(Int32 InputNeuronCount, Int32 OutputNeuronCount, Int32 Seed)
        {
            if (InputNeuronCount < 1)
                throw new ArgumentException("There must be at least one neuron in the input layer.", "InputNeuronCount");
            else if (OutputNeuronCount < 1)
                throw new ArgumentException("There must be at least one neuron in the output layer.", "OutputNeuronCount");

            this.InputNeuronCount = InputNeuronCount;
            this.OutputNeuronCount = OutputNeuronCount;
            this.TopAncestorSeed = Seed;

            AllNeurons = new OrderedDictionary(InputNeuronCount + OutputNeuronCount + 1);

            TheRandomizer = new System.Random(Seed);

            for (int i = 0; i < InputNeuronCount; i++)
                AddNeuron(NeatNeuronType.Input);

            AddNeuron(NeatNeuronType.Bias).Value = 1;

            for (int i = 0; i < OutputNeuronCount; i++)
                AddNeuron(NeatNeuronType.Output);
        }

        public NeatNeuralNetwork(NeatNeuralNetwork Main)
        {
            if (Main == null)
                throw new ArgumentException("A null value cannot be cloned.", "Main");

            this.TopAncestorSeed = Main.TopAncestorSeed;
            TheRandomizer = new System.Random(Main.TheRandomizer.Next());

            this.InputNeuronCount = Main.InputNeuronCount;
            this.OutputNeuronCount = Main.OutputNeuronCount;

            AllNeurons = new OrderedDictionary(Main.AllNeurons.Count);

            foreach (DictionaryEntry aDictionaryEntry in Main.AllNeurons)
            {
                AllNeurons.Add(aDictionaryEntry.Key, new NeatNeuron(aDictionaryEntry.Value as NeatNeuron));
            }
        }

        public NeatNeuralNetwork(Stream TheStream)
        {
            using (BinaryReader TheBinaryReader = new BinaryReader(TheStream))
            {
                InputNeuronCount = TheBinaryReader.ReadInt32();
                OutputNeuronCount = TheBinaryReader.ReadInt32();
                AllNeurons = new OrderedDictionary(InputNeuronCount + OutputNeuronCount + 1);

                TopAncestorSeed = TheBinaryReader.ReadInt32();
                TheRandomizer = new System.Random(TheBinaryReader.ReadInt32());

                int AllNeuronsCount = TheBinaryReader.ReadInt32();
                for (int i = 0; i < AllNeuronsCount; i++)
                {
                    uint Key = TheBinaryReader.ReadUInt32();
                    NeatNeuronType TheNeuronType = (NeatNeuronType)TheBinaryReader.ReadInt32();
                    NeatNeuron TheNeuron = new NeatNeuron(Key, TheNeuronType);

                    int IncomingConnectionsCount = TheBinaryReader.ReadInt32();
                    for (int j = 0; j < IncomingConnectionsCount; j++)
                    {
                        double TheWeight = TheBinaryReader.ReadDouble();
                        uint OtherNeuronKey = TheBinaryReader.ReadUInt32();
                        bool Enabled = TheBinaryReader.ReadBoolean();
                        TheNeuron.IncomingConnections.Add(new NeatConnection(TheWeight, OtherNeuronKey, Enabled));
                    }

                    AllNeurons.Add(Key, TheNeuron);
                }
            }
        }

        public void Serialize(Stream TheStream)
        {
            using (BinaryWriter TheBinaryWriter = new BinaryWriter(TheStream))
            {
                TheBinaryWriter.Write(InputNeuronCount);
                TheBinaryWriter.Write(OutputNeuronCount);
                TheBinaryWriter.Write(TopAncestorSeed);
                TheBinaryWriter.Write(TheRandomizer.Next());

                TheBinaryWriter.Write(AllNeurons.Count);
                foreach (DictionaryEntry aDictionaryEntry in AllNeurons)
                {
                    TheBinaryWriter.Write((uint)aDictionaryEntry.Key);

                    NeatNeuron TheNeuron = aDictionaryEntry.Value as NeatNeuron;
                    TheBinaryWriter.Write((int)TheNeuron.TheNeuronType);

                    TheBinaryWriter.Write(TheNeuron.IncomingConnections.Count);
                    foreach (NeatConnection aNeatConnection in TheNeuron.IncomingConnections)
                    {
                        TheBinaryWriter.Write(aNeatConnection.Weight);
                        TheBinaryWriter.Write(aNeatConnection.OtherNeuronKey);
                        TheBinaryWriter.Write(aNeatConnection.Enabled);
                    }
                }
            }
        }

        private NeatNeuron AddNeuron(NeatNeuronType TheNeuronType = NeatNeuronType.Hidden)
        {
            NeatNeuron NewNeuron = new NeatNeuron((uint)AllNeurons.Count, TheNeuronType);
            AllNeurons.Add((uint)AllNeurons.Count, NewNeuron);
            return NewNeuron;
        }

        private NeatNeuron InsertNeuron(int index, NeatNeuronType TheNeuronType = NeatNeuronType.Hidden)
        {
            NeatNeuron NewNeuron = new NeatNeuron((uint)AllNeurons.Count, TheNeuronType);
            AllNeurons.Insert(index, (uint)AllNeurons.Count, NewNeuron);
            return NewNeuron;
        }

        public void CalculateGpuArrays(List<NeatGpuDimensions> Dimensions,
                                        List<NeatGpuIndicies> Indicies,
                                        List<NeatGpuConnection> Connections,
                                        int BufferInputNeuronIndex,
                                        ref int BufferNeuronValuesIndex,
                                        ref int BufferOutputNeuronIndex,
                                        ref int BufferTotalNeuronCount,
                                        ref int BufferOutputNeuronCount)
        {
            //Indicies.Add(new NeatGpuIndicies());
            Indicies.Add(new NeatGpuIndicies(Connections.Count, BufferInputNeuronIndex, BufferNeuronValuesIndex, BufferOutputNeuronIndex));

            int ConnectionsCount = 0;
            for (int i = InputNeuronCount + 1; i < AllNeurons.Count; i++)
            {
                NeatNeuron CurrentNeuron = AllNeurons[i] as NeatNeuron;

                foreach (NeatConnection aNeatConnection in CurrentNeuron.IncomingConnections)
                    if (aNeatConnection.Enabled)
                    {
                        Connections.Add(new NeatGpuConnection((int)aNeatConnection.OtherNeuronKey, (int)CurrentNeuron.Key, aNeatConnection.Weight));
                        ConnectionsCount++;
                    }
            }

            Dimensions.Add(new NeatGpuDimensions(InputNeuronCount, OutputNeuronCount, TotalNeuronCount, ConnectionsCount));

            BufferNeuronValuesIndex += TotalNeuronCount;
            BufferOutputNeuronIndex += OutputNeuronCount;
            BufferTotalNeuronCount += TotalNeuronCount;
            BufferOutputNeuronCount += OutputNeuronCount;
        }

        public double[] FeedForward(double[] Input)
        {
            // Validation Checks
            if (Input == null)
                throw new ArgumentException("The input array cannot be set to null.", "Input");
            else if (Input.Length != InputNeuronCount)
                throw new ArgumentException("The input array's length does not match the number of neurons in the input layer.", "Input");

            double[] Result = new double[OutputNeuronCount];

            for (int i = 0; i < InputNeuronCount; i++)
                (AllNeurons[i] as NeatNeuron).Value = (Input[i]);

            (AllNeurons[InputNeuronCount] as NeatNeuron).Value = 1;
            for (int i = InputNeuronCount + 1; i < AllNeurons.Count; i++)
            {
                NeatNeuron CurrentNeuron = AllNeurons[i] as NeatNeuron;

                CurrentNeuron.Value = 0;

                foreach (NeatConnection aNeatConnection in CurrentNeuron.IncomingConnections)
                    if (aNeatConnection.Enabled)
                        CurrentNeuron.Value += aNeatConnection.Weight * ((AllNeurons[aNeatConnection.OtherNeuronKey] as NeatNeuron).Value);

                CurrentNeuron.Value = ReLU(CurrentNeuron.Value);
            }

            for (int i = 0; i < OutputNeuronCount; i++)
                Result[i] = ReLU((AllNeurons[i + AllNeurons.Count - OutputNeuronCount] as NeatNeuron).Value);

            return Result;
        }

        public void Mutate(double PointMutationProbability = 0.3,
                           double ConnectionMutationProbablility = 0.5,
                           double NodeMutationProbablility = 0.1,
                           double EnableDisableConnectionMutationProbability = 0.1,
                           double MutationAmount = 2.0,
                           bool UsePointNodeMutation = true)
        {
            if (UsePointNodeMutation)
                PointNodeMutate(PointMutationProbability, MutationAmount);
            else
                PointMutate(PointMutationProbability, MutationAmount);

            EnableDisableConnectionMutate(EnableDisableConnectionMutationProbability);

            NodeMutate(NodeMutationProbablility);

            if (TheRandomizer.NextDouble() < ConnectionMutationProbablility)
                AddConnectionMutate(ConnectionMutationProbablility);
        }

        public void PointMutate(double MutationProbability = 0.3, double MutationAmount = 2.0)
        {
            if (MutationProbability < 0 || MutationProbability > 1)
                throw new ArgumentException("A probability must be a value between 0 and 1.", "MutationProbablity");
            else if (MutationAmount <= 0)
                throw new ArgumentException("The mutation amount must be greater than 0.", "MutationAmount");

            for (int i = 0; i < AllNeurons.Count; i++)
            {
                foreach (NeatConnection aNeatConnection in (AllNeurons[i] as NeatNeuron).IncomingConnections)
                {
                    if (aNeatConnection.Enabled && TheRandomizer.NextDouble() < MutationProbability)
                    {
                        if (TheRandomizer.NextDouble() < 0.5)
                            aNeatConnection.Weight = TheRandomizer.NextDouble() * (MutationAmount * 2) - MutationAmount;
                        else
                            aNeatConnection.Weight += TheRandomizer.NextDouble() * (MutationAmount * 2) - MutationAmount;
                    }
                }
            }
        }

        public void PointNodeMutate(double MutationProbability = 0.3, double MutationAmount = 2.0)
        {
            if (MutationProbability < 0 || MutationProbability > 1)
                throw new ArgumentException("A probability must be a value between 0 and 1.", "MutationProbablity");
            else if (MutationAmount <= 0)
                throw new ArgumentException("A the mutation amount must be greater than 0.", "MutationAmount");

            for (int i = 0; i < AllNeurons.Count; i++)
            {
                if (TheRandomizer.NextDouble() < MutationProbability)
                {
                    foreach (NeatConnection aNeatConnection in (AllNeurons[i] as NeatNeuron).IncomingConnections)
                    {
                        if (aNeatConnection.Enabled)
                        {
                            if (TheRandomizer.NextDouble() < 0.5)
                                aNeatConnection.Weight = TheRandomizer.NextDouble() * (MutationAmount * 2) - MutationAmount;
                            else
                                aNeatConnection.Weight += TheRandomizer.NextDouble() * (MutationAmount * 2) - MutationAmount;
                        }
                    }
                }
            }
        }

        public void AddConnectionMutate(double MutationAmount = 2.0)
        {
            if (MutationAmount <= 0)
                throw new ArgumentException("A the mutation amount must be greater than 0.", "MutationAmount");

            int FirstNeuronIndex = TheRandomizer.Next(AllNeurons.Count - OutputNeuronCount);
            int SecondNeuronIndex = FirstNeuronIndex;

            while (FirstNeuronIndex == SecondNeuronIndex)
                SecondNeuronIndex = TheRandomizer.Next(InputNeuronCount + 1, AllNeurons.Count);

            if (FirstNeuronIndex > SecondNeuronIndex)
            {
                int Tmp = SecondNeuronIndex;
                SecondNeuronIndex = FirstNeuronIndex;
                FirstNeuronIndex = Tmp;
            }

            NeatNeuron FirstNeuron = AllNeurons[FirstNeuronIndex] as NeatNeuron;
            NeatNeuron SecondNeuron = AllNeurons[SecondNeuronIndex] as NeatNeuron;
            foreach (NeatConnection aNeatConnection in SecondNeuron.IncomingConnections)
            {
                if (aNeatConnection.OtherNeuronKey == FirstNeuron.Key)
                    return;
            }

            SecondNeuron.IncomingConnections.Add(new NeatConnection(TheRandomizer.NextDouble() * (MutationAmount * 2) - MutationAmount, FirstNeuron.Key));
        }

        public void NodeMutate(double MutationProbability = 0.1, double MutationAmount = 2.0)
        {
            if (MutationProbability < 0 || MutationProbability > 1)
                throw new ArgumentException("A probability must be a value between 0 and 1.", "MutationProbablity");
            else if (MutationAmount <= 0)
                throw new ArgumentException("The mutation amount must be greater than 0.", "MutationAmount");

            for (int i = 0; i < AllNeurons.Count; i++)
            {
                foreach (NeatConnection aNeatConnection in (AllNeurons[i] as NeatNeuron).IncomingConnections)
                {
                    if (aNeatConnection.Enabled && TheRandomizer.NextDouble() < MutationProbability)
                    {
                        int NewNeuronIndex = i;
                        if (NewNeuronIndex > TotalNeuronCount - OutputNeuronCount)
                            NewNeuronIndex = TotalNeuronCount - OutputNeuronCount;

                        NeatNeuron NewNeuron = InsertNeuron(NewNeuronIndex);
                        NewNeuron.IncomingConnections.Add(new NeatConnection(TheRandomizer.NextDouble() * (MutationAmount * 2) - MutationAmount, aNeatConnection.OtherNeuronKey));

                        aNeatConnection.OtherNeuronKey = NewNeuron.Key;
                    }
                }
            }
        }

        public void EnableDisableConnectionMutate(double MutationProbability = 0.1)
        {
            if (MutationProbability < 0 || MutationProbability > 1)
                throw new ArgumentException("A probability must be a value between 0 and 1.", "MutationProbablity");

            for (int i = 0; i < AllNeurons.Count; i++)
            {
                foreach (NeatConnection aNeatConnection in (AllNeurons[i] as NeatNeuron).IncomingConnections)
                {
                    if (TheRandomizer.NextDouble() < MutationProbability) aNeatConnection.Enabled = !aNeatConnection.Enabled;
                }
            }
        }

        private double ReLU(double x)
        {
            return Math.Max(x, x / 20);
        }
    }
}