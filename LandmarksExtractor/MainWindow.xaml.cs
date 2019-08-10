using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using WinForms = System.Windows.Forms;
using System.Linq;

namespace LandmarksExtractor
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread processingThread;       

        // Initialize RealSense:
        private PXCMSenseManager senseManager;
        private int nframes;
        private PXCMFaceModule faceModule;
        private PXCMFaceConfiguration faceConfig;
        private PXCMFaceData faceData;
        private string landmarks = null;


        private string time = DateTime.Now.ToString("M_d_hh_mm_ss");
        //private string output_folder = @"C:\Users\pedro\source\repos\HelloLandmarks\output\";
        private string output_folder = null;//@"C:\Release\Records\output\";
        private string output_file = null;
        //private string input_folder = @"C:\Users\pedro\source\repos\HelloLandmarks\input\";
        private string input_folder = null; //= @"C:\Release\Records\Record_1_@28-11-2018_22-30\";
        List<string> dirs;
        //private string input_folder = @"C:\Release\Records\Record_2_@29-11-2018_12-17\";
        private string input_file = null;

        DateTime startTime;
        private int frameIndex;
        private long frameTimeStamp;

        public MainWindow()
        {
            InitializeComponent();     
            startTime = DateTime.Now;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            processingThread.Abort();
            if (faceData != null) faceData.Dispose();
            faceConfig.Dispose();
            senseManager.Dispose();
        }

        private void ProcessingThread()
        {
            //int video_number;

            foreach (var d in dirs)
            {
                string summary = null;

                if (Directory.EnumerateFileSystemEntries(d).Any())//If the folder is not empty
                {
                    WriteToFile(d); //Initialize the .csv   
                    List<string> fileList = new List<string>(Directory.GetFiles(d, "*.rssdk"));
                    foreach (var input_file in fileList)
                    {
                        int lostFrames = 0;
                        System.Console.WriteLine(input_file);
                        //for (video_number = 1; video_number <= 30; video_number++)
                        //{
                        // Instantiate and initialize the SenseManager
                        senseManager = PXCMSenseManager.CreateInstance();

                        /*PXCMCaptureManager captureMgr = senseManager.captureManager;                

                        if (captureMgr == null)
                        {
                            throw new Exception("PXCMCaptureManager null");
                        }*/


                        //input_file = input_folder + "video" + video_number + ".rssdk";
                        //input_file = input_folder + "video2_new.rssdk";

                        // Recording mode: true
                        // Playback mode: false
                        // Settings for playback mode (read rssdk files and extract landmarks)
                        senseManager.captureManager.SetFileName(input_file, false);
                        //captureMgr.SetFileName(input_file, false);

                        senseManager.captureManager.SetRealtime(false);
                        //senseManager.captureManager.SetPause(false);

                        //PXCMCapture.Device device = captureMgr.QueryDevice();
                        nframes = senseManager.captureManager.QueryNumberOfFrames();

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            textBox2.Text = nframes.ToString();
                            textBox3.Text = input_file;
                        }));

                        senseManager.EnableFace();
                        faceModule = senseManager.QueryFace();
                        faceConfig = faceModule.CreateActiveConfiguration();
                        faceConfig.landmarks.maxTrackedFaces = 1;
                        faceConfig.landmarks.isEnabled = true;
                        faceConfig.detection.maxTrackedFaces = 1;
                        faceConfig.detection.isEnabled = true;
                        faceConfig.EnableAllAlerts();
                        faceConfig.ApplyChanges();

                        senseManager.Init();

                        // This string stores all data before saving to csv file
                        landmarks = null;
                        //landmarks += "User ID" + ";" + "Frame Index" + ";" + "Time Stamp" + ";" + "landmarkIndex" + ";" + "X" + ";" + "Y" + ";" + "Z" + '\n';

                        // Start AcquireFrame/ReleaseFrame loop
                        while (senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                        {
                            // Retrieve face data
                            faceModule = senseManager.QueryFace();
                            if (faceModule != null)
                            {
                                // Retrieve the most recent processed data
                                faceData = faceModule.CreateOutput();
                                faceData.Update();
                            }
                            if (faceData != null)
                            {
                                Int32 nfaces = faceData.QueryNumberOfDetectedFaces();
                                frameIndex = senseManager.captureManager.QueryFrameIndex();
                                frameTimeStamp = senseManager.captureManager.QueryFrameTimeStamp();
                                if (nfaces == 0) //If none face was detected, we will consider as a "lost frame"
                                {
                                    lostFrames += 1;
                                }

                                for (Int32 i = 0; i < nfaces; i++)
                                {   
                                    PXCMFaceData.Face face = faceData.QueryFaceByIndex(i);
                                    PXCMFaceData.LandmarksData landmarkData = face.QueryLandmarks();

                                    //var point3 = new PXCMPoint3DF32(); ????
                                    if (landmarkData != null)
                                    {
                                        PXCMFaceData.LandmarkPoint[] landmarkPoints;
                                        landmarkData.QueryPoints(out landmarkPoints);
                                        
                                        //textBox1.Text = frameIndex.ToString();

                                        Application.Current.Dispatcher.BeginInvoke(new Action(() => textBox1.Text = frameIndex.ToString()));

                                        landmarks += input_file.Split('\\').Last() + ";" + i + ";" + frameIndex + ";" + frameTimeStamp + ";"; // Begin line with frame info

                                        for (int j = 0; j < landmarkPoints.Length; j++) // Writes landmarks coordinates along the line 
                                        {
                                            //get world coordinate
                                            landmarks += landmarkPoints[j].world.x.ToString() + ";" + landmarkPoints[j].world.y.ToString() + ";" + landmarkPoints[j].world.z.ToString() + ";";
                                            if (j % 100 == 0)
                                                WriteToFile(d); // After 100 frames, writes to file and empties landmarks string
                                        }

                                        landmarks += '\n'; // Breaks line after the end of the frame coordinates
                                                           //Console.WriteLine(landmarks);
                                                           //landmarks += "--------------------------" + '\n';
                                    }
                                }
                            }
                            // Release the frame
                            if (faceData != null) faceData.Dispose();
                            senseManager.ReleaseFrame();
                        }
                        summary += d + ";" + input_file + ";" + nframes + ";" + lostFrames + ";" + (nframes-lostFrames) + '\n';
                        WriteSummary(summary);
                        summary = null;
                        WriteToFile(d);

                        senseManager.Dispose();
                    }
                }
            }
        }

        private void WriteSummary(string summary)
        {
            string filename = output_folder + "\\summary.csv";

            if (File.Exists(filename))
            {
                using (System.IO.StreamWriter sw = File.AppendText(filename))
                {
                    sw.Write(summary);
                }
            }
            else
            {
                using (System.IO.StreamWriter fs = File.AppendText(filename))
                {
                    string header = "Record" + ";" + "Video Index" + ";" + "Total Frames" + ";" + "Lost Frames" + ";" + "Useful Frames" + "\n"; // + "landmarkIndex" + ";" + "X" + ";" + "Y" + ";" + "Z" + '\n';
                    fs.Write(header);                    
                }
            }            
        }

        private void WriteToFile(string folderName)
        {            
            output_file = output_folder + "\\" + folderName.Split('\\').Last() + ".csv";
            
            if (File.Exists(output_file)) 
            {
                using (System.IO.StreamWriter sw = File.AppendText(output_file))
                {
                    //get world coordinate
                    sw.Write(landmarks);
                }
                landmarks = null;
            }
            else // Writes header on the beginning of the file
            {
                using (System.IO.StreamWriter fs = File.AppendText(output_file))
                {
                    landmarks += "Video Index" + ";" + "User ID" + ";" + "Frame Index" + ";" + "Time Stamp" + ";"; // + "landmarkIndex" + ";" + "X" + ";" + "Y" + ";" + "Z" + '\n';

                    for (int lm = 0; lm < 78; lm++) // Dataframe headers with landmark numbers 
                    {
                        landmarks += "land_" + lm + "_X" + ";";
                        landmarks += "land_" + lm + "_Y" + ";";
                        landmarks += "land_" + lm + "_Z" + ";";
                    }
                    landmarks += '\n';
                    fs.Write(landmarks);
                    landmarks = null;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (input_folder == null || output_folder == null)
            {
                string message = "Please, select the Source and Output directories!";
                string caption = "Missing root folder";
                WinForms.MessageBoxButtons buttons = WinForms.MessageBoxButtons.OK;
                WinForms.DialogResult result;

                // Displays the MessageBox.
                result = WinForms.MessageBox.Show(message, caption, buttons);
               // if (result == System.Windows.Forms.DialogResult.OK)
                //{
                //    ChooseFolder();
                //}
            }
            else
            {
                processingThread = new Thread(new ThreadStart(ProcessingThread));
                processingThread.Start();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog1 = new WinForms.FolderBrowserDialog();

            // Show the FolderBrowserDialog.
            WinForms.DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath;
                textBox5.Text = folderName;
                input_folder = folderName;
                dirs = new List<string>(Directory.EnumerateDirectories(folderName));
                //foreach (var d in dirs)
                //System.Console.WriteLine(d);
            }
        }

        private void OutputButton_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog1 = new WinForms.FolderBrowserDialog();

            // Show the FolderBrowserDialog.
            WinForms.DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == WinForms.DialogResult.OK)
            {
                string folderName = folderBrowserDialog1.SelectedPath;
                textBox4.Text = folderName;
                output_folder = folderName;
                //dirs = new List<string>(Directory.EnumerateDirectories(folderName));
                //foreach (var d in dirs)
                //System.Console.WriteLine(d);
            }
        }
    }
}
