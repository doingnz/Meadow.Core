﻿using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Meadow
{
    public class MeadowOnLinux<TPinout> : IMeadowDevice, IApp
        where TPinout : IPinDefinitions, new()
    {
        private SysFsGpioDriver _ioController;

        public TPinout Pins { get; }
        public DeviceCapabilities Capabilities { get; }

        public LinuxSerialPortNameDefinitions SerialPortNames
        {
            get
            {
                if(typeof(TPinout) == typeof(JetsonNanoPinout))
                {
                    return new JetsonNanoSerialPortNameDefinitions();
                }
                else if (typeof(TPinout) == typeof(RaspberryPiPinout))
                {
                    return new RaspberryPiSerialPortNameDefinitions();
                }

                throw new PlatformNotSupportedException();
            }
        }

        public MeadowOnLinux()
        {
            Pins = new TPinout();
            Capabilities = new DeviceCapabilities(
                new AnalogCapabilities(false, null),
                new NetworkCapabilities(false, true)
                );
        }

        public void Initialize()
        {
            _ioController = new SysFsGpioDriver();
        }

        public IPin GetPin(string pinName)
        {
            return Pins.AllPins.First(p => string.Compare(p.Name, pinName) == 0);
        }

        public II2cBus CreateI2cBus(int busNumber = 0)
        {
            return CreateI2cBus(busNumber, II2cController.DefaultI2cBusSpeed);
        }

        public II2cBus CreateI2cBus(int busNumber, Frequency frequency)
        {
            return new I2CBus(busNumber, frequency);
        }

        public II2cBus CreateI2cBus(IPin[] pins, Frequency frequency)
        {
            return CreateI2cBus(pins[0], pins[1], frequency);
        }

        public II2cBus CreateI2cBus(IPin clock, IPin data, Frequency frequency)
        {
            // TODO: implement this based on channel caps (this is Jetson specific right now)

            if (clock == Pins["PIN05"] && data == Pins["PIN03"])
            {
                return new I2CBus(1, frequency);
            }
            else if (clock == Pins["PIN28"] && data == Pins["PIN27"])
            {
                return new I2CBus(0, frequency);
            }

            throw new ArgumentOutOfRangeException("Requested pins are not I2C bus pins");
        }

        public ISerialMessagePort CreateSerialMessagePort(SerialPortName portName, byte[] suffixDelimiter, bool preserveDelimiter, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, int readBufferSize = 512)
        {
            var classicPort = CreateSerialPort(portName, baudRate, dataBits, parity, stopBits, readBufferSize);
            return SerialMessagePort.From(classicPort, suffixDelimiter, preserveDelimiter);
        }

        public ISerialMessagePort CreateSerialMessagePort(SerialPortName portName, byte[] prefixDelimiter, bool preserveDelimiter, int messageLength, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, int readBufferSize = 512)
        {
            var classicPort = CreateSerialPort(portName, baudRate, dataBits, parity, stopBits, readBufferSize);
            return SerialMessagePort.From(classicPort, prefixDelimiter, preserveDelimiter, messageLength);
        }

        public ISerialPort CreateSerialPort(SerialPortName portName, int baudRate = 9600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, int readBufferSize = 1024)
        {
            return new LinuxSerialPort(portName, baudRate, dataBits, parity, stopBits, readBufferSize);
        }

        public IDigitalOutputPort CreateDigitalOutputPort(IPin pin, bool initialState = false, OutputType initialOutputType = OutputType.PushPull)
        {
            // TODO: move to the GPIO character driver to support things like resistor mode
            return new SysFsDigitalOutputPort(_ioController, pin, initialState);
        }

        public IDigitalInputPort CreateDigitalInputPort(IPin pin, InterruptMode interruptMode = InterruptMode.None, ResistorMode resistorMode = ResistorMode.Disabled, double debounceDuration = 0, double glitchDuration = 0)
        {
            // TODO: move to the GPIO character driver to support things like resistor mode
            return new SysFsDigitalInputPort(_ioController, pin, new SysFsDigitalChannelInfo(pin.Name), interruptMode);
        }

        // ----- BELOW HERE ARE NOT YET IMPLEMENTED -----

        public IAnalogInputPort CreateAnalogInputPort(IPin pin, int sampleCount = 5, int sampleIntervalMs = 40, float voltageReference = 3.3F)
        {
            throw new NotImplementedException();
        }

        public IBiDirectionalPort CreateBiDirectionalPort(IPin pin, bool initialState = false, InterruptMode interruptMode = InterruptMode.None, ResistorMode resistorMode = ResistorMode.Disabled, PortDirectionType initialDirection = PortDirectionType.Input, double debounceDuration = 0, double glitchDuration = 0, OutputType output = OutputType.PushPull)
        {
            throw new NotImplementedException();
        }

        public IPwmPort CreatePwmPort(IPin pin, float frequency = 100, float dutyCycle = 0.5F, bool invert = false)
        {
            throw new NotImplementedException();
        }

        public ISpiBus CreateSpiBus(IPin clock, IPin mosi, IPin miso, SpiClockConfiguration config)
        {
            throw new NotImplementedException();
        }

        public ISpiBus CreateSpiBus(IPin clock, IPin mosi, IPin miso, long speedkHz = 375)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void SetClock(DateTime dateTime)
        {
            throw new NotImplementedException();
        }

        public void WatchdogEnable(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void WatchdogReset()
        {
            throw new NotImplementedException();
        }

        public void WillSleep()
        {
            throw new NotImplementedException();
        }

        public void OnWake()
        {
            throw new NotImplementedException();
        }

        public void WillReset()
        {
            throw new NotImplementedException();
        }
    }
}
