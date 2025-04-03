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
            bool currentconnection = false;
            
            socket.ReceiveTimeout = 0;
            //in deze while loop zoekt server naar client voor connection
            while (!connection)
            {
                var clientMessage = ReceiveMessage(socket, ref clientEndPoint);

                //checken of eerste message hello van client is
                if (clientMessage.MsgType != MessageType.Hello)
                {
                    var errorMessage = new Message()
                    {
                        MsgId = 7534445,
                        MsgType = MessageType.Error,
                        Content = "Domain not found"
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
                        Content = "Welcome from server"
                    };
                    SendMessage(socket, clientEndPoint, messageWelcome);
                }

                connection = true;
                currentconnection = true;
                
                socket.ReceiveTimeout = 10000;
                //na een goede handshake wordt met deze while loop de connection vastgezet totdat server end message verstuurt
                while (currentconnection)
                {
                    var newmessage = ReceiveMessage(socket, ref clientEndPoint);

                    if (newmessage.MsgType == MessageType.End) 
                    {
                        var endMessage = new Message()
                        {
                            MsgId = 91377,
                            MsgType = MessageType.End,
                            Content = "End of DNSLookup"
                        };
                        SendMessage(socket, clientEndPoint, endMessage);

                        currentconnection = false;
                        break;
                    }
                    
                    // hier ontvangen we nieuwe message van client (dnslookup)
                    if (newmessage.MsgType == MessageType.DNSLookup)
                    {

                        //zoeken naar dnsrecord
                        string clientrequest = null;
                        bool isValidFormat = true;
                        
                        try 
                        {
                            clientrequest = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize(newmessage.Content));
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine("Invalid request format received");
                            isValidFormat = false;
                            
                            var errorMessage = new Message()
                            {
                                MsgId = 7534445,
                                MsgType = MessageType.Error,
                                Content = "Domain not found"
                            };
                            SendMessage(socket, clientEndPoint, errorMessage);
                            continue;
                        }
                        
                        if (isValidFormat) 
                        {
                            bool found = false;
                            foreach (var temp in records)
                            {   
                                if (temp.Name == clientrequest)
                                {
                                    //send DNSLookupReply message here with same MsgId as original request
                                    var DNSReply = new Message()
                                    {
                                        MsgId = newmessage.MsgId,
                                        MsgType = MessageType.DNSLookupReply,
                                        Content = temp
                                    };
                                    SendMessage(socket, clientEndPoint, DNSReply);

                                    //hier ontvangen we de ack van de client, doen we niks mee
                                    var ack = ReceiveMessage(socket, ref clientEndPoint);
                                    System.Console.WriteLine($"received ack id: {ack.Content}");
                                    found = true;
                                    break;
                                }
                            }

                            //als DNSRecord niet gevonden is:
                            if (!found) 
                            {
                                var errorMessage = new Message()
                                {
                                    MsgId = 7534445,
                                    MsgType = MessageType.Error,
                                    Content = "Domain not found"
                                };
                                SendMessage(socket, clientEndPoint, errorMessage);
                            }
                        }
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

        try 
        {
            int bytesReceived = socket.ReceiveFrom(buffer, ref remoteEndPoint);
            string jsonString = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            
            Message receivedMessage = JsonSerializer.Deserialize<Message>(jsonString);
            
            Console.WriteLine($"Received message: Type={receivedMessage.MsgType}, ID={receivedMessage.MsgId}");
            
            return receivedMessage;
        } 
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            Console.WriteLine("Send end message, clossing connection...");
            return new Message
            {
                MsgId = -1,
                MsgType = MessageType.End,
                Content = "End message from server"
            };
        }
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