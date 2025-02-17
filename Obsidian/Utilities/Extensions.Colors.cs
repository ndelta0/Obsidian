﻿using Obsidian.API;
using System;

namespace Obsidian.Utilities
{
    public static partial class Extensions
    {
        public static void RenderColoredConsoleMessage(this string message)
        {
            var output = Console.Out;
            int start = 0;
            int end = message.Length - 1;

            for (int i = 0; i < end; i++)
            {
                if (message[i] != '&' && message[i] != '§')
                    continue;

                // Validate color code
                char colorCode = message[i + 1];
                if (!ChatColor.TryParse(colorCode, out var color))
                    continue;

                // Print text with previous color
                if (start != i)
                {
                    output.Write(message.AsSpan(start, i - start));
                }

                // Change color
                if (colorCode == 'r')
                {
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = color.ConsoleColor.Value;
                }

                // Skip color code
                i++;
                start = i + 1;
            }

            // Print remaining text if any
            if (start != message.Length)
                output.Write(message.AsSpan(start));

            Console.ResetColor();
        }
    }
}
