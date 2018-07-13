using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace SpeedDate.Test
{
    [TestFixture]
    public class TestUtil
    {
        [Test]
        public void ShouldValidatePassword()
        {
            const string password = "asdfasdf";
            
            var hash = Util.CreateHash(password);
            
            Util.ValidatePassword(password, string.Empty).ShouldBeFalse();
            Util.ValidatePassword(password, hash).ShouldBeTrue();
        }

        [Test]
        public void GenerateRandomString_ShouldThrowOnNegativeLength()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => Util.CreateRandomString(-1));
        }

        [Test]
        public void GenerateRandomString_ShouldHaveCorrectLength()
        {
            for (var i = 0; i < 20; i++)
            {
                Util.CreateRandomString(i).Length.ShouldBe(i);
            }
        }
    }
}
