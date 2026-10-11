using System;
using System.Collections.Generic;
using System.Text;

namespace CCUserSpotPoller {
    public class CC11SpotMessage {
        // Field 1: Message Identifier (Always "CC11")
        public string MessageType { get; set; } = string.Empty;

        // Field 2: Frequency in kHz (e.g., 14025.0)
        public double Frequency { get; set; }

        // Field 3: DX Callsign
        public string DxCallsign { get; set; } = string.Empty;

        // Field 4: Date string (Format: DD-MMM-YYYY)
        public string Date { get; set; } = string.Empty;

        // Field 5: Time string (Format: HHMMZ or HH:MM)
        public string Time { get; set; } = string.Empty;

        // Field 6: Spotter Callsign
        public string SpotterCallsign { get; set; } = string.Empty;

        // Field 7: User/Skimmer Comments
        public string Comments { get; set; } = string.Empty;

        // Field 8: Network Hops Remaining
        public int Hops { get; set; }

        // Field 9: Node that received the spot
        public string RxViaNode { get; set; } = string.Empty;

        // Field 10: Node where the spot originated
        public string OriginNode { get; set; } = string.Empty;

        // Field 11: Spotter ITU Zone
        public int SpotterItuZone { get; set; }

        // Field 12: Spotter CQ Zone
        public int SpotterCqZone { get; set; }

        // Field 13: DX ITU Zone
        public int DxItuZone { get; set; }

        // Field 14: DX CQ Zone
        public int DxCqZone { get; set; }

        // Field 15: Spotter State/Province (US/VE only)
        public string SpotterState { get; set; } = string.Empty;

        // Field 16: DX State/Province (US/VE only)
        public string DxState { get; set; } = string.Empty;

        // Field 17: Spotter DXCC Entity ID
        public int SpotterDxccId { get; set; }

        // Field 18: DX DXCC Entity ID
        public int DxDxccId { get; set; }

        // Field 19: Spotter 4 or 6-character Grid Square
        public string SpotterGrid { get; set; } = string.Empty;

        // Field 20: DX 4 or 6-character Grid Square
        public string DxGrid { get; set; } = string.Empty;

        // Field 21: Reserved / Blank Field
        public string Reserved { get; set; } = string.Empty;

        // Field 22: Spot Type (e.g., "M" for Manual, "S" for Skimmer)
        public string SpotType { get; set; } = string.Empty;

        // Field 23: Spotter IP Address
        public string SpotterIp { get; set; } = string.Empty;

        // Field 24: Unix Epoch Timestamp (Seconds elapsed since Jan 1, 1970)
        public long Timestamp { get; set; }
    }

}
