using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Exam
{
    // Main window of the application
    public class MainForm : Form
    {
        // UI Controls
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblHash;
        private TextBox txtHash;
        private Button btnGenerate;
        private Button btnStartStop;
        private ProgressBar progressBar;
        private Label lblProgress;
        private Label lblTime;
        private Label lblResult;
        private RichTextBox txtLog;

        // State
        private System.Windows.Forms.Timer _timer;   // Updates elapsed time display every 100 ms
        private DateTime _startTime;
        private bool _isRunning = false;
        private string _currentHash = "";

        public MainForm()
        {
            // We build all controls manually — no designer file needed
            InitializeComponents();
        }

        // Builds and lays out all UI controls
        private void InitializeComponents()
        {
            this.Text = "FinalExam";
            this.Size = new Size(600, 620);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 20;
            int labelX = 20, controlX = 160, controlW = 400;

            // Password row
            lblPassword = new Label { Text = "Password:", Location = new Point(labelX, y + 3), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(controlX, y), Width = 200, ReadOnly = true };
            btnGenerate = new Button { Text = "Generate", Location = new Point(370, y), Width = 90 };


            y += 40;

            // Hash row
            lblHash = new Label { Text = "SHA256 Hash:", Location = new Point(labelX, y + 3), AutoSize = true };
            txtHash = new TextBox { Location = new Point(controlX, y), Width = controlW, ReadOnly = true };

            y += 40;

            // Start/Stop button
            btnStartStop = new Button
            {
                Text = "Start BruteForce",
                Location = new Point(labelX, y),
                Width = 150,
                Height = 35,
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };


            y += 50;

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(labelX, y),
                Width = 540,
                Height = 25,
                Minimum = 0,
                Maximum = 100
            };

            y += 35;

            // Progress percentage label
            lblProgress = new Label
            {
                Text = "Progress: 0%",
                Location = new Point(labelX, y),
                AutoSize = true
            };

            y += 30;

            // Elapsed time label
            lblTime = new Label
            {
                Text = "Elapsed: 0.00s",
                Location = new Point(labelX, y),
                AutoSize = true
            };

            y += 30;

            // Result label
            lblResult = new Label
            {
                Text = "Result:",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            y += 40;

            // Log box
            var lblLog = new Label { Text = "Log:", Location = new Point(labelX, y), AutoSize = true };
            y += 20;
            txtLog = new RichTextBox
            {
                Location = new Point(labelX, y),
                Width = 540,
                Height = 180,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Courier New", 9)
            };

            // Add all UI things
            this.Controls.AddRange(new Control[]
            {
                lblPassword, txtPassword, btnGenerate,
                lblHash, txtHash,
                btnStartStop,
                progressBar,
                lblProgress,
                lblTime,
                lblResult,
                lblLog, txtLog
            });


        }





        // Switches the UI into "attack running" state
        private void StartUI()
        {
            _isRunning = true;
            _startTime = DateTime.Now;
            _timer.Start();
            btnStartStop.Text = "Stop";
            btnStartStop.BackColor = Color.DarkRed;
            btnGenerate.Enabled = false;
            lblResult.Text = "Result: Searching...";
            lblResult.ForeColor = Color.DarkOrange;
        }

        // Switches the UI back to "idle" state
        private void StopUI()
        {
            _isRunning = false;
            _timer.Stop();
            btnStartStop.Text = "Start Attack";
            btnStartStop.BackColor = Color.Green;
            btnGenerate.Enabled = true;
        }

        // Appends a timestamped line to the log box
        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.ScrollToCaret();
        }
    }
}
