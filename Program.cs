using System;
using System.Net.Sockets; // Ez kell a Wi-Fi kommunikációhoz
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using swed64;

namespace HardwareIsolatedInput;

class AutomationHost
{
    private static volatile bool isAutomationEnabled = false;
    private static readonly Random randomGenerator = new Random();

    // Billentyűfigyelés (pl. egér oldalsó gomb vagy ALT billentyű)
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    static void Main()
    {
        Console.Title = "Hardware-Isolated Input Automation System (Wi-Fi Edition)";
        Console.WriteLine("Initializing Hardware Bridge...");
        
        // Rendszer inicializálás szimuláció
        Thread.Sleep(2000); 

        // ---------------------------------------------------------
        // ⚙️ KONFIGURÁCIÓ
        // ---------------------------------------------------------
        string targetProcess = "cs2";
        string targetModule = "client.dll";
        
        // PICO BEÁLLÍTÁSOK (Ezt írd át a Pico IP címére!)
        string picoIp = "192.168.1.100"; // <--- ITT ÍRD ÁT!
        int picoPort = 65432;            // Ennek egyeznie kell a main.py-ban lévővel

        // Memóriaolvasó inicializálása
        swed64.swed memoryInterface = new swed64.swed();
        memoryInterface.GetProcess(targetProcess);
        IntPtr moduleBase = memoryInterface.GetModuleBase(targetModule);

        // Memória Offsetek (CS2 aktuális offsetjei)
        int primaryContextOffset = 0x18560D0;
        int objectDirectoryOffset = 0x1A020A8;
        int objectIdIndexOffset = 0x1458;
        int groupIdentifierOffset = 0x3E3;
        int integrityValueOffset = 0x344;
        int stateFlagOffset = 0xEF;
        int objectHandleOffset = 0x824;
        int labelOffset = 0x660;

        int ACTIVATION_KEY = 0x05; // 0x05 = Egér oldalsó gomb (XBUTTON1)

        // Vizualizációs változók
        IntPtr directoryAddress = IntPtr.Zero;
        IntPtr directoryEntry = IntPtr.Zero;
        DateTime lastDirectoryUpdate = DateTime.MinValue;
        StateMonitor monitor = new StateMonitor();

        // Gombfigyelő szál indítása
        Thread inputThread = new Thread(() => MonitorInput(ACTIVATION_KEY));
        inputThread.IsBackground = true;
        inputThread.Start();

        // Vizualizációs szál (hogy lássuk, mit lát a program)
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

        // ---------------------------------------------------------
        // 📶 HÁLÓZATI KAPCSOLÓDÁS (TCP CLIENT)
        // ---------------------------------------------------------
        TcpClient client = new TcpClient();
        try
        {
            Console.WriteLine($"Kapcsolódás a Pico-hoz ({picoIp}:{picoPort})...");
            
            // Kapcsolódási kísérlet a Pico Wi-Fi szerveréhez
            client.Connect(picoIp, picoPort); 
            NetworkStream stream = client.GetStream();
            
            Console.WriteLine("✅ SIKER! Wi-Fi Hardware Interface Online.");
            Console.WriteLine("A rendszer készen áll. Tartsd lenyomva az aktiváló gombot.");

            while (true)
            {
                if (isAutomationEnabled)
                {
                    // Memóriaolvasás logika
                    IntPtr localContext = memoryInterface.ReadPointer(moduleBase, primaryContextOffset);
                    directoryAddress = memoryInterface.ReadPointer(moduleBase, objectDirectoryOffset);

                    int localGroup = memoryInterface.ReadInt(localContext, groupIdentifierOffset);
                    int targetIndex = memoryInterface.ReadInt(localContext, objectIdIndexOffset);

                    IntPtr targetEntry = memoryInterface.ReadPointer(directoryAddress, 0x8 * ((targetIndex & 0x7FFF) >> 9) + 0x10);
                    IntPtr targetObject = memoryInterface.ReadPointer(targetEntry, 0x78 * (targetIndex & 0x1FF));

                    int targetIntegrity = memoryInterface.ReadInt(targetObject, integrityValueOffset);
                    
                    // Inaktív állapot ellenőrzése
                    bool isInactive = (ReadByte(memoryInterface, targetObject, stateFlagOffset) != 0);

                    // LOGIKA: Ha az ellenfél él és célozható
                    if (targetIntegrity > 0 && !isInactive)
                    {
                        // Parancs küldése Wi-Fi-n keresztül
                        // A main.py a "CLICK" parancsot várja, lezárva egy új sorral (\n)
                        string command = "CLICK\n";
                        byte[] data = Encoding.ASCII.GetBytes(command);
                        
                        // Küldés a socketre
                        stream.Write(data, 0, data.Length);
                        
                        // Hardveres késleltetés szimuláció (humanizálás)
                        Thread.Sleep(randomGenerator.Next(45, 98));
                    }
                }
                Thread.Sleep(1); // CPU kímélés
            }
        }
        catch (SocketException sockEx)
        {
            Console.WriteLine($"\n[HÁLÓZATI HIBA] Nem sikerült csatlakozni a Pico-hoz!");
            Console.WriteLine($"Ellenőrizd: 1. A Pico IP címe jó-e ({picoIp})? 2. A Pico csatlakozott-e a Wi-Fi-re?");
            Console.WriteLine($"Részletek: {sockEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Rendszerhiba: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Interface Disconnected.");
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
            // Gombállapot figyelése
            isAutomationEnabled = (GetAsyncKeyState(key) & 0x8000) != 0;
            Thread.Sleep(1);
        }
    }
}
