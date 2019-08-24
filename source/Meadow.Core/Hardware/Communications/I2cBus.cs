﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Meadow.Devices;
using static Meadow.Core.Interop;

namespace Meadow.Hardware
{
    /// <summary>
    /// Represents an I2C communication channel that conforms to the ICommunicationBus
    /// contract.
    /// </summary>
    public class I2cBus : II2cBus
    {
        private SemaphoreSlim _busSemaphore = new SemaphoreSlim(1, 1);

        private const int SCL_PIN = 6;
        private const int SDA_PIN = 7;

        /// <summary>
        /// I2C bus used to communicate with a device (sensor etc.).
        /// </summary>
        /// <remarks>
        /// This I2CDevice is static and shared across all instances of the I2CBus.
        /// Communication with difference devices is made possible by changing the
        /// </remarks>
        private static I2cPeripheral _device;

        ///// <summary>
        ///// Configuration property for this I2CDevice.
        ///// </summary>
        //private readonly I2cPeripheral.Configuration _configuration;

        /// <summary>
        ///     Timeout for I2C transactions.
        /// </summary>
        private readonly ushort _transactionTimeout = 100;

        private IIOController IOController { get; }

        /// <summary>
        /// Default constructor for the I2CBus class.  This is private to prevent the
        /// developer from calling it.
        /// </summary>
        private I2cBus(
            IIOController ioController,
            IPin clock,
            II2cChannelInfo clockChannel,
            IPin data,
            II2cChannelInfo dataChannel,
          ushort speed,
            ushort transactionTimeout = 100)
        {
            IOController = ioController;
            this.Enable();
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:Meadow.Foundation.Core.I2CBus" /> class.
        ///// </summary>
        ///// <param name="address">Address of the device.</param>
        ///// <param name="speed">Bus speed in kHz.</param>
        ///// <param name="transactionTimeout">Transaction timeout in milliseconds.</param>
        //public I2cBus(ushort speed, ushort transactionTimeout = 100)
        //{
        //    _configuration = new I2cPeripheral.Configuration(address, speed);
        //    if (_device == null) {
        //        _device = new I2cPeripheral(_configuration);
        //    }
        //    _transactionTimeout = transactionTimeout;
        //}

        // TODO: Speed should have default?
        public static I2cBus From(IIOController ioController, IPin clock, IPin data, ushort speed, ushort transactionTimeout = 100)
        {
            var clockChannel = clock.SupportedChannels.OfType<II2cChannelInfo>().First();
            if (clockChannel == null || clockChannel.ChannelFunction != I2cChannelFunctionType.Clock)
            {
                throw new Exception($"Pin {clock.Name} does not have I2C Clock capabilities");
            }

            var dataChannel = data.SupportedChannels.OfType<II2cChannelInfo>().First();
            if (dataChannel == null || dataChannel.ChannelFunction != I2cChannelFunctionType.Data)
            {
                throw new Exception($"Pin {clock.Name} does not have I2C Data capabilities");
            }

            return new I2cBus(ioController, clock, clockChannel, data, dataChannel, speed, transactionTimeout);
        }

        /// <summary>
        /// Write a single byte to the device.
        /// </summary>
        /// <param name="value">Value to be written (8-bits).</param>
        public void WriteByte(byte peripheralAddress, byte value)
        {
            byte[] data = { value };
            WriteBytes(peripheralAddress, data);
        }

        /// <summary>
        /// Write a number of bytes to the device.
        /// </summary>
        /// <remarks>
        /// The number of bytes to be written will be determined by the length of the byte array.
        /// </remarks>
        /// <param name="values">Values to be written.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void WriteBytes(byte peripheralAddress, params byte[] values)
        {
            SendData(peripheralAddress, values);
        }

        /// <summary>
        /// Write an unsigned short to the device.
        /// </summary>
        /// <param name="address">Address to write the first byte to.</param>
        /// <param name="value">Value to be written (16-bits).</param>
        /// <param name="order">Indicate if the data should be written as big or little endian.</param>
        public void WriteUShort(byte peripheralAddress, byte address, ushort value, ByteOrder order = ByteOrder.LittleEndian)
        {
            var data = new byte[2];
            if (order == ByteOrder.LittleEndian)
            {
                data[0] = (byte)(value & 0xff);
                data[1] = (byte)((value >> 8) & 0xff);
            }
            else
            {
                data[0] = (byte)((value >> 8) & 0xff);
                data[1] = (byte)(value & 0xff);
            }
            WriteRegisters(peripheralAddress, address, data);
        }

        /// <summary>
        /// Write a number of unsigned shorts to the device.
        /// </summary>
        /// <remarks>
        /// The number of bytes to be written will be determined by the length of the byte array.
        /// </remarks>
        /// <param name="address">Address to write the first byte to.</param>
        /// <param name="values">Values to be written.</param>
        /// <param name="order">Indicate if the data should be written as big or little endian.</param>
        public void WriteUShorts(byte peripheralAddress, byte address, ushort[] values, ByteOrder order = ByteOrder.LittleEndian)
        {
            var data = new byte[2 * values.Length];
            for (var index = 0; index < values.Length; index++)
            {
                if (order == ByteOrder.LittleEndian)
                {
                    data[2 * index] = (byte)(values[index] & 0xff);
                    data[(2 * index) + 1] = (byte)((values[index] >> 8) & 0xff);
                }
                else
                {
                    data[2 * index] = (byte)((values[index] >> 8) & 0xff);
                    data[(2 * index) + 1] = (byte)(values[index] & 0xff);
                }
            }
            WriteRegisters(peripheralAddress, address, data);
        }

        /// <summary>
        /// Write data to the device and also read some data from the device.
        /// </summary>
        /// <remarks>
        /// The number of bytes to be written and read will be determined by the length of the byte arrays.
        /// </remarks>
        /// <param name="write">Array of bytes to be written to the device.</param>
        /// <param name="length">Amount of data to read from the device.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public byte[] WriteRead(byte peripheralAddress, byte[] write, ushort length)
        {
            //_device.Config = _configuration;
            //var read = new byte[length];
            //I2cPeripheral.I2CTransaction[] transaction =
            //{
            //    I2cPeripheral.CreateWriteTransaction(write),
            //    I2cPeripheral.CreateReadTransaction(read)
            //};
            //var bytesTransferred = 0;
            //var retryCount = 0;

            //while (_device.Execute(transaction, _transactionTimeout) != (write.Length + read.Length)) {
            //    if (retryCount > 3) {
            //        throw new Exception("WriteRead: Retry count exceeded.");
            //    }
            //    retryCount++;
            //}

            ////while (bytesTransferred != (write.Length + read.Length))
            ////{
            ////    if (retryCount > 3)
            ////    {
            ////        throw new Exception("WriteRead: Retry count exceeded.");
            ////    }
            ////    retryCount++;
            ////    bytesTransferred = _device.Execute(transaction, _transactionTimeout);
            ////}
            //return read;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Write data into a single register.
        /// </summary>
        /// <param name="address">Address of the register to write to.</param>
        /// <param name="value">Value to write into the register.</param>
        public void WriteRegister(byte peripheralAddress, byte address, byte value)
        {
            byte[] data = { address, value };
            WriteBytes(peripheralAddress, data);
        }

        /// <summary>
        /// Write data to one or more registers.
        /// </summary>
        /// <remarks>
        /// This method assumes that the register address is written first followed by the data to be
        /// written into the first register followed by the data for subsequent registers.
        /// </remarks>
        /// <param name="address">Address of the first register to write to.</param>
        /// <param name="data">Data to write into the registers.</param>
        public void WriteRegisters(byte peripheralAddress, byte address, byte[] data)
        {
            var registerAndData = new byte[data.Length + 1];
            registerAndData[0] = address;
            Array.Copy(data, 0, registerAndData, 1, data.Length);
            WriteBytes(peripheralAddress, registerAndData);
        }

        /// <summary>
        ///  Read the specified number of bytes from the I2C device.
        /// </summary>
        /// <returns>The bytes.</returns>
        /// <param name="numberOfBytes">Number of bytes.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public byte[] ReadBytes(byte peripheralAddress, ushort numberOfBytes)
        {
            return ReadData(peripheralAddress, numberOfBytes);
        }

        /// <summary>
        /// Read a register from the device.
        /// </summary>
        /// <param name="address">Address of the register to read.</param>
        public byte ReadRegister(byte peripheralAddress, byte address)
        {
            byte[] registerAddress = { address };
            var result = WriteRead(peripheralAddress, registerAddress, 1);
            return result[0];
        }

        /// <summary>
        /// Read one or more registers from the device.
        /// </summary>
        /// <param name="address">Address of the first register to read.</param>
        /// <param name="length">Number of bytes to read from the device.</param>
        public byte[] ReadRegisters(byte peripheralAddress, byte address, ushort length)
        {
            byte[] registerAddress = { address };
            return WriteRead(peripheralAddress, registerAddress, length);
        }

        /// <summary>
        /// Read an usingned short from a pair of registers.
        /// </summary>
        /// <param name="address">Register address of the low byte (the high byte will follow).</param>
        /// <param name="order">Order of the bytes in the register (little endian is the default).</param>
        /// <returns>Value read from the register.</returns>
        public ushort ReadUShort(byte peripheralAddress, byte address, ByteOrder order = ByteOrder.LittleEndian)
        {
            var data = ReadRegisters(peripheralAddress, address, 2);
            ushort result = 0;
            if (order == ByteOrder.LittleEndian)
            {
                result = (ushort)((data[1] << 8) + data[0]);
            }
            else
            {
                result = (ushort)((data[0] << 8) + data[1]);
            }
            return result;
        }

        /// <summary>
        /// Read the specified number of unsigned shorts starting at the register
        /// address specified.
        /// </summary>
        /// <param name="address">First register address to read from.</param>
        /// <param name="number">Number of unsigned shorts to read.</param>
        /// <param name="order">Order of the bytes (Little or Big endian)</param>
        /// <returns>Array of unsigned shorts.</returns>
        public ushort[] ReadUShorts(byte peripheralAddress, byte address, ushort number, ByteOrder order = ByteOrder.LittleEndian)
        {
            var data = ReadRegisters(peripheralAddress, address, (ushort)((2 * number) & 0xffff));
            var result = new ushort[number];
            for (var index = 0; index < number; index++)
            {
                if (order == ByteOrder.LittleEndian)
                {
                    result[index] = (ushort)((data[(2 * index) + 1] << 8) + data[2 * index]);
                }
                else
                {
                    result[index] = (ushort)((data[2 * index] << 8) + data[(2 * index) + 1]);
                }
            }
            return result;
        }

        public void Reset()
        {
        }

        public uint Frequency { get; private set; }

        private void Enable()
        {
            // NOP for now (calling a read, write, etc initialized the bus the first time
        }

        private void BusScan()
        {
            for (byte i = 0; i < 128; i++)
            {
                var cr2 = 1u << i | STM32.I2C_CR2_START | STM32.I2C_CR2_AUTOEND & ~(STM32.I2C_CR2_RD_WRN);
                UPD.SetRegister(STM32.MEADOW_I2C1_BASE + STM32.I2C_CR2_OFFSET, cr2);

                // wait for STOP, NACK or TIMEOUT
                var isr = UPD.GetRegister(STM32.MEADOW_I2C1_BASE + STM32.I2C_ISR_OFFSET);
                Console.WriteLine("ISR: {isr:X}");
                Thread.Sleep(10);

            }

        }

        private void Disable()
        {            
            UPD.Ioctl(Nuttx.UpdIoctlFn.I2CShutdown);
        }

        public byte[] WriteReadData(byte peripheralAddress, int byteCountToRead, params byte[] dataToWrite)
        {
            var rxBuffer = new byte[byteCountToRead];
            var rxGch = GCHandle.Alloc(rxBuffer, GCHandleType.Pinned);
            var txGch = GCHandle.Alloc(dataToWrite, GCHandleType.Pinned);

            try
            {
                var command = new Nuttx.UpdI2CCommand()
                {
                    Address = peripheralAddress,
                    Frequency = (int)this.Frequency,
                    TxBufferLength = dataToWrite.Length,
                    TxBuffer = txGch.AddrOfPinnedObject(),
                    RxBufferLength = rxBuffer.Length,
                    RxBuffer = rxGch.AddrOfPinnedObject(),
                };

//                Console.Write($" +WriteReadData. Sending {dataToWrite.Length} bytes, requesting {byteCountToRead}");
                var result = UPD.Ioctl(Nuttx.UpdIoctlFn.I2CData, ref command);
//                Console.WriteLine($" returned {result}");

                // TODO: handle ioctl errors.  Common values:
                // -116 = timeout
                // -112 = address not found

                return rxBuffer;
            }
            finally
            {
                if (rxGch.IsAllocated)
                {
                    rxGch.Free();
                }
                if (txGch.IsAllocated)
                {
                    txGch.Free();
                }
            }
        }

        private byte[] ReadData(byte address, int count)
        {
            var rxBuffer = new byte[count];
            var gch = GCHandle.Alloc(rxBuffer, GCHandleType.Pinned);

            _busSemaphore.Wait();

            try
            {
                var command = new Nuttx.UpdI2CCommand()
                {
                    Address = address,
                    Frequency = (int)this.Frequency,
                    TxBufferLength = 0,
                    TxBuffer = IntPtr.Zero,
                    RxBufferLength = rxBuffer.Length,
                    RxBuffer = gch.AddrOfPinnedObject(),
                };

                Console.Write(" +ReadData");
                var result = UPD.Ioctl(Nuttx.UpdIoctlFn.I2CData, ref command);
                Console.WriteLine($" returned {result}");

                // TODO: handle ioctl errors.  Common values:
                // -116 = timeout

                return rxBuffer;
            }
            finally
            {
                _busSemaphore.Release();

                if (gch.IsAllocated)
                {
                    gch.Free();
                }
            }
        }

        private void SendData(byte address, byte[] data)
        {
            var gch = GCHandle.Alloc(data, GCHandleType.Pinned);

            _busSemaphore.Wait();

            try
            {
                var command = new Nuttx.UpdI2CCommand()
                {
                    Address = address,
                    Frequency = (int)this.Frequency,
                    TxBufferLength = data.Length,
                    TxBuffer = gch.AddrOfPinnedObject(),
                    RxBufferLength = 0,
                    RxBuffer = IntPtr.Zero
                };

                Console.Write(" +SendData");
                var result = UPD.Ioctl(Nuttx.UpdIoctlFn.I2CData, ref command);
                Console.WriteLine($" returned {result}");

                // TODO: handle ioctl errors.  Common values:
                // -116 = timeout                                           
            }
            finally
            {
                _busSemaphore.Release();

                if (gch.IsAllocated)
                {
                    gch.Free();
                }
            }
        }
    }
}
