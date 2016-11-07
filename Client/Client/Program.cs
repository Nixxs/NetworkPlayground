using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Lidgren.Network;

namespace Client
{
    class Program
    {
        private static NetClient s_client;

        [STAThread]
        static void Main(string[] args)
        {
            NetPeerConfiguration config = new NetPeerConfiguration("chat");// define the config object
            config.AutoFlushSendQueue = false;// set a config setting
            s_client = new NetClient(config);// create the client object and set it's config 

            // I don't know whata scynchronization context is but aparently this is what i need to do if running from console
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            // define what to do with messages from the server
            s_client.RegisterReceivedCallback(new SendOrPostCallback(GotMessage));// run this when the client gets a message from the server

            // define the host details
            string host = "localhost";
            int port = 14242;
            // immediatly connect to the server on start up
            s_client.Start(); //start the client object running 
            NetOutgoingMessage hail = s_client.CreateMessage("Hi I'd like to connect to you please sir");
            s_client.Connect(host, port, hail);


            Thread.Sleep(1); // give it a second to connect before starting the input loop
            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();

                NetOutgoingMessage om = s_client.CreateMessage(input);
                s_client.SendMessage(om, NetDeliveryMethod.ReliableOrdered);
                Console.WriteLine("You: " + input);
                s_client.FlushSendQueue();
            }

        }

        public static void GotMessage(object peer)
        {
            NetIncomingMessage im;
            while ((im = s_client.ReadMessage()) != null)
            {
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        string text = im.ReadString();
                        Console.WriteLine(text);
                        break;
                    case NetIncomingMessageType.StatusChanged: // when the client connects to the server
                        NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();
                        string reason = im.ReadString();
                        if (status == NetConnectionStatus.Connected)
                        {
                            Console.WriteLine("You have connected to the server!");
                            Console.WriteLine(status + " : " + reason);
                        }
                        break;
                    case NetIncomingMessageType.Data: // got a message from the server
                        string message = im.ReadString();
                        Console.WriteLine(message);
                        break;
                    default:
                        Console.WriteLine("Got some kind of random type: " + im.MessageType);
                        break;
                }
                s_client.Recycle(im);
            }
        }
    }
}
