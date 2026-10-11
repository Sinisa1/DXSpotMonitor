using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common {
    public class DxKeeperHost {
        public string Host = "PPS";//Name of the computer the app is running on. Used for customised settings in App.config
        public int Port = 52001;
        public IPAddress IPAddress;// = IPAddress.Parse("192.168.0.111");
        public IPEndPoint endPoint;
            public DxKeeperHost() {
            IPAddress = IPAddress.Parse("127.0.0.1");
            endPoint = new IPEndPoint(IPAddress, Port);
        }

        // 

    }
}
