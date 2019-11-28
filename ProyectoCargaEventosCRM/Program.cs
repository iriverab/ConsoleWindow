using ProyectoCargaEventosCRM.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProyectoCargaEventosCRM
{
    class Program
    {
        static void Main(string[] args)
        {
            DataController call = new DataController();
            call.ProcesarEventos();
        }
    }
}
