using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Calculator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        List<string> example = new List<string>();
        private string historyFilePath = "calculator_history.txt";

        private bool CanAddCharacter(string currentText, string characterToAdd)
        {
            string textWithoutSpaces = currentText.Replace(" ", "");

            if (textWithoutSpaces.Length >= 20)
            {
                return false;
            }
            return true;
        }

        private string HandlePercentages(string expr)
        {
            while (expr.Contains("%"))
            {
                int percentIndex = expr.IndexOf("%");
                int startIndex = percentIndex - 1;

                while (startIndex >= 0 && (char.IsDigit(expr[startIndex]) || expr[startIndex] == '.'))
                {
                    startIndex--;
                }
                startIndex++;

                string numberStr = expr.Substring(startIndex, percentIndex - startIndex);
                if (double.TryParse(numberStr, out double number))
                {
                    double percentValue = number / 100.0;
                    expr = expr.Remove(startIndex, percentIndex - startIndex + 1);
                    expr = expr.Insert(startIndex, percentValue.ToString());
                }
                else
                {
                    throw new Exception("Ошибка в проценте");
                }
            }
            return expr;
        }

        private string HandleInverse(string expr)
        {
            while (expr.Contains("^(-1)"))
            {
                int invIndex = expr.IndexOf("^(-1)");
                int startIndex = invIndex - 1;

                while (startIndex >= 0 && (char.IsDigit(expr[startIndex]) || expr[startIndex] == '.'))
                {
                    startIndex--;
                }
                startIndex++;

                string numberStr = expr.Substring(startIndex, invIndex - startIndex);

                if (double.TryParse(numberStr, out double x))
                {
                    if (x == 0) throw new Exception("Деление на ноль");
                    double res = 1.0 / x;
                    expr = expr.Remove(startIndex, invIndex + 5 - startIndex);
                    expr = expr.Insert(startIndex, res.ToString());
                }
            }
            return expr;
        }

        private string HandleTrigonometry(string expr)
        {
            while (expr.Contains("sin("))
            {
                int funcIndex = expr.IndexOf("sin(");
                int endIndex = FindClosingBracket(expr, funcIndex + 4);

                if (endIndex == -1) throw new Exception("Ошибка в скобках sin");

                string innerExpr = expr.Substring(funcIndex + 4, endIndex - funcIndex - 4);
                double innerValue = EvaluateExpression(innerExpr);
                double result = Math.Sin(innerValue * Math.PI / 180.0);

                expr = expr.Remove(funcIndex, endIndex - funcIndex + 1);
                expr = expr.Insert(funcIndex, result.ToString());
            }

            while (expr.Contains("cos("))
            {
                int funcIndex = expr.IndexOf("cos(");
                int endIndex = FindClosingBracket(expr, funcIndex + 4);

                if (endIndex == -1) throw new Exception("Ошибка в скобках cos");

                string innerExpr = expr.Substring(funcIndex + 4, endIndex - funcIndex - 4);
                double innerValue = EvaluateExpression(innerExpr);
                double result = Math.Cos(innerValue * Math.PI / 180.0);

                expr = expr.Remove(funcIndex, endIndex - funcIndex + 1);
                expr = expr.Insert(funcIndex, result.ToString());
            }

            while (expr.Contains("tan("))
            {
                int funcIndex = expr.IndexOf("tan(");
                int endIndex = FindClosingBracket(expr, funcIndex + 4);

                if (endIndex == -1) throw new Exception("Ошибка в скобках tan");

                string innerExpr = expr.Substring(funcIndex + 4, endIndex - funcIndex - 4);
                double innerValue = EvaluateExpression(innerExpr);
                double result = Math.Tan(innerValue * Math.PI / 180.0);

                expr = expr.Remove(funcIndex, endIndex - funcIndex + 1);
                expr = expr.Insert(funcIndex, result.ToString());
            }

            return expr;
        }

        private string HandleSquareRoot(string expr)
        {
            while (expr.Contains("√("))
            {
                int sqrtIndex = expr.IndexOf("√(");
                int endIndex = FindClosingBracket(expr, sqrtIndex + 2);

                if (endIndex == -1) throw new Exception("Ошибка в скобках корня");

                string innerExpr = expr.Substring(sqrtIndex + 2, endIndex - sqrtIndex - 2);
                double innerValue = EvaluateExpression(innerExpr);

                if (innerValue < 0) throw new Exception("Корень из отрицательного числа");

                double result = Math.Sqrt(innerValue);
                expr = expr.Remove(sqrtIndex, endIndex - sqrtIndex + 1);
                expr = expr.Insert(sqrtIndex, result.ToString());
            }
            return expr;
        }

        private string HandlePowers(string expr)
        {
            while (expr.Contains("^"))
            {
                int powIndex = expr.IndexOf("^");

                int leftStart = powIndex - 1;
                while (leftStart >= 0 && (char.IsDigit(expr[leftStart]) || expr[leftStart] == '.'))
                {
                    leftStart--;
                }
                leftStart++;

                int rightEnd = powIndex + 1;
                while (rightEnd < expr.Length && (char.IsDigit(expr[rightEnd]) || expr[rightEnd] == '.'))
                {
                    rightEnd++;
                }

                string leftNum = expr.Substring(leftStart, powIndex - leftStart);
                string rightNum = expr.Substring(powIndex + 1, rightEnd - powIndex - 1);

                if (double.TryParse(leftNum, out double baseNum) && double.TryParse(rightNum, out double exponent))
                {
                    double result = Math.Pow(baseNum, exponent);
                    expr = expr.Remove(leftStart, rightEnd - leftStart);
                    expr = expr.Insert(leftStart, result.ToString());
                }
            }
            return expr;
        }

        private double EvaluateBasicArithmetic(string expression)
        {
            List<string> terms = SplitByAdditionSubtraction(expression);
            double result = 0;
            bool isFirst = true;

            foreach (string term in terms)
            {
                double value = EvaluateMultiplicationDivision(term);
                if (isFirst)
                {
                    result = value;
                    isFirst = false;
                }
                else
                {
                    if (term.StartsWith("+") || char.IsDigit(term[0]) || term[0] == '.')
                    {
                        result += value;
                    }
                    else if (term.StartsWith("-"))
                    {
                        result -= Math.Abs(value);
                    }
                }
            }

            return result;
        }

        private double EvaluateMultiplicationDivision(string expression)
        {
            List<string> factors = SplitByMultiplicationDivision(expression);
            double result = 1;
            bool isFirst = true;
            string lastOperator = "*";

            foreach (string factor in factors)
            {
                double value = double.Parse(factor);

                if (isFirst)
                {
                    result = value;
                    isFirst = false;
                }
                else if (lastOperator == "*")
                {
                    result *= value;
                }
                else if (lastOperator == "/")
                {
                    if (value == 0) throw new Exception("Деление на ноль");
                    result /= value;
                }
            }

            return result;
        }

        private List<string> SplitByAdditionSubtraction(string expression)
        {
            List<string> terms = new List<string>();
            string currentTerm = "";

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if ((c == '+' || c == '-') && i > 0)
                {
                    terms.Add(currentTerm);
                    currentTerm = c.ToString();
                }
                else
                {
                    currentTerm += c;
                }
            }

            if (!string.IsNullOrEmpty(currentTerm))
            {
                terms.Add(currentTerm);
            }

            return terms;
        }

        private List<string> SplitByMultiplicationDivision(string expression)
        {
            List<string> factors = new List<string>();
            string currentFactor = "";
            string operators = "";

            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];

                if (c == '*' || c == '/')
                {
                    if (!string.IsNullOrEmpty(currentFactor))
                    {
                        factors.Add(currentFactor);
                        operators += c;
                        currentFactor = "";
                    }
                }
                else
                {
                    currentFactor += c;
                }
            }

            if (!string.IsNullOrEmpty(currentFactor))
            {
                factors.Add(currentFactor);
            }

            return factors;
        }
        private int FindClosingBracket(string expression, int startIndex)
        {
            int bracketCount = 1;
            for (int i = startIndex; i < expression.Length; i++)
            {
                if (expression[i] == '(') bracketCount++;
                if (expression[i] == ')') bracketCount--;
                if (bracketCount == 0) return i;
            }
            return -1;
        }

        private double EvaluateExpression(string expression)
        {
            expression = expression.Replace(" ", "");

            expression = HandlePercentages(expression);

            expression = HandleInverse(expression);

            expression = HandleTrigonometry(expression);

            expression = HandleSquareRoot(expression);

            expression = HandlePowers(expression);

            return EvaluateBasicArithmetic(expression);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!Charge) return;

            if (e.Key == Key.Back)
            {
                if (Example.Text.Length > 0)
                    Example.Text = Example.Text.Remove(Example.Text.Length - 1);
            }
            else if (e.Key == Key.Delete)
            {
                example.Clear();
                Example.Text = string.Empty;
                Example2.Text = string.Empty;
            }
            else if (e.Key == Key.OemMinus)
            {
                if (CanAddCharacter(Example.Text, " - "))
                    Example.Text += " - ";
            }
            else if (e.Key == Key.OemPlus)
            {
                if (CanAddCharacter(Example.Text, " + "))
                    Example.Text += " + ";
            }
            else if (e.Key == Key.Multiply)
            {
                if (CanAddCharacter(Example.Text, " * "))
                    Example.Text += " * ";
            }
            else if (e.Key == Key.OemPeriod)
            {
                if (CanAddCharacter(Example.Text, "."))
                    Example.Text += ".";
            }
            else if (e.Key == Key.Divide)
            {
                if (CanAddCharacter(Example.Text, " / "))
                    Example.Text += " / ";
            }
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
            {
                string digit = (e.Key - Key.D0).ToString();
                if (CanAddCharacter(Example.Text, digit))
                    Example.Text += digit;
            }
            else if (e.Key == Key.Enter)
            {
                Answer_Click(sender, e);
            }
        }
        private void Delite_Click(object sender, RoutedEventArgs e)
        {
            example.Clear();
            Example.Text = string.Empty;
            Example2.Text = string.Empty;
        }

        private void DeliteOneNumber_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Example.Text))
            {
                Example.Text = Example.Text.Remove(Example.Text.Length - 1);
            }
        }

        private void One_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "1"))
                Example.Text += "1";
        }
        private void Two_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "2"))
                Example.Text += "2";
        }
        private void Three_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "3"))
                Example.Text += "3";
        }

        private void Four_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "4"))
                Example.Text += "4";
        }

        private void Five_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "5"))
                Example.Text += "5";
        }

        private void Six_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "6"))
                Example.Text += "6";
        }

        private void Seven_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "7"))
                Example.Text += "7";
        }

        private void Eight_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "8"))
                Example.Text += "8";
        }

        private void Nine_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "9"))
                Example.Text += "9";
        }
        private void Null_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "0"))
                Example.Text += "0";
        }

        private void Plus_Click(object sender, RoutedEventArgs e)
        {
            string operatorToAdd = " + ";
            if (CanAddCharacter(Example.Text, operatorToAdd))
                Example.Text += operatorToAdd;
        }

        private void Minus_Click(object sender, RoutedEventArgs e)
        {
            string operatorToAdd = " - ";
            if (CanAddCharacter(Example.Text, operatorToAdd))
                Example.Text += operatorToAdd;
        }

        private void Multiplication_Click(object sender, RoutedEventArgs e)
        {
            string operatorToAdd = " * ";
            if (CanAddCharacter(Example.Text, operatorToAdd))
                Example.Text += operatorToAdd;
        }

        private void Dividion_Click(object sender, RoutedEventArgs e)
        {
            string operatorToAdd = " / ";
            if (CanAddCharacter(Example.Text, operatorToAdd))
                Example.Text += operatorToAdd;
        }
        private void Point_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "."))
                Example.Text += ".";
        }

        private void Percent_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "%"))
                Example.Text += "%";
        }

        private void Degree_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "^"))
                Example.Text += "^";
        }

        private void Root_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "√("))
                Example.Text += "√(";
        }

        private void BracetsLeft_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "("))
                Example.Text += "(";
        }

        private void BracetsRight_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, ")"))
                Example.Text += ")";
        }

        private void Sin_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "sin("))
                Example.Text += "sin(";
        }

        private void Cos_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "cos("))
                Example.Text += "cos(";
        }

        private void Tan_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "tan("))
                Example.Text += "tan(";
        }

        private void Invers_Click(object sender, RoutedEventArgs e)
        {
            if (CanAddCharacter(Example.Text, "^(-1)"))
                Example.Text += "^(-1)";
        }

        bool Swap = false;

        private void SwapMod_Click(object sender, RoutedEventArgs e)
        {
            if (!Swap)
            {
                FirstRow.Height = new GridLength(1, GridUnitType.Star);
                SecondRow.Height = new GridLength(1, GridUnitType.Star);
                Swap = true;
                this.Resources["ButtonColor"] = new SolidColorBrush(Colors.White);
                Tema = true;
            }
            else
            {
                FirstRow.Height = new GridLength(0, GridUnitType.Star);
                SecondRow.Height = new GridLength(0, GridUnitType.Star);
                Swap = false;
                this.Resources["ButtonColor"] = new SolidColorBrush(Color.FromRgb(153, 149, 140));
                Tema = false;
            }
        }

        private void SaveToHistory(string record)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(historyFilePath, true))
                {
                    writer.WriteLine(record);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения в историю: {ex.Message}");
            }
        }

        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            if (!Charge) return;

            string expression = Example.Text;
            if (string.IsNullOrWhiteSpace(expression)) return;

            try
            {
                double result = EvaluateExpression(expression);
                string resultString = result.ToString();
                string historyRecord = $"{expression} = {resultString}";

                SaveToHistory(historyRecord);

                Example2.Text = $"{expression} = {resultString}";
                Example.Text = resultString;
            }
            catch (Exception ex)
            {
                Example2.Text = $"Ошибка: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Ошибка вычисления: {ex.Message}");
            }
        }

        bool Charge = false;
        private void OnOff_Click(object sender, RoutedEventArgs e)
        {
            if (!Charge)
            {
                Example.Background = new SolidColorBrush(Color.FromRgb(175, 218, 252));
                Example2.Background = new SolidColorBrush(Color.FromRgb(175, 218, 252));
                FullWindow.Background = new SolidColorBrush(Color.FromRgb(175, 218, 252));
                UpperWindow.Background = new SolidColorBrush(Color.FromRgb(175, 218, 252));
                Butareya.Visibility= Visibility.Visible;
                Time.Visibility= Visibility.Visible;
                ButtonsGrid.IsEnabled = true;
                Charge = true;
            }
            else
            {
                Example.Background = new SolidColorBrush(Color.FromRgb(35, 40, 43));
                Example2.Background = new SolidColorBrush(Color.FromRgb(35, 40, 43));
                FullWindow.Background = new SolidColorBrush(Color.FromRgb(35, 40, 43));
                UpperWindow.Background = new SolidColorBrush(Color.FromRgb(35, 40, 43));
                Butareya.Visibility = Visibility.Hidden;
                Time.Visibility = Visibility.Hidden;
                ButtonsGrid.IsEnabled = false;
                Example.Text = string.Empty;
                Example2.Text = string.Empty;
                Charge = false;
            }
        }

        bool Tema = false;

        private void Thema_Click(object sender, RoutedEventArgs e)
        {
            if (!Tema)
            {
                this.Resources["ButtonColor"] = new SolidColorBrush(Colors.White);
                Tema = true;
            }
            else
            {
                this.Resources["ButtonColor"] = new SolidColorBrush(Color.FromRgb(153, 149, 140));
                Tema = false;
            }
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            Menu.PlacementTarget = sender as Button;
            Menu.IsOpen = true;
        }

        private void DeliteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("calculator_history.txt"))
                File.Delete("calculator_history.txt");

            Example.Text = "";
            Example2.Text = "";
        }
    }
}