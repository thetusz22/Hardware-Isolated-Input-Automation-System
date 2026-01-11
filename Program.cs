using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using swed64;

namespace HardwareIsolatedInput;

class AutomationHost
{
    private static volatile bool isAutomationEnabled = false;
    private static readonly Random randomGenerator = new Random();

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    static void Main()
    {
        Console.Title = "Hardware-Isolated Input Automation System";
        Console.WriteLine("Initializing Hardware Bridge...");
        
        // Emulate system initialization time
        Thread.Sleep(2000); 

        string targetProcess = "cs2";
        string targetModule = "client.dll";

        swed64.swed memoryInterface = new swed64.swed();
        memoryInterface.GetProcess(targetProcess);

        IntPtr moduleBase = memoryInterface.GetModuleBase(targetModule);

        // Memory Configuration Map
        int primaryContextOffset = 0x18560D0;
        int objectDirectoryOffset = 0x1A020A8;
        int objectIdIndexOffset = 0x1458;
        int groupIdentifierOffset = 0x3E3;
        int integrityValueOffset = 0x344;
        int stateFlagOffset = 0xEF;
        int objectHandleOffset = 0x824;
        int labelOffset = 0x660;

        int ACTIVATION_KEY = 0x05; // Input Trigger Key

        SerialPort dataPort = new SerialPort("COM6", 115200)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            RtsEnable = true,
            DtrEnable = true
        };

        IntPtr directoryAddress = IntPtr.Zero;
        IntPtr directoryEntry = IntPtr.Zero;
        DateTime lastDirectoryUpdate = DateTime.MinValue;

        StateMonitor monitor = new StateMonitor();

        Thread inputThread = new Thread(() => MonitorInput(ACTIVATION_KEY));
        inputThread.IsBackground = true;
        inputThread.Start();

        Thread vizThread = new Thread(() =>
        {
            while (true)
            {
                if ((DateTime.Now - lastDirectoryUpdate).TotalSeconds > 3)
                {
                    directoryAddress = memoryInterface.ReadPointer(moduleBase, objectDirectoryOffset);
                    directoryEntry = memoryInterface.ReadPointer(directoryAddress, 0x10);
                    lastDirectoryUpdate = DateTime.Now;
                }

                monitor.RenderStateTable(directoryEntry, directoryAddress, memoryInterface,
                        objectHandleOffset,
                        integrityValueOffset,
                        labelOffset);

                Thread.Sleep(200);
            }
        });
        vizThread.IsBackground = true;
        vizThread.Start();

        try
        {
            dataPort.Open();
            Console.WriteLine("✅ Hardware Interface Online.");

            while (true)
            {
                if (isAutomationEnabled)
                {
                    IntPtr localContext = memoryInterface.ReadPointer(moduleBase, primaryContextOffset);
                    directoryAddress = memoryInterface.ReadPointer(moduleBase, objectDirectoryOffset);

                    int localGroup = memoryInterface.ReadInt(localContext, groupIdentifierOffset);
                    int targetIndex = memoryInterface.ReadInt(localContext, objectIdIndexOffset);

                    IntPtr targetEntry = memoryInterface.ReadPointer(directoryAddress, 0x8 * ((targetIndex & 0x7FFF) >> 9) + 0x10);
                    IntPtr targetObject = memoryInterface.ReadPointer(targetEntry, 0x78 * (targetIndex & 0x1FF));

                    int targetIntegrity = memoryInterface.ReadInt(targetObject, integrityValueOffset);
                    int targetGroup = memoryInterface.ReadInt(targetObject, groupIdentifierOffset);
                    
                    // Check for inactive state
                    bool isInactive = (ReadByte(memoryInterface, targetObject, stateFlagOffset) != 0);

                    // Logic: If integrity checks pass and object is active
                    if (targetIntegrity > 0 && !isInactive)
                    {
                        // Send actuation signal to external hardware
                        dataPort.WriteLine("F");
                        
                        // Hardware latency simulation
                        Thread.Sleep(randomGenerator.Next(45, 98));
                    }
                }
                Thread.Sleep(1); // Cycle hygiene
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"System Error: {ex.Message}");
        }
        finally
        {
            if (dataPort.IsOpen)
            {
                dataPort.Close();
                Console.WriteLine("Hardware Interface Disconnected.");
            }
        }
    }

    static byte ReadByte(swed64.swed memoryInterface, IntPtr addr, int offset)
    {
        byte[] data = memoryInterface.ReadBytes(addr, offset, 1);
        return data is { Length: > 0 } ? data[0] : (byte)0;
    }

    static void MonitorInput(int key)
    {
        while (true)
        {
            isAutomationEnabled = (GetAsyncKeyState(key) & 0x8000) != 0;
            Thread.Sleep(1);
        }
    }
}
