using System;
namespace Meadow.Hardware
{
    /// <summary>
    /// Provides data for events that come from an IDigitalInputTimerPort.
    /// </summary>
    public class DigitalInputTimerPortEventArgs : EventArgs, ITimeChangeResult
    {
        /// <summary>
        /// Frequency value, in Hz.
        /// </summary>
        public float Value { get; set; }
        public DateTime New { get; set; }
        public DateTime Old { get; set; }

        public TimeSpan Delta { get { return New - Old; } }
        //public DateTime Delta { get { return DateTime.MinValue.Add(New - Old); } }

        public DigitalInputTimerPortEventArgs() { }

        public DigitalInputTimerPortEventArgs(float value, DateTime time, DateTime previous)
        {
            this.Value = value;
            this.New = time;
            this.Old = previous;
        }
    }
}