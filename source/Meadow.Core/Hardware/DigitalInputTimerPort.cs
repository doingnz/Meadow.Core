using System;
using System.Collections.Generic;
using System.Linq;

namespace Meadow.Hardware
{
    /// <summary>
    /// Represents a timer port that is capable of reading the frequency of
    /// digital input signal changes.
    /// </summary>
    public class DigitalInputTimerPort : DigitalInputTimerPortBase
    {
        protected IIOController IOController { get; set; }

        private DateTime LastEventTime { get; set; } = DateTime.MinValue;

        protected DigitalInputTimerPort(
            IPin pin,
            IIOController ioController,
            IDigitalChannelInfo channel,
            InterruptMode interruptMode = InterruptMode.EdgeRising
            ) : base(pin, channel)
        {
            this.IOController = ioController;
            this.IOController.FrequencyChanged += OnFrequencyChanged;

            // attempt to reserve
            var success = DeviceChannelManager.ReservePin(pin, ChannelConfigurationType.DigitalInput);
            if (success.Item1) {
                // make sure the pin is configured as a digital input with the proper state
                ioController.ConfigureInputTimer(pin, interruptMode);
            } else {
                throw new PortInUseException();
            }
        }

        public static DigitalInputTimerPort From(
            IPin pin,
            IIOController ioController,
            InterruptMode interruptMode = InterruptMode.EdgeRising
            )
        {
            var chan = pin.SupportedChannels.OfType<IDigitalChannelInfo>().FirstOrDefault();
            //TODO: may need other checks here.
            if (chan == null) {
                throw new Exception("Unable to create an input port on the pin, because it doesn't have a digital channel");
            }
            if (interruptMode != InterruptMode.None && (!chan.InterruptCapable)) {
                throw new Exception("Unable to create input; channel is not capable of interrupts");
            }

            var port = new DigitalInputTimerPort(pin, ioController, chan, interruptMode);
            return port;
        }

        void OnFrequencyChanged(IPin pin, float frequency)
        {
            if (pin == this.Pin) {
                var capturedLastTime = LastEventTime; // note: doing this for latency reasons. kind of. sort of. bad time good time. all time.
                this.LastEventTime = DateTime.Now;
                RaiseChangedAndNotify(new DigitalInputTimerPortEventArgs(frequency, this.LastEventTime, capturedLastTime));
            }
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // TODO: we should consider moving this logic to the finalizer
            // but the problem with that is that we don't know when it'll be called
            // but if we do it in here, we may need to check the _disposed field
            // elsewhere
            if (!disposed) {
                if (disposing) {
                    this.IOController.FrequencyChanged -= OnFrequencyChanged;
                    DeviceChannelManager.ReleasePin(Pin);
                    IOController.UnconfigureGpio(Pin);
                }
                disposed = true;
            }
        }

        // Finalizer
        ~DigitalInputTimerPort()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the current State of the input (True == high, False == low)
        /// </summary>
        public override float Frequency {
            get {
                //TODO: @PeterM; maybe the IOController needs a GetFrequency() method?
                //return this.IOController.GetDiscrete(this.Pin);
                return 0f;
            }
        }

    }
}