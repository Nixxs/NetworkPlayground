using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Lidgren.Network;

namespace Server
{
    class Program
    {
        private static NetServer s_server;

        [STAThread]
        static void Main(string[] args)
        {
            NetPeerConfiguration config = new NetPeerConfiguration("chat");
            config.MaximumConnections = 100;
            config.Port = 14242;
            s_server = new NetServer(config);
            s_server.Start(); // start the server
            while (true)
            {
                NetIncomingMessage im;
                // start network server code
                while ((im = s_server.ReadMessage()) != null)
                {
                    switch (im.MessageType)
                    {
                        case NetIncomingMessageType.DebugMessage:
                        case NetIncomingMessageType.ErrorMessage:
                        case NetIncomingMessageType.WarningMessage:
                        case NetIncomingMessageType.VerboseDebugMessage:// if we get some kind of error message print it to the screen
                            string text = im.ReadString();
                            Console.WriteLine(text);
                            break;
                        case NetIncomingMessageType.StatusChanged:// if the connection status changes, like if a user connects to the server do this. 
                            NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();
                            string reason = im.ReadString();
                            // print the new users connection ID to the screen
                            Console.WriteLine(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " " + status + " " + reason);
                            if (status == NetConnectionStatus.Connected)
                            {
                                Console.WriteLine("Hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
                            }
                            break;
                        case NetIncomingMessageType.Data: // if we get some message from a client
                            string message = im.ReadString();
                            Console.WriteLine("Recieved message, Broadcasting: " + message);
                            List<NetConnection> all = s_server.Connections; // get a copy of all the current connections
                            all.Remove(im.SenderConnection); // remove the original message sender from the list of connections

                            if (all.Count > 0)// if there is more than 1 user connected to the server send the message, otherwise why bother
                            {
                                NetOutgoingMessage outgoing_message = s_server.CreateMessage(); // create out going message object
                                outgoing_message.Write(NetUtility.ToHexString(im.SenderConnection.RemoteUniqueIdentifier) + " said: " + message); // define the message try using outgoing_message.Data to see if we can send objects
                                s_server.SendMessage(outgoing_message, all, NetDeliveryMethod.ReliableOrdered, 0); // send the message 
                            }
                            break;
                        default:
                            Console.WriteLine("Recieved and unknown type: " + im.MessageType);
                            break;
                    }
                    s_server.Recycle(im); // tells compiler to reuse the message creator pointer so it doesn't need to run the garbage collector
                }
                // if the server message ever returns null sleep for 1s and ping again
                Thread.Sleep(1);
            }
        }
    }
}
