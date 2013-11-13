using System;
using System.Net;

namespace socketSrv
{
   
    [Serializable()]
    public class registerPeer
	{
		private IPAddress _peerIP;
		private string _peerHostname;
		private Int32 _port;
		
		public IPAddress peerIP {
			get { return _peerIP; }
			set {_peerIP = value; }
		}

        public string peerHostname
        {
            get { return _peerHostname; }
            set { _peerHostname = value; }
		}
		
		public Int32 port {
			get {return _port; }
			set {_port = value; }
		}
	}
}

