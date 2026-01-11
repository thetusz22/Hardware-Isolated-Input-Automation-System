# 🕹️ Hardware-Isolated Input Automation System

![Python](https://img.shields.io/badge/Python-3.9-3776AB?style=for-the-badge&logo=python&logoColor=white)
![Raspberry Pi](https://img.shields.io/badge/Raspberry%20Pi-Edge%20Device-C51A4A?style=for-the-badge&logo=Raspberry-Pi&logoColor=white)
![Linux](https://img.shields.io/badge/Linux-USB%20Gadget%20API-FCC624?style=for-the-badge&logo=linux&logoColor=black)
![Networking](https://img.shields.io/badge/Networking-TCP%2FIP%20Sockets-007ACC?style=for-the-badge&logo=rss&logoColor=white)

## 📖 Overview

This project is a **Proof of Concept (PoC)** demonstrating how to decouple software automation logic from the execution environment using **Edge Computing**.

The system consists of two distinct components:
1.  **Host Machine (Windows):** Runs a lightweight analysis script that monitors process memory states in real-time.
2.  **Edge Device (Raspberry Pi 4/Zero):** Configured as a driverless **USB HID (Human Interface Device)** using the Linux USB Gadget API.

By offloading input generation to an external hardware device, the automation becomes indistinguishable from physical human input at the hardware level.

---

## 🏗️ Architecture

The system uses a **Split-Processing Architecture** to ensure isolation and low latency.

```mermaid
graph LR
    subgraph Host PC [Windows Host]
        A[Target Application] -->|Memory Read| B(Analysis Script)
        B -->|Calculate Coordinates| C{Socket Client}
    end

    C -->|TCP/IP Packet < 2ms| D{Socket Server}

    subgraph Edge Device [Raspberry Pi / Linux]
        D -->|Parse Command| E[Input Logic]
        E -->|Write Binary| F[/dev/hidg0/]
    end

    F ==>|USB Cable| A
    style Edge Device fill:#f9f,stroke:#333,stroke-width:2px
