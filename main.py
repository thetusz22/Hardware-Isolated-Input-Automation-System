# main.py
import network
import socket
import time
import struct
import usb_hid
from machine import Pin

# -------------------------------------------------------------------------
# ⚙️ KONFIGURÁCIÓ
# -------------------------------------------------------------------------
SSID = "WIFI_NEVE_IDE"      # Írd át!
PASSWORD = "WIFI_JELSZO_IDE" # Írd át!
PORT = 65432

# -------------------------------------------------------------------------
# 🖱️ HID EGÉR FUNKCIÓK
# -------------------------------------------------------------------------
def move_mouse(x, y, click=False):
    """
    Fogadja az X, Y koordinátákat és a klikk parancsot,
    majd átküldi USB-n a PC-nek.
    """
    # Értékek korlátozása -127 és 127 közé (Signed Byte)
    x = max(-127, min(127, int(x)))
    y = max(-127, min(127, int(y)))
    buttons = 1 if click else 0
    
    # Megkeressük az aktív egér eszközt
    mouse = None
    for device in usb_hid.devices:
        if device.usage == 0x02: # Mouse usage ID
            mouse = device
            break
            
    if mouse:
        # Riport küldése: [Gombok, X, Y, Görgő]
        report = struct.pack("Bbbb", buttons, x, y, 0)
        mouse.send_report(report)
        
        # Ha klikkeltünk, azonnal el is engedjük a gombot
        if click:
            mouse.send_report(struct.pack("Bbbb", 0, 0, 0, 0))

# -------------------------------------------------------------------------
# 📶 WI-FI KEZELÉS
# -------------------------------------------------------------------------
def connect_wifi():
    wlan = network.WLAN(network.STA_IF)
    wlan.active(True)
    # Energiatakarékosság kikapcsolása a gyors válaszidőért (Low Latency)
    wlan.config(pm=0xa11140) 
    wlan.connect(SSID, PASSWORD)
    
    # Várakozás a kapcsolatra (max 10mp)
    max_wait = 10
    while max_wait > 0:
        if wlan.status() < 0 or wlan.status() >= 3:
            break
        max_wait -= 1
        time.sleep(1)

    if wlan.status() == 3:
        ip = wlan.ifconfig()[0]
        print(f"Connected! IP: {ip}")
        return ip
    return None

# -------------------------------------------------------------------------
# 🚀 FŐ PROGRAM (SZERVER)
# -------------------------------------------------------------------------
def main():
    # LED visszajelzés setup
    try:
        led = Pin("LED", Pin.OUT)
    except:
        led = Pin(25, Pin.OUT) # Régebbi Pico esetén Pin 25

    ip = connect_wifi()
    if not ip:
        print("Wi-Fi connection failed!")
        # Hiba jelzése gyors villogással
        while True:
            led.toggle()
            time.sleep(0.1)

    # Socket szerver indítása
    addr = socket.getaddrinfo('0.0.0.0', PORT)[0][-1]
    s = socket.socket()
    s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    s.bind(addr)
    s.listen(1)
    
    print(f"Listening on {ip}:{PORT}")
    led.on() # Folyamatos fény jelzi, hogy kész a fogadásra

    while True:
        try:
            cl, addr = s.accept()
            print('Client connected:', addr)
            cl_file = cl.makefile('rwb', 0)
            
            while True:
                line = cl_file.readline()
                if not line: break
                
                try:
                    # Parancs értelmezése
                    # Elvárt formátum: "MOVE X Y" vagy "CLICK"
                    cmd = line.decode().strip()
                    
                    if cmd.startswith("MOVE"):
                        parts = cmd.split()
                        if len(parts) >= 3:
                            move_mouse(int(parts[1]), int(parts[2]))
                            
                    elif cmd == "CLICK":
                        move_mouse(0, 0, click=True)
                        
                except Exception as e:
                    print(f"Error processing command: {e}")
                    
            cl.close()
            print('Client disconnected')
        except OSError:
            pass

if __name__ == "__main__":
    main()
