namespace GDNetDebug
{
    public static class NetworkFormatter
    {
        // ФОРМАТИРОВАНИЕ БАЙТ:
        public static string FormatBytes(long bytes)
        {
            if (bytes < 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        }

        // ФОРМАТИРОВАНИЕ БИТ/СЕК:
        public static string FormatBitrate(long bitsPerSecond)
        {
            if (bitsPerSecond < 1000) return $"{bitsPerSecond} bps";
            if (bitsPerSecond < 1000_000) return $"{bitsPerSecond / 1000.0:F1} Kbps";
            if (bitsPerSecond < 1000_000_000) return $"{bitsPerSecond / 1000_000.0:F1} Mbps";
            return $"{bitsPerSecond / 1000_000_000.0:F2} Gbps";
        }

        // ФОРМАТИРОВАНИЕ БАЙТ/СЕК:
        public static string FormatBytesPerSecond(long bytesPerSecond)
        {
            long bits = bytesPerSecond * 8;
            return $"{FormatBytes(bytesPerSecond)}/s ({FormatBitrate(bits)})";
        }

        // ФОРМАТИРОВАНИЕ КОЛИЧЕСТВА ПАКЕТОВ:
        public static string FormatPackets(long packets)
        {
            if (packets < 1000) return packets.ToString();
            if (packets < 1000_000) return $"{packets / 1000.0:F1}K";
            return $"{packets / 1000_000.0:F1}M";
        }

        // ФОРМАТИРОВАНИЕ ПИНГА:
        public static string FormatPing(int pingMs)
        {
            if (pingMs < 10) return $"{pingMs}ms 🟢";
            if (pingMs < 50) return $"{pingMs}ms 🟡";
            if (pingMs < 100) return $"{pingMs}ms 🟠";
            return $"{pingMs}ms 🔴";
        }
    }
}
