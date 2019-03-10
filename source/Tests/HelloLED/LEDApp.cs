﻿using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Hardware;

namespace HelloLED
{
    class LEDApp : AppBase<F7Micro, LEDApp>
    {
        IDigitalOutputPort _redLED;
        IDigitalOutputPort _blueLED;
        IDigitalOutputPort _greenLED;
        //private DigitalOutputPort _d00;

        public void Run()
        {
//            CreateOutputs();
//            ShowLights();

            WalkOutputs();

            // WatchInputs();

            //LoopbackTest();
        }

        public void CreateOutputs()
        {
            _redLED = Device.CreateDigitalOutputPort(Device.Pins.OnboardLEDRed);
            _blueLED = Device.CreateDigitalOutputPort(Device.Pins.OnboardLEDBlue);
            _greenLED = Device.CreateDigitalOutputPort(Device.Pins.OnboardLEDGreen);

            //_d00 = new DigitalOutputPort(Device.Pins.D00, false);
        }

        public void ShowLights()
        {
            var state = false;

            while(true)
            {
                state = !state;

                Console.WriteLine($"State: {state}");

                _redLED.State = state;
                Thread.Sleep(200);
                _greenLED.State = state;
                Thread.Sleep(200);
                _blueLED.State = state;
                Thread.Sleep(200);

                //_d00.State = state;
                //Thread.Sleep(200);
            }
        }

        //public void LoopbackTest()
        //{
        //    // assumes D04 is tied directly to D05
        //    var output = new DigitalOutputPort(Device.Pins.D04);
        //    var input = new DigitalInputPort(Device.Pins.D05);

        //    var state = false;

        //    while(true)
        //    {
        //        state = !state;
        //        output.State = state;
        //        Console.WriteLine($"state: {input.State}");
        //        Thread.Sleep(1000);
        //    }
        //}

        //public void WatchInputs()
        //{
        //    var d05 = new DigitalInputPort(Device.Pins.D05, false, ResistorMode.Disabled);

        //    while(true)
        //    {
        //        Console.WriteLine($"D05: {d05.State}");
        //        Thread.Sleep(500);
        //    }
        //}

        public void WalkOutputs()
        {
            var p = 0;

            while (true)
            {
                p = -3;
                TogglePin(Device.Pins.OnboardLEDRed);
                TogglePin(Device.Pins.OnboardLEDBlue);
                TogglePin(Device.Pins.OnboardLEDGreen);
                // Meadow supports using the analog pins as digitals as well!
                TogglePin(Device.Pins.A00);
                TogglePin(Device.Pins.A01);
                TogglePin(Device.Pins.A02);
                TogglePin(Device.Pins.A03);
                TogglePin(Device.Pins.A04);
                TogglePin(Device.Pins.A05);
                TogglePin(Device.Pins.D00);
                TogglePin(Device.Pins.D01);
                TogglePin(Device.Pins.D02);
                TogglePin(Device.Pins.D03);
                TogglePin(Device.Pins.D04);
                TogglePin(Device.Pins.D05);
                TogglePin(Device.Pins.D06);
                TogglePin(Device.Pins.D07);
                TogglePin(Device.Pins.D08);
                TogglePin(Device.Pins.D09);
                TogglePin(Device.Pins.D10);
                TogglePin(Device.Pins.D11);
                TogglePin(Device.Pins.D12);
                TogglePin(Device.Pins.D13);
                TogglePin(Device.Pins.D14);
                TogglePin(Device.Pins.D15);
                Console.WriteLine(string.Empty);
            }

            void TogglePin(IPin pin)
            {
                switch (p)
                {
                    case -3:
                        Console.Write("R");
                        break;
                    case -2:
                        Console.Write("B");
                        break;
                    case -1:
                        Console.Write("G");
                        break;
                    default:
                        Console.Write($"{p % 10}");
                        break;
                }

                // initialize on
                using (var port = Device.CreateDigitalOutputPort(pin, true))

                {
                    Thread.Sleep(1000);

                    // turn off
                    port.State = false;
                }
                p++;
            }
        }
    }
}
