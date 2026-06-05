# 📡 WiFi Radar

A real-time WiFi signal visualization tool built with WPF (.NET).  
It captures RSSI values from nearby WiFi interfaces and visualizes signal changes as a live motion graph with a 60-second rolling window.

---


The system reads WiFi signal strength (RSSI) from the system network interface and computes:

- Signal variation (ΔRSSI)
- Motion intensity estimation
- Time-series visualization

No external hardware is required. (unless your pc only have 1 wifi card)

---

## 🛠 Requirements

- Windows 10/11
- .NET 6 or later (WPF)
- WiFi adapter compatible with `netsh wlan show interfaces`

---

## Usage

1. Clone the repository
```bash
git clone https://github.com/yourname/wifi-radar.git
