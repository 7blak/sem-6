
using System.Numerics;
using System.Text;

namespace lab1
{
    public class Calculator
    {
        public static int Calc(string input)
        {
            if (input == null || input.Length == 0)
                return 0;

            int index = 0;
            List<string> delimiters = new List<string>() { ",", "\n", "]" };

            if (input.StartsWith("//"))
            {
                index = 2;
                if (input[index] != '[')
                    delimiters.Add(input[index].ToString());
                else
                {
                    index++;
                    StringBuilder sb = new StringBuilder();
                    while (index < input.Length)
                    {
                        if (input[index] != ']')
                        {
                            sb.Append(input[index++]);
                        }
                        else
                        {
                            delimiters.Add(sb.ToString());
                            sb.Clear();
                            if (input[++index] != '[')
                                break;
                            ++index;
                        }
                    }
                }
            }

            string[] operators = delimiters.ToArray();

            string[] numbers = input.Split(operators, StringSplitOptions.RemoveEmptyEntries);

            int result = 0;

            for (int i = 0; i < numbers.Length; i++)
            {
                if (!int.TryParse(numbers[i], out int num))
                    continue;
                if (num < 0)
                    throw new ArgumentException("Negative numbers are not allowed!");
                if (num > 1000)
                    num = 0;
                result += num;
            }
            return result;
        }
    }
}