using Common;
//using Data;
using System;
using System.Data;

namespace Data {
    public class LogDataETL:DataETLBase {
        SettingsLog settings_ = null;
        public LogDataETL( SettingsLog sett) : base(sett.SQLServerConnectionString) {
            settings_=sett;
        }      

    }
}
