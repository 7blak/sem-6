using lab1;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void EmptyStringTest()
        {
            int expectedResult = 0;
            int actualResult = Calculator.Calc("");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void SingleNumberTest()
        {
            int expectedResult = 42;
            int actualResult = Calculator.Calc("42");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TwoNumbersCommaTest()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("10,23");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TwoNumbersNewlineTest()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("10\n23");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void ThreeNumbersTest()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("5\n10,18");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Negative numbers not allowed!")]
        public void NegativeNumberTest()
        {
            Calculator.Calc("-23,10");

            Assert.Fail();
        }

        [TestMethod]
        public void NumbersGreaterThan1000Test()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("10,1001,3333,23");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CustomSingleDelimiterTest()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("//#10#23");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CustomMultiDelimiterTest()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("//[#$#]10#$#23");

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CustomMultiMultiDelimiterTest()
        {
            int expectedResult = 33;
            int actualResult = Calculator.Calc("//[#$#][%%%]5%%%15#$#13");

            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}