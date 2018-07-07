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
            
            Util.ValidatePassword(password, hash).ShouldBeTrue();
        }
    }
}
