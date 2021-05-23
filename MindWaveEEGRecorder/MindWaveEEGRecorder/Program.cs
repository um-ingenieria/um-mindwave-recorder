using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;
using NeuroSky.ThinkGear;
using NeuroSky.ThinkGear.Algorithms;
using System.Data.SqlClient;

namespace MindWaveEEGRecorder
{
    class Program
    {
        static Connector connector;
        static SqlConnection Conexion = new SqlConnection("Data Source=DESKTOP-KQBRIL0\\SQLEXPRESS;Initial Catalog=UM_NEUROSKY;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False");
        static SqlCommand cmd;
        static string prueba;
        static double taim;

        string sourcePath;
        string targetPath;
        string sourceFile;
        string destFile;

        static string testName;
        static string testDate;
        static string testComments;
        static string portName;

        static double poorsignal = 0;
        static string path;
        static uint EegPowerMax = 0;
        static decimal TaskFamiliarityMax = 0;
        static decimal TaskFamiliarityMin = -1000;
        static decimal MentalEffortMax = 0;
        static decimal MentalEffortMin = 1000;
        static ulong AttentionTot = 0;
        static ulong MeditationTot = 0;
        static uint AttentionCount = 0;
        static uint MeditationCount = 0;
        static decimal MentalEffortTot = 0;
        static decimal TaskFamiliarityTot = 0;
        static uint DeltaTot = 0;
        static uint ThetaTot = 0;
        static uint Alpha1Tot = 0;
        static uint Alpha2Tot = 0;
        static uint Beta1Tot = 0;
        static uint Beta2Tot = 0;
        static uint Gamma1Tot = 0;
        static uint Gamma2Tot = 0;
        static uint MentalEffortCount = 0;
        static uint TaskFamiliarityCount = 0;
        static uint EegPowerCount = 0;


        public static void Main(string[] args)
        {
            Console.Write("This program connects to your NeuroSky MindWave EEG headset to record and display the data. Turn your headset on, ensure the USB dongle is plugged in, and make sure the ThinkGear Connector is running. Also ensure that the MindWave Manager recognizes your headset. " + Environment.NewLine + Environment.NewLine);

            Console.Write("Enter Test Name: ");
            testName = Console.ReadLine();
            testDate = DateTime.Now.ToString("M/d/yyyy");
            path = "..\\..\\..\\Tests\\" + testName + "-" + DateTime.Now.ToString("M-d-yyyy") + "\\";

            

            Console.Write("Enter summary of the test you are performing: ");
            testComments = Console.ReadLine();

            Console.WriteLine(Environment.NewLine + "When ready, enter the port name below and press Enter. If you're unsure, leave it blank and all ports will be scanned." + Environment.NewLine);
            Console.Write("Enter port name (e.g. COM4): ");
            portName = Console.ReadLine();

            // Initialize a new Connector and add event handlers
            connector = new Connector();
            connector.DeviceConnected += new EventHandler(OnDeviceConnected);
            connector.DeviceConnectFail += new EventHandler(OnDeviceFail);
            connector.DeviceValidating += new EventHandler(OnDeviceValidating);

            // Scan for devices across COM ports
            // The COM port named will be the first COM port that is checked.
            connector.ConnectScan(portName);

            connector.setBlinkDetectionEnabled(false);
            connector.setMentalEffortEnable(true);
            connector.setMentalEffortRunContinuous(true);
            connector.setTaskFamiliarityEnable(true);
            connector.setTaskFamiliarityRunContinuous(true);

            //Esto te pregunta por siempre si queres poner algo, hasta que le pones end
            while (true)
            {
                Console.Write("Comment: ");
                string runComment = Console.ReadLine();
                if (runComment != null)
                {
                    if (runComment == "end")
                    {
                        File.AppendAllText(path + "info.txt", "|" + EegPowerMax);
                        connector.Close();
                        Environment.Exit(0);
                    }
                    else
                    {
                        if(runComment == "fin")
                        {
                            Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
                        }
                        else
                        {
                            TimeSpan tt = DateTime.UtcNow - new DateTime(1970, 1, 1);
                            int secondsSinceEpoch = (int)tt.TotalSeconds;
                            File.AppendAllText(path + "comments.txt", secondsSinceEpoch + "|" + runComment + "\n");
                        }
                    }
                    
                    
                }
            }

            Thread.Sleep(43200000);

            connector.Close();
            Environment.Exit(0);

        }

        // Called when a device is connected 
        static void OnDeviceConnected(object sender, EventArgs e)
        {
            //Es un listener de eventos
            Connector.DeviceEventArgs de = (Connector.DeviceEventArgs)e;
            Console.WriteLine("Device found on: " + de.Device.PortName);

            //Te pide crear un archivo para cada session
            try
            {
                // Determine whether the directory exists. 
                if (Directory.Exists(path))
                {
                    Console.WriteLine("That path exists already. Restart the program and enter a different Test Name.");
                    Thread.Sleep(2000);
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
                Console.WriteLine("The directory was created successfully at {0}.", Directory.GetCreationTime(path));
            }
            catch (Exception ex)
            {
                Console.WriteLine("The process failed: {0}", ex.ToString());
            }
            finally { }

            //String myDocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //File.Create(myDocumentPath + "info.txt");
            File.AppendAllText(path + "info.txt", testName + "|" + testDate + "|" + testComments);
            
            try
            {
                string fileName = "results.html";
                string sourcePath = "";
                string targetPath = path;
                string sourceFile = System.IO.Path.Combine(sourcePath, fileName);
                string destFile = System.IO.Path.Combine(targetPath, fileName);
                System.IO.File.Copy(sourceFile, destFile, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong!");
            }
            finally { }


            Console.WriteLine("Recording Started.");

            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("To end the test, type \"end\" into the comment prompt and press Enter.");
            Console.ResetColor();

            Console.Write("Comment: ");
            de.Device.DataReceived += new EventHandler(OnDataReceived);
        }

        // Called when scanning fails
        static void OnDeviceFail(object sender, EventArgs e)
        {
            Console.WriteLine("No devices found! :(");
        }

        // Called when each port is being validated
        static void OnDeviceValidating(object sender, EventArgs e)
        {
            Console.WriteLine("Validating: ");
        }

        // Called when data is received from a device
        static void OnDataReceived(object sender, EventArgs e)
        {
            
            Device.DataEventArgs de = (Device.DataEventArgs)e;
            DataRow[] tempDataRowArray = de.DataRowArray;

            TGParser tgParser = new TGParser();
            tgParser.Read(de.DataRowArray);

            string[] keys = { "PoorSignal", "Attention", "Meditation", "MentalEffort", "TaskFamiliarity", "EegPowerDelta", "EegPowerTheta", "EegPowerAlpha1", "EegPowerAlpha2", "EegPowerBeta1", "EegPowerBeta2", "EegPowerGamma1", "EegPowerGamma2" };
            for (int i = 0; i < tgParser.ParsedData.Length; i++)
            {
                string row = tgParser.ParsedData[i]["Time"] + " ";
                Console.WriteLine("-");
                Console.WriteLine("------ TIME ------ -> "+tgParser.ParsedData[i]["Time"]);
                Console.WriteLine("-");
                taim = tgParser.ParsedData[i]["Time"];
                bool trip = false;
                foreach (string key in keys)
                {
                    if (tgParser.ParsedData[i].ContainsKey(key))
                    {
                        trip = true;
                        row += tgParser.ParsedData[i][key] + " ";

                        if (key == "PoorSignal" && tgParser.ParsedData[i][key] > 0)
                        {
                            Console.WriteLine("");
                            Console.WriteLine("There is a poor signal. Please adjust headset or check battery.");
                            Console.Write("Comment:");
                            poorsignal = tgParser.ParsedData[i][key];
                        }
                        if (key == "Attention")
                        {
                            AttentionTot = (uint)tgParser.ParsedData[i][key];
                            Console.WriteLine("AttentionTot: " + (uint)tgParser.ParsedData[i][key]);
                            AttentionCount++;
                        }
                        if (key == "Meditation")
                        {
                            MeditationTot = (uint)tgParser.ParsedData[i][key];
                            Console.WriteLine("MeditationTot: " + (uint)tgParser.ParsedData[i][key]);
                            MeditationCount++;
                        }
                        string subStr = key.Substring(0, 8);
                        if (subStr == "EegPower")
                        {
                            if (EegPowerMax < tgParser.ParsedData[i][key])
                            {
                                EegPowerMax = (uint)tgParser.ParsedData[i][key];
                            }
                            if (key == "EegPowerDelta")
                            {
                                EegPowerCount++;
                                DeltaTot = (uint)tgParser.ParsedData[i][key];
                                Console.WriteLine("Delta: " + (uint)tgParser.ParsedData[i][key]);

                                ThetaTot = (uint)tgParser.ParsedData[i]["EegPowerTheta"];
                                Console.WriteLine("Theta: " + (uint)tgParser.ParsedData[i]["EegPowerTheta"]);

                                Alpha1Tot = (uint)tgParser.ParsedData[i]["EegPowerAlpha1"];
                                Console.WriteLine("Alpha1Tot: " + (uint)tgParser.ParsedData[i]["EegPowerAlpha1"]);
                               
                                Alpha2Tot = (uint)tgParser.ParsedData[i]["EegPowerAlpha2"];
                                Console.WriteLine("Alpha2Tot: " + (uint)tgParser.ParsedData[i]["EegPowerAlpha2"]);

                                Beta1Tot = (uint)tgParser.ParsedData[i]["EegPowerBeta1"];
                                Console.WriteLine("Beta1Tot: " + (uint)tgParser.ParsedData[i]["EegPowerBeta1"]);

                                Beta2Tot = (uint)tgParser.ParsedData[i]["EegPowerBeta2"];
                                Console.WriteLine("Beta2Tot: " + (uint)tgParser.ParsedData[i]["EegPowerBeta2"]);

                                Gamma1Tot = (uint)tgParser.ParsedData[i]["EegPowerGamma1"];
                                Console.WriteLine("Gamma1Tot: " + (uint)tgParser.ParsedData[i]["EegPowerGamma1"]);

                                Gamma2Tot = (uint)tgParser.ParsedData[i]["EegPowerGamma2"];
                                Console.WriteLine("Gamma2Tot: " + (uint)tgParser.ParsedData[i]["EegPowerGamma2"]);
                                //poorsignal = tgParser.ParsedData[i]["PoorSignal"];
                                Console.WriteLine(" --- ");
                                try
                                {
                                    Conexion.Open();
                                } catch(Exception ex){
                                    Console.WriteLine("Error opening a conection" + ex.Message);
                                    throw ex;
                                }
                                
                                //string SqlQuery = "INSERT INTO TESIS_DATOS_MINDWAVE(HORARIO_DE_PC,TEST_NAME,TEST_COMMENT,PORT,ROW_TIME,POORSIGNAL,ATTENTION,MEDITATION,EGGPOWER,EegPowerDelta,EegPowerTheta,EegPowerAlpha1,EegPowerAlpha2,EegPowerBeta1,EegPowerBeta2,EegPowerGamma1,EegPowerGamma2) VALUES('" + DateTime.Now + "','" + null + "','" + null + "','" + null + "'," + taim + "," + tgParser.ParsedData[i]["PoorSignal"] + "," + 0 + "," + 0 + "," + EegPowerMax + "," + DeltaTot + "," + ThetaTot + "," + Alpha1Tot + "," + Alpha2Tot + "," + Beta1Tot + "," + Beta2Tot + "," + Gamma1Tot + "," + Gamma2Tot + ")";
                                Console.WriteLine("antes");
                                
                                //string SqlQuery = "INSERT INTO TESIS_DATOS_PRUEBA(NUMERO) VALUES("+1+")";
                                string SqlQuery = "INSERT INTO NEUROSKY_DATOS(HORARIO_DE_PC,TEST_NAME,TEST_COMMENT,PORT,ROW_TIME,POORSIGNAL,ATTENTION,MEDITATION,EGGPOWER,EegPowerDelta,EegPowerTheta,EegPowerAlpha1,EegPowerAlpha2,EegPowerBeta1,EegPowerBeta2,EegPowerGamma1,EegPowerGamma2) VALUES('" + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss.fff tt") + "','"+testName+"','"+testComments+"','"+portName+"'," + taim + "," + poorsignal +"," + AttentionTot + "," + MeditationTot + "," + EegPowerMax + "," + DeltaTot + "," + ThetaTot + "," + Alpha1Tot + "," + Alpha2Tot + "," + Beta1Tot + "," + Beta2Tot + "," + Gamma1Tot + "," + Gamma2Tot + ")";
                                //," + 1 + "
                                //string SqlQuery = "INSERT INTO TESIS_DATOS_MINDWAVE(HORARIO_DE_PC,TEST_NAME,TEST_COMMENT,PORT,ROW_TIME,POORSIGNAL,ATTENTION,MEDITATION,EGGPOWER,EegPowerDelta,EegPowerTheta,EegPowerAlpha1,EegPowerAlpha2,EegPowerBeta1,EegPowerBeta2,EegPowerGamma1,EegPowerGamma2) VALUES('" + DateTime.Now + "','" + testName.ToString() + "','" + testComments.ToString() + "','" + portName.ToString() + "'," + tgParser.ParsedData[i]["Time"] + "," + tgParser.ParsedData[i]["PoorSignal"] + "," + tgParser.ParsedData[i]["Attention"] + "," + tgParser.ParsedData[i]["Meditation"] + "," + tgParser.ParsedData[i]["EegPower"] + "," + tgParser.ParsedData[i]["EegPowerDelta"] + "," + tgParser.ParsedData[i]["EegPowerTheta"] + "," + tgParser.ParsedData[i]["EegPowerAlpha1"] + "," + tgParser.ParsedData[i]["EegPowerAlpha2"] + "," + tgParser.ParsedData[i]["EegPowerBeta1"] + "," + tgParser.ParsedData[i]["EegPowerBeta2"] + "," + tgParser.ParsedData[i]["EegPowerGamma1"] + "," + tgParser.ParsedData[i]["EegPowerGamma2"] + ")";
                                cmd = new SqlCommand(SqlQuery, Conexion);
                                prueba = SqlQuery;
                                int N = cmd.ExecuteNonQuery();
                                Conexion.Close();
                            }
                        }
                    }
                    else
                    {
                        row += "- ";
                    }
                }
                if (trip == true)
                {
                    File.AppendAllText(path + "data.txt", row + "\n");
                }
            }
        }
    }
}
