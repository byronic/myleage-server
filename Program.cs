using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MileageServer
{
    class Server
    {
        // VERSIONING.
        public const int MajorVersion = 1;
        public const int MinorVersion = 2;
        public const int Build = 3352;

        public static bool close = false; // true if server needs to stop

        // other fun configury things
        public static string MOTD = "Welcome to WMileage v" + MajorVersion.ToString() + "." + MinorVersion.ToString() + "." + Build.ToString() + "\n\r" + Connection.MSGCYN + "by byron lagrone" + Connection.MSGCLR + "\n\r\n\r";
        const int PortNumber = 8888; // change if we need to change the port
        const int BacklogSize = 20;

        static void Main(string[] args)
        {
            System.Console.WriteLine("WMileage Server, v" + MajorVersion.ToString() + "." + MinorVersion.ToString() + "." + Build.ToString());

            // create sockets and start listening!
            Socket server = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);
            server.Bind(new IPEndPoint(IPAddress.Any, PortNumber));
            server.Listen(BacklogSize);
            System.Console.WriteLine("Server Started!");
            while (!close)
            {
                Socket conn = server.Accept();
                if (close)
                {
                    server.Close();
                    break;
                }
                new Connection(conn);
            }
            System.Console.WriteLine("Server has stopped. Press any key to exit.");
            System.Console.ReadKey();
        }

        public static void bug(string username, string report)
        {
            // someone (either a user or an admin) has reported a bug! OH NO!!!!!
            try
            {
                System.IO.StreamWriter file = new System.IO.StreamWriter("bugs.txt", true);
                file.WriteLine("[" + username + "]: " + report);
                file.Close();
            }
            catch (Exception)
            {
                err(username + " tried to put in the bug: " + report + " ... but it failed!");
            }
        }

        public static void err(string msg)
        {
            System.Console.WriteLine("[ERR]: " + msg);
        }

        public static void msg(string message)
        {
            System.Console.WriteLine(":: " + message);
        }
    }

    class Connection
    {
        // ANSI color code definitions for supported clients; don't forget to send MSGCLR at the end of a line or their telnet sticks the color!
        public static char ESC = '\x1B';
        public static string MSGRED = ESC + "[1m" + ESC + "[31m";
        public static string MSGGRN = ESC + "[1m" + ESC + "[32m";
        public static string MSGYEL = ESC + "[1m" + ESC + "[33m";
        public static string MSGBLU = ESC + "[1m" + ESC + "[34m";
        public static string MSGPUR = ESC + "[1m" + ESC + "[35m";
        public static string MSGCYN = ESC + "[1m" + ESC + "[36m";
        public static string MSGCLR = ESC + "[0m";

        // if the client supports colors, go ahead and initialize them
        /*void initColors(bool support)
        { For legacy purposes, here are the colors from before we removed support :(
            if (support)
            {
                ESC = '\x1B';
                MSGRED = ESC + "[1m" + ESC + "[31m";
                MSGGRN = ESC + "[1m" + ESC + "[32m";
                MSGYEL = ESC + "[1m" + ESC + "[33m";
                MSGBLU = ESC + "[1m" + ESC + "[34m";
                MSGPUR = ESC + "[1m" + ESC + "[35m";
                MSGCYN = ESC + "[1m" + ESC + "[36m";
                MSGCLR = ESC + "[0m";
            }
        } */
        // end ANSI color code definitions

        // static client databases
        static int clients = 0; // total number of clients connected
        public static ArrayList names = new ArrayList(); // names[clientID] would return the correct one from a local instance
        
        // UI declaratives
        public static string menu = "\r\n\r\n" + MSGCYN + "WMileage v" + Server.MajorVersion.ToString() + "." + Server.MinorVersion.ToString() + "." + Server.Build.ToString() + MSGCLR + "\n\r1) New Mileage or Expense\n\r2) View Current Totals\r\n3) Edit Entries\r\n4) View Past Totals\r\n5) End Current Expense Period and Archive\r\n6) Report a Problem or Request a Feature\r\n7) Exit WMileage\r\n";

        // under the hood
        static object BigLock = new object();
        Socket socket;
        public StreamReader Reader;
        public StreamWriter Writer;
        static ArrayList connections = new ArrayList();
        string tempString; // I hate that this exists. He's for holding the password before entering reg code during account creation.

        // instance-specific client data
        public int clientID;
        public int clientMODE = 0; // what mode are we in?
        Account account = new Account();
        Miles entry; // a mileage or expense entry

        public Connection(Socket socket)
        {
            this.socket = socket;
            Reader = new StreamReader(new NetworkStream(socket, false));
            Writer = new StreamWriter(new NetworkStream(socket, true));
            new Thread(ClientLoop).Start();
        }

        void ClientLoop()
        {
            try
            {
                lock (BigLock)
                {
                    OnConnect();
                }
                string line = " ";
                while (!Server.close)
                {
                    lock (BigLock)
                    {
                        foreach (Connection conn in connections)
                        {
                            conn.Writer.Flush();
                        }
                    }
                    try
                    {
                        line = Reader.ReadLine();
                        if (line == null)
                            break;
                    }
                    catch(Exception)
                    {
                        break; // we disconnected!
                    }
                    lock (BigLock)
                    {
                        ProcessLine(line);
                    }
                }
            }
            finally
            {
                lock (BigLock)
                {
                    socket.Close();
                    OnDisconnect();
                }
            }
        }

        void OnConnect()
        {
            clientID = clients;
            clients++;
            names.Add("");
            System.Console.WriteLine("Client " + clientID.ToString() + " has connected.");
            Writer.WriteLine(Server.MOTD);
            Writer.Write("\n\r Press <ENTER> to log in, or type a username to begin creating an account.\n\r\n\r");
            connections.Add(this);
        }

        void OnDisconnect()
        {
            System.Console.WriteLine("Client " + clientID.ToString() + " has disconnected.");
            connections.Remove(this);
            if (connections.Count == 0)
            {
                System.Console.WriteLine("No active connections.");
            }
        }

        void ProcessLine(string line)
        {
            switch (clientMODE)
            {
                case -11: // enter registration code
                    {
                        tempString = line.Trim();
                        Writer.Write("Enter your registration code: ");
                        clientMODE = -10;
                    } break;
                case -10: // create a new account!
                    {
                        switch (account.create(names[clientID].ToString(), tempString, line.Trim()))
                        {
                            case 0:
                                {
                                    Writer.WriteLine("Success! Your account has been created.\n\r\n\r" + MSGGRN + "Logged in!\n\r" + MSGCLR);
                                    clientMODE = 100;
                                } break;
                            case 2:
                                {
                                    Writer.WriteLine("An account with that name already exists.\n\r\n\r Press <ENTER> to log in, or type a username to begin creating an account.\n\r\n\r");
                                    clientMODE = 0;
                                } break;
                            case 3:
                                {
                                    Writer.WriteLine("That registration code appears invalid.\n\r\n\r Press <ENTER> to log in, or type a username to begin creating an account.\n\r\n\r");
                                    clientMODE = 0;
                                } break;
                            default:
                                {
                                    Writer.WriteLine("An unknown error occurred.\n\rIf it persists, please contact Byronic support.\n\r\n\r Press <ENTER> to log in, or type a username to begin creating an account.\n\r\n\r");
                                    clientMODE = 0;
                                } break;
                        }
                    } break;
                case -2: // logon -- username stage
                    {
                        // the line received should be username
                        names[clientID] = line.Trim();
                        Writer.Write("Password: ");
                        clientMODE = -1; // wait for password
                    } break;
                case -1:
                    {  // logon -- password stage
                        switch (account.login(names[clientID].ToString(), line.Trim()))
                        {
                            case 0:
                                {
                                    if (account.admin)
                                        clientMODE = 8888;
                                    else
                                        clientMODE = 100;
                                    Writer.WriteLine(menu);
                                } break;
                            default:
                                {
                                    Writer.Write(MSGRED + "Failed to authenticate. " + MSGCLR + "Check your username and password, then try again.\n\r\n\r Press <ENTER> to log in, or type a username to begin creating an account.\n\r\n\r");
                                    clientMODE = 0;
                                } break;
                        }
                    } break;
                case 0:
                    {
                        string inp = line.Trim();
                        if (inp == "")
                        {
                            Writer.Write("Username: ");
                            clientMODE = -2; // logon time
                        }
                        else
                        {
                            names[clientID] = inp;
                            Writer.Write("Enter a password for this account: ");
                            clientMODE = -11; // create new account
                        }
                    } break;
                case 100: // normal mode, chat and whatever
                    {
                        try
                        {
                            clientMODE += Int16.Parse(line);
                        }
                        catch(Exception)
                        {
                            Writer.Write(menu + MSGRED + "I didn't recognize " + line + " as valid input. Sorry!\r\n" + MSGCLR);
                        }
                        if (clientMODE > 107 || clientMODE < 101)
                            clientMODE = 100;
                        switch (clientMODE)
                        {
                            case 100: Writer.Write(menu + MSGRED + "I didn't recognize " + line + " as valid input. Sorry!\r\n" + MSGCLR);
                                break;
                            case 101: Writer.WriteLine("\r\n\r\n\r\n\r\nEnter a plain number for mileage.\r\nEnter a $ plus a number to record an expense.\r\nOr, just press <ENTER> to cancel.");
                                break;
                            case 102: // view current totals
                                Writer.WriteLine("\r\n\r\n\r\n" + DataManager.getCurrentTotals(names[clientID].ToString()) + "\r\nPress <ENTER> to continue\r\n");
                                break;
                            case 103: // edit entries
                                {
                                    Writer.WriteLine("\r\n\r\n\r\n\r\n" + DataManager.getCurrentEntries(names[clientID].ToString()));
                                    Writer.WriteLine("\r\nTo delete an entry, enter its ID.\r\nLeave blank and press <ENTER> to return to the menu.\r\n");
                                } break;
                            case 104: // view past totals
                                {
                                    Writer.WriteLine("\r\n\r\n\r\n\r\n" + DataManager.getArchive(names[clientID].ToString()));
                                    Writer.WriteLine("\r\nPress <ENTER> to continue...\r\n\r\n");
                                } break;
                            case 105: // end current period
                                {
                                    Writer.WriteLine(MSGRED + "\r\n\r\n   Archiving totals cannot be undone.  \r\n    Are you certain(y/n)?\r\n\r\n" + MSGCLR);
                                } break;
                            case 106: // reporting a problem!
                                {
                                    Writer.WriteLine(MSGCYN + "\r\n\r\n\r\n   Report a problem or leave a comment! \r\n\r\n What's on your mind?\r\n\r\n" + MSGCLR);
                                } break;
                            case 107: // exit!
                                {
                                    socket.Close();
                                    OnDisconnect();
                                } break;
                        }

                    } break;
                case 101: // mileage or expense was entered
                    {
                        entry = new Miles();
                        int num = 0;
                        // for now, just spit it back at them
                        if (line == "")
                        {
                            clientMODE = 100;
                            Writer.WriteLine(menu);
                        }
                        else if (line.StartsWith("$"))
                        {
                            try
                            {
                                num = Int16.Parse(line.TrimStart('$'));
                                entry.start(names[clientID].ToString(), true, num);
                                Writer.WriteLine("\r\nEnter a comment about the expense (it can be blank), then <ENTER>.\r\n");
                                clientMODE = 151;
                            }
                            catch (Exception)
                            {
                                Writer.WriteLine(menu + MSGRED + "\r\n I didn't understand that input :( \r\n" + MSGCLR);
                                clientMODE = 100;
                            }
                        }
                        else
                        {
                            try
                            {
                                num = Int16.Parse(line);
                                entry.start(names[clientID].ToString(), false, num);
                                Writer.WriteLine("\r\nEnter a comment about the mileage (it can be blank), then <ENTER>.\r\n");
                                clientMODE = 151;
                            }
                            catch (Exception)
                            {
                                Writer.WriteLine(menu + MSGRED + "\r\n I didn't understand that input :( \r\n" + MSGCLR);
                                clientMODE = 100;
                            }
                        }
                    } break;
                case 151: // mileage or expense got a comment
                    {
                        // since the comment is done, that should be all we need.
                        entry.finish(line);
                        Writer.WriteLine(menu);
                        clientMODE = 100;
                    } break;
                case 102: // just got done displaying current totals, return to menu
                    {
                        Writer.WriteLine(menu);
                        clientMODE = 100;
                    }
                    break;
                case 104: // just got done displaying archived totals, return to menu
                    {
                        Writer.WriteLine(menu);
                        clientMODE = 100;
                    } break;
                case 105: // just asked the user if they were CERTAIN they wished to archive
                    {
                        if (line == "y")
                        {
                            // they said they're sure!
                            Writer.WriteLine("\r\n\r\nOK. Please enter a friendly name for this set of totals.\r\nYou can enter anything.\r\nMay I suggest using the date range?\r\n");
                            clientMODE = 1050;
                        }
                        else
                        {
                            Writer.WriteLine(menu + MSGRED + "You canceled the archive. No archiving was done.\r\n" + MSGCLR);
                            clientMODE = 100;
                        }
                    } break;
                case 106: // report a problem or request a feature!
                    {
                        Server.bug(names[clientID].ToString(), line);
                        Writer.WriteLine(menu + MSGCYN + "\r\n Your feedback has been posted. \r\n" + MSGCLR);
                        clientMODE = 100;
                    } break;
                case 1050: // time to archive!!!
                    {
                        if(DataManager.endCurrentPeriod(names[clientID].ToString(), line))
                            Writer.WriteLine(menu + MSGCYN + "\r\n Archiving is completed!\r\n" + MSGCLR);
                        else
                            Writer.WriteLine(menu + MSGRED + "\r\n Oh no, something went wrong while archiving! :(\r\n" + MSGCLR);
                        clientMODE = 100;
                    } break;
                case 103: // entry deletion
                    {
                        if (line == "")
                        {
                            Writer.WriteLine(menu);
                            clientMODE = 100;
                        }
                        else
                        {
                            try
                            {
                                int var = Int16.Parse(line);
                                if (DataManager.deleteEntry(names[clientID].ToString(), var))
                                {
                                    Writer.WriteLine(DataManager.getCurrentEntries(names[clientID].ToString()) + MSGCYN + "\r\n\r\nEntry deleted.\r\n" + MSGCLR);
                                    Writer.WriteLine("\r\nTo delete another entry, enter its ID.\r\nLeave blank and press <ENTER> to return to the menu.\r\n");
                                }
                                else
                                {
                                    Writer.WriteLine(DataManager.getCurrentEntries(names[clientID].ToString()) + MSGRED + "\r\n\r\nFailed to delete!!!!\r\n" + MSGCLR);
                                    Writer.WriteLine("\r\nTo delete an entry, enter its ID.\r\nLeave blank and press <ENTER> to return to the menu.\r\n");
                                }
                            }
                            catch (Exception)
                            {
                                Writer.WriteLine("\r\nI didn't recognize that input. :(\r\nPlease try again.\r\n");
                                Writer.WriteLine("\r\nTo delete an entry, enter its ID.\r\nLeave blank and press <ENTER> to return to the menu.\r\n");
                            }
                        }
                    }
                    break;
                case 8888: // admin mode!
                    {
                        if (line.StartsWith("/"))
                        {
                            // pretend we requested status
                            // remember to add the function later
                            Writer.WriteLine(names[clientID].ToString());
                        }
                        else if (line.StartsWith(".serversay"))
                        {
                            // serversay is a server broadcast command, the message goes to all clients (even non-authenticated users)
                            foreach (Connection conn in connections)
                            {
                                conn.Writer.WriteLine(MSGRED + "[ SERVER ]: " + line.Remove(0, 11) + MSGCLR);
                            }
                        }
                        else if (line.StartsWith(".serversave"))
                        {
                            
                        }
                        else if (line.StartsWith(".servershutdown"))
                        {
                            //shutdown the server -- note in manual that admin should make sure to perform a save before doing this!
                            // because you can't do a foreach connection loop with a broadcast and then set .close to true
                            Server.close = true;
                        }
                        else
                        {
                            // it must just be chat!
                            foreach (Connection conn in connections)
                            {
                                if (conn.clientMODE > 99) // anything above 99 is considered to be 'receiving normal chat messages'
                                { //note that admins get to be cyan, my favorite color
                                    conn.Writer.WriteLine(MSGCYN + "[" + names[clientID] + "]: " + MSGCLR + line.Trim());
                                }
                            }
                        }
                    } break;
            }
        }
    }
}
