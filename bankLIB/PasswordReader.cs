using System;

namespace bankLIB
{
    public static class PasswordReader
    {
        public static string ReadPassword()
        {
            string password = string.Empty;

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                // Enter finishes password input
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                // Backspace removes the previous character
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password[..^1];

                        // Remove the last asterisk from the terminal
                        Console.Write("\b \b");
                    }

                    continue;
                }

                // Only accept characters that produce actual text
                if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            }

            return password;
        }
    }
}
