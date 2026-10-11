using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;

namespace Common {

    public static class CommonMethod {
        public static List<T> ConvertToList<T>(DataTable dt) {
            var columnNames = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();
            var properties = typeof(T).GetProperties();
            return dt.AsEnumerable().Select(row => {
                var objT = Activator.CreateInstance<T>();
                foreach (var pro in properties) {
                    if (columnNames.Contains(pro.Name.ToLower())) {
                        try {
                            pro.SetValue(objT, row[pro.Name]);
                        } catch (Exception ex) { }
                    }
                }
                return objT;
            }).ToList();
        }
    }
    public static class FreqModeConversion {


        //public int FreqHzInt {
        //    get {
        //        int HzInt = (int)(FreqMHz*1000.0*1000.0);
        //        return HzInt;
        //    }
        //}
    }
    public class RetCode {
        public int Code { get; set; }
        public int IntValue { get; set; }
        public string Msg { get; set; }
        public List<string> MsgLIst { get; set; }

        public RetCode() {
            Code=0;
            IntValue=0;
            Msg="";
            MsgLIst=new List<string>();
        }
    }

    public class Timing {
        public int Seq { get; set; }

        public DateTime Dt { get; set; }
        public string Comment { get; set; }
        public DateTime DtPrev { get; set; }
        public TimeSpan Elapsed { get; set; }
        public double ElapsedSec { get { return Elapsed.TotalSeconds; } }

    }

    public static class TimingLog {
        public static bool enabled = true;  // QSOLogTiming.txt
        public static int CurrSeq { get; set; }
        public static int MaxSeq { get; set; }
        public static DateTime StartDt { get; set; }
        public static TimeSpan Elapsed { get; set; }
        public static double ElapsedSec { get { return Math.Round(Elapsed.TotalSeconds, 4); } }


        public static List<Timing> Log { get; set; }

        public static List<string> ToList {
            get {

                Calculate();
                //          List<string> outList = Log.Where(x => x.ElapsedSec > .00001).Select(x => $" {x.Dt.ToString("yyyy-MM-dd HH:mm:ss.fffff")}  {Math.Round(x.ElapsedSec, 4).ToString("##0.00000")},  Seq:{x.Seq},   {x.Comment}").ToList<String>();
                //  List<string> outList = Log.Select(x => $" {x.Dt.ToString("yyyy-MM-dd HH:mm:ss.fffff")}  {Math.Round(x.ElapsedSec, 4).ToString("##0.00000")},  Seq:{x.Seq},   {x.Comment}").ToList<String>();
                List<string> outList = Log.Select(x => $"{Math.Round(x.ElapsedSec, 4).ToString("##0.000")}, {x.Comment}").ToList<String>();

                return outList;
            }
        }
        public static string ToListText {
            get {
                return string.Join(Environment.NewLine, ToList);
            }
        }

        public static void Reset() {
            Log=new List<Timing>();
            StartDt=DateTime.Now;
            CurrSeq=-1;
            MaxSeq=-1;
            Elapsed=new TimeSpan();

        }
        public static void Add(string comment) {
            if (Log==null) {
                Log=new List<Timing>();
                StartDt=DateTime.Now;
            }
            Log.Add(new Timing { Seq=++CurrSeq, Dt=DateTime.Now, Comment=comment });
        }
        public static void Calculate() {
            if(Log.Count==0) {
                return;
            }
            MaxSeq=Log.Max(x => x.Seq);
            foreach (Timing item in Log.Where(x => x.Seq>0).ToList<Timing>()) {
                item.DtPrev=Log[item.Seq-1].Dt;
                item.Elapsed=item.Dt-item.DtPrev;
            }
            if (MaxSeq>0) {
                Elapsed=Log[MaxSeq].Dt-Log[0].Dt;
            }

        }

    }

    public static class Util {
        public static string letters = $"ABCDEFGHIJKLMNOPQRSTUVWXYZ/? "; // Allowed characters in hex string

        public static string numbers = $"0123456789"; // Allowed characters in hex string
        public static string GetAllowedCharacters(string type) {
            string outString = "";
            ///?
            ///
            switch (type) {
                case "WINKEYER":
                    outString=$"{letters}{numbers}/? ";
                    break;
                case "CALL_ENTRY_TEXTBOX":
                    outString=$"{letters}{numbers}/ ";
                    break;
            }
            return outString;
        }

        public static bool Chance(int percent) {
            if(percent <= 0) return false;
            if(percent >= 100) return true;
            // Use a cryptographically strong random number for better distribution
            int value = RandomNumberGenerator.GetInt32(1,101); // 1 to 100 inclusive
            return value <= percent;
        }

        // Returns true if the key is a letter (A-Z) or number (0-9)
        public static bool IsLetterOrNumberKey(Key key) {
            // Letters: Key.A to Key.Z
            if (key>=Key.A&&key<=Key.Z)
                return true;

            // Numbers: Key.D0 to Key.D9 (main keyboard)
            if (key>=Key.D0&&key<=Key.D9)
                return true;

            // Numbers: Key.NumPad0 to Key.NumPad9 (numpad)
            if (key>=Key.NumPad0&&key<=Key.NumPad9)
                return true;

            return false;
        }

        // Method to parse a fixed-format string into tokens based on CSV position list
        public static List<string> ParseFixedFormatString(string input,string positionsCsv) {
            var positions = positionsCsv.Split(',').Select(p => int.Parse(p.Trim())).ToArray();

            var tokens = new List<string>();
            for(int i = 0;i < positions.Length;i++) {
                int start = positions[i];
                int end = (i + 1 < positions.Length) ? positions[i + 1] : input.Length;
                string token = input.Substring(start,end - start).Trim();
                tokens.Add(token);
            }

            return tokens;
        }


        public static string RepeatStringLinq(this string text, int n, int maxLen = 0) {
            string repeated = string.Concat(System.Linq.Enumerable.Repeat(text, (int)n));
            if (maxLen>0) {
                repeated=repeated.Substring(0, Math.Min(repeated.Length, (int)maxLen));
            }
            return repeated;
        }


        public static string GetSubstringByString(string inputString, string left, string right) {// Extracts text between left and right delimiters
            string outString = "";
            int indexLeft = inputString.IndexOf(left);
            if (indexLeft==-1) {
                return outString;
            }
            int indexRight = inputString.IndexOf(right);
            if (indexRight==-1) {
                return outString;
            }
            return inputString.Substring((indexLeft+left.Length), (indexRight-inputString.IndexOf(left)-left.Length));
        }

        public static int GetSubstringByStringInt(string inputString, string left, string right) {
            int outInt = 0;
            string tmp = GetSubstringByString(inputString, left, right);
            int.TryParse(tmp, out outInt);
            return outInt;
        }
        public static double GetSubstringByStringDouble(string inputString, string left, string right) {
            double outDbl = 0;
            string tmp = GetSubstringByString(inputString, left, right);
            double.TryParse(tmp, out outDbl);
            return outDbl;
        }

        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {  //https://stackoverflow.com/questions/4238345/asynchronously-wait-for-taskt-to-complete-with-timeout

            using (var timeoutCancellationTokenSource = new CancellationTokenSource()) {

                var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
                if (completedTask==task) {
                    timeoutCancellationTokenSource.Cancel();
                    return await task;  // Very important in order to propagate exceptions
                } else {
                    throw new TimeoutException("The operation has timed out.");
                }
            }
        }
        public static string DateYYYYMMDD_HHMMSS(DateTime date) {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public static string DateYYYYMMDD_HHMM(DateTime date) {
            return date.ToString("yyyy-MM-dd HHmm");
        }
        public static string TimeHHMMSS(DateTime date) {
            return date.ToString("HHmmss");
        }
        public static string DateYYYYMMDD_HHMMSS1(DateTime date) {
            return date.ToString("yyyy-MM-dd_HHmmss");
        }
        public static int DateDiffWeeks(DateTime? dt1, DateTime dt2) {
            return DateDiffWeeks((DateTime)dt1, dt2);
        }
        public static int DateDiffWeeks(DateTime dt1, DateTime dt2) {
            System.TimeSpan diff = dt2.Subtract(dt1);
            return (int)Math.Round(diff.TotalDays/7.0, 0);
        }

        public static int DateDiffHours(DateTime? dt1, DateTime dt2) {
            return DateDiffHours((DateTime)dt1, dt2);
        }

        public static int DateDiffHours(DateTime dt1, DateTime dt2) {
            System.TimeSpan diff = dt2.Subtract(dt1);
            return (int)Math.Round(diff.TotalHours, 0);
        }
        public static int DateDiffMinutes(DateTime dt1, DateTime dt2) {
            System.TimeSpan diff = dt2.Subtract(dt1);
            return (int)Math.Round(diff.TotalMinutes, 0);
        }
        public static int DateDiffSeconds(DateTime dt1, DateTime dt2) {
            System.TimeSpan diff = dt2.Subtract(dt1);
            return (int)Math.Round(diff.TotalSeconds, 0);
        }
        public static int DateDiffAbsMinutes(DateTime dt1, DateTime dt2) {
            System.TimeSpan diff = dt2.Subtract(dt1);
            return Math.Abs((int)Math.Round(diff.TotalMinutes, 0));
        }



        private static readonly string[] Formats = { "d-MMM-yyyy HHmm","dd-MMM-yyyy HHmm" };

        /// <summary>
        /// Combines separate date (e.g., "31-Jul-2026") and time (e.g., "1923" or "1923Z") strings
        /// into a UTC DateTime object.
        /// </summary>
        /// <param name="dateStr">Date string in d-MMM-yyyy or dd-MMM-yyyy format.</param>
        /// <param name="timeStr">Time string in HHmm or HHmmZ format.</param>
        /// <returns>A UTC DateTime if parsing succeeds; otherwise, null.</returns>
        public static DateTime? CombineToUtcDateTime(string dateStr,string timeStr) {
            if(string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(timeStr))
                return null;

            // Clean up inputs and strip trailing 'Z' if present in the time string
            string cleanDate = dateStr.Trim();
            string cleanTime = timeStr.Replace("Z","",StringComparison.OrdinalIgnoreCase).Trim();

            // Pad single-digit minutes if necessary (e.g. "915" -> "0915")
            if(cleanTime.Length == 3) {
                cleanTime = "0" + cleanTime;
            }

            string combined = $"{cleanDate} {cleanTime}";

            if(DateTime.TryParseExact(
                    combined,
                    Formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime result)) {
                return result;
            }

            return null;
        }


        public static void PlaySound(string soundFile = "Exclamation") {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer($@"ResourceFiles\{soundFile}.wav");
            player.Play();

        }
        public static string GetCSVListElement(string inList, int seq, string defaultVal = "") { // Splitss CSV into arr and returns seq if exists, if not returns default value
            string val = defaultVal;

            string[] arr = inList.Split(',');
            if (seq<=arr.Length-1) {
                arr[seq]=arr[seq].Trim();
                if (arr[seq].Length>0) {
                    val=arr[seq];
                }
            }
            return val;
        }
        public static string ParseDelimitedExtractLastElement(string inList, char delim, ref string lastElement) {
            lastElement="";
            string beginElements = ""; // all elements except the last one a/b/c beginElements=a/b  a/b beginElements=a  a beginElements=a
            List<string> list = inList.Split(delim).ToList<string>();
            if (list.Count==1) {
                lastElement="";
                beginElements=inList; // just the first element, no list
            } else {
                lastElement=list[list.Count-1];
                list.RemoveAt(list.Count-1);
                beginElements=string.Join(delim.ToString(), list);
            }
            return beginElements;
        }

        public static byte convertByteToBCD(byte value) {
            return (byte)((value/10*16)+(value%10));
        }

        public static string AddTextToTextQueue(int maxNumOfCharsInQueue, string queueChar, string text) {
            text=text+queueChar;
            if (text.Length>maxNumOfCharsInQueue) { //Cut from the left. Scrolling left. Displays up to maxNumOfCharsInQueue recently types chars
                                                    //              text=text.Substring(1, Math.Max(maxNumOfCharsInQueue, 1));

                text=text.Substring(1, Math.Max(maxNumOfCharsInQueue, 1));
            }
            return text;
        }

        public static string AddTextKeepLastNChars(string input, string newChars, int maxLen = 100) {
            if (input==null) input="";
            if (newChars==null) newChars="";
            string combined = input+newChars;
            if (combined.Length<=maxLen)
                return combined;
            return combined.Substring(combined.Length-maxLen, maxLen);
        }

        public static string ModeConvert(string mode) {
            switch (mode) {
                case "CW-R":
                case "CW-U":
                case "CW-L":
                    return "CW";

                case "LSB":
                case "USB":
                case "SSB":
                    return "SSB";
                default:
                    return mode;
            }

        }
        public static int BoolStringToInt(string boolVal) {
            boolVal=boolVal.ToUpper();
            if (boolVal=="Y"||boolVal=="YES"||boolVal=="TRUE") {
                return 1;
            } else {
                return 0;
            }
        }
        public static bool IntegerStringToBool(string intlVal) {
           
            if (intlVal=="1") {
                return true;
            } else {
                return false;
            }
        }
        public static int GetNumbersInt(string input) {
            int result = 0;
            string digits = new string(input.Where(c => char.IsDigit(c)).ToArray());
            int.TryParse(digits, out result);
            return result;
        }
        public static string GetNumbersString(string input) {
            int result = 0;
            string digits = new string(input.Where(c => char.IsDigit(c)).ToArray());
            int.TryParse(digits, out result);
            return result.ToString();
        }
        public static double GetNumbersDouble(string input) {
            double result = 0;
            double.TryParse(new string(input.Where(c => char.IsDigit(c)).ToArray()), out result);
            return result;
        }

        //public static List<int> GetNumbersList(string input) { //"10 of 15 points" => List of 10, 15
        //                                                       //          List<int> numbers = input.Split().Where(x => x.All(char.IsDigit)).Select(x => int.Parse(x)).ToList<int>();
        // List<   Tuple<string, string>> charTypes =new List<Tuple<string, string>> ();
        //    string prevType = "";
        //    string currType = "";
        //    string currItem = "";
        //    char[] characters= input.ToCharArray();

        //    foreach (char c in characters) {
        //        currType= char.IsDigit(c) ?"INT":"STR";
        //        if (currType!=prevType) {
        //            if(prevType .Length>0) { // not the starting point
        //                charTypes.Add(new Tuple<string, string> (  prevType, currItem ));
        //            }
        //            currItem="";
        //        }
        //        currItem+=c;
        //        prevType = currType;    
        //    }

        //    return new List<int>();
        //}

        public static List<Tuple<string, string>> GetCharSequencesWitTypesList(string input) { //"10 of 15 points" => List of 10, 15
                                                                                               //          List<int> numbers = input.Split().Where(x => x.All(char.IsDigit)).Select(x => int.Parse(x)).ToList<int>();
            List<Tuple<string, string>> charTypes = new List<Tuple<string, string>>();
            string prevType = "";
            string currType = "";
            string currItem = "";
            char[] characters = input.ToCharArray();


            //   FIX THIS case , last digit   string seq5 = Util.GetLastNumberSequenceAsString("43X6XXX8"); returns 6 instead of 8

            foreach (char c in characters) {
                currType=char.IsDigit(c) ? "INT" : "STR";
                if (currType!=prevType) {
                    if (prevType.Length>0) { // not the starting point
                        charTypes.Add(new Tuple<string, string>(prevType, currItem));
                    }
                    currItem="";
                }
                currItem+=c;
                prevType=currType;
            }

            return charTypes;
        }

        public static string GetLastNumberSequenceAsString(string input, int defaultValue = 0) { //Gets last sequance of numbers from PFX as string 9A1AA =>1, 4X6ZZ => 6, YU7AOP =>7
                                                                                                 // List<int> numbers =
            List<Tuple<string, string>> charTypes = GetCharSequencesWitTypesList(input);
            List<int> numericSequences = charTypes.Where(x => x.Item1=="INT").Select(x => int.Parse(x.Item2)).ToList<int>();
            int lastSeq = GetLastNumberFromListInt(numericSequences, defaultValue);
            return lastSeq.ToString();
            //string seq1 = Util.GetLastNumberSequenceAsString("4X6XXX");
            //string seq2 = Util.GetLastNumberSequenceAsString("YU7AOP");
            //string seq3 = Util.GetLastNumberSequenceAsString("C31AAA");
            //string seq4 = Util.GetLastNumberSequenceAsString("43X67XXX");
            //string seq5 = Util.GetLastNumberSequenceAsString("43X6XXX8");
            //string seq6 = Util.GetLastNumberSequenceAsString("43X6XXX8ss");
        }





        public static int GetLastNumberFromListInt(List<int> list, int defaultValue = 0) {
            if (list.Count>0) {
                return list[list.Count-1];
            } else {
                return defaultValue;
            }
        }

        //public static string GetLastNumberSequenceAsString(string input, int defaultValue = 0) { //Gets last sequance of numbers from PFX as string 9A1AA =>1, 4X6ZZ => 6, YU7AOP =>7
        //    List<int> numbers = GetNumbersList(input);
        //    int lastSeq = GetLastNumberFromListInt(numbers, defaultValue);
        //    return lastSeq.ToString();  
        //}

        public static string GetCharacters(string input) { // All chars other than digits
            string chars = new string(input.Where(c => !char.IsDigit(c)).ToArray());
            return chars;
        }
        public static string GetLetters(string input) {
            string chars = new string(input.Where(c => char.IsLetter(c)).ToArray());
            return chars;
        }
        public static bool IsNumeric(string value) {
            return value.All(char.IsNumber);
        }
        public static bool IsNullOrEmpty(string value) {
            if (value==null) {
                return true;
            } else {
                value=value.Trim();
                if (value.Length==0) {
                    return true;
                }
            }
            return false;
        }
        public static List<string> FileReadToList(string filePath) {
            List<string> lines = new List<string>();
            StreamReader streamreader = null;
            try {
                if (File.Exists(filePath)) {
                    streamreader=File.OpenText(filePath);
                    string line = streamreader.ReadLine();
                    while (line!=null) {
                        lines.Add(line);
                        line=streamreader.ReadLine();
                    }
                } else {
                    lines.Add("File not found: "+filePath);
                }
            } catch (Exception ex) {
                lines.Add("Error: "+ex.Message);
            } finally {
                if (streamreader!=null) {
                    streamreader.Close();
                }
            }
            return lines;
        }

        public static RetCode FileWriteLine(string filePath, string line) {
            RetCode ret = new RetCode();
            FileWriteLines(filePath, new List<string> { line }, DeleteExistingLines: false);
            return ret;
        }
        public static RetCode FileWriteLines(string filePath, List<string> lines, bool DeleteExistingLines = true) {
            RetCode ret = new RetCode();

            StreamWriter streamwriter = null;
            try {
                if (DeleteExistingLines) {
                    if (File.Exists(filePath)) File.Delete(filePath);
                }
                streamwriter=File.AppendText(filePath);

                foreach (var line in lines) {
                    streamwriter.WriteLine(line);
                }

            } catch (Exception ex) {
                ret.Code=-1;
                ret.Msg=ex.Message;
            } finally {
                streamwriter.Flush();
                streamwriter.Close();
            }
            return ret;
        }

        public static TEnum ParseEnum<TEnum>(string item, bool ignorecase = default(bool))
            where TEnum : struct {
            TEnum tenumResult = default(TEnum);
            return Enum.TryParse<TEnum>(item, ignorecase, out tenumResult) ?
                   tenumResult : default(TEnum);
        }

        public static string GetLotWYN(string lotwCode) {
            lotwCode=lotwCode.Trim().ToUpper();
            if (lotwCode==null||lotwCode.Length==0) {
                return "n";
            }

            if (lotwCode=="N") {
                return "n";
            }
            return "Y";
        }

        public static DrawingColor BackColorContest(bool condition) {

            //      Color enabledColor = Color.FromArgb(255, (byte)(0), (byte)(0), (byte)(5));
            DrawingColor enabledColor = DrawingColor.Azure;// Color.FromArgb(255, (byte)(0), (byte)(0), (byte)(5));
            return condition ? enabledColor : DrawingColor.LightGray;
        }

        // ✅ ADD STATIC COMPILED REGEX
        private static readonly Regex CallsignPattern = new Regex(
            @"^[A-Z0-9]{1,3}[0-9][A-Z]{1,4}(/[A-Z0-9]+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public static bool IsValidCallsign(string call) {
            return CallsignPattern.IsMatch(call);
        }
        public static DateTime GetLocalTime(double UTCShift) {
            return DateTime.UtcNow.AddHours(UTCShift);
        }
        public static string GetUTC_YYYYMMDD_HHMM() {
            return Util.DateYYYYMMDD_HHMM(DateTime.UtcNow);
        }

        public static bool StringToBool(string str) {
            if (str==null) {
                return false;
            }
            switch (str.ToUpper().Trim()) {
                case "Y":
                case "YES":
                case "TRUE":
                    return true;
            }
            return false;
        }

        public static bool ReadAppSettingsBool(string key) {
            string tmp = ConfigurationManager.AppSettings.Get(key);
            return Util.StringToBool(tmp);
        }



        public static int FindRightMostNumericIndex(string inString) { // Finds the index of the last figure in a string 1aa2sssss =>  figure 2 index =3

            char[] chars = inString.ToArray();
            int rightmostNumericIndex = -1;
            for (int index = chars.Length-1; index>=0; index--) {
                if (Util.IsNumeric(chars[index].ToString())) {
                    rightmostNumericIndex=index;
                    break;
                }
            }
            return rightmostNumericIndex;
        }

        public static char GetLastChar(string str) {
            char lastCharacter = '\0';
            if (str.Length>0) {
                lastCharacter=str[str.Length-1];
            }
            return lastCharacter;
        }



        /// <summary>
        /// Generic method to run a task on a background thread with a specific timeout, if the task fails,
        /// notifies a user
        /// </summary>
        /// <typeparam name="T">Return type of function</typeparam>
        /// <param name="TaskAction">Function delegate for task to perform</param>
        /// <param name="TimeoutSeconds">Time to allow before task times out</param>
        ///https://stackoverflow.com/questions/13513650/how-to-set-timeout-for-a-line-of-c-sharp-code
        /// <returns></returns>
        /// 

        // var response = RunTaskWithTimeout<ReturnType>(
        //   (Func<ReturnType>)delegate { return SomeMethod(someInput); }, 30);

        public static T RunTaskWithTimeout<T>(Func<T> TaskAction, int TimeoutSeconds) {
            Task<T> backgroundTask;

            try {
                backgroundTask=Task.Factory.StartNew(TaskAction);
                backgroundTask.Wait(new TimeSpan(0, 0, TimeoutSeconds));
            } catch (AggregateException ex) {
                // task failed
                var failMessage = ex.Flatten().InnerException.Message;
                return default(T);
            } catch (Exception ex) {
                // task failed
                var failMessage = ex.Message;
                return default(T);
            }

            if (!backgroundTask.IsCompleted) {
                // task timed out
                return default(T);
            }

            // task succeeded
            return backgroundTask.Result;
        }


        public static DataTable ConvertCSVtoDataTable(string strFilePath) {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader(strFilePath)) {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers) {
                    dt.Columns.Add(header);
                }
                while (!sr.EndOfStream) {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i<headers.Length; i++) {
                        dr[i]=rows[i];
                    }
                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }
        public static DataTable ConvertCSVtoDataTableHeaderDefined(string strFilePath, List<string> columnnList, string delimiter, List<string> lineStartIgnore) {
            DataTable dt = new DataTable();
            int nLine = 0;
            using (StreamReader sr = new StreamReader(strFilePath)) {
                //    string[] headers = sr.ReadLine().Split(',');
                foreach (string col in columnnList) {
                    dt.Columns.Add(new DataColumn { ColumnName=col, DataType=typeof(String) });
                }
                while (!sr.EndOfStream) {
                    nLine++;
                    string line = sr.ReadLine().Trim();

                    if (line.StartsWith("K3KU")) { //debug

                    }


               
                    if (lineStartIgnore.Contains(line.Substring(0, 1))) {
                        continue; // skip comments
                    }
                    string[] items = line.Split(delimiter);
                    DataRow dr = dt.NewRow();
                    for (int nCol = 0; nCol<columnnList.Count; nCol++) {
                        try { dr[nCol]=items[nCol]; }
                        catch {

                        }
                      //  }
                    }
                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }
        public static DataTable ConvertCSVtoDataTableHeaderDefinedOQP(string strFilePath,List<string> columnnList,string delimiter,List<string> lineStartIgnore) {
            DataTable dt = new DataTable();
            using(StreamReader sr = new StreamReader(strFilePath)) {
                //    string[] headers = sr.ReadLine().Split(',');
                foreach(string col in columnnList) {
                    dt.Columns.Add(new DataColumn { ColumnName=col,DataType=typeof(String) });
                }
                while(!sr.EndOfStream) {
                    string line = sr.ReadLine().Trim();

                    if(line.StartsWith("K3KU")) { //debug

                    }


                    if(line.Length<5) { continue; }
                    if(lineStartIgnore.Contains(line.Substring(0,1))) {
                        continue; // skip comments
                    }
                    string[] items = line.Split(',');
                    DataRow dr = dt.NewRow();
                    for(int nCol = 0;nCol<columnnList.Count;nCol++) {
                        if(items.Length>nCol) { // Some files like NAQP have missing delimiters for blank columns  IC8JOH,SERG,,          Missing comma=>       IN3BJS,GIANNI,
                            dr[nCol]=items[nCol];
                        }
                    }
                    dt.Rows.Add(dr);
                }
            }
            return dt;
        }
    }

    public class DateTimeInfo {
        public string Type { get; set; }  // UTC LOCAL
        public DateTime dt { get; set; }
        public DateTimeInfo(DateTime dt_) {
            dt=dt_;
        }
        public DateTimeInfo(DateTime? dt_) {
            if (dt_==null) {
                dt=DateTime.MinValue;
            } else {
                dt=(DateTime)dt_;
            }
        }

        public DateTimeInfo() {

        }

        public string GetDateYYYYMMDD() {
            return dt.ToString("yyyyMMdd");
        }
        public string GetDateYYYYMMDD1() {
            return dt.ToString("yyyy-MM-dd");
        }
        public string GetDateTimeYYYYMMDD_HHMMSS() {
            return dt.ToString("yyyyMMdd_HHmmss");
        }
        public string GetDateTimeDD_HHMM() {
            return dt.ToString("dd_HHmm");
        }
        public string GetDateTimeDD_HHMMFMT() {
            return dt.ToString("dd_HH:mm");
        }
        public string GetDateTimeHHMM_DD() {
            return dt.ToString("HH:mm dd");
        }

        public string GetDateTimeHHMM_YYYYMMDD() {
            return dt.ToString("HH:mm yyyyMMdd");
        }

        public string GetDateTimeYYYYMMDspaceHHMMSS() {
            return dt.ToString("yyyyMMdd HHmmss");
        }
        public string GetTimeHHMMSS() {
            return dt.ToString("HHmmss");
        }
        public string GetTimeHHMMSS1() {
            return dt.ToString("HH:mm:ss");
        }
        public string GetTimeHHMM1() {
            return dt.ToString("HH:mm");
        }
        public string GetTimeSS() {
            return dt.ToString("ss");
        }
        public string GetDateTimeDisplayLongFMT() {
            return dt.ToString("yyyy-MM-dd HH:mm");
        }
        public string GetDateTimeDisplayLongFMTSS() {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public static class Extensions {

        public static string Left(this string str, int length) {
            str=(str??"").Trim();
            return str.Substring(0, Math.Min(length, str.Length));
        }

        public static string Right(this string str, int length) {
            str=(str??"").Trim();
            return str.Substring(str.Length-Math.Min(length, str.Length));
        }
        public static List<List<T>> SplitList<T>(this List<T> source, int length) {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index/length)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
        public static int ToInt(this bool value) {
            return value ? 1 : 0;
        }
        public static DataTable ToDataTable<T>(this IEnumerable<T> data) {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            foreach (PropertyDescriptor prop in properties) {

                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType)??prop.PropertyType);
            }
            foreach (T item in data) {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    row[prop.Name]=prop.GetValue(item)??DBNull.Value;
                table.Rows.Add(row);
            }
            return table;
        }
        public static bool IsValidTimeFormat(this string input) {
            TimeSpan dummyOutput;
            return TimeSpan.TryParse(input, out dummyOutput);
        }

    }
}
