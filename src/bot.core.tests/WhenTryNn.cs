﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Math;
using Accord.Neuro;
using Accord.Neuro.Learning;
using NUnit.Framework;

namespace bot.core.tests
{
    public class WhenTryNn
    {
        [Test]
        public void TestName()
        {
            IActivationFunction function = new BipolarSigmoidFunction();
            var network = new ActivationNetwork(function,
                inputsCount: 2, neuronsCount: new[] { 2, 1 });

            var layer = network.Layers[0] as ActivationLayer;
            var teacher = new PerceptronLearning(network);
            teacher.LearningRate = 0.1;

            var input = new double[4][];
            input[0] = new[] { 0d, 0d };
            input[1] = new[] { 0d, 1d };
            input[2] = new[] { 1d, 0d };
            input[3] = new[] { 1d, 1d };

            var output = new double[4][];
            output[0] = new[] { 0d };
            output[1] = new[] { 0d };
            output[2] = new[] { 0d };
            output[3] = new[] { 1d };

            

            //// Iterate until stop criteria is met
            double error = double.PositiveInfinity;
            double previous;

            var needToStop = false;

            while (!needToStop)
            {
                
            }

            do
            {
                previous = error;

                //    // Compute one learning iteration
                error = teacher.RunEpoch(input, output);

            } while (Math.Abs(previous - error) > 0.01 );

             int[] answers = input.Apply(network.Compute).GetColumn(0).Apply(System.Math.Sign);
			 
			 //https://github.com/accord-net/framework/blob/development/Samples/Neuro/Perceptron/Applications/OneLayerPerceptron.cs
        }
    }
}
