// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");

using Communications;
using Microsoft.Extensions.Logging.Abstractions;
using StarterCode;

WebServer server = new WebServer();

Networking channel = new Networking(NullLogger.Instance, WebServer.onClientConnect, WebServer.onDisconnect, WebServer.onMessage, '\n');
channel.WaitForClients(11001, false);

//"localhost:11001/1/2/3/4/5/";

Console.ReadLine();