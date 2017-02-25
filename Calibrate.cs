using System;
using System.Diagnostics;
using System.Threading;
//using NationalInstruments.VisaNS;
//using NationalInstruments.VisaNS;
using System.Runtime.Remoting.Messaging;
using System.Collections.Generic;
using System.Text;


namespace Examples.AdvancedProgramming.AsynchronousOperations
{
    public struct Fraction
    {
        public static int getGCD(double a, double b)
        {
            //Drop negative signs
            a = Math.Abs(a);
            b = Math.Abs(b);

            //Return the greatest common denominator between two integers
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            if (a == 0)
                return (int)b;
            else
                return (int)a;
        }

        public static int getLCD(int a, int b)
        {
            //Return the Least Common Denominator between two integers
            return (a * b) / getGCD(a, b);
        }
    }
    // Create a class that simulates sampling .
    public class sampleCollector
    {
        public static short[] getSamples(int numSamples, ref double sampleRate, ref int AWGfreq)
        {
            //USB buffer of int16 values
            short[] buffer = new short[numSamples];
            //scale amplitude to max int16 value
            double amplitude = short.MaxValue;
            //create sinewave data
            for (int n = 0; n < buffer.Length; n++)
            {
                buffer[n] = (short)(amplitude * Math.Sin((2 * Math.PI * n * AWGfreq) / sampleRate));
            }
            return buffer;
        }
    }

    // Create an asynchronous delegate that matches the Factorize method.
    public delegate short[] AsyncSampleCaller(int numSamples, ref double sampleRate,ref int AWGfreq);
    public class DemonstrateAsyncPattern
    {
        public const int SAMPLE_RATE = 10000001;
        public Stopwatch stopWatch = new Stopwatch();
        // The waiter object used to keep the main application thread
        // from terminating before the callback method completes.
        ManualResetEvent waiter;
        public int CalcNumSamples(int AWGFreq, out double EquSampRate, out double time2complete)
        {
            int numSamples = 0;
            double tOne = (double)(1 / (double)AWGFreq);
            //double tTwo = (double)(1 / (double)SAMPLE_RATE);
            int fCoincidence = Fraction.getGCD(AWGFreq, SAMPLE_RATE);
            double tCoincidence = 1 / (double)fCoincidence;
            numSamples = (int)(tCoincidence * SAMPLE_RATE);
            time2complete = tCoincidence;
            EquSampRate = (double)AWGFreq*(double)numSamples;
            return numSamples;
        }
        public void PrintError(int AWGFreq)
        {
            System.Console.WriteLine("Usage: Calibrate <AWG_Freq (Hz)>");
            System.Console.WriteLine("AWG Frequency: {0}", AWGFreq);
            System.Console.WriteLine("Real ADC Sample Rate: {0}", SAMPLE_RATE);
            System.Console.WriteLine("Equivalent Sampling Rate: N/A ");
            System.Console.WriteLine("Number of Samples: N/A");
        }

        // Define the method that receives a callback when the results are available.
        public void ProcessSamples(IAsyncResult result)
        {
            double sampleRate = 0;
            int AWGfreq = 0;
            // Extract the delegate from the 
            // System.Runtime.Remoting.Messaging.AsyncResult.
            AsyncSampleCaller sampleDelegate = (AsyncSampleCaller)((AsyncResult)result).AsyncDelegate;

            /*TYPE CASTING*/
            int numSamples = (int)result.AsyncState;

            // Obtain the result.
            short[] buffer = new short[numSamples];
            buffer = sampleDelegate.EndInvoke(ref sampleRate, ref AWGfreq, result);
            //end waveform capture time 
            this.stopWatch.Stop();
            TimeSpan ts = this.stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            System.Console.WriteLine("Waveform capture complete. Run time: "+elapsedTime);
            System.Console.WriteLine(@"Processing data and creating plots with matlab");

            // Create the MATLAB instance 
            // Change to the directory where the function is located 
             MLApp.MLApp matlab = new MLApp.MLApp();
             
          
             matlab.Execute(@"cd c:\temp\");

             // Define the output 
             object result2 = null;

             // Call the MATLAB function myfunc
             matlab.Feval("myfunc", 2, out result2, buffer, sampleRate);

             // Display result 
             object[] res = result2 as object[];

             //Console.WriteLine(res[0]);
             //Console.WriteLine(res[1]);
            System.Console.WriteLine("AWG Frequency: {0}Hz", AWGfreq);
            System.Console.WriteLine("Real ADC Sample Rate: {0}Hz", SAMPLE_RATE);
            System.Console.WriteLine("Equivalent Sampling Rate: {0}Hz ",sampleRate);
            System.Console.WriteLine("Number of Samples: {0}",numSamples);
            System.Console.WriteLine("SFDR: {0}dB",res[0]);
            System.Console.WriteLine("\nDone processing, see matlab for plots. Enter 'e' to exit.");
            string line = Console.ReadLine();
            if (line == "e")
            {
                Environment.Exit(0);
            }
            else
            {
                System.Console.WriteLine("Invalid input, please enter 'e'. Exiting with code 1");
                Environment.Exit(1);
            }
            waiter.Set();
        }

        // The following method demonstrates the asynchronous pattern using a callback method.
        public void GetSamplesUsingCallback(int numSamples, double sampleRate, int AWGfreq)
        {
            
            AsyncSampleCaller sampleDelegate = new AsyncSampleCaller(sampleCollector.getSamples);
            //int temp = 0;
            // Waiter will keep the main application thread from 
            // ending before the callback completes because
            // the main thread blocks until the waiter is signaled
            // in the callback.
            waiter = new ManualResetEvent(false);

            // Define the AsyncCallback delegate.
            AsyncCallback callBack = new AsyncCallback(this.ProcessSamples);

            // Asynchronously invoke the Factorize method.
            IAsyncResult result = sampleDelegate.BeginInvoke(
                                 numSamples,
                                 ref sampleRate,
                                 ref AWGfreq,
                                 callBack,
                                 numSamples);

            // Do some other useful work while 
            // waiting for the asynchronous operation to complete.

            // When no more work can be done, wait.
            waiter.WaitOne();
        }

        /*MAIN TAKES IN COMMAND LINE ARGUMENT OF AWG WAVEFORM FREQUENCY. 
          USES READ AND WRITE FUNCTIONS TO SEND/RECIEVE INFO TO/FROM USER
          MULTIPLE CLASSES AND FUNCTIONS ARE USED. TYPE CASTING CAN BE SEEN 
          IN THE PROCESS SAMPPLES FUNCTION. ARRAY AND LOOP USE CAN BE SEEN IN
          THE SAMPLE COLLECTOR CLASS. ALL REQUIREMENTS SHOULD BE MET FOR C# 
          level 1 MASTERY ACHIEVEMENT*/
  
        public static int Main(string[] args)
        {

            DemonstrateAsyncPattern demonstrator = new DemonstrateAsyncPattern();
            int AWGFreq = 0;
            bool test = int.TryParse(args[0], out AWGFreq);
            if (args.Length == 0)
            {
                System.Console.WriteLine("Invalid usage");
                demonstrator.PrintError(AWGFreq);
                return 1;
            }
            else if ((!test)|| AWGFreq>500000000 || AWGFreq<100)
            {
                System.Console.WriteLine("Please use numeric input for AWG frequency between 100Hz and 500MHz.");
                demonstrator.PrintError(AWGFreq);
                return 1;
            }
            else
            {
                double EquSampRate = 0;
                double time = 0;
                //AWGFreq = 100000000;
                System.Console.WriteLine("Please configure unit to output waveform with freqeucncy specified.\nBegin waveform capture? (y/n)");
                string line = Console.ReadLine();
                if (line == "y")
                {
                    demonstrator.stopWatch.Start();
                    int numSamples = demonstrator.CalcNumSamples(AWGFreq, out EquSampRate, out time);
                    int timeMs = (int)(time * 1000);
                    //sleep amount of time to collect samples calculated by CalcNumSamples and sleep thread 
                    //for that amount of time to simulate real waveform capture
                    Thread.Sleep(timeMs);
                    demonstrator.GetSamplesUsingCallback(numSamples, EquSampRate, AWGFreq);
                    // demonstrator.GetSamplesUsingCallback(numSamples, SAMPLE_RATE, AWGFreq);
                    return 0;
                }
                else if (line == "n")
                {
                    System.Console.WriteLine("Capture cancelled");
                    return 2;
                }
                else
                {
                    System.Console.WriteLine("Invalid input, please enter 'y' or 'n'. Exiting with code 1");
                    return 1;
                }

            }


        }
    }
}
