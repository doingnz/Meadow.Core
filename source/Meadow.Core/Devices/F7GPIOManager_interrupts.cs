﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meadow.Core;
using Meadow.Hardware;
using static Meadow.Core.Interop;
using static Meadow.Core.Interop.STM32;

namespace Meadow.Devices
{

    public partial class F7GPIOManager : IIOController
    {
        public event InterruptHandler Interrupt;

        public event FrequencyChangeHandler FrequencyChanged;

        private Thread _ist = null;
        private List<int> _interruptGroupsInUse = new List<int>();

        public void WireInterrupt(IPin pin, InterruptMode interruptMode,
                     Meadow.Hardware.ResistorMode resistorMode,
                     double debounceDuration, double glitchDuration)
        {
            STM32.ResistorMode stm32Resistor;

            switch (resistorMode)
            {
                case Meadow.Hardware.ResistorMode.InternalPullDown:
                    stm32Resistor = STM32.ResistorMode.PullDown;
                    break;
                case Meadow.Hardware.ResistorMode.InternalPullUp:
                    stm32Resistor = STM32.ResistorMode.PullUp;
                    break;
                default:
                    stm32Resistor = STM32.ResistorMode.Float;
                    break;
            }

            var designator = GetPortAndPin(pin);
            WireInterrupt(designator.port, designator.pin, interruptMode, stm32Resistor, debounceDuration, glitchDuration);
        }

        private void WireInterrupt(GpioPort port, int pin, InterruptMode interruptMode,
                    STM32.ResistorMode resistorMode,
                    double debounceDuration, double glitchDuration)
        {
            Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0, $" + Wire Interrupt {interruptMode}");

            if (interruptMode != InterruptMode.None)
            {
                lock (_interruptGroupsInUse)
                {
                    Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0, $" interrupt group {pin}");

                    // interrupt group is effectively the pin number
                    if (_interruptGroupsInUse.Contains(pin))
                    {
                        Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0, $" interrupt group {pin} in use");
                        throw new InterruptGroupInUseException(pin);
                    }
                    else
                    {
                        Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0, $" interrupt group {pin} not in use");
                        _interruptGroupsInUse.Add(pin);
                    }
                }

                var cfg = new Interop.Nuttx.UpdGpioInterruptConfiguration()
                {
                    Enable = 1,
                    Port = (uint)port,
                    Pin = (uint)pin,
                    RisingEdge = (uint)(interruptMode == InterruptMode.EdgeRising || interruptMode == InterruptMode.EdgeBoth ? 1 : 0),
                    FallingEdge = (uint)(interruptMode == InterruptMode.EdgeFalling || interruptMode == InterruptMode.EdgeBoth ? 1 : 0),
                    ResistorMode = (uint)resistorMode,

                    // Nuttx side expects 1 - 10000 to represent .1 - 1000 milliseconds
                    DebounceDuration = (uint)(debounceDuration * 10),
                    GlitchDuration = (uint)(glitchDuration * 10)
                };

                if (_ist == null)
                {
                    _ist = new Thread(InterruptServiceThreadProc)
                    {
                        IsBackground = true
                    };

                    _ist.Start();
                }

                Output.WriteLineIf((DebugFeatures & (DebugFeature.GpioDetail | DebugFeature.Interrupts)) != 0,
                    $"Calling ioctl from WireInterrupt() enable Input: {port}{pin}, ResistorMode:0x{cfg.ResistorMode:x02}, debounce:{debounceDuration}, glitch:{glitchDuration}");

                var result = UPD.Ioctl(Nuttx.UpdIoctlFn.RegisterGpioIrq, ref cfg);

                if (result != 0)
                {
                    var err = UPD.GetLastError();

                    Output.WriteLineIf((DebugFeatures & (DebugFeature.GpioDetail | DebugFeature.Interrupts)) != 0,
                            $"failed to register interrupts: {err}");
                }
            }
            else
            {
                var cfg = new Interop.Nuttx.UpdGpioInterruptConfiguration()
                {
                    Enable = 0,   // Disable
                    Port = (uint)port,
                    Pin = (uint)pin,
                    RisingEdge = 0,
                    FallingEdge = 0,
                    ResistorMode = 0,
                    DebounceDuration = 0,
                    GlitchDuration = 0
                };
                Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0,
                    $"Calling ioctl to disable interrupts for Input: {port}{pin}");

                var result = UPD.Ioctl(Nuttx.UpdIoctlFn.RegisterGpioIrq, ref cfg);

                lock (_interruptGroupsInUse)
                {
                    if (_interruptGroupsInUse.Contains(pin))
                    {
                        _interruptGroupsInUse.Remove(pin);
                    }
                    else
                    {
                        Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0,
                            $"Int group: {pin} not in use");
                    }
                }
            }
        }

        private void InterruptServiceThreadProc(object o)
        {
            IntPtr queue = Interop.Nuttx.mq_open(new StringBuilder("/mdw_int"), Nuttx.QueueOpenFlag.ReadOnly);
            Output.WriteLineIf((DebugFeatures & (DebugFeature.GpioDetail | DebugFeature.Interrupts)) != 0,
                $"IST Started reading queue {queue.ToInt32():X}");
            
            // We get 2 bytes from Nuttx. the first is the GPIOs port and pin the second
            // the debounced state of the GPIO
            var rx_buffer = new byte[2];

            while (true)
            {
                int priority = 0;
                try
                {
                    Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0,
                        $"+mq_receive...");

                    var result = Interop.Nuttx.mq_receive(queue, rx_buffer, rx_buffer.Length, ref priority);

                    Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0,
                        $"-mq_receive...");

                    Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0,
                        $"queue data arrived: {BitConverter.ToString(rx_buffer)}");

                    // byte 1 contains the port and pin, byte 2 contains the stable state.
                    if (result >= 0)
                    {
                        var irq = rx_buffer[0];
                        bool state = rx_buffer[1] == 0 ? false : true;
                        var port = irq >> 4;
                        var pin = irq & 0xf;
                        var key = $"P{(char)(65 + port)}{pin}";

                        Output.WriteLineIf((DebugFeatures & DebugFeature.Interrupts) != 0,
                            $"Interrupt on {key} state:{state}");

                        lock (_interruptPins)
                        {
                            if (_interruptPins.ContainsKey(key))
                            {
                                Task.Run(() =>
                                {
                                    var ipin = _interruptPins[key];
                                    Interrupt?.Invoke(ipin, state);
                                });
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"IST: {ex.Message}");
                    Thread.Sleep(5000);
                }
            }
        }
    }


    /* ===== MEADOW GPIO PIN MAP =====
        BOARD PIN   SCHEMATIC       CPU PIN   MDW NAME  ALT FN   INT GROUP
        J301-1      RESET                                           - 
        J301-2      3.3                                             - 
        J301-3      VREF                                            - 
        J301-4      GND                                             - 
        J301-5      DAC_OUT1        PA4         A0                  4
        J301-6      DAC_OUT2        PA5         A1                  5
        J301-7      ADC1_IN3        PA3         A2                  3
        J301-8      ADC1_IN7        PA7         A3                  7
        J301-9      ADC1_IN10       PC0         A4                  0
        J301-10     ADC1_IN11       PC1         A5                  1
        J301-11     SPI3_CLK        PC10        SCK                 10
        J301-12     SPI3_MOSI       PB5         MOSI    AF6         5
        J301-13     SPI3_MISO       PC11        MISO    AF6         11
        J301-14     UART4_RX        PI9         D00     AF8         9
        J301-15     UART4_TX        PH13        D01     AF8         13
        J301-16     PC6             PC6         D02                 6
        J301-17     CAN1_RX         PB8         D03     AF9         8
        J301-18     CAN1_TX         PB9         D04     AF9         9

        J302-4      PE3             PE3         D15                 3
        J302-5      PG3             PG3         D14                 3
        J302-6      USART1_RX       PB15        D13     AF4         15
        J302-7      USART1_TX       PB14        D12     AF4         14
        J302-8      PC9             PC9         D11                 9
        J302-9      PH10            PH10        D10                 10
        J302-10     PB1             PB1         D09                 1
        J302-11     I2C1_SCL        PB6         D08     AF4         6
        J302-12     I2C1_SDA        PB7         D07     AF4         7
        J302-13     PB0             PB0         D06                 0
        J302-14     PC7             PC7         D05                 7

        LED_B       PA0
        LED_G       PA1
        LED_R       PA2
    */
}
