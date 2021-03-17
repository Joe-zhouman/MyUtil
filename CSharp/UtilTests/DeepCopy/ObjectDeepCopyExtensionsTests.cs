using Microsoft.VisualStudio.TestTools.UnitTesting;
using Util.DeepCopy;

namespace UtilTests.DeepCopy
{
    [TestClass()]
    public class ObjectDeepCopyExtensionsTests
    {
        [TestMethod()]
        public void MemberwiseCopyTest() {
            int[] a = {0, 0, 0, 0};
            var aMemberwiseCopy = a.MemberwiseCopy();
            a[0] = 1; 
            Assert.AreNotEqual(1, aMemberwiseCopy[0]);
        }
        [TestMethod()]
        public void ShallowCopyTest()
        {
            int[] a = { 0, 0, 0, 0 };
            var aShallowCopy = a;
            a[0] = 1;
            Assert.AreEqual(1, aShallowCopy[0]);
        }
        [TestMethod()]
        public void DeepCopyTest()
        {
            int[] a = { 0, 0, 0, 0 };
            var aDeepCopy = a.DeepCopy();
            a[0] = 1;
            Assert.AreNotEqual(1, aDeepCopy[0]);
        }
    }
}