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
        var serverEndPoint = new IPEndPoint(serverIP, setting.ServerPortNumber);

        byte[] receiveBuffer = new byte[1024]; 
        EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
        int bytesReceived = socket.ReceiveFrom(receiveBuffer, ref senderEndPoint);
        string receivedMessage = Encoding.ASCII.GetString(receiveBuffer, 0, bytesReceived);
        Console.WriteLine($"Received from server: {receivedMessage}");
        //TODO: [Receive and print Welcome from server]

        // TODO: [Create and send DNSLookup Message]


        //TODO: [Receive and print DNSLookupReply from server]


        //TODO: [Send Acknowledgment to Server]

        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

        //TODO: [Receive and print End from server]





    }
}