using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServidorTurnos
{
    class TurnsServer
    {

        private string[] students;
        private string teacherPass;
        private int[] ports = { 135, 0 };
        private List<string> queue = new List<string>();
        public static readonly object l = new object();
        public Socket s;
        public IPEndPoint ie;
        public string usuario;
        private int TestPort(string puerto)
        {

            int puertoValido = Convert.ToInt32(puerto);

            if (puertoValido >= IPEndPoint.MinPort && puertoValido <= IPEndPoint.MaxPort)
            {
                return puertoValido;
            }

            return -1;
        }

        private bool ReadData()
        {
            string linea;
            bool correcto = true;
            int puerto;
            int cont = 0;
            try
            {

                using (StreamReader sr = new StreamReader(Environment.GetEnvironmentVariable("USERPROFILE") + "/list.txt"))
                {

                    while ((linea = sr.ReadLine()) != null)
                    {
                        cont++;
                        if (cont == 1)
                        {
                            students = linea.ToLower().Split(',');

                        }
                        else
                        {
                            if (cont == 2)
                            {
                                teacherPass = linea;
                            }
                            else
                            {
                                puerto = TestPort(linea);
                                if (puerto != -1)
                                {
                                    ports[1] = puerto;
                                }
                                else
                                {
                                    correcto = false;
                                }
                            }
                        }

                    }
                    if (cont == 3)
                    {
                        correcto = true;
                    }
                    else
                    {
                        correcto = false;
                    }
                }
            }
            catch (IOException e)
            {

                Console.WriteLine(e.Message);

                correcto = false;
            }
            return correcto;

        }

        public void init()
        {
            bool flagPuerto = true;
            bool flag = true;
            bool flagCorrecto = true;
            int puerto = ports[0];


            if (ReadData() != false)
            {

                while (flagPuerto)
                {
                    s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    ie = new IPEndPoint(IPAddress.Any, puerto);
                    flagPuerto = false;
                    try
                    {
                        s.Bind(ie);
                        s.Listen(3);
                        Console.WriteLine("Conectado al puerto " + ie.Port);
                    }
                    catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                    {

                        if (!flag)
                        {
                            Console.WriteLine("Ningun puerto libre");
                            flagCorrecto = false;

                        }
                        else
                        {
                            Console.WriteLine("Puerto {0} ocupado", puerto);
                            puerto = ports[1];
                            flag = false;
                            flagPuerto = true;

                        }

                    }

                }
                while (flagCorrecto)
                {
                    Socket sCliente = s.Accept();
                    Thread hilo = new Thread(Client);
                    hilo.Start(sCliente);
                }

            }
        }

        public void Client(object socket)
        {
            bool conexion = true;
            bool profesor = true;
            string comando;
            Socket scliente = (Socket)socket;
            IPEndPoint ieCLiente = (IPEndPoint)scliente.RemoteEndPoint;

            using (NetworkStream ns = new NetworkStream(scliente))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {
                sw.WriteLine("Introduce nombre de usuario");
                sw.Flush();

                while (conexion)
                {
                    try
                    {

                        usuario = sr.ReadLine();
                        if (usuario != null)
                        {
                            if (students.Contains(usuario))//Si es estudiante
                            {
                                sw.WriteLine("Introduce comando");
                                sw.Flush();

                                comando = sr.ReadLine();
                                if (comando != null)
                                {

                                    if (comando == "add")
                                    {
                                        lock (l)
                                        {
                                            if (!queue.Contains(usuario))
                                            {
                                                queue.Add(usuario);
                                                sw.WriteLine("Eres el numero {0} en la lista de espera", queue.IndexOf(usuario) + 1);
                                                sw.Flush();
                                                conexion = false;
                                                scliente.Close();
                                            }
                                            else
                                            {
                                                sw.WriteLine("Ya estas en la lista");
                                                sw.Flush();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        scliente.Close();
                                        conexion = false;
                                    }
                                }
                            }
                            else if (usuario.Equals(teacherPass))// Si es profesor (ADMIN)
                            {
                                sw.WriteLine("Introduce comando");
                                sw.Flush();

                                while (profesor)
                                {
                                    comando = sr.ReadLine();
                                    if (comando != null)
                                    {
                                        if (comando.IndexOf(" ") != -1) //Comprobamos si hay un espacio
                                        {
                                            string[] numeros = comando.Split(' ');

                                            try
                                            {
                                                int num1 = Convert.ToInt32(numeros[1]);
                                                int num2 = Convert.ToInt32(numeros[2]);

                                                lock (l)
                                                {

                                                    if (queue.Count() >= num2)
                                                    {
                                                        queue.RemoveRange(num1, num2 - num1 + 1); //+1 para incluir el ultimo --
                                                    }

                                                }
                                            }
                                            catch (FormatException)
                                            {
                                                Console.WriteLine("Error de formato");
                                            }
                                            catch (OverflowException)
                                            {
                                                Console.WriteLine("Numero demasiado largo");
                                            }
                                            catch (ArgumentOutOfRangeException)
                                            {
                                                Console.WriteLine("Valor fuera de rango");

                                            }
                                        }
                                        else
                                        {
                                            switch (comando)
                                            {
                                                case "list":
                                                    foreach (string item in queue)
                                                    {
                                                        sw.WriteLine(item);
                                                        sw.Flush();
                                                    }
                                                    break;
                                                case "exit":
                                                    sw.WriteLine("Hasta pronto");
                                                    sw.Flush();
                                                    profesor = false;
                                                    conexion = false;
                                                    scliente.Close();
                                                    break;
                                                default:
                                                    break;
                                            }

                                        }
                                    }

                                }
                            }
                            else
                            {
                                conexion = false; //Si no es ni alumno ni profesor
                                scliente.Close();
                            }

                        }
                    }
                    catch (IOException e)
                    {

                        Console.WriteLine(e.Message);

                    }
                }

            }
        }
    }
}
