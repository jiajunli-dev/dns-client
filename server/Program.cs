using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}


class ServerUDP
{
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // TODO: [Read the JSON file and return the list of DNSRecords]



    public static void start()
    {
        
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        var ipAddress = IPAddress.Parse(setting.ServerIPAddress);
        var endpoint = new IPEndPoint(ipAddress, setting.ServerPortNumber);
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(endpoint);

        // TODO:[Receive and print a received Message from the client]
        IPAddress clientIP = IPAddress.Parse(setting.ClientIPAddress);
        EndPoint clientEndPoint = new IPEndPoint(clientIP, setting.ClientPortNumber);

        while (true) 
        {
            Console.WriteLine("Server on port 3200 is listening...");

            // TODO:[Receive and print Hello]
            ReceiveMessage(socket, ref clientEndPoint);
            
            // TODO:[Send Welcome to the client]
            var messageWelcome = new Message()
            {
                MsgId = 2,
                MsgType = MessageType.Welcome,
                Content = "Welcome from DNS server"
            };
            SendMessage(socket, clientEndPoint, messageWelcome);
        }

        // TODO:[Receive and print DNSLookup]


        // TODO:[Query the DNSRecord in Json file]

        // TODO:[If found Send DNSLookupReply containing the DNSRecord]



        // TODO:[If not found Send Error]


        // TODO:[Receive Ack about correct DNSLookupReply from the client]


        // TODO:[If no further requests receieved send End to the client]

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