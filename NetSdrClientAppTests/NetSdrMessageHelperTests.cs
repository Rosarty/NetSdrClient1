using NetSdrClientApp.Messages;
using NUnit.Framework;
using System;
using System.Linq;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void GetControlItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(actualCode, Is.EqualTo((short)code));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));
            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

   
        // ✅ Новий тест 2 — Перевірка на нульову довжину параметрів
        [Test]
        public void GetControlItemMessage_ShouldWork_WithZeroLengthParameters()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, Array.Empty<byte>());

            Assert.That(msg.Length, Is.GreaterThanOrEqualTo(4)); // тільки заголовок + код
        }

        [Test]
        public void GetControlItemMessage_ShouldEncodeAndDecodeProperly()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;

            // Вхідні дані (у параметрах передаємо байти з відомим вмістом)
            byte[] parameters = Enumerable.Range(0, 10).Select(i => (byte)i).ToArray();

            // Виклик методу
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            // Розбір повідомлення
            ushort num = BitConverter.ToUInt16(msg, 0);
            var decodedType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var decodedLength = num - ((int)decodedType << 13);
            var decodedCode = BitConverter.ToInt16(msg, 2);
            var decodedParameters = msg.Skip(4).ToArray();

            // Перевірки
            Assert.Multiple(() =>
            {
                Assert.That(decodedType, Is.EqualTo(type), "Тип повідомлення не збігається");
                Assert.That(decodedCode, Is.EqualTo((short)code), "Код повідомлення не збігається");
                Assert.That(decodedLength, Is.EqualTo(msg.Length), "Довжина повідомлення некоректна");
                Assert.That(decodedParameters, Is.EqualTo(parameters), "Параметри повідомлення некоректно передані");
            });
        }
        // ✅ Новий тест 3 — Інший тип повідомлення для DataItem
        [Test]
        public void GetDataItemMessage_OtherType_WorksCorrectly()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem1;
            int parametersLength = 100;

            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var num = BitConverter.ToUInt16(msg.Take(2).ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            Assert.That(actualType, Is.EqualTo(type));
        }

        // ✅ Новий тест 4 — Некоректний тип або код (якщо метод повинен це обробляти)
        [Test]
        public void GetControlItemMessage_InvalidCode_ShouldNotCrash()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var invalidCode = (NetSdrMessageHelper.ControlItemCodes)9999;

            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, invalidCode, new byte[10]);
            Assert.That(msg.Length, Is.GreaterThan(0));
        }
    }
}

