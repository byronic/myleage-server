using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MileageServer
{
    class Account
    {
        public bool authenticated = false; // returns true if user is logged on; also if user's password changes could react instantly
        public bool admin = false; // is the user a server admin?

        public int create(string user, string pass, string reg)
        {
            // auto generated accounts can't be admins
            // check to see if account exists...
            try
            {
                System.IO.StreamReader test = new System.IO.StreamReader("Accounts\\" + user + ".account");
                // if we made it this far, the file exists
                test.Close();
                Server.err("Attempted to create account " + user + ", but it already existed.");
                return 2; // account already exists
            }
            catch (System.IO.FileNotFoundException)
            {
                // the account doesn't exist!
                // is the registration code valid?
                try
                {
                    System.IO.StreamReader test = new System.IO.StreamReader("Serials\\" + reg);
                    // if we made it this far, the file exists -- which means valid reg code! yay!!!!
                    test.Close();
                    System.IO.File.Delete("Serials\\" + reg);
                    System.IO.StreamWriter file = new System.IO.StreamWriter("Accounts\\" + user + ".account");
                    file.WriteLine(pass);
                    file.WriteLine("user");
                    file.Close();
                    Server.msg("user " + user + " created account successfully!");
                    return 0; // success!
                }
                catch (Exception)
                {
                    Server.err("Attempted to register account " + user + " with reg code " + reg + ", but the reg code wasn't valid.");
                    return 3;
                }
            }
            catch (Exception)
            {
            }
            Server.err("Encountered a generic error when creating an account.");
            return 1; // generic error
        }

        public int login(string user, string pass)
        {
            try
            {
                string[] file = System.IO.File.ReadAllLines("Accounts\\" + user + ".account");
                if (file[0] == pass)
                {
                    authenticated = true;
                    if (file[1] == "admin")
                        admin = true;
                    Server.msg("user " + user + " logged on successfully!");
                    return 0; // success!
                }

            }
            catch (System.IO.FileNotFoundException)
            {
                // the account doesn't exist!
                Server.err("A user tried to log in as " + user + ", a non-existent account.");
                return 2;
            }
            catch (Exception e)
            {
                Server.err(e.ToString());
                return 3; // could be a corrupt user file or something funkified
            }
            return 1;
        }
    }
}
