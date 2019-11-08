using System;
using System.Net;
using System.Collections.Generic;

namespace SGame
{
    class Program
    {

        int freeID = 0;
        Dictionary<string,int> players = new Dictionary<string,int>();

    public void ConnectPlayer(HttpListenerResponse response)
    {
        int playerID = freeID;
        freeID++;
        string playerToken = Guid.NewGuid().ToString();
        players[playerToken] = playerID;
        string responseString = "<HTML><BODY>Connected player, token = " + playerToken + "</BODY></HTML>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer,0,buffer.Length);
        output.Close();
    }

    public void DisconnectPlayer(HttpListenerResponse response, string token)
    {
        string responseString = "<HTML><BODY>";
        responseString += "Request to disconnect player with token " + token + "<br>";
        if(players.ContainsKey(token))
        {
            players.Remove(token);
            responseString += "Disconnected.<br>";
        }
        else
        {
            responseString += "Player with such token is not connected.<br>";
        }
        responseString += "</BODY></HTML>";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer,0,buffer.Length);
        output.Close();
    }

    // This example requires the System and System.Net namespaces.
    public void SimpleListenerExample(string[] prefixes)
    {
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine ("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
            throw new ArgumentException("prefixes");
            
            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }
            listener.Start();
            while(true)
            {

            
                Console.WriteLine("Listening...");
                // Note: The GetContext method blocks while waiting for a request. 
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.
                string requestUrl = request.RawUrl.Substring(1);
                if(requestUrl == "exit")
                    break;
                else if (requestUrl == "connect")
                    ConnectPlayer(response);
                else if (requestUrl.StartsWith("disconnect"))
                    DisconnectPlayer(response, new string(requestUrl.Split("/")[1]));
            }
            listener.Stop();
        }

        static void Main(string[] args)
        {
            string[] endpoints = new string[]{"http://localhost:8000/"}; 
            Program P = new Program();
            P.SimpleListenerExample(endpoints);
        }
    }
}
