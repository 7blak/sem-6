using lab1;

namespace TestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void EmptyStringTest()
        {

            // Given
            string input = "";
            int expectedResult = 0;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void SingleNumberTest()
        {
            // Given
            string input = "42";
            int expectedResult = 42;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TwoNumbersCommaTest()
        {
            // Given
            string input = "10,23";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TwoNumbersNewlineTest()
        {
            // Given
            string input = "10\n23";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void ThreeNumbersTest()
        {
            // Given
            string input = "5\n10,18";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Negative numbers not allowed!")]
        public void NegativeNumberTest()
        {
            // Given
            string input = "-23,10";

            // When
            Calculator.Calc(input);

            // Then
            Assert.Fail();
        }

        [TestMethod]
        public void NumbersGreaterThan1000Test()
        {
            // Given
            string input = "10,1001,3333,23";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CustomSingleDelimiterTest()
        {
            // Given
            string input = "//#10#23";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CustomMultiDelimiterTest()
        {
            // Given
            string input = "//[#$#]10#$#23";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void CustomMultiMultiDelimiterTest()
        {
            // Given
            string input = "//[#$#][%%%]5%%%15#$#13";
            int expectedResult = 33;

            // When
            int actualResult = Calculator.Calc(input);

            // Then
            Assert.AreEqual(expectedResult, actualResult);
        }
    }
}