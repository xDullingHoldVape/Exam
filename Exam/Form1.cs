using C_FinalTask;
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
        private BFEngine _engine; // nullable: not created until start button is clicked
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
            btnGenerate.Click += BtnGenerate_Click;

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
            btnStartStop.Click += BtnStartStop_Click;


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

            // Timer: refreshes the elapsed-time label every 100 ms
            _timer = new System.Windows.Forms.Timer { Interval = 100 };
            _timer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.Now - _startTime).TotalSeconds;
                lblTime.Text = $"Elapsed: {elapsed:F2}s";
            };

            Log($"Using {BFEngine.ThreadCount} thread(s) (CPU cores - 1)");


        }

        // "Generate" button
        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            var manager = new PasswordManager();
            string password = manager.GeneratePassword();
            string hash = PasswordManager.HashPassword(password);

            // Show the password and its hash
            txtPassword.Text = password;
            txtHash.Text = hash;
            _currentHash = hash;

            // Reset display fields for a fresh run
            lblResult.Text = "Result: ";
            progressBar.Value = 0;
            lblProgress.Text = "Progress: 0%";
            lblTime.Text = "Elapsed: 0.00s";

            Log($"Generated password: {password} (length {password.Length})");
            Log($"Hash: {hash}");
        }

        // Start and Stop attack button
        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                // Stopping the brute force attack
                _engine?.Stop();
                StopUI();
                Log("Attack stopped by user.");
            }
            else
            {
                // If there is no password
                if (string.IsNullOrEmpty(_currentHash))
                {
                    MessageBox.Show("Please generate a password first!");
                    return;
                }

                StartUI();
                Log($"Starting multi-thread attack with {BFEngine.ThreadCount} threads...");

                // Create the engine (it will run on a background thread)
                _engine = new BFEngine(_currentHash);

                // Progress callback — called from worker threads, so marshal to UI thread
                _engine.OnProgress = (checkedCount, total) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        int pct = (int)(checkedCount * 100L / total);
                        progressBar.Value = Math.Min(pct, 100);
                        lblProgress.Text = $"Progress: {pct}% ({checkedCount:N0} / {total:N0})";
                    }));
                };

                // Found / finished callback
                _engine.OnFound = (password, elapsed) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        _timer.Stop();
                        _isRunning = false;

                        if (password != null)
                        {
                            // Password found
                            lblResult.Text = $"Result: FOUND  \"{password}\"";
                            lblResult.ForeColor = Color.DarkGreen;
                            progressBar.Value = 100;
                            Log($"✓ Multi-thread found: \"{password}\" in {elapsed:F2}s");

                            // Run single-thread on a background thread for comparison
                            Log("Running single-thread for comparison...");
                            string hashCopy = _currentHash;
                            BFEngine engineRef = _engine; // there was a mistake CS8602, so I added that ref 
                            Thread singleThread = new Thread(() =>
                            {
                                double singleTime = engineRef.RunSingleThread(hashCopy);
                                this.Invoke((Action)(() =>
                                {
                                    Log($"── Performance Comparison ──────────────────");
                                    Log($"  Multi-thread ({BFEngine.ThreadCount} threads): {elapsed:F2}s");
                                    Log($"  Single-thread:                  {singleTime:F2}s");
                                    double speedup = singleTime / elapsed;
                                    Log($"  Speedup: {speedup:F2}x faster with multi-threading");
                                    Log($"────────────────────────────────────────────");
                                }));
                            });
                            singleThread.IsBackground = true;
                            singleThread.Start();
                        }
                        else
                        {
                            // Not found / stopped
                            lblResult.Text = "Result: Not found (stopped)";
                            lblResult.ForeColor = Color.DarkRed;
                            Log($"Attack ended. Elapsed: {elapsed:F2}s");
                        }

                        StopUI();
                    }));
                };

                // Launch the brute-force engine on a dedicated background thread
                Thread attackThread = new Thread(() => _engine.StartMultiThread());
                attackThread.IsBackground = true;
                attackThread.Start();
            }
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
