using swed64;
using System;
using System.Collections.Generic;
using System.Threading;

namespace HardwareIsolatedInput
{
    public class StateMonitor
    {
        private readonly Random rng = new Random();
        private readonly Dictionary<string, int> rowMap = new(); // identifier -> console row
        private int baseRow = -1;

        public void RenderStateTable(IntPtr listEntry, IntPtr entityList, swed64.swed memoryInterface, int handleOffset, int integrityOffset, int labelOffset, int groupOffset = 0x3E3)
        {
            if (listEntry == IntPtr.Zero || entityList == IntPtr.Zero)
            {
                // Waiting for valid memory pointers
                return;
            }

            if (baseRow == -1)
            {
                Console.WriteLine("System Objects Monitor:");
                Console.WriteLine(new string('-', 60));
                baseRow = Console.CursorTop;
            }

            int nextRow = baseRow;

            for (int i = 0; i < 64; i++)
            {
                IntPtr controller = memoryInterface.ReadPointer(listEntry, i * 0x78);
                if (controller == IntPtr.Zero) continue;

                int handle = memoryInterface.ReadInt(controller, handleOffset);
                if (handle == 0) continue;

                IntPtr listEntry2 = memoryInterface.ReadPointer(entityList, 0x8 * ((handle & 0x7FFF) >> 9) + 0x10);
                IntPtr obj = memoryInterface.ReadPointer(listEntry2, 0x78 * (handle & 0x1FF));
                if (obj == IntPtr.Zero) continue;

                uint integrity = memoryInterface.ReadUInt(obj, integrityOffset);
                string label = memoryInterface.ReadString(controller, labelOffset, 16).Trim();
                if (string.IsNullOrWhiteSpace(label)) continue;

                int group = memoryInterface.ReadInt(controller, groupOffset);

                string bar = GenerateIntegrityBar((int)integrity);

                if (!rowMap.TryGetValue(label, out int row))
                {
                    row = nextRow++;
                    rowMap[label] = row;
                }

                // Render Object Label with Group coloring
                Console.SetCursorPosition(0, row);
                Console.ForegroundColor = GetGroupColor(group);
                Console.Write($"{label,-16}");

                // Reset for separator and Status
                Console.ResetColor();
                Console.Write(" | ");

                // Render Integrity Status
                Console.ForegroundColor = GetStatusColor((int)integrity);
                Console.Write($"{integrity,3} %");

                Console.ResetColor();
                Console.Write(" | ");

                // Render Visual Bar
                Console.ForegroundColor = GetStatusColor((int)integrity);
                Console.Write($"{bar}");

                Console.ResetColor();
                Console.Write("   "); // clear lingering characters
            }
        }

        static ConsoleColor GetGroupColor(int group)
        {
            return group switch
            {
                2 => ConsoleColor.Red,      // Group A
                3 => ConsoleColor.Cyan,     // Group B
                _ => ConsoleColor.Gray      // Unclassified
            };
        }

        static ConsoleColor GetStatusColor(int value)
        {
            if (value > 75) return ConsoleColor.Green;
            if (value > 35) return ConsoleColor.Yellow;
            if (value > 0) return ConsoleColor.Red;
            return ConsoleColor.DarkGray;
        }

        static string GenerateIntegrityBar(int value, int max = 100, int barLen = 20)
        {
            int filled = (int)((value / (float)max) * barLen);
            return new string('█', filled) + new string('░', barLen - filled);
        }
    }
}
