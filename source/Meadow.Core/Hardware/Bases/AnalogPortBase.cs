using System;
namespace Meadow.Hardware
{
    /// <summary>
    /// Provides a base implementation for much of the common tasks of 
    /// implementing IAnalogPort
    /// </summary>
    public abstract class AnalogPortBase : PortBase<IAnalogChannelInfo>, IAnalogPort
    {
        public new IAnalogChannelInfo Channel { get; protected set; }

        protected AnalogPortBase(IPin pin, IAnalogChannelInfo channel)
            : base(pin, channel)
        {
            this.Channel = channel;
        }
    }
}
