using System;
using System.Collections.Generic;

namespace Meadow.Hardware
{
    /// <summary>
    /// Provides a base implementation for digital input timer ports.
    /// </summary>
    public abstract class DigitalInputTimerPortBase : DigitalPortBase, IDigitalInputTimerPort
    {
        /// <summary>
        /// Occurs when the frequency is changed.
        /// </summary>
        public event EventHandler<DigitalInputTimerPortEventArgs> Changed = delegate { };

        public abstract float Frequency { get; }

        protected List<IObserver<DigitalInputTimerPortEventArgs>> _observers { get; set; } = new List<IObserver<DigitalInputTimerPortEventArgs>>();

        protected DigitalInputTimerPortBase(
            IPin pin,
            IDigitalChannelInfo channel
            )
            : base(pin, channel)
        {
        }

        protected void RaiseChangedAndNotify(DigitalInputTimerPortEventArgs changeResult)
        {
            Changed?.Invoke(this, changeResult);
            _observers.ForEach(x => x.OnNext(changeResult));
        }

        public IDisposable Subscribe(IObserver<DigitalInputTimerPortEventArgs> observer)
        {
            if (!_observers.Contains(observer)) _observers.Add(observer);
            return new Unsubscriber(_observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<DigitalInputTimerPortEventArgs>> _observers;
            private IObserver<DigitalInputTimerPortEventArgs> _observer;

            public Unsubscriber(List<IObserver<DigitalInputTimerPortEventArgs>> observers, IObserver<DigitalInputTimerPortEventArgs> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }

    }
}