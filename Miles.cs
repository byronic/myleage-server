using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MileageServer
{
    class Miles
    {
        bool expense = false; // true if expense, false if mileage
        int num; // the actual number! meat and potato-ey!
        string name; // the name of the user!

        public void start(string user, bool isExpense, int number)
        {
            name = user;
            expense = isExpense;
            num = number;
        }

        public void finish(string comment)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter("Data\\" + name + ".current", true);
            if (expense)
                file.WriteLine("$" + num.ToString());
            else
                file.WriteLine(num.ToString());
            file.WriteLine(comment);
            file.Close();
            Server.msg("User " + name + " wrote a new entry: " + num.ToString() + " " + comment);
        }
    }
}
