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
        // State variables
        private BFEngine _engine;
        private System.Windows.Forms.Timer _elapsedTimer;
        private System.Windows.Forms.Timer _displayTimer;
        private DateTime _startTime;
        private bool _isRunning = false;
        private string _currentHash = "";

        public MainForm()
        {
            // Initialize all UI components manually — no designer file needed
            InitializeComponents();
        }

        // Builds and lays out all UI controls
        private void InitializeComponents()
        {
            this.Text = "Password Brute Force Cracker";
            this.Size = new Size(600, 620);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.StartPosition = FormStartPosition.CenterScreen;

            int y = 20;
            int labelX = 20, controlX = 160, controlW = 400;

            // Password input row
            lblPassword = new Label { Text = "Password:", Location = new Point(labelX, y + 3), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(controlX, y), Width = 200 };
            btnGenerate = new Button { Text = "Generate", Location = new Point(370, y), Width = 90 };
            btnGenerate.Click += OnGenerateButtonClick;

            y += 40;

            // Hash display row
            lblHash = new Label { Text = "SHA256 Hash:", Location = new Point(labelX, y + 3), AutoSize = true };
            txtHash = new TextBox { Location = new Point(controlX, y), Width = controlW, ReadOnly = true };

            y += 40;

            // Start/Stop attack button
            btnStartStop = new Button
            {
                Text = "Start Attack",
                Location = new Point(labelX, y),
                Width = 150,
                Height = 35,
                BackColor = Color.Green,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnStartStop.Click += OnStartStopButtonClick;

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

            // Result display label
            lblResult = new Label
            {
                Text = "Result:",
                Location = new Point(labelX, y),
                AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            y += 40;

            // Log output box
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

            // Add all controls to form
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

            // Timer to update elapsed time display (every 100 ms)
            _elapsedTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _elapsedTimer.Tick += (s, e) =>
            {
                double elapsed = (DateTime.Now - _startTime).TotalSeconds;
                lblTime.Text = $"Elapsed: {elapsed:F2}s";
            };

            LogMessage($"Using {BFEngine.ThreadCount} thread(s) (CPU cores - 1)");
        }

        // Generate button click handler
        private void OnGenerateButtonClick(object sender, EventArgs e)
        {
            string password = txtPassword.Text.Trim();

            // If field is empty - generate new password
            if (string.IsNullOrEmpty(password))
            {
                var manager = new PasswordManager();
                password = manager.GeneratePassword();
            }

            // Hash the password
            string hash = PasswordManager.HashPassword(password);

            // Display password and hash
            txtPassword.Text = password;
            txtHash.Text = hash;
            _currentHash = hash;

            // Reset progress display for fresh run
            lblResult.Text = "Result:";
            progressBar.Value = 0;
            lblProgress.Text = "Progress: 0%";
            lblTime.Text = "Elapsed: 0.00s";

            LogMessage($"Password set: {password} (length {password.Length})");
            LogMessage($"Hash: {hash}");
        }

        // Start/Stop attack button click handler
        private void OnStartStopButtonClick(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                // Stop the brute force attack
                _engine?.Stop();
                StopAttackUI();
                LogMessage("Attack stopped by user.");
            }
            else
            {
                // Validate that password is set
                if (string.IsNullOrEmpty(_currentHash))
                {
                    MessageBox.Show("Please generate or enter a password first!", "No Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StartAttackUI();
                LogMessage($"Starting multi-threaded attack with {BFEngine.ThreadCount} threads...");

                // Create the brute force engine
                _engine = new BFEngine(_currentHash);

                // Progress update callback from worker threads
                _engine.OnProgress = (checkedCount, totalCombinations) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        int percentage = (int)(checkedCount * 100L / totalCombinations);
                        progressBar.Value = Math.Min(percentage, 100);
                    }));
                };

                // Password found or attack finished callback
                _engine.OnFound = (foundPassword, elapsedSeconds) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        _elapsedTimer.Stop();
                        _displayTimer?.Stop();
                        _isRunning = false;

                        if (foundPassword != null)
                        {
                            // Password successfully found
                            lblResult.Text = $"Result: FOUND - \"{foundPassword}\"";
                            lblResult.ForeColor = Color.DarkGreen;
                            progressBar.Value = 100;
                            LogMessage($"Password cracked: \"{foundPassword}\" in {elapsedSeconds:F2}s");

                            // Run single-threaded version for performance comparison
                            LogMessage("Running single-threaded version for comparison...");
                            string hashCopy = _currentHash;
                            BFEngine engineRef = _engine;

                            Thread comparisonThread = new Thread(() =>
                            {
                                double singleThreadTime = engineRef.RunSingleThread(hashCopy);
                                this.Invoke((Action)(() =>
                                {
                                    LogMessage("────── Performance Comparison ──────");
                                    LogMessage($"  Multi-thread ({BFEngine.ThreadCount} threads): {elapsedSeconds:F2}s");
                                    LogMessage($"  Single-thread:               {singleThreadTime:F2}s");
                                    double speedup = singleThreadTime / elapsedSeconds;
                                    LogMessage($"  Speedup: {speedup:F2}x faster with multi-threading");
                                    LogMessage("────────────────────────────────────");
                                }));
                            });
                            comparisonThread.IsBackground = true;
                            comparisonThread.Start();
                        }
                        else
                        {
                            // Password not found or attack was stopped
                            lblResult.Text = "Result: Not found (stopped)";
                            lblResult.ForeColor = Color.DarkRed;
                            LogMessage($"Attack ended. Elapsed: {elapsedSeconds:F2}s");
                        }

                        StopAttackUI();
                    }));
                };

                // Start the attack on a background thread
                Thread attackThread = new Thread(() => _engine.StartMultiThread());
                attackThread.IsBackground = true;
                attackThread.Start();
            }
        }

        // Switches UI to "attack running" state
        private void StartAttackUI()
        {
            _isRunning = true;
            _startTime = DateTime.Now;
            _elapsedTimer.Start();

            // Timer to smoothly update password counter every 50ms
            _displayTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _displayTimer.Tick += (s, e) =>
            {
                if (_engine != null && _isRunning)
                {
                    long checked_count = _engine.CheckedCount;
                    long total_combinations = _engine.TotalCombinations();

                    int percentage = (int)(checked_count * 100L / total_combinations);
                    progressBar.Value = Math.Min(percentage, 100);
                    lblProgress.Text = $"Progress: {percentage}% ({checked_count:N0} / {total_combinations:N0})";
                }
            };
            _displayTimer.Start();

            btnStartStop.Text = "Stop Attack";
            btnStartStop.BackColor = Color.DarkRed;
            btnGenerate.Enabled = false;
            lblResult.Text = "Result: Searching...";
            lblResult.ForeColor = Color.DarkOrange;
        }

        // Switches UI back to "idle" state
        private void StopAttackUI()
        {
            _isRunning = false;
            _elapsedTimer.Stop();
            _displayTimer?.Stop();
            btnStartStop.Text = "Start Attack";
            btnStartStop.BackColor = Color.Green;
            btnGenerate.Enabled = true;
        }

        // Add timestamped message to log box
        private void LogMessage(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.ScrollToCaret();
        }
    }
}