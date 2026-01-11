# boot.py
import usb_hid

# Csak az Egeret (Mouse) engedélyezzük, így "stealth" módban lesz.
# A számítógép szabványos HID eszközként ismeri fel.
usb_hid.enable((usb_hid.Device.MOUSE,))
