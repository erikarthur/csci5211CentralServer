using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace socketSrv
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private List<peerInstance> peerList = new List<peerInstance>();

        public Server()
        {
            this.tcpListener = new TcpListener(IPAddress.Any, 4000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            Random randomNumberGenerator = new Random();
            int peerNumber;

            byte[] netMessage = new byte[4096];
            byte[] message = new byte[4092];

            byte[] messageLength = new byte[4];
            int bytesRead, nextMsgBytesRead;

            int messageBytes = 0;
			clientStream.ReadTimeout = System.Threading.Timeout.Infinite;
            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(netMessage, 0, 4096);

                    if (bytesRead > 3)
                    {
                        //strip off first 4 bytes and get the message length
                        System.Buffer.BlockCopy(netMessage, 0, messageLength, 0, sizeof(Int32));

                        //if (BitConverter.IsLittleEndian)
                        //    Array.Reverse(message);  //convert from big endian to little endian

                        messageBytes = BitConverter.ToInt32(messageLength, 0);
                    }

                    while (bytesRead != messageBytes)
                    {
                        nextMsgBytesRead = clientStream.Read(netMessage, bytesRead, 4096 - bytesRead);
                        bytesRead += nextMsgBytesRead;

                        //bugbug - need a watchdog timer for timeouts
                        //bugbug - need to handle the case of more data than expected from the network
                    }

                    //we finally got it all
                    //copy netMessage into message buffer
                    //IPAddress messageIP = new IPAddress();
                    byte[] addressBytes = new byte[4];
                    byte[] portBytes = new byte[sizeof(Int32)];
					byte[] cmdBytes = new byte[sizeof(Int32)];
                    System.Buffer.BlockCopy(netMessage, 4, addressBytes, 0, 4);
                    System.Buffer.BlockCopy(netMessage, 8, portBytes, 0, 4);
					System.Buffer.BlockCopy(netMessage, 12, cmdBytes, 0, 4);

                    IPAddress messageIP = new IPAddress(addressBytes);
                    Int32 port = BitConverter.ToInt32(portBytes, 0);
					Int32 cmd = BitConverter.ToInt32(cmdBytes, 0);
                    //BitConverter.
                    ////convert endianness if necessary
                    //if (BitConverter.IsLittleEndian)
                    //    Array.Reverse(message);  //convert from big endian to little endian

                    //convert byte array to memorystream for deserialization
                    //MemoryStream stream = new MemoryStream();
                    //bytesRead = 
                    //stream.Write(message, 0, bytesRead - 4);

                    //peerInstance peer = new peerInstance();
                    //peer.peerIP = address[1];
                    //peer.peerHostname = Dns.GetHostName();
                    //peer.peerPort = 3000 + RNG.Next(3000);

                    //MemoryStream clientMsgStream = SerializeToStream(peer);
					switch (cmd)
					{
					case 0:
						peerInstance newPeer = new peerInstance(); 

						newPeer.peerIP = messageIP;
						newPeer.peerPort = port;
						//add peer and check peer count
						//assuming peer hasn't been added previously

						if (peerList.Count() < 2)
							peerNumber = 0;
						else
							peerNumber = randomNumberGenerator.Next(peerList.Count());

						//add the peer to peerList
						peerList.Add(new peerInstance());
						int newPeerCnt = peerList.Count()-1;

						peerList[newPeerCnt].peerIP = newPeer.peerIP;
						
						peerList[newPeerCnt].peerPort = newPeer.peerPort;

						//create peer variable to send back to client
						peerInstance peerMsgToClient = new peerInstance();

						peerMsgToClient.peerIP = peerList[peerNumber].peerIP;
						
						peerMsgToClient.peerPort = peerList[peerNumber].peerPort;
						Console.WriteLine("----------------New Connection-------------");
						Console.WriteLine(peerList[newPeerCnt].peerIP + ", Server Port: " + peerList[newPeerCnt].peerPort);
						Console.WriteLine("-------------------------------------------");

						addressBytes = peerList[peerNumber].peerIP.GetAddressBytes();
						portBytes = BitConverter.GetBytes(peerList[peerNumber].peerPort);
						int response = 0;
						cmdBytes = BitConverter.GetBytes(response);

						int clientMsgStreamLength = (int)(addressBytes.Length + portBytes.Length + sizeof(Int32) + sizeof(Int32));

						//copy to byte array
						byte[] buffer = new byte[4096];  //add 4 bytes for the message length at the front

						byte[] intBytes = BitConverter.GetBytes(clientMsgStreamLength);

						System.Buffer.BlockCopy(intBytes, 0, buffer, 0, 4);  //prepends length to buffer
						System.Buffer.BlockCopy(addressBytes, 0, buffer, 4, addressBytes.Length);
						System.Buffer.BlockCopy(portBytes, 0, buffer, 4 + addressBytes.Length, portBytes.Length);
						System.Buffer.BlockCopy(cmdBytes, 0, buffer, 4 + addressBytes.Length + portBytes.Length, cmdBytes.Length);
						clientStream.Write(buffer, 0, clientMsgStreamLength);
						clientStream.Flush();
						break;

					case 1:
						int i;
						for (i=0;i<peerList.Count();i++)
						{
							if ((peerList[i].peerIP.Address == messageIP.Address) && (peerList[i].peerPort == port))
							{
								peerNumber = i;
								break;
							}
						}

						//i now contains the peerNumber to remove
						if (i > peerList.Count())
						{
							//error
							Console.WriteLine("Peer not found");
						}
						else
						{
							peerNumber = i;
							addressBytes = peerList[peerNumber].peerIP.GetAddressBytes();
							portBytes = BitConverter.GetBytes(peerList[peerNumber].peerPort);
							response = 0;
							cmdBytes = BitConverter.GetBytes(response);

							clientMsgStreamLength = (int)(addressBytes.Length + portBytes.Length + sizeof(Int32) + sizeof(Int32));

							//copy to byte array
							buffer = new byte[4096];  //add 4 bytes for the message length at the front

							intBytes = BitConverter.GetBytes(clientMsgStreamLength);

							System.Buffer.BlockCopy(intBytes, 0, buffer, 0, 4);  //prepends length to buffer
							System.Buffer.BlockCopy(addressBytes, 0, buffer, 4, addressBytes.Length);
							System.Buffer.BlockCopy(portBytes, 0, buffer, 4 + addressBytes.Length, portBytes.Length);
							System.Buffer.BlockCopy(cmdBytes, 0, buffer, 4 + addressBytes.Length + portBytes.Length, cmdBytes.Length);
							clientStream.Write(buffer, 0, clientMsgStreamLength);
							clientStream.Flush();

							Console.WriteLine("----------------Removed Connection-------------");
							Console.WriteLine(peerList[peerNumber].peerIP + ", Server Port: " + peerList[peerNumber].peerPort);
							Console.WriteLine("-------------------------------------------");
							peerList.RemoveAt(peerNumber);

						}
						break;
					
					default:
						//msg other than 1 or 0.  probably should assert or something.
						break;
					}
                    
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                //System.Diagnostics.Debug.WriteLine(encoder.GetString(message, 0, bytesRead));
            }

            tcpClient.Close();
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            int x;

            Server s = new Server();
            while (true)
            {
                x = 0;
            }
        }
    }
}
