# 🕹️ Hardware-Isolated Input Automation System

![MicroPython](https://img.shields.io/badge/MicroPython-HW%20Logic-2B3A42?style=for-the-badge&logo=python&logoColor=white)
![Raspberry Pi Pico](https://img.shields.io/badge/RP2350-Pico%202%20W-C51A4A?style=for-the-badge&logo=Raspberry-Pi&logoColor=white)
![Networking](https://img.shields.io/badge/Networking-Wi--Fi%20Sockets-007ACC?style=for-the-badge&logo=rss&logoColor=white)
![C#](https://img.shields.io/badge/C%23-Host%20Controller-239120?style=for-the-badge&logo=csharp&logoColor=white)

## 📖 Overview

This project is a **Proof of Concept (PoC)** demonstrating how to decouple software automation logic from the execution environment using **IoT Edge Computing**.

The system consists of two distinct components:
1.  **Host Machine (Windows):** Runs a C# analysis tool that monitors process memory states and sends commands over Wi-Fi.
2.  **Edge Device (Raspberry Pi Pico 2 W):** Acts as a wireless, driverless **USB HID (Human Interface Device)**. It receives commands via TCP Sockets and emulates physical mouse movements.

By offloading input generation to an external microcontroller, the automation becomes indistinguishable from physical human input at the hardware level.

---

## 🏗️ Architecture

The system uses a **Wireless Split-Processing Architecture** to ensure isolation.

```mermaid
flowchart LR
    subgraph Host PC [Windows Host]
        A[Target Application] -->|Memory Read| B(C# Controller)
        B -->|Calculate Vectors| C{TCP Client}
    end

    C -.->|Wi-Fi Packet Wireless| D{TCP Server}

    subgraph Edge Device [Raspberry Pi Pico 2 W]
        D -->|Parse Command| E[MicroPython Logic]
        E -->|Inject Input| F[USB HID Interface]
    end

    F ==>|USB Cable| A
    style Edge Device fill:#f9f,stroke:#333,stroke-width:2px
