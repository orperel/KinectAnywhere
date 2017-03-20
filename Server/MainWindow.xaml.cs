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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: add comments
        UdpClient Client = new UdpClient(11000);
        IPEndPoint ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 11000); // endpoint where server is listening

        ConcurrentDictionary<string, List<Skeleton>> skeletons = new ConcurrentDictionary<string, List<Skeleton>>();

        private SkelRecorder skelRec;

        private SkelDisplay skelDisp;

        private System.Windows.Forms.Timer timer1;

        private SkelReplay skelRep;

        private DateTime SESSION_TIMESTAMP = DateTime.Parse("13:21:58.238");

        //CallBack
        private void recv(System.IAsyncResult res)
        {
            // TODO: check port of RemoteIpEndPoint
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
            byte[] received = Client.EndReceive(res, ref RemoteIpEndPoint);

            Stream ms = new MemoryStream(received);
            BinaryFormatter bf = new BinaryFormatter();
            object obj = bf.Deserialize(ms);

            List<Skeleton> skeletonList = (List<Skeleton>)obj;

            var remoteIPString = RemoteIpEndPoint.Address.ToString();

            skeletons[remoteIPString] = skeletonList;

            if (this.skelRec != null)
            {
                foreach (Skeleton skel in skeletonList)
                {
                    this.skelRec.recordSkelFrame(skel, 0, DateTime.Now);
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
            this.skelRec.createFile(0);

            this.skelDisp = new SkelDisplay(null);

            //this.skelRep = new SkelReplay(SESSION_TIMESTAMP, 1);

            InitializeComponent();
            
            try
            {
                Client.BeginReceive(new AsyncCallback(recv), null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, List<Skeleton>> entry in this.skeletons)
            {
                if (entry.Value.Count != 0)
                    this.skelDisp.drawSkeletons(entry.Value);
            }

            //if (!this.skelRep.replaySessionFrame(this.skelDisp.drawSkeletons))
            //    this.Close();
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {            
            Image.Source = this.skelDisp.imageSource;

            // Timer for showSkeletons
            this.timer1 = new System.Windows.Forms.Timer();
            this.timer1.Tick += new EventHandler(timer1_Tick);
            this.timer1.Interval = 30; // Milliseconds (Kinect works with 30fps)
            this.timer1.Start();
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