using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private int frames;
        private PXCMFaceModule faceModule;
        private PXCMFaceConfiguration faceConfig;
        private PXCMFaceData faceData;


        private string time = DateTime.Now.ToString("M_d_hh_mm_ss");
        //private string output_folder = @"C:\Users\pedro\source\repos\HelloLandmarks\output\";
        private string output_folder = @"C:\Release\Records\output\";
        private string output_file = null;
        //private string input_folder = @"C:\Users\pedro\source\repos\HelloLandmarks\input\";
        private string input_folder = @"C:\Release\Records\Record_1_@28-11-2018_22-30\";
        //private string input_folder = @"C:\Release\Records\Record_2_@29-11-2018_12-17\";
        private string input_file = null;

        DateTime startTime;
        private int frameIndex;
        private long frameTimeStamp;

        public MainWindow()
        {
            InitializeComponent();

            textBox1.Text = "Click to initiate extraction.";

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
            int video_number;

            for (video_number = 1; video_number <= 30; video_number++)
            {
                // Instantiate and initialize the SenseManager
                senseManager = PXCMSenseManager.CreateInstance();

                /*PXCMCaptureManager captureMgr = senseManager.captureManager;                

                if (captureMgr == null)
                {
                    throw new Exception("PXCMCaptureManager null");
                }*/


                input_file = input_folder + "video" + video_number + ".rssdk";
                //input_file = input_folder + "video2_new.rssdk";

                // Recording mode: true
                // Playback mode: false
                // Settings for playback mode (read rssdk files and extract landmarks)
                senseManager.captureManager.SetFileName(input_file, false);
                //captureMgr.SetFileName(input_file, false);

                senseManager.captureManager.SetRealtime(false);
                //senseManager.captureManager.SetPause(false);

                //PXCMCapture.Device device = captureMgr.QueryDevice();
                frames = senseManager.captureManager.QueryNumberOfFrames();

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    textBox2.Text = frames.ToString();
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
                string landmarks = null;
                landmarks += "User ID" + ";" + "Frame Index" + ";" + "Time Stamp" + ";" + "landmarkIndex" + ";" + "X" + ";" + "Y" + ";" + "Z" + '\n';

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
                        for (Int32 i = 0; i < nfaces; i++)
                        {
                            PXCMFaceData.Face face = faceData.QueryFaceByIndex(i);
                            PXCMFaceData.LandmarksData landmarkData = face.QueryLandmarks();

                            //var point3 = new PXCMPoint3DF32(); ????
                            if (landmarkData != null)
                            {
                                PXCMFaceData.LandmarkPoint[] landmarkPoints;
                                landmarkData.QueryPoints(out landmarkPoints);
                                frameIndex = senseManager.captureManager.QueryFrameIndex();
                                frameTimeStamp = senseManager.captureManager.QueryFrameTimeStamp();
                                //textBox1.Text = frameIndex.ToString();

                                Application.Current.Dispatcher.BeginInvoke(new Action(() => textBox1.Text = frameIndex.ToString()));

                                for (int j = 0; j < landmarkPoints.Length; j++)
                                {
                                    //get world coordinate
                                    landmarks += i + ";" + frameIndex + ";" + frameTimeStamp + ";" + j + ";" + landmarkPoints[j].world.x.ToString() + ";" + landmarkPoints[j].world.y.ToString() + ";" + landmarkPoints[j].world.z.ToString() + '\n';
                                }
                                //Console.WriteLine(landmarks);
                                //landmarks += "--------------------------" + '\n';
                            }
                        }
                    }
                    // Release the frame
                    if (faceData != null) faceData.Dispose();
                    senseManager.ReleaseFrame();
                }

                output_file = output_folder + "video" + video_number + "_" + time + ".csv";
                using (System.IO.StreamWriter sw = File.AppendText(output_file))
                {
                    //get world coordinate
                    sw.Write(landmarks);
                }

                senseManager.Dispose();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            processingThread = new Thread(new ThreadStart(ProcessingThread));
            processingThread.Start();
        }

    }
}
