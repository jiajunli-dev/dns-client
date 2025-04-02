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
    static string filepath = "DNSrecords.json";
    static string dnsContent = File.ReadAllText(filepath); 
    static List<DNSRecord>? records = JsonSerializer.Deserialize<List<DNSRecord>>(dnsContent);

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


            bool connection = false;
            //in deze while loop zoekt server naar client voor connection
            while (!connection)
            {

                var clientMessage = ReceiveMessage(socket, ref clientEndPoint);

                //checken of eerste message hello van client is
                if (clientMessage.MsgType != MessageType.Hello)
                {
                    var errorMessage = new Message()
                    {
                        MsgId = 401,
                        MsgType = MessageType.Error,
                        Content = "New clients need to send an hello msg first"
                    };
                    SendMessage(socket, clientEndPoint, errorMessage);
                }
                
                //als eerste message daadwerkelijk hello van client is, verstuurt de server een welcome en is de handshake compleet
                else
                {
                    var messageWelcome = new Message()
                    {
                        MsgId = 2,
                        MsgType = MessageType.Welcome,
                        Content = "Welcome from DNS server"
                    };
                    SendMessage(socket, clientEndPoint, messageWelcome);
                }
                
                bool currentconnection = true;
                //na een goede handshake wordt met deze while loop de connection vastgezet totdat server end message verstuurt
                while (currentconnection)
                {
                    var newmessage = ReceiveMessage(socket, ref clientEndPoint);

                    // psuedocode:
                    // timer.start;
                    // if (timer.time == 10 sec)
                    //{
                    //      sendmessage(endmessage);
                    //      currentconnection = false;
                    //}

                    //deze code hieronder moet uiteindelijk weg, zet hierboven een timer voor het ontvangen van een nieuwe message. als timer verloopt dan stuurt server end message//
                    if (clientMessage.MsgType == MessageType.End)
                    {
                        connection = true;

                        var endMessage = new Message()
                        {
                            MsgId = 5,
                            MsgType = MessageType.End,
                            Content = "Connection terminated"
                        };
                        
                        SendMessage(socket, clientEndPoint, endMessage);
                        currentconnection = false;
                    }
                    ///////////////////////////////////////////////////////////////
                    

                    if (clientMessage.MsgType == MessageType.DNSLookup)
                    {

                        //zoeken naar dnsrecord
                        var clientrequest = clientMessage.Content as DNSRecord;
                        foreach (var temp in records)
                        {
                            if (temp.Name == clientrequest.Name && temp.Type == clientrequest.Type)
                            {
                                //send DNSLookupReply message here with same MsgId as original request
                                var DNSReply = new Message()
                                {
                                    MsgId = clientMessage.MsgId,
                                    MsgType = MessageType.DNSLookupReply,
                                    Content = temp
                                };
                                SendMessage(socket, clientEndPoint, DNSReply);

                                //hier ontvangen we de ack van de client, doen we niks mee
                                var ack = ReceiveMessage(socket, ref clientEndPoint);
                            }
                        }

                        //als DNSRecord niet gevonden is:
                        var errorMessage = new Message()
                        {
                            MsgId = 401,
                            MsgType = MessageType.Error,
                            Content = $"Unable to find Record with name: {clientrequest.Name} and type: {clientrequest.Type}"
                        };
                        SendMessage(socket, clientEndPoint, errorMessage);
                    }
                }
            }
        }

        // TODO:[Receive and print DNSLookup] -----> done


        // TODO:[Query the DNSRecord in Json file] -----> done

        // TODO:[If found Send DNSLookupReply containing the DNSRecord] -----> done



        // TODO:[If not found Send Error] done


        // TODO:[Receive Ack about correct DNSLookupReply from the client] -----> done


        // TODO:[If no further requests receieved send End to the client] -----> alleen nog timer om connection met server te eindigen

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