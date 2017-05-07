namespace KinectAnywhere
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Windows;
    using Microsoft.Kinect;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Diagnostics;
    using System.Windows.Media;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: rmove
        private DateTime SESSION_TIMESTAMP = DateTime.Parse("22:42:45.887");
        private int numOfCameras = 3;
        private Calibration calibration;
        bool isRecord = true;
        bool isTesting = false;

        // TODO: add comments   
        UdpClient Client = new UdpClient(11000);
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening

        ConcurrentDictionary<string, List<Skeleton>> skeletons = new ConcurrentDictionary<string, List<Skeleton>>();
        ConcurrentDictionary<string, int> cameras = new ConcurrentDictionary<string, int>();

        private SkelRecorder skelRec;

        private SkelDisplay skelDisp;

        private System.Windows.Forms.Timer timer1;

        //CallBack
        private void recv(System.IAsyncResult res)
        {
            // TODO: check port of RemoteIpEndPoint
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
            byte[] received = Client.EndReceive(res, ref RemoteIpEndPoint);

            Stream ms = new MemoryStream(received);
            BinaryFormatter bf = new BinaryFormatter();
            object obj1 = bf.Deserialize(ms);
            object obj2 = bf.Deserialize(ms);

            List<Skeleton> skeletonList = (List<Skeleton>)obj1;
            DateTime timestamp = (DateTime)obj2;

            var remoteIPString = RemoteIpEndPoint.Address.ToString();

            bool isFirstConnectionForClient = !skeletons.ContainsKey(remoteIPString);

            if (isFirstConnectionForClient)
            {
                int cameraId = cameras.Count; // Assign camera id for client by ip
                cameras[remoteIPString] = cameraId;
                this.skelRec.createFile(cameraId);
            }

            skeletons[remoteIPString] = skeletonList;

            if (this.skelRec != null)
            {
                foreach (Skeleton skel in skeletonList)
                {
                    int cameraId = cameras[remoteIPString];
                    this.skelRec.recordSkelFrame(skel, cameraId, timestamp);
                }
            }

            Client.BeginReceive(new AsyncCallback(recv), null);
        }

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.skelRec = new SkelRecorder();

            this.skelDisp = new SkelDisplay(null);

            InitializeComponent();

            // TODO: Delete this
            if (isTesting)
                AnnTester.initNeuralNetworkTest();

            try
            {
                if (!isTesting)
                {
                    if (isRecord)
                        Client.BeginReceive(new AsyncCallback(recv), null);
                    else
                        this.calibration = new Calibration(SESSION_TIMESTAMP, numOfCameras, this.skelDisp);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Dictionary<Skeleton, int> allSkeletons = new Dictionary<Skeleton, int>();

            foreach (KeyValuePair<string, List<Skeleton>> entry in this.skeletons)
            {
                foreach (Skeleton skel in entry.Value)
                {
                    allSkeletons.Add(skel, cameras[entry.Key]);
                }
            }

            this.skelDisp.drawSkeletons(allSkeletons);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            /* Tests. TODO: Delete*/
            if (isTesting)
            {
                DrawingGroup drawingGroup = new DrawingGroup();
                Image.Source = new DrawingImage(drawingGroup);
                AnnTester.runNeuralNetworkTest(drawingGroup);
            }
            /* --- */
            else
            {
                this.calibration.calibrate();
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {            
            Image.Source = this.skelDisp.imageSource;

            if (isRecord)
            {
                // Timer for showSkeletons
                this.timer1 = new System.Windows.Forms.Timer();
                this.timer1.Tick += new EventHandler(timer1_Tick);
                this.timer1.Interval = 30; // Milliseconds (Kinect works with 30fps)
                this.timer1.Start();
            }
            else {
                // Timer for showSkeletons
                this.timer1 = new System.Windows.Forms.Timer();
                this.timer1.Tick += new EventHandler(timer2_Tick);
                this.timer1.Interval = 120;
                this.timer1.Start();
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.skelRec.closeFiles();
            this.skelRec = null;
        }
    }
}