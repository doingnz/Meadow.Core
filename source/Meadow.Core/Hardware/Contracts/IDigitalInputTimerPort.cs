using System;
namespace Meadow.Hardware
{
    public interface IDigitalInputTimerPort : IDigitalPort, IObservable<DigitalInputTimerPortEventArgs>
    {
        float Frequency { get; }
    }
}
