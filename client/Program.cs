using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{
    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    public static void start()
    {
        //TODO: [Create endpoints and socket]
        var ipAdress = IPAddress.Parse(setting.ClientIPAddress);
        var endpoint = new IPEndPoint(ipAdress, setting.ClientPortNumber);
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(endpoint);

        //TODO: [Create and send HELLO]
        IPAddress serverIP = IPAddress.Parse(setting.ServerIPAddress);
        EndPoint serverEndPoint = new IPEndPoint(serverIP, setting.ServerPortNumber);

        var helloMessage = new Message()
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from DNS client"
        };
        SendMessage(socket, serverEndPoint, helloMessage);
        //TODO: [Receive and print Welcome from server]
        ReceiveMessage(socket, ref serverEndPoint);

        var endMessage = new Message()
        {
            MsgId = 1,
            MsgType = MessageType.End,
            Content = "End from DNS client"
        };
        SendMessage(socket, serverEndPoint, endMessage);
        // TODO: [Create and send DNSLookup Message]


        //TODO: [Receive and print DNSLookupReply from server]


        //TODO: [Send Acknowledgment to Server]

        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

        //TODO: [Receive and print End from server]


    }

    private static void SendMessage(Socket socket, EndPoint clientEndPoint, Message message)
    {
        string jsonString = JsonSerializer.Serialize(message);
        byte[] sendData = Encoding.UTF8.GetBytes(jsonString);
        socket.SendTo(sendData, clientEndPoint);

        Console.WriteLine($"Sent message: Type={message.MsgType}, ID={message.MsgId}");
    }

    private static Message ReceiveMessage(Socket socket, ref EndPoint remoteEndPoint)
    {
        byte[] buffer = new byte[4096];
        
        int bytesReceived = socket.ReceiveFrom(buffer, ref remoteEndPoint);
        string jsonString = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
        
        Message receivedMessage = JsonSerializer.Deserialize<Message>(jsonString);
        
        Console.WriteLine($"Received message: Type={receivedMessage.MsgType}, ID={receivedMessage.MsgId}");
        
        return receivedMessage;
    }
}