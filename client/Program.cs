using System.Collections.Immutable;
using System.ComponentModel;
using System.Data.Common;
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

    static Dictionary<int, DNSRecord> requests = new Dictionary<int, DNSRecord>()
    {
        { 1, new DNSRecord { Type = "A", Name = "www.outlook.com", TTL = 3600 } },
        { 2, new DNSRecord { Type = "A", Name = "example.com", Priority = 10, TTL = 3600 } },
        { 3, new DNSRecord { Type = "A", Name = "www.nonexistent.com", TTL = 3600 } },
        { 4, new DNSRecord { Type = "A", Name = "www.sample.com", TTL = 3600 } },
    }; 

    public static void start()
    {
        //TODO: [Create endpoints and socket]
        var ipAdress = IPAddress.Parse(setting.ClientIPAddress);
        var endpoint = new IPEndPoint(ipAdress, setting.ClientPortNumber);
        
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        InitializeSocket(socket, endpoint);

        //TODO: [Create and send HELLO]
        //TODO: [Receive and print Welcome from server]
        IPAddress serverIP = IPAddress.Parse(setting.ServerIPAddress);
        EndPoint serverEndPoint = new IPEndPoint(serverIP, setting.ServerPortNumber);

        SendMessage(socket, serverEndPoint, messageFactory(1, MessageType.Hello, "Hello from client"));
        var message = ReceiveMessage(socket, ref serverEndPoint);

        // TODO: [Create and send DNSLookup Message]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply
        int msgId = 2;
        if (message.MsgType == MessageType.Welcome) 
        {
            //TODO: [Receive and print DNSLookupReply from server]
            //TODO: [Send Acknowledgment to Server]
            //TODO: [Send next DNSLookup to server]
            foreach (var request in requests)
            {
                Console.WriteLine($"Looking up DNS record for {request.Value.Name}");
                
                SendMessage(socket, serverEndPoint, messageFactory(msgId++, MessageType.DNSLookup, request.Value.Name));
                var response = ReceiveMessage(socket, ref serverEndPoint);
                
                if (response.MsgType == MessageType.DNSLookupReply)
                {
                    Console.WriteLine("DNS record found:");
                    var record = JsonSerializer.Deserialize<DNSRecord>(JsonSerializer.Serialize(response.Content));

                    Console.WriteLine($"  Name: {record.Name}");
                    Console.WriteLine($"  Type: {record.Type}");
                    Console.WriteLine($"  Value: {record.Value}");
                    Console.WriteLine($"  TTL: {record.TTL}");
                    
                    if (record.Priority.HasValue) Console.WriteLine($"  Priority: {record.Priority}");
                    
                    SendMessage(socket, serverEndPoint, messageFactory(msgId++, MessageType.Ack, response.MsgId.ToString()));
                }
                else if (response.MsgType == MessageType.Error)
                {
                    Console.WriteLine($"Error: {response.Content}");
                }
            }
            //TODO: [Receive and print End from server]
            var endResponse = ReceiveMessage(socket, ref serverEndPoint);
            if (endResponse.MsgType == MessageType.End)
            {
                System.Console.WriteLine($"{endResponse.Content}");
                socket.Close();
            }
        }
        else 
        {
            Console.WriteLine("No reponse from the server, closing connection...");
            socket.Close();
        }
    }

    private static void InitializeSocket(Socket socket, EndPoint endpoint)
    {
        try
        {
            socket.Bind(endpoint);
            Console.WriteLine($"Server socket successfully bound to {endpoint}");
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Console.WriteLine($"Error: Port {setting.ServerPortNumber} is already in use by another application.");
                Console.WriteLine("Please change the port in Setting.json or stop the other application.");
                return;
            }
            else
            {
                Console.WriteLine($"Socket binding error: {ex.Message} (Error code: {ex.SocketErrorCode})");
                return;
            }
        }
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

    private static Message messageFactory(int id, MessageType msgType, object? obj)
    {
        return new Message()
        {
            MsgId = id,
            MsgType = msgType,
            Content = obj
        };
    }
}