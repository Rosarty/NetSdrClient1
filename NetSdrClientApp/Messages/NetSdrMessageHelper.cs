using System;
using System.Collections.Generic;
using System.Linq;

namespace NetSdrClientApp.Messages
{
    public static class NetSdrMessageHelper
    {
        private const short _maxMessageLength = 8191;
        private const short _maxDataItemMessageLength = 8194;
        private const short _msgHeaderLength = 2;
        private const short _msgControlItemLength = 2;
        private const short _msgSequenceNumberLength = 2;

        public enum MsgTypes
        {
            SetControlItem,
            CurrentControlItem,
            ControlItemRange,
            Ack,
            DataItem0,
            DataItem1,
            DataItem2,
            DataItem3
        }

        public enum ControlItemCodes
        {
            None = 0,
            IQOutputDataSampleRate = 0x00B8,
            RFFilter = 0x0044,
            ADModes = 0x008A,
            ReceiverState = 0x0018,
            ReceiverFrequency = 0x0020
        }

        // ✅ Універсальний метод побудови повідомлення
        private static byte[] BuildMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            var itemCodeBytes = itemCode != ControlItemCodes.None
                ? BitConverter.GetBytes((ushort)itemCode)
                : Array.Empty<byte>();

            var headerBytes = GetHeader(type, itemCodeBytes.Length + parameters.Length);

            return headerBytes
                .Concat(itemCodeBytes)
                .Concat(parameters)
                .ToArray();
        }

        public static byte[] GetControlItemMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
            => BuildMessage(type, itemCode, parameters);

        public static byte[] GetDataItemMessage(MsgTypes type, byte[] parameters)
            => BuildMessage(type, ControlItemCodes.None, parameters);

        public static bool TranslateMessage(byte[] msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort sequenceNumber, out byte[] body)
        {
            itemCode = ControlItemCodes.None;
            sequenceNumber = 0;

            if (msg == null || msg.Length < _msgHeaderLength)
                throw new ArgumentException("Invalid message format");

            var data = msg.AsEnumerable();

            TranslateHeader(data.Take(_msgHeaderLength).ToArray(), out type, out int msgLength);
            data = data.Skip(_msgHeaderLength);

            msgLength -= _msgHeaderLength;

            if (type < MsgTypes.DataItem0)
            {
                var value = BitConverter.ToUInt16(data.Take(_msgControlItemLength).ToArray());
                data = data.Skip(_msgControlItemLength);
                msgLength -= _msgControlItemLength;

                itemCode = Enum.IsDefined(typeof(ControlItemCodes), value)
                    ? (ControlItemCodes)value
                    : ControlItemCodes.None;
            }
            else
            {
                sequenceNumber = BitConverter.ToUInt16(data.Take(_msgSequenceNumberLength).ToArray());
                data = data.Skip(_msgSequenceNumberLength);
                msgLength -= _msgSequenceNumberLength;
            }

            body = data.ToArray();
            return body.Length == msgLength;
        }

        public static IEnumerable<int> GetSamples(ushort sampleSize, byte[] body)
        {
            sampleSize /= 8;
            if (sampleSize > 4)
                throw new ArgumentOutOfRangeException(nameof(sampleSize));

            for (int i = 0; i + sampleSize <= body.Length; i += sampleSize)
            {
                var sample = body.Skip(i).Take(sampleSize)
                    .Concat(Enumerable.Repeat((byte)0, 4 - sampleSize))
                    .ToArray();
                yield return BitConverter.ToInt32(sample);
            }
        }

        private static byte[] GetHeader(MsgTypes type, int msgLength)
        {
            int lengthWithHeader = NormalizeMessageLength(type, msgLength);
            return BitConverter.GetBytes((ushort)(lengthWithHeader + ((int)type << 13)));
        }

        private static void TranslateHeader(byte[] header, out MsgTypes type, out int msgLength)
        {
            var num = BitConverter.ToUInt16(header);
            type = (MsgTypes)(num >> 13);
            msgLength = num - ((int)type << 13);
            msgLength = NormalizeMessageLength(type, msgLength);
        }

        // ✅ Винесена спільна логіка розрахунку довжини
        private static int NormalizeMessageLength(MsgTypes type, int msgLength)
        {
            int lengthWithHeader = msgLength + _msgHeaderLength;
            if (type >= MsgTypes.DataItem0 && lengthWithHeader == _maxDataItemMessageLength)
                return 0;
            if (msgLength < 0 || lengthWithHeader > _maxMessageLength)
                throw new ArgumentException("Message length exceeds allowed value");
            return lengthWithHeader;
        }

        internal static object CreateDummyMessage()
        {
            throw new NotImplementedException();
        }
    }
}
