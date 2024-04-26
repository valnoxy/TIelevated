namespace TIelevated.Common
{
    public class Output
    {
        public enum Style
        {
            Success,
            Information,
            Warning,
            Danger,
            Default
        }

        public static void WriteLine(string value, Style style = Style.Default)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            switch (style)
            {
                case Style.Success:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("*");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("] ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case Style.Information:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("i");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("] ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case Style.Warning:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("!");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("] ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Style.Danger:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("!");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("] ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case Style.Default:
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("*");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("] ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            Console.WriteLine(value);
            Console.ResetColor();
        }

        public static void Write(string value, Style style = Style.Default, bool continueLine = false)
        {
            if (!continueLine)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[");
                switch (style)
                {
                    case Style.Success:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("*");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("] ");
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case Style.Information:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("i");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("] ");
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    case Style.Warning:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("!");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("] ");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case Style.Danger:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("!");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("] ");
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case Style.Default:
                    default:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("*");
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("] ");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }
            }

            switch (style)
            {
                case Style.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case Style.Information:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case Style.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Style.Danger:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case Style.Default:
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            Console.Write(value);
            Console.ResetColor();
        }
    }
}
