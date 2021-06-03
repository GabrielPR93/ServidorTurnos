using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServidorTurnos
{
    class Program
    {
        static void Main(string[] args)
        {
            TurnsServer turnsServer = new TurnsServer();
            turnsServer.init();

        }
    }
}
