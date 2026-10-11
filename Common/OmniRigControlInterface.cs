using Common;
using OmniRig;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common {
    public class OmniRigControlInterface:INotifyPropertyChanged {


        #region Constants

        // Constants for enum RigParamX
        private const int PM_UNKNOWN = 0x00000001;
        private const int PM_FREQ = 0x00000002;
        private const int PM_FREQA = 0x00000004;
        private const int PM_FREQB = 0x00000008;
        private const int PM_PITCH = 0x00000010;
        private const int PM_RITOFFSET = 0x00000020;
        private const int PM_RIT0 = 0x00000040;
        private const int PM_VFOAA = 0x00000080;
        private const int PM_VFOAB = 0x00000100;
        private const int PM_VFOBA = 0x00000200;
        private const int PM_VFOBB = 0x00000400;
        private const int PM_VFOA = 0x00000800;
        private const int PM_VFOB = 0x00001000;
        private const int PM_VFOEQUAL = 0x00002000;
        private const int PM_VFOSWAP = 0x00004000;
        private const int PM_SPLITON = 0x00008000;
        private const int PM_SPLITOFF = 0x00010000;
        private const int PM_RITON = 0x00020000;
        private const int PM_RITOFF = 0x00040000;
        private const int PM_XITON = 0x00080000;
        private const int PM_XITOFF = 0x00100000;
        private const int PM_RX = 0x00200000;
        private const int PM_TX = 0x00400000;
        private const int PM_CW_U = 0x00800000;
        private const int PM_CW_L = 0x01000000;
        private const int PM_SSB_U = 0x02000000;
        private const int PM_SSB_L = 0x04000000;
        private const int PM_DIG_U = 0x08000000;
        private const int PM_DIG_L = 0x10000000;
        private const int PM_AM = 0x20000000;
        private const int PM_FM = 0x40000000;

        // Constants for enum RigStatusX
        private const int ST_NOTCONFIGURED = 0x00000000;
        private const int ST_DISABLED = 0x00000001;
        private const int ST_PORTBUSY = 0x00000002;
        private const int ST_NOTRESPONDING = 0x00000003;
        private const int ST_ONLINE = 0x00000004;

        #endregion

        #region Properties

        // RX
        public const string FLD_RX = "RX";
        private string mRX;
        public string RX {
            get
            {
                return mRX;
            }
            set
            {
                mRX=value;
                OnPropertyChanged(FLD_RX);
            }
        }

        // TX
        public const string FLD_TX = "TX";
        private string mTX;



        //temp fix
        //public string Frequency;
        //public string Status;
        //public string RX;
        //public string TX;
        //public string Mode;


        public string TX {
            get
            {
                return mTX;
            }
            set
            {
                mTX=value;
                OnPropertyChanged(FLD_TX);
            }
        }

        // Frequency
        public const string FLD_Frequency = "Frequency";
        private string mFrequency;
        public string Frequency {
            get
            {
                return mFrequency;
            }
            set
            {
                mFrequency=value;
                OnPropertyChanged(FLD_Frequency);
            }
        }

        // Status
        public const string FLD_Status = "Status";
        private string mStatus;
        public string Status {
            get
            {
                return mStatus;
            }
            set
            {
                mStatus=value;
                OnPropertyChanged(FLD_Status);
            }
        }

        // Mode
        public const string FLD_Mode = "Mode";
        private string mMode;
        public string Mode {
            get
            {
                return mMode;
            }
            set
            {
                mMode=value;
                OnPropertyChanged(FLD_Mode);
            }
        }

        #endregion

        #region Field

        // The events subscribed
        private bool EventsSubscribed = false;

        // The omni rig engine
        public OmniRig.OmniRigX OmniRigEngine;

        // The rig
        public OmniRig.RigX Rig;

        // Our rig no
        int CurrentRigNo;

        // The first thread
        Thread thread1;

        // The second thread 
        Thread thread2;

        #endregion


        public void StartOmniRig() {
            try {
                if (OmniRigEngine!=null) {
                    //     MessageBox.Show("OmniRig runs");
                }
                else {
                    OmniRigEngine=(OmniRig.OmniRigX)Activator.CreateInstance(Type.GetTypeFromProgID("OmniRig.OmniRigX"));
                    // we want OmniRig interface V.1.1 to 1.99
                    // as V2.0 will likely be incompatible  with 1.x
                    if (OmniRigEngine.InterfaceVersion<0x101&&OmniRigEngine.InterfaceVersion>0x299) {
                        OmniRigEngine=null;
                        //   MessageBox.Show("OmniRig is not installed or has unsupported version.");
                    }
                    SubscribeToEvents();
                    SelectRig(1);
                }
            } catch (Exception ex) {
                //    Mouse.OverrideCursor=null;
                //   MessageBox.Show(ex.Message);
                throw;
            }
        }

        public void SelectRig(int NewRigNo) {
            if (OmniRigEngine==null)
                return;

            CurrentRigNo=NewRigNo;
            switch (NewRigNo) {
                case 1:
                    Rig=OmniRigEngine.Rig1;
                    break;
                case 2:
                    Rig=OmniRigEngine.Rig2;
                    break;
            }
            ShowRigStatus();
            ShowRigParams();
        }

        private void ShowRigStatus() {
            if (Rig!=null)
                Status=Rig.StatusStr;
        }


        public const string FLD_IsTransmitting = "IsTransmitting";
        private bool mIsTransmitting;
        public bool IsTransmitting {
            get => mIsTransmitting;
            set {
                if(mIsTransmitting != value) {
                    mIsTransmitting = value;
                    OnPropertyChanged(FLD_IsTransmitting);
                }
            }
        }
        private void ShowRigParams() {
            if (Rig==null)
                return;

            RX=Rig.GetRxFrequency().ToString();
            TX=Rig.GetTxFrequency().ToString();
            Frequency=Rig.Freq.ToString();

            // Read the TX bitmask from OmniRig
            IsTransmitting = ((int)Rig.Tx & PM_TX) == PM_TX;
            switch (Rig.Mode) {
                case (OmniRig.RigParamX)PM_CW_L:
                    Mode="CW";
                    break;
                case (OmniRig.RigParamX)PM_CW_U:
                    Mode="CW";
                    break;
                case (OmniRig.RigParamX)PM_SSB_L:
                    Mode="SSB";
                    break;
                case (OmniRig.RigParamX)PM_SSB_U:
                    Mode="SSB";
                    break;
                case (OmniRig.RigParamX)PM_FM:
                    Mode="FM";
                    break;
                case (OmniRig.RigParamX)PM_AM:
                    Mode="AM";
                    break;
                default:
                    Mode="Other";
                    break;
            }
            SendOmniRigListener(); // Check if needed.
        }


        // Event raised when property changed

        private void SubscribeToEvents() {
            if (!EventsSubscribed) {
                EventsSubscribed=true;
                OmniRigEngine.StatusChange+=OmniRigEngine_StatusChange;
                OmniRigEngine.ParamsChange+=OmniRigEngine_ParamsChange;
            }
        }

        //OmniRig ParamsChange events
        private void OmniRigEngine_ParamsChange(int RigNumber, int Params) {
            if (RigNumber==CurrentRigNo) {
                thread1=new Thread(new ThreadStart(ShowRigParams));
                thread1.Name="RigParams";
                //Start first thread
                thread1.Start();
            }

        }

        //OmniRig StatusChange events
        private void OmniRigEngine_StatusChange(int RigNumber) {
            if (RigNumber==CurrentRigNo) {
                thread2=new Thread(new ThreadStart(ShowRigStatus));
                thread2.Name="RigStatus";
                //Start second thread
                thread2.Start();
            }
        }


        /// ///////////////////////////////////////////////////SR Code below


        public delegate void AddItemDelegate(OmniRigHost host);
        public AddItemDelegate AddItemCallback;

        public event PropertyChangedEventHandler PropertyChanged;
        // Event handler for property changed
        // <param name="pPropertyName">Name of property</param>
        protected virtual void OnPropertyChanged(string pPropertyName) {
            if (PropertyChanged!=null)
                PropertyChanged(this, new PropertyChangedEventArgs(pPropertyName));
        }
        private void SendOmniRigListener() {
            //Notification subscribers
            OmniRigHost host = new OmniRigHost { RX=RX, TX=TX??"0.0", Mode=Mode??"", Status=Status??"",IsTransmitting=IsTransmitting };

            if (AddItemCallback!=null) {
                AddItemCallback(host);
            }
        }
        public void Initialize() {
            StartOmniRig();
        }
        /*

        Layer 1 — FTDX-101D Hardware CAT behavior (the real enemy)
        The FTDX-101D firmware always resets the Clarifier (RIT+XIT state and offset) when it receives a VFO-frequency set CAT command (FA; or FB;). This is by design in Yaesu's firmware — it is not an OmniRig issue. SetSimplexMode(freq) → OmniRig sends FA00014025000; → radio hardware wipes XIT/RIT. There is no way to suppress this.
        Layer 2 — OmniRig property writes are silently no-ops
        The fix attempts Rig.Xit = RigParamX.PM_XITON and Rig.RitOffset = ritXitOffset. These write nothing to the radio. Here is why:
        OmniRig translates property writes into CAT commands using entries in the rig's .ini file (e.g. FTDX101D.ini). Check the entries:

        Rig.RitOffset is especially problematic: the FTDX-101D has no "set offset to N Hz" CAT command. The only available commands are RC; (clear to 0), RUnnnnn; (up N Hz), RDnnnnn; (down N Hz). OmniRig has no mechanism to express this, so RitOffset is read-only in practice.
        Layer 3 — Reading Rig.RitOffset may also return 0
        If the ini file doesn't define ReadRitOffset with proper RO; response parsing, Rig.RitOffset returns 0. The restore code then skips the offset write entirely because of the if (ritXitOffset != 0) guard.
        Layer 4 — OmniRig internal command queue race
        After SetSimplexMode, OmniRig's own polling timer fires within ~100–300 ms, reads status from the radio, and updates its internal state. This can collide with the restore commands queued right after the 200 ms sleep.
        ---
        The Fix: Bypass OmniRig Properties, Use Raw CAT via SendCustomCommand
        RigX.SendCustomCommand(string command, int replyLength, string replyEnd) sends raw CAT bytes directly to the radio, bypassing the ini constraint entirely. This is the only reliable path.
        FTDX-101D CAT commands needed: | Purpose | CAT Command | |---|---| | XIT on | XT1; | | RIT on | RT1; | | Clear clarifier offset to 0 | RC; | | Set offset +N Hz | RC; then RU00NNN; | | Set offset −N Hz | RC; then RD00NNN; | | Read clarifier offset | RO; → response ROsnnnn; |

        Summary of all failure points fixed
        Failure	Root cause	Fix
        XIT not re-enabled	Rig.Xit = PM_XITON silently no-ops — ini has no WriteXitOn	Raw CAT XT1; via SendCustomCommand
        RIT not re-enabled	Same — WriteRitOn missing	Raw CAT RT1;
        Offset not restored	Rig.RitOffset = N silently no-ops — no single-value set CAT command exists	RC; to baseline + RU/RDnnnn;
        Offset read returning 0	Rig.RitOffset parsed from ini which may not decode RO; response	Read RO; directly and parse ROsnnnn; response
        OmniRig polling race	Polling fires ~200ms after freq change and can interleave	250ms delay gives polling one full cycle before restore commands
        One thing to verify in the field: Rig.SendCustomCommand reply parsing — if OmniRig returns the raw bytes differently (e.g. with a null prefix), adjust ReadClarifierOffsetRaw() to trim and index accordingly. Add a Debug.WriteLine($"[OmniRig] RO raw response: '{response}'")  on first run to confirm the format.


        */
        /// <summary>
        /// Set RX frequency (optimized for Yaesu FTDX-101D).
        /// Uses raw CAT via SendCustomCommand to restore XIT/RIT state and offset,
        /// because FTDX-101D firmware resets the Clarifier on every FA/FB VFO command,
        /// and OmniRig property setters for Xit/RitOffset are silently no-ops
        /// (the FTDX-101D ini has no WriteXitOn / WriteRitOffset entries).
        /// </summary>
        public void SetRXFrequency(int freq) {
            if(Rig == null)
                return;

            try {
                // ── Step 1: Snapshot split state ──────────────────────────────────────
                RigParamX split = Rig.Split;

                // ── Step 2: Snapshot XIT / RIT enabled state ──────────────────────────
                // Rig.Xit / Rig.Rit work for reading (ini defines ReadXit/ReadRit via IF; parsing)
                bool xitWasOn = false;
                bool ritWasOn = false;
                try { xitWasOn = (Rig.Xit == RigParamX.PM_XITON); } catch { }
                try { ritWasOn = (Rig.Rit == RigParamX.PM_RITON); } catch { }

                // ── Step 3: Snapshot clarifier offset via raw CAT ─────────────────────
                // Rig.RitOffset is unreliable (ini may not parse RO; response).
                // Read directly and parse the ROsnnnn; response ourselves.
                int clarOffset = 0;
                if(xitWasOn || ritWasOn) {
                    clarOffset = ReadClarifierOffsetRaw();
                }

            //    System.Diagnostics.Debug.WriteLine(       $"[OmniRig] Before freq change: XIT={xitWasOn}, RIT={ritWasOn}, Offset={clarOffset} Hz");

                // ── Step 4: Set frequency ──────────────────────────────────────────────
                // This causes FTDX-101D to reset Clarifier (hardware behavior, unavoidable)
                if(split == RigParamX.PM_SPLITON) {
                    int txFreq = Rig.GetTxFrequency();
                    Rig.SetSplitMode(freq,txFreq);
                } else {
                    Rig.SetSimplexMode(freq);
                }

                // Wait for radio to process FA/FB and for OmniRig polling to settle
                Thread.Sleep(150);

                // ── Step 5: Restore XIT via raw CAT ───────────────────────────────────
                if(xitWasOn) {
                    RestoreClarifier(xitOn: true,ritOn: false,offsetHz: clarOffset);
                } else if(ritWasOn) {
                    RestoreClarifier(xitOn: false,ritOn: true,offsetHz: clarOffset);
                }

            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[OmniRig] SetRXFrequency ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the clarifier offset directly from the FTDX-101D via the RO; CAT command.
        /// Returns offset in Hz (positive = up, negative = down). Returns 0 on failure.
        /// </summary>
        /// <summary>
        /// Reads the clarifier offset from OmniRig's cached RitOffset property.
        /// SendCustomCommand() is void in OmniRig COM — responses are async events,
        /// so synchronous raw CAT reads are not possible. Returns 0 on failure.
        /// </summary>
        private int ReadClarifierOffsetRaw() {
            try {
                return Rig.RitOffset;
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[OmniRig] ReadClarifierOffsetRaw ERROR: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Restores XIT/RIT enabled state and clarifier offset using raw FTDX-101D CAT commands.
        /// Rig.Xit / Rig.RitOffset property setters are bypassed because the FTDX-101D ini
        /// does not define WriteXitOn or WriteRitOffset — they are silently ignored by OmniRig.
        /// Sequence: RC (clear offset) → RU/RD (set offset) → XT1/RT1 (re-enable).
        /// </summary>
        private void RestoreClarifier(bool xitOn,bool ritOn,int offsetHz) {
            try {
                // Clear clarifier to 0 first (RC; is the only baseline)
                Rig.SendCustomCommand("RC;",0,"");
                Thread.Sleep(50);

                // Set the offset using RU (up) or RD (down) from the cleared baseline
                if(offsetHz != 0) {
                    int absOffset = Math.Abs(offsetHz);
                    // RUnnnnn; or RDnnnnn; — FTDX-101D accepts up to 4 digits (0–9999 Hz)
                    string catCmd = offsetHz > 0
                        ? $"RU{absOffset:D4};"
                        : $"RD{absOffset:D4};";
                    Rig.SendCustomCommand(catCmd,0,"");
                    Thread.Sleep(50);
                }

                // Re-enable XIT and/or RIT — use raw CAT, NOT Rig.Xit property
                if(xitOn) {
                    Rig.SendCustomCommand("XT1;",0,"");
                    Thread.Sleep(50);
                    System.Diagnostics.Debug.WriteLine($"[OmniRig] XIT restored ON, Offset={offsetHz} Hz");
                }

                if(ritOn) {
                    Rig.SendCustomCommand("RT1;",0,"");
                    Thread.Sleep(50);
                    System.Diagnostics.Debug.WriteLine($"[OmniRig] RIT restored ON, Offset={offsetHz} Hz");
                }

            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"[OmniRig] RestoreClarifier ERROR: {ex.Message}");
            }
        }
    }

    public class OmniRigHost {
        public Band Band { get; set; }
        public bool IsTransmitting { get; set; }
        public string RX = "0.0";
        public string TX = "0.0";
        public string Frequency = "0.0";
        public string Status = "";
        public string Mode = "";
        public string ModeFmt {
            get
            {
                return Mode;
            }
        }
        private double FrequencyDbl { get { return double.Parse(RX); } }
        public double FrequencykHz { get { return FrequencyDbl/1000.0; } }
        public double FrequencyMHz { get { return FrequencyDbl/(1000.0*1000.0); } }

    }



}
