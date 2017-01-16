﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RTLSProvider.ItemSense;

namespace RTLSProvider
{
    class Program
    {
        static int Main(string[] args)
        {
            bool install = false,
                uninstall = false,
                console = false,
                rethrow = false;

            try
            {
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "-i":
                        case "-install":
                            install = true;
                            break;
                        case "-u":
                        case "-uninstall":
                            uninstall = true;
                            break;
                        case "-c":
                        case "-console":
                            console = true;
                            break;
                        case "-t":
                            var x = new RtlsMessage()
                            {
                                Epc = "234556",
                                TimeStamp = DateTime.Now.ToUniversalTime(),
                                ItemSenseZone = "zomeeererer"
                            };
                            Console.WriteLine(JsonConvert.SerializeObject(x));
                            return 0;
                        default:
                            Console.Error.WriteLine
                                ("Argument not expected: " + arg);
                            break;
                    }
                }

                if (uninstall)
                {
                    Install(true, args);
                }
                if (install)
                {
                    Install(false, args);
                }
                if (console)
                {
                    using (var process = new ConnectorService())
                    {
                        Console.WriteLine("Starting...");
                        process.Startup();
                        Console.WriteLine("System running; press any key to stop");
                        Console.ReadKey(true);
                        process.Shutdown();
                        Console.WriteLine("System stopped");
                    }
                }
                else if (!(install || uninstall))
                {
                    rethrow = true; // so that windows sees error...
                    ServiceBase[] services =
                        {new ConnectorService()};
                    ServiceBase.Run(services);
                    rethrow = false;
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (rethrow) throw;
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "Swallow the rollback exception and let" +
                            "the real cause bubble up.")]
        internal static void Install(bool undo, string[] args)
        {
            try
            {
                Console.WriteLine(undo ? "uninstalling" : "installing");

                using (AssemblyInstaller inst =
                    new AssemblyInstaller(typeof(Program).Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        if (undo)
                        {
                            inst.Uninstall(state);
                        }
                        else
                        {
                            inst.Install(state);
                            inst.Commit(state);
                        }
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch
                        {
                            // Swallow the rollback exception
                            // and let the real cause bubble up.
                        }

                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        private void SendMessage()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "impinj",
                Password = "It3ms3ns3"
            };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "locate", durable: false, exclusive: false, autoDelete: false,
                        arguments: null);
                    var message = "Hello World";
                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: "", routingKey: "locate", basicProperties: null, body: body);
                    Console.WriteLine(" Sent {0}", message);
                }
            }
            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }
    }
}