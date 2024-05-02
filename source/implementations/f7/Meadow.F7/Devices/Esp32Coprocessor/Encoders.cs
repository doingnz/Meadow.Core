using Meadow.Hardware;
using System;
using System.Text;

namespace Meadow.Devices.Esp32.MessagePayloads
{
    /// <summary>
    /// Encapsulate the various message encoder and extract methods.
    /// </summary>
    public static class Encoders
    {
        /// <summary>
        /// Calculate the amount of memory that should be allocated for the receive buffer.
        /// 
        /// SPI reception on the ESP32 should be on a 32-bit boundary and also a multiple of 
        /// 4 bytes long with a minimum length of 8 bytes (See the article linked below).
        /// 
        /// https://docs.espressif.com/projects/esp-idf/en/latest/api-reference/peripherals/spi_slave.html#restrictions-and-known-issues
        /// </summary>
        /// <returns>Size of the buffer that should be used.</returns>
        /// <param name="requestedSize">Requested size.</param>
        public static UInt32 CalculateSpiBufferSize(UInt32 requestedSize)
        {
            UInt32 result = requestedSize;

            if (result < 8)
            {
                result = 8;
            }
            //
            //  The buffer should always be 4 bytes longer than needed.  During development it
            //  was found that the last four bytes of any transmission were being discarded.
            //  Empirical tests proved this for 24, 32 and 40 byte packets.
            //
            //  The work around is to increase the packet size by 4 and have dummy data in the
            //  last four bytes and discard the bytes.
            //
            //
            //  See support post: https://esp32.com/viewtopic.php?f=13&t=10117
            //
            requestedSize += 4;
            if ((requestedSize & 3) != 0)
            {
                result = (requestedSize & 0xfffffffc) + 4;
            }
            else
            {
                result += 4;
            }
            return result;
        }

        /// <summary>
        /// Seed for the Crc32 algorithm.
        /// </summary>
        private const UInt32 CRC32_SEED = 0xffffffff;

        /// <summary>
        ///     Calculate the 32-bit CRC for the array of bytes.
        /// </summary>
        /// <param name="buffer">Buffer of data.</param>
        /// <returns>32-bit CRC for the array of bytes.</returns>
        private static UInt32 Crc32(byte[] buffer)
        {
            return Crc32(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Calculate the 32-bit CRC for the array of bytes.
        /// </summary>
        /// <param name="data">Buffer of data.</param>
        /// <param name="start">Offset into the buffer at which the CRC calculation will start</param>
        /// <param name="length">Amount of data to calculate the CRC</param>
        /// <returns>32-bit CRC for the array of bytes.</returns>
        private static UInt32 Crc32(byte[] data, int start, int length)
        {
            UInt32 crc = CRC32_SEED;

            for (int index = start; index < length; index++)
            {
                crc = ProgressiveCrc32(crc, data[index]);
            }

            return crc;
        }

        /// <summary>
        ///     Progressively calculate the Crc32 value.
        /// </summary>
        /// <remarks>
        ///     This allows method allows the Crc32 value to be built up as the data bytes
        ///     from the serial port are received.  This means that all of the data frame
        ///     does not need to be present in order to calculate this value.
        /// </remarks>
        /// <param name="currentChecksum">Current value for the _originalChecksum.</param>
        /// <param name="data">ApplicationData byte to be processed.</param>
        /// <returns>Next value for the Crc32.</returns>
        private static UInt32 ProgressiveCrc32(UInt32 currentChecksum, byte data)
        {
            UInt32 crc = currentChecksum;
            crc ^= data;
            for (UInt32 index = 0; index < 8; index++)
            {
                UInt32 mask = (UInt32)(-(crc & 0x01));
                crc = (crc >> 1) ^ (0xedb88320 & mask);
            }
            return crc;
        }

        /// <summary>
        ///     Take two bytes from the buffer and encode them as a 16-bit integer.
        ///     Note that the data should be encoded as LSB first.
        /// </summary>
        /// <param name="buffer">Buffer holding the value.</param>
        /// <param name="offset">Offset into the buffer for the LSB.</param>
        public static UInt16 ExtractUInt16(byte[] buffer, int offset = 0)
        {
            UInt16 result;

            result = buffer[offset];
            result |= (UInt16)(buffer[offset + 1] << 8);
            return result;
        }

        /// <summary>
        ///     Encode a 16-bit integer as two bytes in the buffer.  The data is
        ///     encoded LSB first.
        /// </summary>
        /// <param name="value">Value to encode.</param>
        /// <param name="buffer">Buffer to store the value.</param>
        /// <param name="offset">Offset into the buffer for the first byte to store.</param>
        public static void EncodeUInt16(UInt16 value, byte[] buffer, int offset = 0)
        {
            buffer[offset] = (byte)(value & 0xff);
            buffer[offset + 1] = (byte)((value >> 8) & 0xff);
        }

        /// <summary>
        ///     Take four bytes from the buffer and encode them as a 32-bit unsigned integer.
        ///     Note that the data should be encoded as LSB first.
        /// </summary>
        /// <param name="buffer">Buffer holding the 32-bit value to decode.</param>
        /// <param name="offset">Offset of the 32-bit in the buffer.</param>
        public static UInt32 ExtractUInt32(byte[] buffer, int offset = 0)
        {
            UInt32 result;

            result = buffer[offset];
            result |= (UInt32)(buffer[offset + 1] << 8);
            result |= (UInt32)(buffer[offset + 2] << 16);
            result |= (UInt32)(buffer[offset + 3] << 24);
            return result;
        }

        /// <summary>
        ///     Encode a 32-bit unsigned integer as four bytes in the buffer.  The data is
        ///     encoded LSB first.
        /// </summary>
        /// <param name="value">Value to encode.</param>
        /// <param name="buffer">Buffer to store the value.</param>
        /// <param name="offset">Offset into the buffer for the first byte to store.</param>
        public static void EncodeUInt32(UInt32 value, byte[] buffer, int offset = 0)
        {
            buffer[offset] = (byte)(value & 0xff);
            buffer[offset + 1] = (byte)((value >> 8) & 0xff);
            buffer[offset + 2] = (byte)((value >> 16) & 0xff);
            buffer[offset + 3] = (byte)((value >> 24) & 0xff);
        }

        /// <summary>
        ///     Take four bytes from the buffer and encode them as a 32-bit integer.
        ///     Note that the data should be encoded as LSB first.
        /// </summary>
        /// <param name="buffer">Buffer holding the 32-bit value to decode.</param>
        /// <param name="offset">Offset of the 32-bit in the buffer.</param>
        public static Int32 ExtractInt32(byte[] buffer, int offset = 0)
        {
            Int32 result;

            result = buffer[offset];
            result |= buffer[offset + 1] << 8;
            result |= buffer[offset + 2] << 16;
            result |= buffer[offset + 3] << 24;
            return result;
        }

        /// <summary>
        ///     Encode a 32-bit integer as four bytes in the buffer.  The data is
        ///     encoded LSB first.
        /// </summary>
        /// <param name="value">Value to encode.</param>
        /// <param name="buffer">Buffer to store the value.</param>
        /// <param name="offset">Offset into the buffer for the first byte to store.</param>
        public static void EncodeInt32(Int32 value, byte[] buffer, int offset = 0)
        {
            buffer[offset] = (byte)(value & 0xff);
            buffer[offset + 1] = (byte)((value >> 8) & 0xff);
            buffer[offset + 2] = (byte)((value >> 16) & 0xff);
            buffer[offset + 3] = (byte)((value >> 24) & 0xff);
        }

        /// <summary>
        ///     Encode a string into the buffer as a stream of bytes.
        /// </summary>
        /// <remarks>
        ///     An additional null terminating byte is added to the end of the string.
        /// </remarks>
        /// <param name="str">String to encode.</param>
        /// <param name="buffer">Buffer to hold the characters.</param>
        /// <param name="offset">Offset into the buffer for the first character.</param>
        public static void EncodeString(string str, byte[] buffer, int offset = 0)
        {
            char[] data = str.ToCharArray();
            foreach (char c in data)
            {
                buffer[offset] = (byte)c;
                offset++;
            }
            buffer[offset] = 0;
        }

        /// <summary>
        ///     Extract a string from the buffer.
        /// </summary>
        /// <remarks>
        ///     Extraction will start offset bytes into the buffer and will
        ///     stop when a 0 (C string terminator) byte in encountered.
        /// </remarks>
        /// <returns>The string.</returns>
        /// <param name="buffer">Buffer holding the data to be decoded.</param>
        /// <param name="offset">Offset into the buffer of the string.</param>
        public static string ExtractString(byte[] buffer, int offset = 0)
        {
            string result = "";

            while (buffer[offset] != 0)
            {
                result += (char)buffer[offset];
                offset++;
            }
            return result;
        }

        /// <summary>
        /// Encode a SystemConfiguration object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="systemConfiguration">SystemConfiguration object to be encoded.</param>
        /// <returns>Byte array containing the encoded SystemConfiguration object.</returns>
        public static byte[] EncodeSystemConfiguration(MessagePayloads.SystemConfiguration systemConfiguration)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 6;
            length += 6;
            length += 6;
            length += systemConfiguration.DeviceName.Length + 1;
            length += systemConfiguration.DefaultAccessPoint.Length + 1;
            length += systemConfiguration.BuildBranchName.Length + 1;
            length += 33;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            buffer[offset] = systemConfiguration.MaximumMessageQueueLength;
            offset += 1;
            EncodeInt32(systemConfiguration.MaximumRetryCount, buffer, offset);
            offset += 4;
            buffer[offset] = systemConfiguration.Antenna;
            offset += 1;
            Array.Copy(systemConfiguration.BoardMacAddress, 0, buffer, offset, 6);
            offset += 6;
            Array.Copy(systemConfiguration.SoftApMacAddress, 0, buffer, offset, 6);
            offset += 6;
            Array.Copy(systemConfiguration.BluetoothMacAddress, 0, buffer, offset, 6);
            offset += 6;
            EncodeString(systemConfiguration.DeviceName, buffer, offset);
            offset += systemConfiguration.DeviceName.Length + 1;
            EncodeString(systemConfiguration.DefaultAccessPoint, buffer, offset);
            offset += systemConfiguration.DefaultAccessPoint.Length + 1;
            buffer[offset] = systemConfiguration.ResetReason;
            offset += 1;
            EncodeUInt32(systemConfiguration.VersionMajor, buffer, offset);
            offset += 4;
            EncodeUInt32(systemConfiguration.VersionMinor, buffer, offset);
            offset += 4;
            EncodeUInt32(systemConfiguration.VersionRevision, buffer, offset);
            offset += 4;
            EncodeUInt32(systemConfiguration.VersionBuild, buffer, offset);
            offset += 4;
            buffer[offset] = systemConfiguration.BuildDay;
            offset += 1;
            buffer[offset] = systemConfiguration.BuildMonth;
            offset += 1;
            buffer[offset] = systemConfiguration.BuildYear;
            offset += 1;
            buffer[offset] = systemConfiguration.BuildHour;
            offset += 1;
            buffer[offset] = systemConfiguration.BuildMinute;
            offset += 1;
            buffer[offset] = systemConfiguration.BuildSecond;
            offset += 1;
            EncodeUInt32(systemConfiguration.BuildHash, buffer, offset);
            offset += 4;
            EncodeString(systemConfiguration.BuildBranchName, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a SystemConfiguration object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SystemConfiguration object.</returns>
        public static MessagePayloads.SystemConfiguration ExtractSystemConfiguration(byte[] buffer, int offset)
        {
            SystemConfiguration systemConfiguration = new MessagePayloads.SystemConfiguration();

            systemConfiguration.MaximumMessageQueueLength = buffer[offset];
            offset += 1;
            systemConfiguration.MaximumRetryCount = ExtractInt32(buffer, offset);
            offset += 4;
            systemConfiguration.Antenna = buffer[offset];
            offset += 1;
            systemConfiguration.BoardMacAddress = new byte[6];
            Array.Copy(buffer, offset, systemConfiguration.BoardMacAddress, 0, 6);
            offset += 6;
            systemConfiguration.SoftApMacAddress = new byte[6];
            Array.Copy(buffer, offset, systemConfiguration.SoftApMacAddress, 0, 6);
            offset += 6;
            systemConfiguration.BluetoothMacAddress = new byte[6];
            Array.Copy(buffer, offset, systemConfiguration.BluetoothMacAddress, 0, 6);
            offset += 6;
            systemConfiguration.DeviceName = ExtractString(buffer, offset);
            offset += systemConfiguration.DeviceName.Length + 1;
            systemConfiguration.DefaultAccessPoint = ExtractString(buffer, offset);
            offset += systemConfiguration.DefaultAccessPoint.Length + 1;
            systemConfiguration.ResetReason = buffer[offset];
            offset += 1;
            systemConfiguration.VersionMajor = ExtractUInt32(buffer, offset);
            offset += 4;
            systemConfiguration.VersionMinor = ExtractUInt32(buffer, offset);
            offset += 4;
            systemConfiguration.VersionRevision = ExtractUInt32(buffer, offset);
            offset += 4;
            systemConfiguration.VersionBuild = ExtractUInt32(buffer, offset);
            offset += 4;
            systemConfiguration.BuildDay = buffer[offset];
            offset += 1;
            systemConfiguration.BuildMonth = buffer[offset];
            offset += 1;
            systemConfiguration.BuildYear = buffer[offset];
            offset += 1;
            systemConfiguration.BuildHour = buffer[offset];
            offset += 1;
            systemConfiguration.BuildMinute = buffer[offset];
            offset += 1;
            systemConfiguration.BuildSecond = buffer[offset];
            offset += 1;
            systemConfiguration.BuildHash = ExtractUInt32(buffer, offset);
            offset += 4;
            systemConfiguration.BuildBranchName = ExtractString(buffer, offset);
            return systemConfiguration;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SystemConfiguration object.
        /// </summary>
        /// <param name="systemConfiguration">SystemConfiguration object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SystemConfiguration object.</returns>
        public static int EncodedSystemConfigurationBufferSize(MessagePayloads.SystemConfiguration systemConfiguration)
        {
            int result = 0;
            result += systemConfiguration.DeviceName.Length;
            result += systemConfiguration.DefaultAccessPoint.Length;
            result += systemConfiguration.BuildBranchName.Length;
            return result + 54;
        }

        /// <summary>
        /// Encode a ConfigurationValue object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="configurationValue">ConfigurationValue object to be encoded.</param>
        /// <returns>Byte array containing the encoded ConfigurationValue object.</returns>
        public static byte[] EncodeConfigurationValue(MessagePayloads.ConfigurationValue configurationValue)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(configurationValue.ValueLength + 4);
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(configurationValue.Item, buffer, offset);
            offset += 4;
            EncodeUInt32(configurationValue.ValueLength, buffer, offset);
            offset += 4;
            if (configurationValue.ValueLength > 0)
            {
                Array.Copy(configurationValue.Value, 0, buffer, offset, configurationValue.ValueLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a ConfigurationValue object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ConfigurationValue object.</returns>
        public static MessagePayloads.ConfigurationValue ExtractConfigurationValue(byte[] buffer, int offset)
        {
            ConfigurationValue configurationValue = new MessagePayloads.ConfigurationValue();

            configurationValue.Item = ExtractUInt32(buffer, offset);
            offset += 4;
            configurationValue.ValueLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (configurationValue.ValueLength > 0)
            {
                configurationValue.Value = new byte[configurationValue.ValueLength];
                Array.Copy(buffer, offset, configurationValue.Value, 0, configurationValue.ValueLength);
            }
            return configurationValue;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ConfigurationValue object.
        /// </summary>
        /// <param name="configurationValue">ConfigurationValue object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ConfigurationValue object.</returns>
        public static int EncodedConfigurationValueBufferSize(MessagePayloads.ConfigurationValue configurationValue)
        {
            int result = 0;
            result += (int)configurationValue.ValueLength;
            return result + 8;
        }

        /// <summary>
        /// Encode a ErrorEvent object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="errorEvent">ErrorEvent object to be encoded.</param>
        /// <returns>Byte array containing the encoded ErrorEvent object.</returns>
        public static byte[] EncodeErrorEvent(MessagePayloads.ErrorEvent errorEvent)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(errorEvent.ErrorDataLength + 4);
            length += 5;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(errorEvent.ErrorCode, buffer, offset);
            offset += 4;
            buffer[offset] = errorEvent.Interface;
            offset += 1;
            EncodeUInt32(errorEvent.ErrorDataLength, buffer, offset);
            offset += 4;
            if (errorEvent.ErrorDataLength > 0)
            {
                Array.Copy(errorEvent.ErrorData, 0, buffer, offset, errorEvent.ErrorDataLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a ErrorEvent object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ErrorEvent object.</returns>
        public static MessagePayloads.ErrorEvent ExtractErrorEvent(byte[] buffer, int offset)
        {
            ErrorEvent errorEvent = new MessagePayloads.ErrorEvent();

            errorEvent.ErrorCode = ExtractUInt32(buffer, offset);
            offset += 4;
            errorEvent.Interface = buffer[offset];
            offset += 1;
            errorEvent.ErrorDataLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (errorEvent.ErrorDataLength > 0)
            {
                errorEvent.ErrorData = new byte[errorEvent.ErrorDataLength];
                Array.Copy(buffer, offset, errorEvent.ErrorData, 0, errorEvent.ErrorDataLength);
            }
            return errorEvent;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ErrorEvent object.
        /// </summary>
        /// <param name="errorEvent">ErrorEvent object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ErrorEvent object.</returns>
        public static int EncodedErrorEventBufferSize(MessagePayloads.ErrorEvent errorEvent)
        {
            int result = 0;
            result += (int)errorEvent.ErrorDataLength;
            return result + 9;
        }

        /// <summary>
        /// Encode a AccessPointInformation object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="accessPointInformation">AccessPointInformation object to be encoded.</param>
        /// <returns>Byte array containing the encoded AccessPointInformation object.</returns>
        public static byte[] EncodeAccessPointInformation(MessagePayloads.AccessPointInformation accessPointInformation)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += accessPointInformation.NetworkName.Length + 1;
            length += accessPointInformation.Password.Length + 1;
            length += 15;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeString(accessPointInformation.NetworkName, buffer, offset);
            offset += accessPointInformation.NetworkName.Length + 1;
            EncodeString(accessPointInformation.Password, buffer, offset);
            offset += accessPointInformation.Password.Length + 1;
            EncodeUInt32(accessPointInformation.IpAddress, buffer, offset);
            offset += 4;
            EncodeUInt32(accessPointInformation.SubnetMask, buffer, offset);
            offset += 4;
            EncodeUInt32(accessPointInformation.Gateway, buffer, offset);
            offset += 4;
            buffer[offset] = accessPointInformation.WiFiAuthenticationMode;
            offset += 1;
            buffer[offset] = accessPointInformation.Channel;
            offset += 1;
            buffer[offset] = accessPointInformation.Hidden;
            return buffer;
        }

        /// <summary>
        /// Extract a AccessPointInformation object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>AccessPointInformation object.</returns>
        public static MessagePayloads.AccessPointInformation ExtractAccessPointInformation(byte[] buffer, int offset)
        {
            AccessPointInformation accessPointInformation = new MessagePayloads.AccessPointInformation();

            accessPointInformation.NetworkName = ExtractString(buffer, offset);
            offset += accessPointInformation.NetworkName.Length + 1;
            accessPointInformation.Password = ExtractString(buffer, offset);
            offset += accessPointInformation.Password.Length + 1;
            accessPointInformation.IpAddress = ExtractUInt32(buffer, offset);
            offset += 4;
            accessPointInformation.SubnetMask = ExtractUInt32(buffer, offset);
            offset += 4;
            accessPointInformation.Gateway = ExtractUInt32(buffer, offset);
            offset += 4;
            accessPointInformation.WiFiAuthenticationMode = buffer[offset];
            offset += 1;
            accessPointInformation.Channel = buffer[offset];
            offset += 1;
            accessPointInformation.Hidden = buffer[offset];
            return accessPointInformation;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the AccessPointInformation object.
        /// </summary>
        /// <param name="accessPointInformation">AccessPointInformation object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded AccessPointInformation object.</returns>
        public static int EncodedAccessPointInformationBufferSize(MessagePayloads.AccessPointInformation accessPointInformation)
        {
            int result = 0;
            result += accessPointInformation.NetworkName.Length;
            result += accessPointInformation.Password.Length;
            return result + 17;
        }

        /// <summary>
        /// Encode a DisconnectFromAccessPointRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="disconnectFromAccessPointRequest">DisconnectFromAccessPointRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded DisconnectFromAccessPointRequest object.</returns>
        public static byte[] EncodeDisconnectFromAccessPointRequest(MessagePayloads.DisconnectFromAccessPointRequest disconnectFromAccessPointRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 1;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            buffer[offset] = disconnectFromAccessPointRequest.TurnOffWiFiInterface;
            return buffer;
        }

        /// <summary>
        /// Extract a DisconnectFromAccessPointRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>DisconnectFromAccessPointRequest object.</returns>
        public static MessagePayloads.DisconnectFromAccessPointRequest ExtractDisconnectFromAccessPointRequest(byte[] buffer, int offset)
        {
            DisconnectFromAccessPointRequest disconnectFromAccessPointRequest = new MessagePayloads.DisconnectFromAccessPointRequest();

            disconnectFromAccessPointRequest.TurnOffWiFiInterface = buffer[offset];
            return disconnectFromAccessPointRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the DisconnectFromAccessPointRequest object.
        /// </summary>
        /// <param name="disconnectFromAccessPointRequest">DisconnectFromAccessPointRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded DisconnectFromAccessPointRequest object.</returns>
        public static int EncodedDisconnectFromAccessPointRequestBufferSize(MessagePayloads.DisconnectFromAccessPointRequest disconnectFromAccessPointRequest)
        {
            return 1;
        }

        /// <summary>
        /// Encode a ConnectEventData object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="connectEventData">ConnectEventData object to be encoded.</param>
        /// <returns>Byte array containing the encoded ConnectEventData object.</returns>
        public static byte[] EncodeConnectEventData(MessagePayloads.ConnectEventData connectEventData)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 33;
            length += 6;
            length += 18;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(connectEventData.IpAddress, buffer, offset);
            offset += 4;
            EncodeUInt32(connectEventData.SubnetMask, buffer, offset);
            offset += 4;
            EncodeUInt32(connectEventData.Gateway, buffer, offset);
            offset += 4;
            int amount = connectEventData.Ssid.Length >= 33 ? 33 - 1 : connectEventData.Ssid.Length;
            for (int index = 0; index < amount; index++)
            {
                buffer[index] = (byte)connectEventData.Ssid[index];
            }
            buffer[amount] = 0;
            offset += 33;
            Array.Copy(connectEventData.Bssid, 0, buffer, offset, 6);
            offset += 6;
            buffer[offset] = connectEventData.Channel;
            offset += 1;
            buffer[offset] = connectEventData.AuthenticationMode;
            offset += 1;
            EncodeUInt32(connectEventData.Reason, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a ConnectEventData object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ConnectEventData object.</returns>
        public static MessagePayloads.ConnectEventData ExtractConnectEventData(byte[] buffer, int offset)
        {
            ConnectEventData connectEventData = new MessagePayloads.ConnectEventData();

            connectEventData.IpAddress = ExtractUInt32(buffer, offset);
            offset += 4;
            connectEventData.SubnetMask = ExtractUInt32(buffer, offset);
            offset += 4;
            connectEventData.Gateway = ExtractUInt32(buffer, offset);
            offset += 4;
            for (int index = 0; (buffer[index + offset] != 0) && (index < (33 - 1)); index++)
            {
                connectEventData.Ssid += Convert.ToChar(buffer[index + offset]);
            }
            offset += 33;
            connectEventData.Bssid = new byte[6];
            Array.Copy(buffer, offset, connectEventData.Bssid, 0, 6);
            offset += 6;
            connectEventData.Channel = buffer[offset];
            offset += 1;
            connectEventData.AuthenticationMode = buffer[offset];
            offset += 1;
            connectEventData.Reason = ExtractUInt32(buffer, offset);
            return connectEventData;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ConnectEventData object.
        /// </summary>
        /// <param name="connectEventData">ConnectEventData object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ConnectEventData object.</returns>
        public static int EncodedConnectEventDataBufferSize(MessagePayloads.ConnectEventData connectEventData)
        {
            return 57;
        }

        /// <summary>
        /// Encode a NodeConnectionChangeEventData object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="nodeConnectionChangeEventData">NodeConnectionChangeEventData object to be encoded.</param>
        /// <returns>Byte array containing the encoded NodeConnectionChangeEventData object.</returns>
        public static byte[] EncodeNodeConnectionChangeEventData(MessagePayloads.NodeConnectionChangeEventData nodeConnectionChangeEventData)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 6;
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(nodeConnectionChangeEventData.IpAddress, buffer, offset);
            offset += 4;
            Array.Copy(nodeConnectionChangeEventData.MacAddress, 0, buffer, offset, 6);
            return buffer;
        }

        /// <summary>
        /// Extract a NodeConnectionChangeEventData object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>NodeConnectionChangeEventData object.</returns>
        public static MessagePayloads.NodeConnectionChangeEventData ExtractNodeConnectionChangeEventData(byte[] buffer, int offset)
        {
            NodeConnectionChangeEventData nodeConnectionChangeEventData = new MessagePayloads.NodeConnectionChangeEventData();

            nodeConnectionChangeEventData.IpAddress = ExtractUInt32(buffer, offset);
            offset += 4;
            nodeConnectionChangeEventData.MacAddress = new byte[6];
            Array.Copy(buffer, offset, nodeConnectionChangeEventData.MacAddress, 0, 6);
            return nodeConnectionChangeEventData;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the NodeConnectionChangeEventData object.
        /// </summary>
        /// <param name="nodeConnectionChangeEventData">NodeConnectionChangeEventData object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded NodeConnectionChangeEventData object.</returns>
        public static int EncodedNodeConnectionChangeEventDataBufferSize(MessagePayloads.NodeConnectionChangeEventData nodeConnectionChangeEventData)
        {
            return 10;
        }

        /// <summary>
        /// Encode a DisconnectEventData object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="disconnectEventData">DisconnectEventData object to be encoded.</param>
        /// <returns>Byte array containing the encoded DisconnectEventData object.</returns>
        public static byte[] EncodeDisconnectEventData(MessagePayloads.DisconnectEventData disconnectEventData)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 42;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            buffer[offset] = disconnectEventData.SsidLength;
            offset += 1;
            Array.Copy(buffer, offset, disconnectEventData.Bssid, 0, 6);
            offset += 6;
            buffer[offset] = disconnectEventData.Rssi;
            offset += 1;
            buffer[offset] = disconnectEventData.Reason;
            return buffer;
        }

        /// <summary>
        /// Extract a DisconnectEventData object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>DisconnectEventData object.</returns>
        public static MessagePayloads.DisconnectEventData ExtractDisconnectEventData(byte[] buffer, int offset)
        {
            // determine our core version
            int protocolVersion;

            try
            {
                protocolVersion = Core.Interop.Nuttx.meadow_os_native_protocol_version();
            }
            catch (EntryPointNotFoundException)
            {
                protocolVersion = 0; // pre 2.0
            }

            switch (protocolVersion)
            {
                case 0:
                    return new DisconnectEventData
                    {
                        Reason = (byte)NetworkDisconnectReason.Unspecified
                    };
                default:
                    // implemented in Protocol 1+ (v 2.0+)
                    var ssid = Encoding.UTF8.GetString(buffer, offset, 33).TrimEnd('\0');
                    offset += 33;
                    var ssidLength = buffer[offset++];
                    var bssid = new byte[6];
                    Array.Copy(buffer, offset, bssid, 0, 6);
                    offset += 6;
                    var rssi = buffer[offset++];
                    var reason = buffer[offset];

                    return new DisconnectEventData
                    {
                        Ssid = ssid,
                        SsidLength = ssidLength,
                        Bssid = bssid,
                        Rssi = rssi,
                        Reason = reason
                    };
            }
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the DisconnectEventData object.
        /// </summary>
        /// <param name="disconnectEventData">DisconnectEventData object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded DisconnectEventData object.</returns>
        public static int EncodedDisconnectEventDataBufferSize(MessagePayloads.DisconnectEventData disconnectEventData)
        {
            return 42;
        }

        /// <summary>
        /// Encode a AccessPoint object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="accessPoint">AccessPoint object to be encoded.</param>
        /// <returns>Byte array containing the encoded AccessPoint object.</returns>
        public static byte[] EncodeAccessPoint(MessagePayloads.AccessPoint accessPoint)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 33;
            length += 6;
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            int amount = accessPoint.Ssid.Length >= 33 ? 33 - 1 : accessPoint.Ssid.Length;
            for (int index = 0; index < amount; index++)
            {
                buffer[index] = (byte)accessPoint.Ssid[index];
            }
            buffer[amount] = 0;
            offset += 33;
            Array.Copy(accessPoint.Bssid, 0, buffer, offset, 6);
            offset += 6;
            buffer[offset] = accessPoint.PrimaryChannel;
            offset += 1;
            buffer[offset] = accessPoint.SecondaryChannel;
            offset += 1;
            buffer[offset] = (byte)accessPoint.Rssi;
            offset += 1;
            buffer[offset] = accessPoint.AuthenticationMode;
            offset += 1;
            EncodeUInt32(accessPoint.Protocols, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a AccessPoint object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>AccessPoint object.</returns>
        public static MessagePayloads.AccessPoint ExtractAccessPoint(byte[] buffer, int offset)
        {
            AccessPoint accessPoint = new MessagePayloads.AccessPoint();

            for (int index = 0; (buffer[index + offset] != 0) && (index < (33 - 1)); index++)
            {
                accessPoint.Ssid += Convert.ToChar(buffer[index + offset]);
            }
            offset += 33;
            accessPoint.Bssid = new byte[6];
            Array.Copy(buffer, offset, accessPoint.Bssid, 0, 6);
            offset += 6;
            accessPoint.PrimaryChannel = buffer[offset];
            offset += 1;
            accessPoint.SecondaryChannel = buffer[offset];
            offset += 1;
            accessPoint.Rssi = (sbyte)buffer[offset];
            offset += 1;
            accessPoint.AuthenticationMode = buffer[offset];
            offset += 1;
            accessPoint.Protocols = ExtractUInt32(buffer, offset);
            return accessPoint;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the AccessPoint object.
        /// </summary>
        /// <param name="accessPoint">AccessPoint object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded AccessPoint object.</returns>
        public static int EncodedAccessPointBufferSize(MessagePayloads.AccessPoint accessPoint)
        {
            return 47;
        }

        /// <summary>
        /// Encode a AccessPointList object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="accessPointList">AccessPointList object to be encoded.</param>
        /// <returns>Byte array containing the encoded AccessPointList object.</returns>
        public static byte[] EncodeAccessPointList(MessagePayloads.AccessPointList accessPointList)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(accessPointList.AccessPointsLength + 4);
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(accessPointList.NumberOfAccessPoints, buffer, offset);
            offset += 4;
            EncodeUInt32(accessPointList.AccessPointsLength, buffer, offset);
            offset += 4;
            if (accessPointList.AccessPointsLength > 0)
            {
                Array.Copy(accessPointList.AccessPoints, 0, buffer, offset, accessPointList.AccessPointsLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a AccessPointList object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>AccessPointList object.</returns>
        public static MessagePayloads.AccessPointList ExtractAccessPointList(byte[] buffer, int offset)
        {
            AccessPointList accessPointList = new MessagePayloads.AccessPointList();

            accessPointList.NumberOfAccessPoints = ExtractUInt32(buffer, offset);
            offset += 4;
            accessPointList.AccessPointsLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (accessPointList.AccessPointsLength > 0)
            {
                accessPointList.AccessPoints = new byte[accessPointList.AccessPointsLength];
                Array.Copy(buffer, offset, accessPointList.AccessPoints, 0, accessPointList.AccessPointsLength);
            }
            return accessPointList;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the AccessPointList object.
        /// </summary>
        /// <param name="accessPointList">AccessPointList object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded AccessPointList object.</returns>
        public static int EncodedAccessPointListBufferSize(MessagePayloads.AccessPointList accessPointList)
        {
            int result = 0;
            result += (int)accessPointList.AccessPointsLength;
            return result + 8;
        }

        /// <summary>
        /// Encode a SockAddr object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="sockAddr">SockAddr object to be encoded.</param>
        /// <returns>Byte array containing the encoded SockAddr object.</returns>
        public static byte[] EncodeSockAddr(MessagePayloads.SockAddr sockAddr)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 16;
            length += 15;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            buffer[offset] = sockAddr.Family;
            offset += 1;
            EncodeUInt16(sockAddr.Port, buffer, offset);
            offset += 2;
            EncodeUInt32(sockAddr.Ip4Address, buffer, offset);
            offset += 4;
            EncodeUInt32(sockAddr.FlowInfo, buffer, offset);
            offset += 4;
            Array.Copy(sockAddr.Ip6Address, 0, buffer, offset, 16);
            offset += 16;
            EncodeUInt32(sockAddr.ScopeID, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a SockAddr object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SockAddr object.</returns>
        public static MessagePayloads.SockAddr ExtractSockAddr(byte[] buffer, int offset)
        {
            SockAddr sockAddr = new MessagePayloads.SockAddr();

            sockAddr.Family = buffer[offset];
            offset += 1;
            sockAddr.Port = ExtractUInt16(buffer, offset);
            offset += 2;
            sockAddr.Ip4Address = ExtractUInt32(buffer, offset);
            offset += 4;
            sockAddr.FlowInfo = ExtractUInt32(buffer, offset);
            offset += 4;
            sockAddr.Ip6Address = new byte[16];
            Array.Copy(buffer, offset, sockAddr.Ip6Address, 0, 16);
            offset += 16;
            sockAddr.ScopeID = ExtractUInt32(buffer, offset);
            return sockAddr;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SockAddr object.
        /// </summary>
        /// <param name="sockAddr">SockAddr object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SockAddr object.</returns>
        public static int EncodedSockAddrBufferSize(MessagePayloads.SockAddr sockAddr)
        {
            return 31;
        }

        /// <summary>
        /// Encode a AddrInfo object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="addrInfo">AddrInfo object to be encoded.</param>
        /// <returns>Byte array containing the encoded AddrInfo object.</returns>
        public static byte[] EncodeAddrInfo(MessagePayloads.AddrInfo addrInfo)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(addrInfo.AddrLength + 4);
            length += addrInfo.CanonName.Length + 1;
            length += 28;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(addrInfo.MyHeapAddress, buffer, offset);
            offset += 4;
            EncodeInt32(addrInfo.Flags, buffer, offset);
            offset += 4;
            EncodeInt32(addrInfo.Family, buffer, offset);
            offset += 4;
            EncodeInt32(addrInfo.SocketType, buffer, offset);
            offset += 4;
            EncodeInt32(addrInfo.Protocol, buffer, offset);
            offset += 4;
            EncodeUInt32(addrInfo.AddrLen, buffer, offset);
            offset += 4;
            EncodeUInt32(addrInfo.AddrLength, buffer, offset);
            offset += 4;
            if (addrInfo.AddrLength > 0)
            {
                Array.Copy(addrInfo.Addr, 0, buffer, offset, addrInfo.AddrLength);
                offset += (int)addrInfo.AddrLength;
            }
            EncodeString(addrInfo.CanonName, buffer, offset);
            offset += addrInfo.CanonName.Length + 1;
            EncodeUInt32(addrInfo.Next, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a AddrInfo object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>AddrInfo object.</returns>
        public static MessagePayloads.AddrInfo ExtractAddrInfo(byte[] buffer, int offset)
        {
            AddrInfo addrInfo = new MessagePayloads.AddrInfo();

            addrInfo.MyHeapAddress = ExtractUInt32(buffer, offset);
            offset += 4;
            addrInfo.Flags = ExtractInt32(buffer, offset);
            offset += 4;
            addrInfo.Family = ExtractInt32(buffer, offset);
            offset += 4;
            addrInfo.SocketType = ExtractInt32(buffer, offset);
            offset += 4;
            addrInfo.Protocol = ExtractInt32(buffer, offset);
            offset += 4;
            addrInfo.AddrLen = ExtractUInt32(buffer, offset);
            offset += 4;
            addrInfo.AddrLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (addrInfo.AddrLength > 0)
            {
                addrInfo.Addr = new byte[addrInfo.AddrLength];
                Array.Copy(buffer, offset, addrInfo.Addr, 0, addrInfo.AddrLength);
                offset += (int)addrInfo.AddrLength;
            }
            addrInfo.CanonName = ExtractString(buffer, offset);
            offset += addrInfo.CanonName.Length + 1;
            addrInfo.Next = ExtractUInt32(buffer, offset);
            return addrInfo;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the AddrInfo object.
        /// </summary>
        /// <param name="addrInfo">AddrInfo object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded AddrInfo object.</returns>
        public static int EncodedAddrInfoBufferSize(MessagePayloads.AddrInfo addrInfo)
        {
            int result = 0;
            result += (int)addrInfo.AddrLength;
            result += addrInfo.CanonName.Length;
            return result + 33;
        }

        /// <summary>
        /// Encode a GetAddrInfoRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getAddrInfoRequest">GetAddrInfoRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetAddrInfoRequest object.</returns>
        public static byte[] EncodeGetAddrInfoRequest(MessagePayloads.GetAddrInfoRequest getAddrInfoRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += getAddrInfoRequest.NodeName.Length + 1;
            length += getAddrInfoRequest.ServName.Length + 1;
            length += (int)(getAddrInfoRequest.HintsLength + 4);

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeString(getAddrInfoRequest.NodeName, buffer, offset);
            offset += getAddrInfoRequest.NodeName.Length + 1;
            EncodeString(getAddrInfoRequest.ServName, buffer, offset);
            offset += getAddrInfoRequest.ServName.Length + 1;
            EncodeUInt32(getAddrInfoRequest.HintsLength, buffer, offset);
            offset += 4;
            if (getAddrInfoRequest.HintsLength > 0)
            {
                Array.Copy(getAddrInfoRequest.Hints, 0, buffer, offset, getAddrInfoRequest.HintsLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a GetAddrInfoRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetAddrInfoRequest object.</returns>
        public static MessagePayloads.GetAddrInfoRequest ExtractGetAddrInfoRequest(byte[] buffer, int offset)
        {
            GetAddrInfoRequest getAddrInfoRequest = new MessagePayloads.GetAddrInfoRequest();

            getAddrInfoRequest.NodeName = ExtractString(buffer, offset);
            offset += getAddrInfoRequest.NodeName.Length + 1;
            getAddrInfoRequest.ServName = ExtractString(buffer, offset);
            offset += getAddrInfoRequest.ServName.Length + 1;
            getAddrInfoRequest.HintsLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (getAddrInfoRequest.HintsLength > 0)
            {
                getAddrInfoRequest.Hints = new byte[getAddrInfoRequest.HintsLength];
                Array.Copy(buffer, offset, getAddrInfoRequest.Hints, 0, getAddrInfoRequest.HintsLength);
            }
            return getAddrInfoRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetAddrInfoRequest object.
        /// </summary>
        /// <param name="getAddrInfoRequest">GetAddrInfoRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetAddrInfoRequest object.</returns>
        public static int EncodedGetAddrInfoRequestBufferSize(MessagePayloads.GetAddrInfoRequest getAddrInfoRequest)
        {
            int result = 0;
            result += getAddrInfoRequest.NodeName.Length;
            result += getAddrInfoRequest.ServName.Length;
            result += (int)getAddrInfoRequest.HintsLength;
            return result + 6;
        }

        /// <summary>
        /// Encode a GetAddrInfoResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getAddrInfoResponse">GetAddrInfoResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetAddrInfoResponse object.</returns>
        public static byte[] EncodeGetAddrInfoResponse(MessagePayloads.GetAddrInfoResponse getAddrInfoResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(getAddrInfoResponse.ResLength + 4);
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(getAddrInfoResponse.AddrInfoResponseErrno, buffer, offset);
            offset += 4;
            EncodeUInt32(getAddrInfoResponse.ResLength, buffer, offset);
            offset += 4;
            if (getAddrInfoResponse.ResLength > 0)
            {
                Array.Copy(getAddrInfoResponse.Res, 0, buffer, offset, getAddrInfoResponse.ResLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a GetAddrInfoResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetAddrInfoResponse object.</returns>
        public static MessagePayloads.GetAddrInfoResponse ExtractGetAddrInfoResponse(byte[] buffer, int offset)
        {
            GetAddrInfoResponse getAddrInfoResponse = new MessagePayloads.GetAddrInfoResponse();

            getAddrInfoResponse.AddrInfoResponseErrno = ExtractInt32(buffer, offset);
            offset += 4;
            getAddrInfoResponse.ResLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (getAddrInfoResponse.ResLength > 0)
            {
                getAddrInfoResponse.Res = new byte[getAddrInfoResponse.ResLength];
                Array.Copy(buffer, offset, getAddrInfoResponse.Res, 0, getAddrInfoResponse.ResLength);
            }
            return getAddrInfoResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetAddrInfoResponse object.
        /// </summary>
        /// <param name="getAddrInfoResponse">GetAddrInfoResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetAddrInfoResponse object.</returns>
        public static int EncodedGetAddrInfoResponseBufferSize(MessagePayloads.GetAddrInfoResponse getAddrInfoResponse)
        {
            int result = 0;
            result += (int)getAddrInfoResponse.ResLength;
            return result + 8;
        }

        /// <summary>
        /// Encode a SocketRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="socketRequest">SocketRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded SocketRequest object.</returns>
        public static byte[] EncodeSocketRequest(MessagePayloads.SocketRequest socketRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 16;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(socketRequest.AddressInformation, buffer, offset);
            offset += 4;
            EncodeInt32(socketRequest.Domain, buffer, offset);
            offset += 4;
            EncodeInt32(socketRequest.Type, buffer, offset);
            offset += 4;
            EncodeInt32(socketRequest.Protocol, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a SocketRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SocketRequest object.</returns>
        public static MessagePayloads.SocketRequest ExtractSocketRequest(byte[] buffer, int offset)
        {
            SocketRequest socketRequest = new MessagePayloads.SocketRequest();

            socketRequest.AddressInformation = ExtractUInt32(buffer, offset);
            offset += 4;
            socketRequest.Domain = ExtractInt32(buffer, offset);
            offset += 4;
            socketRequest.Type = ExtractInt32(buffer, offset);
            offset += 4;
            socketRequest.Protocol = ExtractInt32(buffer, offset);
            return socketRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SocketRequest object.
        /// </summary>
        /// <param name="socketRequest">SocketRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SocketRequest object.</returns>
        public static int EncodedSocketRequestBufferSize(MessagePayloads.SocketRequest socketRequest)
        {
            return 16;
        }

        /// <summary>
        /// Encode a IntegerResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="integerResponse">IntegerResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded IntegerResponse object.</returns>
        public static byte[] EncodeIntegerResponse(MessagePayloads.IntegerResponse integerResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(integerResponse.Result, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a IntegerResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>IntegerResponse object.</returns>
        public static MessagePayloads.IntegerResponse ExtractIntegerResponse(byte[] buffer, int offset)
        {
            IntegerResponse integerResponse = new MessagePayloads.IntegerResponse();

            integerResponse.Result = ExtractInt32(buffer, offset);
            return integerResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the IntegerResponse object.
        /// </summary>
        /// <param name="integerResponse">IntegerResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded IntegerResponse object.</returns>
        public static int EncodedIntegerResponseBufferSize(MessagePayloads.IntegerResponse integerResponse)
        {
            return 4;
        }

        /// <summary>
        /// Encode a IntegerAndErrnoResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="integerAndErrnoResponse">IntegerAndErrnoResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded IntegerAndErrnoResponse object.</returns>
        public static byte[] EncodeIntegerAndErrnoResponse(MessagePayloads.IntegerAndErrnoResponse integerAndErrnoResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(integerAndErrnoResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(integerAndErrnoResponse.ResponseErrno, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a IntegerAndErrnoResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>IntegerAndErrnoResponse object.</returns>
        public static MessagePayloads.IntegerAndErrnoResponse ExtractIntegerAndErrnoResponse(byte[] buffer, int offset)
        {
            IntegerAndErrnoResponse integerAndErrnoResponse = new MessagePayloads.IntegerAndErrnoResponse();

            integerAndErrnoResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            integerAndErrnoResponse.ResponseErrno = ExtractInt32(buffer, offset);
            return integerAndErrnoResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the IntegerAndErrnoResponse object.
        /// </summary>
        /// <param name="integerAndErrnoResponse">IntegerAndErrnoResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded IntegerAndErrnoResponse object.</returns>
        public static int EncodedIntegerAndErrnoResponseBufferSize(MessagePayloads.IntegerAndErrnoResponse integerAndErrnoResponse)
        {
            return 8;
        }

        /// <summary>
        /// Encode a ConnectRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="connectRequest">ConnectRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded ConnectRequest object.</returns>
        public static byte[] EncodeConnectRequest(MessagePayloads.ConnectRequest connectRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(connectRequest.AddrLength + 4);
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(connectRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeUInt32(connectRequest.AddrLength, buffer, offset);
            offset += 4;
            if (connectRequest.AddrLength > 0)
            {
                Array.Copy(connectRequest.Addr, 0, buffer, offset, connectRequest.AddrLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a ConnectRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ConnectRequest object.</returns>
        public static MessagePayloads.ConnectRequest ExtractConnectRequest(byte[] buffer, int offset)
        {
            ConnectRequest connectRequest = new MessagePayloads.ConnectRequest();

            connectRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            connectRequest.AddrLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (connectRequest.AddrLength > 0)
            {
                connectRequest.Addr = new byte[connectRequest.AddrLength];
                Array.Copy(buffer, offset, connectRequest.Addr, 0, connectRequest.AddrLength);
            }
            return connectRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ConnectRequest object.
        /// </summary>
        /// <param name="connectRequest">ConnectRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ConnectRequest object.</returns>
        public static int EncodedConnectRequestBufferSize(MessagePayloads.ConnectRequest connectRequest)
        {
            int result = 0;
            result += (int)connectRequest.AddrLength;
            return result + 8;
        }

        /// <summary>
        /// Encode a FreeAddrInfoRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="freeAddrInfoRequest">FreeAddrInfoRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded FreeAddrInfoRequest object.</returns>
        public static byte[] EncodeFreeAddrInfoRequest(MessagePayloads.FreeAddrInfoRequest freeAddrInfoRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(freeAddrInfoRequest.AddrInfoAddress, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a FreeAddrInfoRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>FreeAddrInfoRequest object.</returns>
        public static MessagePayloads.FreeAddrInfoRequest ExtractFreeAddrInfoRequest(byte[] buffer, int offset)
        {
            FreeAddrInfoRequest freeAddrInfoRequest = new MessagePayloads.FreeAddrInfoRequest();

            freeAddrInfoRequest.AddrInfoAddress = ExtractUInt32(buffer, offset);
            return freeAddrInfoRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the FreeAddrInfoRequest object.
        /// </summary>
        /// <param name="freeAddrInfoRequest">FreeAddrInfoRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded FreeAddrInfoRequest object.</returns>
        public static int EncodedFreeAddrInfoRequestBufferSize(MessagePayloads.FreeAddrInfoRequest freeAddrInfoRequest)
        {
            return 4;
        }

        /// <summary>
        /// Encode a TimeVal object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="timeVal">TimeVal object to be encoded.</param>
        /// <returns>Byte array containing the encoded TimeVal object.</returns>
        public static byte[] EncodeTimeVal(MessagePayloads.TimeVal timeVal)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(timeVal.TvSec, buffer, offset);
            offset += 4;
            EncodeUInt32(timeVal.TvUsec, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a TimeVal object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>TimeVal object.</returns>
        public static MessagePayloads.TimeVal ExtractTimeVal(byte[] buffer, int offset)
        {
            TimeVal timeVal = new MessagePayloads.TimeVal();

            timeVal.TvSec = ExtractUInt32(buffer, offset);
            offset += 4;
            timeVal.TvUsec = ExtractUInt32(buffer, offset);
            return timeVal;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the TimeVal object.
        /// </summary>
        /// <param name="timeVal">TimeVal object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded TimeVal object.</returns>
        public static int EncodedTimeValBufferSize(MessagePayloads.TimeVal timeVal)
        {
            return 8;
        }

        /// <summary>
        /// Encode a SetSockOptRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="setSockOptRequest">SetSockOptRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded SetSockOptRequest object.</returns>
        public static byte[] EncodeSetSockOptRequest(MessagePayloads.SetSockOptRequest setSockOptRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(setSockOptRequest.OptionValueLength + 4);
            length += 16;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(setSockOptRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeInt32(setSockOptRequest.Level, buffer, offset);
            offset += 4;
            EncodeInt32(setSockOptRequest.OptionName, buffer, offset);
            offset += 4;
            EncodeUInt32(setSockOptRequest.OptionValueLength, buffer, offset);
            offset += 4;
            if (setSockOptRequest.OptionValueLength > 0)
            {
                Array.Copy(setSockOptRequest.OptionValue, 0, buffer, offset, setSockOptRequest.OptionValueLength);
                offset += (int)setSockOptRequest.OptionValueLength;
            }
            EncodeInt32(setSockOptRequest.OptionLen, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a SetSockOptRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SetSockOptRequest object.</returns>
        public static MessagePayloads.SetSockOptRequest ExtractSetSockOptRequest(byte[] buffer, int offset)
        {
            SetSockOptRequest setSockOptRequest = new MessagePayloads.SetSockOptRequest();

            setSockOptRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            setSockOptRequest.Level = ExtractInt32(buffer, offset);
            offset += 4;
            setSockOptRequest.OptionName = ExtractInt32(buffer, offset);
            offset += 4;
            setSockOptRequest.OptionValueLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (setSockOptRequest.OptionValueLength > 0)
            {
                setSockOptRequest.OptionValue = new byte[setSockOptRequest.OptionValueLength];
                Array.Copy(buffer, offset, setSockOptRequest.OptionValue, 0, setSockOptRequest.OptionValueLength);
                offset += (int)setSockOptRequest.OptionValueLength;
            }
            setSockOptRequest.OptionLen = ExtractInt32(buffer, offset);
            return setSockOptRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SetSockOptRequest object.
        /// </summary>
        /// <param name="setSockOptRequest">SetSockOptRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SetSockOptRequest object.</returns>
        public static int EncodedSetSockOptRequestBufferSize(MessagePayloads.SetSockOptRequest setSockOptRequest)
        {
            int result = 0;
            result += (int)setSockOptRequest.OptionValueLength;
            return result + 20;
        }

        /// <summary>
        /// Encode a GetSockOptRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getSockOptRequest">GetSockOptRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetSockOptRequest object.</returns>
        public static byte[] EncodeGetSockOptRequest(MessagePayloads.GetSockOptRequest getSockOptRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 12;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(getSockOptRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeInt32(getSockOptRequest.Level, buffer, offset);
            offset += 4;
            EncodeInt32(getSockOptRequest.OptionName, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a GetSockOptRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetSockOptRequest object.</returns>
        public static MessagePayloads.GetSockOptRequest ExtractGetSockOptRequest(byte[] buffer, int offset)
        {
            GetSockOptRequest getSockOptRequest = new MessagePayloads.GetSockOptRequest();

            getSockOptRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            getSockOptRequest.Level = ExtractInt32(buffer, offset);
            offset += 4;
            getSockOptRequest.OptionName = ExtractInt32(buffer, offset);
            return getSockOptRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetSockOptRequest object.
        /// </summary>
        /// <param name="getSockOptRequest">GetSockOptRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetSockOptRequest object.</returns>
        public static int EncodedGetSockOptRequestBufferSize(MessagePayloads.GetSockOptRequest getSockOptRequest)
        {
            return 12;
        }

        /// <summary>
        /// Encode a GetSockOptResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getSockOptResponse">GetSockOptResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetSockOptResponse object.</returns>
        public static byte[] EncodeGetSockOptResponse(MessagePayloads.GetSockOptResponse getSockOptResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(getSockOptResponse.OptionValueLength + 4);
            length += 12;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(getSockOptResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(getSockOptResponse.ResponseErrno, buffer, offset);
            offset += 4;
            EncodeUInt32(getSockOptResponse.OptionValueLength, buffer, offset);
            offset += 4;
            if (getSockOptResponse.OptionValueLength > 0)
            {
                Array.Copy(getSockOptResponse.OptionValue, 0, buffer, offset, getSockOptResponse.OptionValueLength);
                offset += (int)getSockOptResponse.OptionValueLength;
            }
            EncodeInt32(getSockOptResponse.OptionLen, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a GetSockOptResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetSockOptResponse object.</returns>
        public static MessagePayloads.GetSockOptResponse ExtractGetSockOptResponse(byte[] buffer, int offset)
        {
            GetSockOptResponse getSockOptResponse = new MessagePayloads.GetSockOptResponse();

            getSockOptResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            getSockOptResponse.ResponseErrno = ExtractInt32(buffer, offset);
            offset += 4;
            getSockOptResponse.OptionValueLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (getSockOptResponse.OptionValueLength > 0)
            {
                getSockOptResponse.OptionValue = new byte[getSockOptResponse.OptionValueLength];
                Array.Copy(buffer, offset, getSockOptResponse.OptionValue, 0, getSockOptResponse.OptionValueLength);
                offset += (int)getSockOptResponse.OptionValueLength;
            }
            getSockOptResponse.OptionLen = ExtractInt32(buffer, offset);
            return getSockOptResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetSockOptResponse object.
        /// </summary>
        /// <param name="getSockOptResponse">GetSockOptResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetSockOptResponse object.</returns>
        public static int EncodedGetSockOptResponseBufferSize(MessagePayloads.GetSockOptResponse getSockOptResponse)
        {
            int result = 0;
            result += (int)getSockOptResponse.OptionValueLength;
            return result + 16;
        }

        /// <summary>
        /// Encode a Linger object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="linger">Linger object to be encoded.</param>
        /// <returns>Byte array containing the encoded Linger object.</returns>
        public static byte[] EncodeLinger(MessagePayloads.Linger linger)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(linger.LOnOff, buffer, offset);
            offset += 4;
            EncodeInt32(linger.LLinger, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a Linger object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>Linger object.</returns>
        public static MessagePayloads.Linger ExtractLinger(byte[] buffer, int offset)
        {
            Linger linger = new MessagePayloads.Linger();

            linger.LOnOff = ExtractInt32(buffer, offset);
            offset += 4;
            linger.LLinger = ExtractInt32(buffer, offset);
            return linger;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the Linger object.
        /// </summary>
        /// <param name="linger">Linger object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded Linger object.</returns>
        public static int EncodedLingerBufferSize(MessagePayloads.Linger linger)
        {
            return 8;
        }

        /// <summary>
        /// Encode a WriteRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="writeRequest">WriteRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded WriteRequest object.</returns>
        public static byte[] EncodeWriteRequest(MessagePayloads.WriteRequest writeRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(writeRequest.BufferLength + 4);
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(writeRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeUInt32(writeRequest.BufferLength, buffer, offset);
            offset += 4;
            if (writeRequest.BufferLength > 0)
            {
                Array.Copy(writeRequest.Buffer, 0, buffer, offset, writeRequest.BufferLength);
                offset += (int)writeRequest.BufferLength;
            }
            EncodeInt32(writeRequest.Count, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a WriteRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>WriteRequest object.</returns>
        public static MessagePayloads.WriteRequest ExtractWriteRequest(byte[] buffer, int offset)
        {
            WriteRequest writeRequest = new MessagePayloads.WriteRequest();

            writeRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            writeRequest.BufferLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (writeRequest.BufferLength > 0)
            {
                writeRequest.Buffer = new byte[writeRequest.BufferLength];
                Array.Copy(buffer, offset, writeRequest.Buffer, 0, writeRequest.BufferLength);
                offset += (int)writeRequest.BufferLength;
            }
            writeRequest.Count = ExtractInt32(buffer, offset);
            return writeRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the WriteRequest object.
        /// </summary>
        /// <param name="writeRequest">WriteRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded WriteRequest object.</returns>
        public static int EncodedWriteRequestBufferSize(MessagePayloads.WriteRequest writeRequest)
        {
            int result = 0;
            result += (int)writeRequest.BufferLength;
            return result + 12;
        }

        /// <summary>
        /// Encode a ReadRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="readRequest">ReadRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded ReadRequest object.</returns>
        public static byte[] EncodeReadRequest(MessagePayloads.ReadRequest readRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(readRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeInt32(readRequest.Count, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a ReadRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ReadRequest object.</returns>
        public static MessagePayloads.ReadRequest ExtractReadRequest(byte[] buffer, int offset)
        {
            ReadRequest readRequest = new MessagePayloads.ReadRequest();

            readRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            readRequest.Count = ExtractInt32(buffer, offset);
            return readRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ReadRequest object.
        /// </summary>
        /// <param name="readRequest">ReadRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ReadRequest object.</returns>
        public static int EncodedReadRequestBufferSize(MessagePayloads.ReadRequest readRequest)
        {
            return 8;
        }

        /// <summary>
        /// Encode a ReadResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="readResponse">ReadResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded ReadResponse object.</returns>
        public static byte[] EncodeReadResponse(MessagePayloads.ReadResponse readResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(readResponse.BufferLength + 4);
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(readResponse.BufferLength, buffer, offset);
            offset += 4;
            if (readResponse.BufferLength > 0)
            {
                Array.Copy(readResponse.Buffer, 0, buffer, offset, readResponse.BufferLength);
                offset += (int)readResponse.BufferLength;
            }
            EncodeInt32(readResponse.ReadResponseResult, buffer, offset);
            offset += 4;
            EncodeInt32(readResponse.ReadResponseErrno, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a ReadResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ReadResponse object.</returns>
        public static MessagePayloads.ReadResponse ExtractReadResponse(byte[] buffer, int offset)
        {
            ReadResponse readResponse = new MessagePayloads.ReadResponse();

            readResponse.BufferLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (readResponse.BufferLength > 0)
            {
                readResponse.Buffer = new byte[readResponse.BufferLength];
                Array.Copy(buffer, offset, readResponse.Buffer, 0, readResponse.BufferLength);
                offset += (int)readResponse.BufferLength;
            }
            readResponse.ReadResponseResult = ExtractInt32(buffer, offset);
            offset += 4;
            readResponse.ReadResponseErrno = ExtractInt32(buffer, offset);
            return readResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ReadResponse object.
        /// </summary>
        /// <param name="readResponse">ReadResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ReadResponse object.</returns>
        public static int EncodedReadResponseBufferSize(MessagePayloads.ReadResponse readResponse)
        {
            int result = 0;
            result += (int)readResponse.BufferLength;
            return result + 12;
        }

        /// <summary>
        /// Encode a CloseRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="closeRequest">CloseRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded CloseRequest object.</returns>
        public static byte[] EncodeCloseRequest(MessagePayloads.CloseRequest closeRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(closeRequest.SocketHandle, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a CloseRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>CloseRequest object.</returns>
        public static MessagePayloads.CloseRequest ExtractCloseRequest(byte[] buffer, int offset)
        {
            CloseRequest closeRequest = new MessagePayloads.CloseRequest();

            closeRequest.SocketHandle = ExtractInt32(buffer, offset);
            return closeRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the CloseRequest object.
        /// </summary>
        /// <param name="closeRequest">CloseRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded CloseRequest object.</returns>
        public static int EncodedCloseRequestBufferSize(MessagePayloads.CloseRequest closeRequest)
        {
            return 4;
        }

        /// <summary>
        /// Encode a GetBatteryChargeLevelResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getBatteryChargeLevelResponse">GetBatteryChargeLevelResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetBatteryChargeLevelResponse object.</returns>
        public static byte[] EncodeGetBatteryChargeLevelResponse(MessagePayloads.GetBatteryChargeLevelResponse getBatteryChargeLevelResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(getBatteryChargeLevelResponse.Level, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a GetBatteryChargeLevelResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetBatteryChargeLevelResponse object.</returns>
        public static MessagePayloads.GetBatteryChargeLevelResponse ExtractGetBatteryChargeLevelResponse(byte[] buffer, int offset)
        {
            GetBatteryChargeLevelResponse getBatteryChargeLevelResponse = new MessagePayloads.GetBatteryChargeLevelResponse();

            getBatteryChargeLevelResponse.Level = ExtractUInt32(buffer, offset);
            return getBatteryChargeLevelResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetBatteryChargeLevelResponse object.
        /// </summary>
        /// <param name="getBatteryChargeLevelResponse">GetBatteryChargeLevelResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetBatteryChargeLevelResponse object.</returns>
        public static int EncodedGetBatteryChargeLevelResponseBufferSize(MessagePayloads.GetBatteryChargeLevelResponse getBatteryChargeLevelResponse)
        {
            return 4;
        }

        /// <summary>
        /// Encode a SendRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="sendRequest">SendRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded SendRequest object.</returns>
        public static byte[] EncodeSendRequest(MessagePayloads.SendRequest sendRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(sendRequest.BufferLength + 4);
            length += 12;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(sendRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeUInt32(sendRequest.BufferLength, buffer, offset);
            offset += 4;
            if (sendRequest.BufferLength > 0)
            {
                Array.Copy(sendRequest.Buffer, 0, buffer, offset, sendRequest.BufferLength);
                offset += (int)sendRequest.BufferLength;
            }
            EncodeInt32(sendRequest.Length, buffer, offset);
            offset += 4;
            EncodeInt32(sendRequest.Flags, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a SendRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SendRequest object.</returns>
        public static MessagePayloads.SendRequest ExtractSendRequest(byte[] buffer, int offset)
        {
            SendRequest sendRequest = new MessagePayloads.SendRequest();

            sendRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            sendRequest.BufferLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (sendRequest.BufferLength > 0)
            {
                sendRequest.Buffer = new byte[sendRequest.BufferLength];
                Array.Copy(buffer, offset, sendRequest.Buffer, 0, sendRequest.BufferLength);
                offset += (int)sendRequest.BufferLength;
            }
            sendRequest.Length = ExtractInt32(buffer, offset);
            offset += 4;
            sendRequest.Flags = ExtractInt32(buffer, offset);
            return sendRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SendRequest object.
        /// </summary>
        /// <param name="sendRequest">SendRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SendRequest object.</returns>
        public static int EncodedSendRequestBufferSize(MessagePayloads.SendRequest sendRequest)
        {
            int result = 0;
            result += (int)sendRequest.BufferLength;
            return result + 16;
        }

        /// <summary>
        /// Encode a SendToRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="sendToRequest">SendToRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded SendToRequest object.</returns>
        public static byte[] EncodeSendToRequest(MessagePayloads.SendToRequest sendToRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(sendToRequest.BufferLength + 4);
            length += (int)(sendToRequest.DestinationAddressLength + 4);
            length += 12;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(sendToRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeUInt32(sendToRequest.BufferLength, buffer, offset);
            offset += 4;
            if (sendToRequest.BufferLength > 0)
            {
                Array.Copy(sendToRequest.Buffer, 0, buffer, offset, sendToRequest.BufferLength);
                offset += (int)sendToRequest.BufferLength;
            }
            EncodeInt32(sendToRequest.Length, buffer, offset);
            offset += 4;
            EncodeInt32(sendToRequest.Flags, buffer, offset);
            offset += 4;
            EncodeUInt32(sendToRequest.DestinationAddressLength, buffer, offset);
            offset += 4;
            if (sendToRequest.DestinationAddressLength > 0)
            {
                Array.Copy(sendToRequest.DestinationAddress, 0, buffer, offset, sendToRequest.DestinationAddressLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a SendToRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SendToRequest object.</returns>
        public static MessagePayloads.SendToRequest ExtractSendToRequest(byte[] buffer, int offset)
        {
            SendToRequest sendToRequest = new MessagePayloads.SendToRequest();

            sendToRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            sendToRequest.BufferLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (sendToRequest.BufferLength > 0)
            {
                sendToRequest.Buffer = new byte[sendToRequest.BufferLength];
                Array.Copy(buffer, offset, sendToRequest.Buffer, 0, sendToRequest.BufferLength);
                offset += (int)sendToRequest.BufferLength;
            }
            sendToRequest.Length = ExtractInt32(buffer, offset);
            offset += 4;
            sendToRequest.Flags = ExtractInt32(buffer, offset);
            offset += 4;
            sendToRequest.DestinationAddressLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (sendToRequest.DestinationAddressLength > 0)
            {
                sendToRequest.DestinationAddress = new byte[sendToRequest.DestinationAddressLength];
                Array.Copy(buffer, offset, sendToRequest.DestinationAddress, 0, sendToRequest.DestinationAddressLength);
            }
            return sendToRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SendToRequest object.
        /// </summary>
        /// <param name="sendToRequest">SendToRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SendToRequest object.</returns>
        public static int EncodedSendToRequestBufferSize(MessagePayloads.SendToRequest sendToRequest)
        {
            int result = 0;
            result += (int)sendToRequest.BufferLength;
            result += (int)sendToRequest.DestinationAddressLength;
            return result + 20;
        }

        /// <summary>
        /// Encode a RecvFromRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="recvFromRequest">RecvFromRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded RecvFromRequest object.</returns>
        public static byte[] EncodeRecvFromRequest(MessagePayloads.RecvFromRequest recvFromRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 16;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(recvFromRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeInt32(recvFromRequest.Length, buffer, offset);
            offset += 4;
            EncodeInt32(recvFromRequest.Flags, buffer, offset);
            offset += 4;
            EncodeInt32(recvFromRequest.GetSourceAddress, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a RecvFromRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>RecvFromRequest object.</returns>
        public static MessagePayloads.RecvFromRequest ExtractRecvFromRequest(byte[] buffer, int offset)
        {
            RecvFromRequest recvFromRequest = new MessagePayloads.RecvFromRequest();

            recvFromRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            recvFromRequest.Length = ExtractInt32(buffer, offset);
            offset += 4;
            recvFromRequest.Flags = ExtractInt32(buffer, offset);
            offset += 4;
            recvFromRequest.GetSourceAddress = ExtractInt32(buffer, offset);
            return recvFromRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the RecvFromRequest object.
        /// </summary>
        /// <param name="recvFromRequest">RecvFromRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded RecvFromRequest object.</returns>
        public static int EncodedRecvFromRequestBufferSize(MessagePayloads.RecvFromRequest recvFromRequest)
        {
            return 16;
        }

        /// <summary>
        /// Encode a RecvFromResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="recvFromResponse">RecvFromResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded RecvFromResponse object.</returns>
        public static byte[] EncodeRecvFromResponse(MessagePayloads.RecvFromResponse recvFromResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(recvFromResponse.BufferLength + 4);
            length += (int)(recvFromResponse.SourceAddressLength + 4);
            length += 12;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(recvFromResponse.BufferLength, buffer, offset);
            offset += 4;
            if (recvFromResponse.BufferLength > 0)
            {
                Array.Copy(recvFromResponse.Buffer, 0, buffer, offset, recvFromResponse.BufferLength);
                offset += (int)recvFromResponse.BufferLength;
            }
            EncodeInt32(recvFromResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(recvFromResponse.ResponseErrno, buffer, offset);
            offset += 4;
            EncodeUInt32(recvFromResponse.SourceAddressLength, buffer, offset);
            offset += 4;
            if (recvFromResponse.SourceAddressLength > 0)
            {
                Array.Copy(recvFromResponse.SourceAddress, 0, buffer, offset, recvFromResponse.SourceAddressLength);
                offset += (int)recvFromResponse.SourceAddressLength;
            }
            EncodeUInt32(recvFromResponse.SourceAddressLen, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a RecvFromResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>RecvFromResponse object.</returns>
        public static MessagePayloads.RecvFromResponse ExtractRecvFromResponse(byte[] buffer, int offset)
        {
            RecvFromResponse recvFromResponse = new MessagePayloads.RecvFromResponse();

            recvFromResponse.BufferLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (recvFromResponse.BufferLength > 0)
            {
                recvFromResponse.Buffer = new byte[recvFromResponse.BufferLength];
                Array.Copy(buffer, offset, recvFromResponse.Buffer, 0, recvFromResponse.BufferLength);
                offset += (int)recvFromResponse.BufferLength;
            }
            recvFromResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            recvFromResponse.ResponseErrno = ExtractInt32(buffer, offset);
            offset += 4;
            recvFromResponse.SourceAddressLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (recvFromResponse.SourceAddressLength > 0)
            {
                recvFromResponse.SourceAddress = new byte[recvFromResponse.SourceAddressLength];
                Array.Copy(buffer, offset, recvFromResponse.SourceAddress, 0, recvFromResponse.SourceAddressLength);
                offset += (int)recvFromResponse.SourceAddressLength;
            }
            recvFromResponse.SourceAddressLen = ExtractUInt32(buffer, offset);
            return recvFromResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the RecvFromResponse object.
        /// </summary>
        /// <param name="recvFromResponse">RecvFromResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded RecvFromResponse object.</returns>
        public static int EncodedRecvFromResponseBufferSize(MessagePayloads.RecvFromResponse recvFromResponse)
        {
            int result = 0;
            result += (int)recvFromResponse.BufferLength;
            result += (int)recvFromResponse.SourceAddressLength;
            return result + 20;
        }

        /// <summary>
        /// Encode a PollRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="pollRequest">PollRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded PollRequest object.</returns>
        public static byte[] EncodePollRequest(MessagePayloads.PollRequest pollRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 18;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(pollRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeUInt16(pollRequest.Events, buffer, offset);
            offset += 2;
            EncodeInt32(pollRequest.Timeout, buffer, offset);
            offset += 4;
            EncodeInt32(pollRequest.Setup, buffer, offset);
            offset += 4;
            EncodeUInt32(pollRequest.SetupMessageId, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a PollRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>PollRequest object.</returns>
        public static MessagePayloads.PollRequest ExtractPollRequest(byte[] buffer, int offset)
        {
            PollRequest pollRequest = new MessagePayloads.PollRequest();

            pollRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            pollRequest.Events = ExtractUInt16(buffer, offset);
            offset += 2;
            pollRequest.Timeout = ExtractInt32(buffer, offset);
            offset += 4;
            pollRequest.Setup = ExtractInt32(buffer, offset);
            offset += 4;
            pollRequest.SetupMessageId = ExtractUInt32(buffer, offset);
            return pollRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the PollRequest object.
        /// </summary>
        /// <param name="pollRequest">PollRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded PollRequest object.</returns>
        public static int EncodedPollRequestBufferSize(MessagePayloads.PollRequest pollRequest)
        {
            return 18;
        }

        /// <summary>
        /// Encode a PollResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="pollResponse">PollResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded PollResponse object.</returns>
        public static byte[] EncodePollResponse(MessagePayloads.PollResponse pollResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 10;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt16(pollResponse.ReturnedEvents, buffer, offset);
            offset += 2;
            EncodeInt32(pollResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(pollResponse.ResponseErrno, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a PollResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>PollResponse object.</returns>
        public static MessagePayloads.PollResponse ExtractPollResponse(byte[] buffer, int offset)
        {
            PollResponse pollResponse = new MessagePayloads.PollResponse();

            pollResponse.ReturnedEvents = ExtractUInt16(buffer, offset);
            offset += 2;
            pollResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            pollResponse.ResponseErrno = ExtractInt32(buffer, offset);
            return pollResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the PollResponse object.
        /// </summary>
        /// <param name="pollResponse">PollResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded PollResponse object.</returns>
        public static int EncodedPollResponseBufferSize(MessagePayloads.PollResponse pollResponse)
        {
            return 10;
        }

        /// <summary>
        /// Encode a InterruptPollResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="interruptPollResponse">InterruptPollResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded InterruptPollResponse object.</returns>
        public static byte[] EncodeInterruptPollResponse(MessagePayloads.InterruptPollResponse interruptPollResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 18;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(interruptPollResponse.SocketHandle, buffer, offset);
            offset += 4;
            EncodeInt32(interruptPollResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(interruptPollResponse.ResponseErrno, buffer, offset);
            offset += 4;
            EncodeUInt16(interruptPollResponse.ReturnedEvents, buffer, offset);
            offset += 2;
            EncodeUInt32(interruptPollResponse.SetupMessageId, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a InterruptPollResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>InterruptPollResponse object.</returns>
        public static MessagePayloads.InterruptPollResponse ExtractInterruptPollResponse(byte[] buffer, int offset)
        {
            InterruptPollResponse interruptPollResponse = new MessagePayloads.InterruptPollResponse();

            interruptPollResponse.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            interruptPollResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            interruptPollResponse.ResponseErrno = ExtractInt32(buffer, offset);
            offset += 4;
            interruptPollResponse.ReturnedEvents = ExtractUInt16(buffer, offset);
            offset += 2;
            interruptPollResponse.SetupMessageId = ExtractUInt32(buffer, offset);
            return interruptPollResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the InterruptPollResponse object.
        /// </summary>
        /// <param name="interruptPollResponse">InterruptPollResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded InterruptPollResponse object.</returns>
        public static int EncodedInterruptPollResponseBufferSize(MessagePayloads.InterruptPollResponse interruptPollResponse)
        {
            return 18;
        }

        /// <summary>
        /// Encode a ListenRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="listenRequest">ListenRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded ListenRequest object.</returns>
        public static byte[] EncodeListenRequest(MessagePayloads.ListenRequest listenRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(listenRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeInt32(listenRequest.BackLog, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a ListenRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>ListenRequest object.</returns>
        public static MessagePayloads.ListenRequest ExtractListenRequest(byte[] buffer, int offset)
        {
            ListenRequest listenRequest = new MessagePayloads.ListenRequest();

            listenRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            listenRequest.BackLog = ExtractInt32(buffer, offset);
            return listenRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the ListenRequest object.
        /// </summary>
        /// <param name="listenRequest">ListenRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded ListenRequest object.</returns>
        public static int EncodedListenRequestBufferSize(MessagePayloads.ListenRequest listenRequest)
        {
            return 8;
        }

        /// <summary>
        /// Encode a BindRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="bindRequest">BindRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded BindRequest object.</returns>
        public static byte[] EncodeBindRequest(MessagePayloads.BindRequest bindRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(bindRequest.AddrLength + 4);
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(bindRequest.SocketHandle, buffer, offset);
            offset += 4;
            EncodeUInt32(bindRequest.AddrLength, buffer, offset);
            offset += 4;
            if (bindRequest.AddrLength > 0)
            {
                Array.Copy(bindRequest.Addr, 0, buffer, offset, bindRequest.AddrLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a BindRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>BindRequest object.</returns>
        public static MessagePayloads.BindRequest ExtractBindRequest(byte[] buffer, int offset)
        {
            BindRequest bindRequest = new MessagePayloads.BindRequest();

            bindRequest.SocketHandle = ExtractInt32(buffer, offset);
            offset += 4;
            bindRequest.AddrLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (bindRequest.AddrLength > 0)
            {
                bindRequest.Addr = new byte[bindRequest.AddrLength];
                Array.Copy(buffer, offset, bindRequest.Addr, 0, bindRequest.AddrLength);
            }
            return bindRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the BindRequest object.
        /// </summary>
        /// <param name="bindRequest">BindRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded BindRequest object.</returns>
        public static int EncodedBindRequestBufferSize(MessagePayloads.BindRequest bindRequest)
        {
            int result = 0;
            result += (int)bindRequest.AddrLength;
            return result + 8;
        }

        /// <summary>
        /// Encode a AcceptRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="acceptRequest">AcceptRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded AcceptRequest object.</returns>
        public static byte[] EncodeAcceptRequest(MessagePayloads.AcceptRequest acceptRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(acceptRequest.SocketHandle, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a AcceptRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>AcceptRequest object.</returns>
        public static MessagePayloads.AcceptRequest ExtractAcceptRequest(byte[] buffer, int offset)
        {
            AcceptRequest acceptRequest = new MessagePayloads.AcceptRequest();

            acceptRequest.SocketHandle = ExtractInt32(buffer, offset);
            return acceptRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the AcceptRequest object.
        /// </summary>
        /// <param name="acceptRequest">AcceptRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded AcceptRequest object.</returns>
        public static int EncodedAcceptRequestBufferSize(MessagePayloads.AcceptRequest acceptRequest)
        {
            return 4;
        }

        /// <summary>
        /// Encode a AcceptResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="acceptResponse">AcceptResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded AcceptResponse object.</returns>
        public static byte[] EncodeAcceptResponse(MessagePayloads.AcceptResponse acceptResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(acceptResponse.AddrLength + 4);
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(acceptResponse.AddrLength, buffer, offset);
            offset += 4;
            if (acceptResponse.AddrLength > 0)
            {
                Array.Copy(acceptResponse.Addr, 0, buffer, offset, acceptResponse.AddrLength);
                offset += (int)acceptResponse.AddrLength;
            }
            EncodeInt32(acceptResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(acceptResponse.ResponseErrno, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a AcceptResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>AcceptResponse object.</returns>
        public static MessagePayloads.AcceptResponse ExtractAcceptResponse(byte[] buffer, int offset)
        {
            AcceptResponse acceptResponse = new MessagePayloads.AcceptResponse();

            acceptResponse.AddrLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (acceptResponse.AddrLength > 0)
            {
                acceptResponse.Addr = new byte[acceptResponse.AddrLength];
                Array.Copy(buffer, offset, acceptResponse.Addr, 0, acceptResponse.AddrLength);
                offset += (int)acceptResponse.AddrLength;
            }
            acceptResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            acceptResponse.ResponseErrno = ExtractInt32(buffer, offset);
            return acceptResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the AcceptResponse object.
        /// </summary>
        /// <param name="acceptResponse">AcceptResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded AcceptResponse object.</returns>
        public static int EncodedAcceptResponseBufferSize(MessagePayloads.AcceptResponse acceptResponse)
        {
            int result = 0;
            result += (int)acceptResponse.AddrLength;
            return result + 12;
        }

        /// <summary>
        /// Encode a IoctlRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="ioctlRequest">IoctlRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded IoctlRequest object.</returns>
        public static byte[] EncodeIoctlRequest(MessagePayloads.IoctlRequest ioctlRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(ioctlRequest.Command, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a IoctlRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>IoctlRequest object.</returns>
        public static MessagePayloads.IoctlRequest ExtractIoctlRequest(byte[] buffer, int offset)
        {
            IoctlRequest ioctlRequest = new MessagePayloads.IoctlRequest();

            ioctlRequest.Command = ExtractInt32(buffer, offset);
            return ioctlRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the IoctlRequest object.
        /// </summary>
        /// <param name="ioctlRequest">IoctlRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded IoctlRequest object.</returns>
        public static int EncodedIoctlRequestBufferSize(MessagePayloads.IoctlRequest ioctlRequest)
        {
            return 4;
        }

        /// <summary>
        /// Encode a IoctlResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="ioctlResponse">IoctlResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded IoctlResponse object.</returns>
        public static byte[] EncodeIoctlResponse(MessagePayloads.IoctlResponse ioctlResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(ioctlResponse.AddrLength + 4);
            length += 12;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(ioctlResponse.AddrLength, buffer, offset);
            offset += 4;
            if (ioctlResponse.AddrLength > 0)
            {
                Array.Copy(ioctlResponse.Addr, 0, buffer, offset, ioctlResponse.AddrLength);
                offset += (int)ioctlResponse.AddrLength;
            }
            EncodeInt32(ioctlResponse.Flags, buffer, offset);
            offset += 4;
            EncodeInt32(ioctlResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(ioctlResponse.ResponseErrno, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a IoctlResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>IoctlResponse object.</returns>
        public static MessagePayloads.IoctlResponse ExtractIoctlResponse(byte[] buffer, int offset)
        {
            IoctlResponse ioctlResponse = new MessagePayloads.IoctlResponse();

            ioctlResponse.AddrLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (ioctlResponse.AddrLength > 0)
            {
                ioctlResponse.Addr = new byte[ioctlResponse.AddrLength];
                Array.Copy(buffer, offset, ioctlResponse.Addr, 0, ioctlResponse.AddrLength);
                offset += (int)ioctlResponse.AddrLength;
            }
            ioctlResponse.Flags = ExtractInt32(buffer, offset);
            offset += 4;
            ioctlResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            ioctlResponse.ResponseErrno = ExtractInt32(buffer, offset);
            return ioctlResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the IoctlResponse object.
        /// </summary>
        /// <param name="ioctlResponse">IoctlResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded IoctlResponse object.</returns>
        public static int EncodedIoctlResponseBufferSize(MessagePayloads.IoctlResponse ioctlResponse)
        {
            int result = 0;
            result += (int)ioctlResponse.AddrLength;
            return result + 16;
        }

        /// <summary>
        /// Encode a GetSockPeerNameRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getSockPeerNameRequest">GetSockPeerNameRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetSockPeerNameRequest object.</returns>
        public static byte[] EncodeGetSockPeerNameRequest(MessagePayloads.GetSockPeerNameRequest getSockPeerNameRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeInt32(getSockPeerNameRequest.SocketHandle, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a GetSockPeerNameRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetSockPeerNameRequest object.</returns>
        public static MessagePayloads.GetSockPeerNameRequest ExtractGetSockPeerNameRequest(byte[] buffer, int offset)
        {
            GetSockPeerNameRequest getSockPeerNameRequest = new MessagePayloads.GetSockPeerNameRequest();

            getSockPeerNameRequest.SocketHandle = ExtractInt32(buffer, offset);
            return getSockPeerNameRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetSockPeerNameRequest object.
        /// </summary>
        /// <param name="getSockPeerNameRequest">GetSockPeerNameRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetSockPeerNameRequest object.</returns>
        public static int EncodedGetSockPeerNameRequestBufferSize(MessagePayloads.GetSockPeerNameRequest getSockPeerNameRequest)
        {
            return 4;
        }

        /// <summary>
        /// Encode a GetSockPeerNameResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="getSockPeerNameResponse">GetSockPeerNameResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded GetSockPeerNameResponse object.</returns>
        public static byte[] EncodeGetSockPeerNameResponse(MessagePayloads.GetSockPeerNameResponse getSockPeerNameResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(getSockPeerNameResponse.AddrLength + 4);
            length += 8;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(getSockPeerNameResponse.AddrLength, buffer, offset);
            offset += 4;
            if (getSockPeerNameResponse.AddrLength > 0)
            {
                Array.Copy(getSockPeerNameResponse.Addr, 0, buffer, offset, getSockPeerNameResponse.AddrLength);
                offset += (int)getSockPeerNameResponse.AddrLength;
            }
            EncodeInt32(getSockPeerNameResponse.Result, buffer, offset);
            offset += 4;
            EncodeInt32(getSockPeerNameResponse.ResponseErrno, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a GetSockPeerNameResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>GetSockPeerNameResponse object.</returns>
        public static MessagePayloads.GetSockPeerNameResponse ExtractGetSockPeerNameResponse(byte[] buffer, int offset)
        {
            GetSockPeerNameResponse getSockPeerNameResponse = new MessagePayloads.GetSockPeerNameResponse();

            getSockPeerNameResponse.AddrLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (getSockPeerNameResponse.AddrLength > 0)
            {
                getSockPeerNameResponse.Addr = new byte[getSockPeerNameResponse.AddrLength];
                Array.Copy(buffer, offset, getSockPeerNameResponse.Addr, 0, getSockPeerNameResponse.AddrLength);
                offset += (int)getSockPeerNameResponse.AddrLength;
            }
            getSockPeerNameResponse.Result = ExtractInt32(buffer, offset);
            offset += 4;
            getSockPeerNameResponse.ResponseErrno = ExtractInt32(buffer, offset);
            return getSockPeerNameResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the GetSockPeerNameResponse object.
        /// </summary>
        /// <param name="getSockPeerNameResponse">GetSockPeerNameResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded GetSockPeerNameResponse object.</returns>
        public static int EncodedGetSockPeerNameResponseBufferSize(MessagePayloads.GetSockPeerNameResponse getSockPeerNameResponse)
        {
            int result = 0;
            result += (int)getSockPeerNameResponse.AddrLength;
            return result + 12;
        }

        /// <summary>
        /// Encode a EventData object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="eventData">EventData object to be encoded.</param>
        /// <returns>Byte array containing the encoded EventData object.</returns>
        public static byte[] EncodeEventData(MessagePayloads.EventData eventData)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 13;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            buffer[offset] = eventData.Interface;
            offset += 1;
            EncodeUInt32(eventData.Function, buffer, offset);
            offset += 4;
            EncodeUInt32(eventData.StatusCode, buffer, offset);
            offset += 4;
            EncodeUInt32(eventData.MessageId, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a EventData object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>EventData object.</returns>
        public static MessagePayloads.EventData ExtractEventData(byte[] buffer, int offset)
        {
            EventData eventData = new MessagePayloads.EventData();

            eventData.Interface = buffer[offset];
            offset += 1;
            eventData.Function = ExtractUInt32(buffer, offset);
            offset += 4;
            eventData.StatusCode = ExtractUInt32(buffer, offset);
            offset += 4;
            eventData.MessageId = ExtractUInt32(buffer, offset);
            return eventData;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the EventData object.
        /// </summary>
        /// <param name="eventData">EventData object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded EventData object.</returns>
        public static int EncodedEventDataBufferSize(MessagePayloads.EventData eventData)
        {
            return 13;
        }

        /// <summary>
        /// Encode a EventDataPayload object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="eventDataPayload">EventDataPayload object to be encoded.</param>
        /// <returns>Byte array containing the encoded EventDataPayload object.</returns>
        public static byte[] EncodeEventDataPayload(MessagePayloads.EventDataPayload eventDataPayload)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(eventDataPayload.PayloadLength + 4);
            length += 4;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt32(eventDataPayload.MessageId, buffer, offset);
            offset += 4;
            EncodeUInt32(eventDataPayload.PayloadLength, buffer, offset);
            offset += 4;
            if (eventDataPayload.PayloadLength > 0)
            {
                Array.Copy(eventDataPayload.Payload, 0, buffer, offset, eventDataPayload.PayloadLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a EventDataPayload object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>EventDataPayload object.</returns>
        public static MessagePayloads.EventDataPayload ExtractEventDataPayload(byte[] buffer, int offset)
        {
            EventDataPayload eventDataPayload = new MessagePayloads.EventDataPayload();

            eventDataPayload.MessageId = ExtractUInt32(buffer, offset);
            offset += 4;
            eventDataPayload.PayloadLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (eventDataPayload.PayloadLength > 0)
            {
                eventDataPayload.Payload = new byte[eventDataPayload.PayloadLength];
                Array.Copy(buffer, offset, eventDataPayload.Payload, 0, eventDataPayload.PayloadLength);
            }
            return eventDataPayload;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the EventDataPayload object.
        /// </summary>
        /// <param name="eventDataPayload">EventDataPayload object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded EventDataPayload object.</returns>
        public static int EncodedEventDataPayloadBufferSize(MessagePayloads.EventDataPayload eventDataPayload)
        {
            int result = 0;
            result += (int)eventDataPayload.PayloadLength;
            return result + 8;
        }

        /// <summary>
        /// Encode a SetAntennaRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="setAntennaRequest">SetAntennaRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded SetAntennaRequest object.</returns>
        public static byte[] EncodeSetAntennaRequest(MessagePayloads.SetAntennaRequest setAntennaRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += 2;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            buffer[offset] = setAntennaRequest.Antenna;
            offset += 1;
            buffer[offset] = setAntennaRequest.Persist;
            return buffer;
        }

        /// <summary>
        /// Extract a SetAntennaRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>SetAntennaRequest object.</returns>
        public static MessagePayloads.SetAntennaRequest ExtractSetAntennaRequest(byte[] buffer, int offset)
        {
            SetAntennaRequest setAntennaRequest = new MessagePayloads.SetAntennaRequest();

            setAntennaRequest.Antenna = buffer[offset];
            offset += 1;
            setAntennaRequest.Persist = buffer[offset];
            return setAntennaRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the SetAntennaRequest object.
        /// </summary>
        /// <param name="setAntennaRequest">SetAntennaRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded SetAntennaRequest object.</returns>
        public static int EncodedSetAntennaRequestBufferSize(MessagePayloads.SetAntennaRequest setAntennaRequest)
        {
            return 2;
        }

        /// <summary>
        /// Encode a BTStackConfig object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="bTStackConfig">BTStackConfig object to be encoded.</param>
        /// <returns>Byte array containing the encoded BTStackConfig object.</returns>
        public static byte[] EncodeBTStackConfig(MessagePayloads.BTStackConfig bTStackConfig)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += bTStackConfig.Config.Length + 1;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeString(bTStackConfig.Config, buffer, offset);
            return buffer;
        }

        /// <summary>
        /// Extract a BTStackConfig object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>BTStackConfig object.</returns>
        public static MessagePayloads.BTStackConfig ExtractBTStackConfig(byte[] buffer, int offset)
        {
            BTStackConfig bTStackConfig = new MessagePayloads.BTStackConfig();

            bTStackConfig.Config = ExtractString(buffer, offset);
            return bTStackConfig;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the BTStackConfig object.
        /// </summary>
        /// <param name="bTStackConfig">BTStackConfig object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded BTStackConfig object.</returns>
        public static int EncodedBTStackConfigBufferSize(MessagePayloads.BTStackConfig bTStackConfig)
        {
            int result = 0;
            result += bTStackConfig.Config.Length;
            return result + 1;
        }

        /// <summary>
        /// Encode a BTDataWriteRequest object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="bTDataWriteRequest">BTDataWriteRequest object to be encoded.</param>
        /// <returns>Byte array containing the encoded BTDataWriteRequest object.</returns>
        public static byte[] EncodeBTDataWriteRequest(MessagePayloads.BTDataWriteRequest bTDataWriteRequest)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(bTDataWriteRequest.DataLength + 4);
            length += 2;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt16(bTDataWriteRequest.Handle, buffer, offset);
            offset += 2;
            EncodeUInt32(bTDataWriteRequest.DataLength, buffer, offset);
            offset += 4;
            if (bTDataWriteRequest.DataLength > 0)
            {
                Array.Copy(bTDataWriteRequest.Data, 0, buffer, offset, bTDataWriteRequest.DataLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a BTDataWriteRequest object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>BTDataWriteRequest object.</returns>
        public static MessagePayloads.BTDataWriteRequest ExtractBTDataWriteRequest(byte[] buffer, int offset)
        {
            BTDataWriteRequest bTDataWriteRequest = new MessagePayloads.BTDataWriteRequest();

            bTDataWriteRequest.Handle = ExtractUInt16(buffer, offset);
            offset += 2;
            bTDataWriteRequest.DataLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (bTDataWriteRequest.DataLength > 0)
            {
                bTDataWriteRequest.Data = new byte[bTDataWriteRequest.DataLength];
                Array.Copy(buffer, offset, bTDataWriteRequest.Data, 0, bTDataWriteRequest.DataLength);
            }
            return bTDataWriteRequest;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the BTDataWriteRequest object.
        /// </summary>
        /// <param name="bTDataWriteRequest">BTDataWriteRequest object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded BTDataWriteRequest object.</returns>
        public static int EncodedBTDataWriteRequestBufferSize(MessagePayloads.BTDataWriteRequest bTDataWriteRequest)
        {
            int result = 0;
            result += (int)bTDataWriteRequest.DataLength;
            return result + 6;
        }

        /// <summary>
        /// Encode a BTGetHandlesResponse object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="bTGetHandlesResponse">BTGetHandlesResponse object to be encoded.</param>
        /// <returns>Byte array containing the encoded BTGetHandlesResponse object.</returns>
        public static byte[] EncodeBTGetHandlesResponse(MessagePayloads.BTGetHandlesResponse bTGetHandlesResponse)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(bTGetHandlesResponse.HandlesLength + 4);
            length += 2;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt16(bTGetHandlesResponse.HandleCount, buffer, offset);
            offset += 2;
            EncodeUInt32(bTGetHandlesResponse.HandlesLength, buffer, offset);
            offset += 4;
            if (bTGetHandlesResponse.HandlesLength > 0)
            {
                Array.Copy(bTGetHandlesResponse.Handles, 0, buffer, offset, bTGetHandlesResponse.HandlesLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a BTGetHandlesResponse object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>BTGetHandlesResponse object.</returns>
        public static MessagePayloads.BTGetHandlesResponse ExtractBTGetHandlesResponse(byte[] buffer, int offset)
        {
            BTGetHandlesResponse bTGetHandlesResponse = new MessagePayloads.BTGetHandlesResponse();

            bTGetHandlesResponse.HandleCount = ExtractUInt16(buffer, offset);
            offset += 2;
            bTGetHandlesResponse.HandlesLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (bTGetHandlesResponse.HandlesLength > 0)
            {
                bTGetHandlesResponse.Handles = new byte[bTGetHandlesResponse.HandlesLength];
                Array.Copy(buffer, offset, bTGetHandlesResponse.Handles, 0, bTGetHandlesResponse.HandlesLength);
            }
            return bTGetHandlesResponse;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the BTGetHandlesResponse object.
        /// </summary>
        /// <param name="bTGetHandlesResponse">BTGetHandlesResponse object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded BTGetHandlesResponse object.</returns>
        public static int EncodedBTGetHandlesResponseBufferSize(MessagePayloads.BTGetHandlesResponse bTGetHandlesResponse)
        {
            int result = 0;
            result += (int)bTGetHandlesResponse.HandlesLength;
            return result + 6;
        }

        /// <summary>
        /// Encode a BTServerDataSet object and return a byte array containing the encoded message.
        /// </summary>
        /// <param name="bTServerDataSet">BTServerDataSet object to be encoded.</param>
        /// <returns>Byte array containing the encoded BTServerDataSet object.</returns>
        public static byte[] EncodeBTServerDataSet(MessagePayloads.BTServerDataSet bTServerDataSet)
        {
            int offset = 0;
            int length = 0;

            //
            //  Calculate the amount of memory needed.
            //
            length += (int)(bTServerDataSet.SetDataLength + 4);
            length += 2;

            //
            //  Now allocate a new buffer and copy the data in to the buffer.
            //
            byte[] buffer = new byte[length];
            Array.Clear(buffer, 0, buffer.Length);
            EncodeUInt16(bTServerDataSet.Handle, buffer, offset);
            offset += 2;
            EncodeUInt32(bTServerDataSet.SetDataLength, buffer, offset);
            offset += 4;
            if (bTServerDataSet.SetDataLength > 0)
            {
                Array.Copy(bTServerDataSet.SetData, 0, buffer, offset, bTServerDataSet.SetDataLength);
            }
            return buffer;
        }

        /// <summary>
        /// Extract a BTServerDataSet object from a byte array.
        /// </summary>
        /// <param name="buffer">Byte array containing the encoded data.</param>
        /// <param name="offset">Offset into the buffer where the encoded data can be found.</param>
        /// <returns>BTServerDataSet object.</returns>
        public static MessagePayloads.BTServerDataSet ExtractBTServerDataSet(byte[] buffer, int offset)
        {
            BTServerDataSet bTServerDataSet = new MessagePayloads.BTServerDataSet();

            bTServerDataSet.Handle = ExtractUInt16(buffer, offset);
            offset += 2;
            bTServerDataSet.SetDataLength = ExtractUInt32(buffer, offset);
            offset += 4;
            if (bTServerDataSet.SetDataLength > 0)
            {
                bTServerDataSet.SetData = new byte[bTServerDataSet.SetDataLength];
                Array.Copy(buffer, offset, bTServerDataSet.SetData, 0, bTServerDataSet.SetDataLength);
            }
            return bTServerDataSet;
        }

        /// <summary>
        /// Calculate the amount of memory required to hold the given instance of the BTServerDataSet object.
        /// </summary>
        /// <param name="bTServerDataSet">BTServerDataSet object to be encoded.</param>
        /// <returns>Number of bytes required to hold the encoded BTServerDataSet object.</returns>
        public static int EncodedBTServerDataSetBufferSize(MessagePayloads.BTServerDataSet bTServerDataSet)
        {
            int result = 0;
            result += (int)bTServerDataSet.SetDataLength;
            return result + 6;
        }

    }
}
