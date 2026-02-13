using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AutoTriage.Core;
using AutoTriage.Core.Decoding;

namespace AutoTriage.Gui
{
    // ResultRow class for DataGridView binding
    public class ResultRow
    {
        public int LineNumber { get; set; }
        public string Timestamp { get; set; } = "";
        public string Code { get; set; } = "";
        public string Severity { get; set; } = "";
        public string LineText { get; set; } = "";
        public Color RowColor { get; set; } = Color.White;
    }

    public class Form1 : Form
    {
        private LogAnalyzer analyzer;
        private AnalysisResult? currentResult;
        private BindingList<ResultRow> displayedRows;
        private BindingSource resultsBindingSource = new BindingSource();

        // NRC filter checkbox (for filtering Negative Response Codes)
        private CheckBox chkNRC = null!;

        // UI Controls
        private SplitContainer mainSplitContainer = null!;
        private TextBox txtLogInput = null!;
        private TextBox txtKeywordFilter = null!;
        private CheckBox chkCritical = null!;
        private CheckBox chkError = null!;
        private CheckBox chkWarning = null!;
        private CheckBox chkSuccess = null!;
        private Button btnLoadFile = null!;
        private Button btnAnalyze = null!;
        private Button btnClearAll = null!;
        private Button btnDecoder = null!;
        private DataGridView dgvResults = null!;
        private Label lblStatus = null!;
        private Label lblLoadedFile = null!;
        private ContextMenuStrip ctxResultsMenu = null!;

        public Form1()
        {
            try
            {
                analyzer = new LogAnalyzer();
                displayedRows = new BindingList<ResultRow>();
                resultsBindingSource.DataSource = displayedRows;
                InitializeCustomUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Form1:\n\n{ex.Message}\n\nStack:\n{ex.StackTrace}", 
                    "Initialization Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                throw;
            }
        }

        private void InitializeCustomUI()
        {
            // Set form size and minimum size
            this.Text = "AutoTriage - Log Analyzer";
            this.Size = new Size(1300, 900);
            this.MinimumSize = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Main split container (top: log input area, bottom: results area)
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BorderStyle = BorderStyle.FixedSingle,
                SplitterWidth = 5
            };
            this.Controls.Add(mainSplitContainer);

            // Set 50/50 split on form shown
            this.Shown += (s, e) => mainSplitContainer.SplitterDistance = (int)(this.ClientSize.Height * 0.5);

            // Maintain 50/50 split on resize
            this.Resize += (s, e) =>
            {
                if (this.WindowState != FormWindowState.Minimized && mainSplitContainer != null)
                {
                    mainSplitContainer.SplitterDistance = (int)(this.ClientSize.Height * 0.5);
                }
            };

            // ===== PANEL 1 (TOP): LOG INPUT AREA =====

            // Label for log input
            var lblLogInput = new Label
            {
                Text = "Paste Log Here:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            mainSplitContainer.Panel1.Controls.Add(lblLogInput);

            // Loaded file path label
            lblLoadedFile = new Label
            {
                Text = "",
                Location = new Point(150, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.DarkBlue
            };
            mainSplitContainer.Panel1.Controls.Add(lblLoadedFile);

            // Action buttons row
            btnLoadFile = new Button
            {
                Text = "Load Log File",
                Location = new Point(10, 40),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnLoadFile.Click += BtnLoadFile_Click;
            mainSplitContainer.Panel1.Controls.Add(btnLoadFile);

            btnAnalyze = new Button
            {
                Text = "Analyze Log",
                Location = new Point(140, 40),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnAnalyze.Click += BtnAnalyze_Click;
            mainSplitContainer.Panel1.Controls.Add(btnAnalyze);

            btnClearAll = new Button
            {
                Text = "Clear All",
                Location = new Point(270, 40),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnClearAll.Click += BtnClearAll_Click;
            mainSplitContainer.Panel1.Controls.Add(btnClearAll);

            btnDecoder = new Button
            {
                Text = "🔧 Decoder Tools",
                Location = new Point(400, 40),
                Size = new Size(140, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnDecoder.Click += BtnDecoder_Click;
            mainSplitContainer.Panel1.Controls.Add(btnDecoder);

            // Raw log search textbox (Wireshark-style: find one match at a time)
            var txtRawLogSearch = new TextBox
            {
                PlaceholderText = "Search raw log...",
                Location = new Point(550, 40),
                Size = new Size(200, 32),
                Font = new Font("Segoe UI", 9F)
            };
            txtRawLogSearch.KeyDown += (s, e) =>
            {
                // Press Enter to find next (like Wireshark)
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (!string.IsNullOrWhiteSpace(txtRawLogSearch.Text))
                    {
                        FindNextInLog(txtRawLogSearch.Text);
                    }
                }
            };
            mainSplitContainer.Panel1.Controls.Add(txtRawLogSearch);

            // Add navigation buttons for raw log search
            var btnFindNext = new Button
            {
                Text = "⬇ Next",
                Location = new Point(760, 40),
                Size = new Size(70, 32),
                Font = new Font("Segoe UI", 8F)
            };
            btnFindNext.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtRawLogSearch.Text))
                {
                    FindNextInLog(txtRawLogSearch.Text);
                }
            };
            mainSplitContainer.Panel1.Controls.Add(btnFindNext);

            var btnFindPrev = new Button
            {
                Text = "⬆ Prev",
                Location = new Point(835, 40),
                Size = new Size(70, 32),
                Font = new Font("Segoe UI", 8F)
            };
            btnFindPrev.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(txtRawLogSearch.Text))
                {
                    FindPrevInLog(txtRawLogSearch.Text);
                }
            };
            mainSplitContainer.Panel1.Controls.Add(btnFindPrev);

            // Log input textbox
            txtLogInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F),
                HideSelection = false,  // Keep selection visible even when not focused
                MaxLength = 0,  // 0 = no limit (allows very large logs)
                // ReadOnly removed - users need to be able to paste logs here
                // Will be set to ReadOnly AFTER analysis to prevent accidental edits
                Location = new Point(10, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(mainSplitContainer.Panel1.Width - 20, mainSplitContainer.Panel1.Height - 90)
            };
            mainSplitContainer.Panel1.Controls.Add(txtLogInput);

            // ===== PANEL 2 (BOTTOM): FILTERS + RESULTS AREA =====

            // Status label (at bottom) - ADD FIRST (Dock.Bottom goes to bottom)
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Text = "Ready",
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.DarkSlateGray,
                BackColor = Color.WhiteSmoke
            };
            mainSplitContainer.Panel2.Controls.Add(lblStatus);

            // Filter panel (compact layout at top) - ADD SECOND (Dock.Top goes to top)
            var filterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,  // Reduced from 120 since NRC is now a single checkbox
                Padding = new Padding(10),
                BackColor = Color.WhiteSmoke
            };

            // Keyword filter
            var lblKeyword = new Label
            {
                Text = "Keyword Filter (comma/space/newline separated):",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            filterPanel.Controls.Add(lblKeyword);

            txtKeywordFilter = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(350, 20),
                Font = new Font("Segoe UI", 9F)
            };
            txtKeywordFilter.TextChanged += TxtKeywordFilter_TextChanged;
            txtKeywordFilter.KeyDown += TxtKeywordFilter_KeyDown;
            filterPanel.Controls.Add(txtKeywordFilter);

            // Severity filters
            var lblSeverity = new Label
            {
                Text = "Severity:",
                Location = new Point(10, 60),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            filterPanel.Controls.Add(lblSeverity);

            chkCritical = new CheckBox
            {
                Text = "Critical",
                Location = new Point(80, 60),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            chkCritical.CheckedChanged += SeverityFilter_CheckedChanged;
            filterPanel.Controls.Add(chkCritical);

            chkError = new CheckBox
            {
                Text = "Error",
                Location = new Point(170, 60),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            chkError.CheckedChanged += SeverityFilter_CheckedChanged;
            filterPanel.Controls.Add(chkError);

            chkWarning = new CheckBox
            {
                Text = "Warning",
                Location = new Point(240, 60),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            chkWarning.CheckedChanged += SeverityFilter_CheckedChanged;
            filterPanel.Controls.Add(chkWarning);

            chkSuccess = new CheckBox
            {
                Text = "Success",
                Location = new Point(330, 60),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            chkSuccess.CheckedChanged += SeverityFilter_CheckedChanged;
            filterPanel.Controls.Add(chkSuccess);

            // NRC filter (Negative Response Codes)
            chkNRC = new CheckBox
            {
                Text = "NRC",
                Location = new Point(420, 60),
                AutoSize = true,
                Checked = true,  // Default: show NRC codes
                Font = new Font("Segoe UI", 9F)
            };
            chkNRC.CheckedChanged += NrcFilter_CheckedChanged;
            filterPanel.Controls.Add(chkNRC);

            mainSplitContainer.Panel2.Controls.Add(filterPanel);

            // Results DataGridView (fills remaining space) - ADD LAST (Dock.Fill takes remaining space)
            dgvResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                ColumnHeadersVisible = true,
                BackgroundColor = Color.White,
                GridColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = true,
                ScrollBars = ScrollBars.Both,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                EnableHeadersVisualStyles = false
            };

            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, dgvResults, new object[] { true });

            dgvResults.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvResults.DefaultCellStyle.Font = new Font("Consolas", 9F);
            dgvResults.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);
            dgvResults.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvResults.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvResults.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 2, 4, 2);

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "colLineNumber", 
                HeaderText = "Line #", 
                DataPropertyName = "LineNumber",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells,
                MinimumWidth = 60,
                DefaultCellStyle = new DataGridViewCellStyle 
                { 
                    WrapMode = DataGridViewTriState.False,
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "colTimestamp", 
                HeaderText = "Timestamp", 
                DataPropertyName = "Timestamp",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells,
                MinimumWidth = 150,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False }
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "colCode", 
                HeaderText = "Code", 
                DataPropertyName = "Code",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells,
                MinimumWidth = 70,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False }
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "colSeverity", 
                HeaderText = "Severity", 
                DataPropertyName = "Severity",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells,
                MinimumWidth = 80,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False }
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn
            { 
                Name = "colLineText", 
                HeaderText = "Line Text", 
                DataPropertyName = "LineText",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 800,
                MinimumWidth = 300,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False }
            });

            dgvResults.DataSource = resultsBindingSource;

            dgvResults.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < displayedRows.Count)
                {
                    var row = displayedRows[e.RowIndex];
                    var dgvRow = dgvResults.Rows[e.RowIndex];
                    dgvRow.DefaultCellStyle.BackColor = row.RowColor;
                    dgvRow.DefaultCellStyle.ForeColor = Color.Black;
                    dgvRow.DefaultCellStyle.SelectionBackColor = Color.LightSteelBlue;
                    dgvRow.DefaultCellStyle.SelectionForeColor = Color.Black;
                }
            };

            // Context menu for right-click payload decoding
            ctxResultsMenu = new ContextMenuStrip();

            var menuDecodePayload = new ToolStripMenuItem("🔍 Decode Payload (UDS/Automotive)");
            menuDecodePayload.Click += MenuDecodePayload_Click;
            ctxResultsMenu.Items.Add(menuDecodePayload);

            var menuRunDecoderTests = new ToolStripMenuItem("🧪 Run Decoder Self-Tests");
            menuRunDecoderTests.Click += MenuRunDecoderTests_Click;
            ctxResultsMenu.Items.Add(menuRunDecoderTests);

            dgvResults.ContextMenuStrip = ctxResultsMenu;

            mainSplitContainer.Panel2.Controls.Add(dgvResults);
            dgvResults.BringToFront();
        }

        private void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            PerformAnalysis();
        }

        private void PerformAnalysis()
        {
            try
            {
                // Read FULL log text without truncation
                string logText = txtLogInput.Text;
                if (string.IsNullOrWhiteSpace(logText))
                {
                    MessageBox.Show("Please paste a log into the input box.", "No Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Normalize line endings: replace \r\n and \r with \n
                logText = logText.Replace("\r\n", "\n").Replace("\r", "\n");

                // Remove non-printable characters that can break matching (except tabs and newlines)
                logText = new string(logText.Where(c => c == '\n' || c == '\t' || (c >= 32 && c < 127) || c >= 128).ToArray());

                // Split into lines (handle both Windows \r\n and Unix \n line endings)
                string[] lines = logText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                // Run analysis
                currentResult = analyzer.Analyze(lines, null);

                // DIAGNOSTIC: Show analysis results
                System.Diagnostics.Debug.WriteLine($"==== ANALYSIS COMPLETED ====");
                System.Diagnostics.Debug.WriteLine($"Input lines array length: {lines.Length}");
                System.Diagnostics.Debug.WriteLine($"currentResult.AllLines.Count: {currentResult.AllLines.Count}");
                System.Diagnostics.Debug.WriteLine($"currentResult.Findings.Count: {currentResult.Findings.Count}");

                // Show first 3 lines captured
                for (int i = 0; i < Math.Min(3, currentResult.AllLines.Count); i++)
                {
                    var line = currentResult.AllLines[i];
                    System.Diagnostics.Debug.WriteLine($"  AllLines[{i}] LineNum={line.LineNumber}, RawText='{line.RawText}'");
                }

                // Update status with detailed information
                lblStatus.Text = string.Format("Parsed: {0} lines | All Lines Tracked: {1} | Findings: {2} | Critical: {3} | Error: {4} | Warning: {5} | Success: {6}",
                    currentResult.TotalLines,
                    currentResult.AllLines.Count,
                    currentResult.Findings.Count,
                    currentResult.CriticalCount,
                    currentResult.ErrorCount,
                    currentResult.WarningCount,
                    currentResult.SuccessCount);
                lblStatus.ForeColor = Color.DarkSlateGray;  // Reset to normal color

                // Apply filters and display
                ApplyFiltersAndDisplay();

                // Make log textbox read-only after analysis to prevent accidental edits
                txtLogInput.ReadOnly = true;
                txtLogInput.BackColor = Color.White;  // Keep white despite read-only

                // Run validation (optional, for debugging)
                analyzer.ValidateKeywordMatching();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during analysis: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TxtKeywordFilter_TextChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"==== TxtKeywordFilter_TextChanged ====");
            System.Diagnostics.Debug.WriteLine($"Current filter text: '{txtKeywordFilter.Text}'");
            System.Diagnostics.Debug.WriteLine($"currentResult is null: {currentResult == null}");

            // Re-apply filters without re-running analysis
            if (currentResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"currentResult.AllLines.Count: {currentResult.AllLines.Count}");
                ApplyFiltersAndDisplay();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("currentResult is NULL - cannot filter!");

                // Show helpful message if user tries to search without analyzing first
                if (!string.IsNullOrWhiteSpace(txtKeywordFilter.Text) && !string.IsNullOrWhiteSpace(txtLogInput.Text))
                {
                    lblStatus.Text = "⚠ Please click 'Analyze Log' button first before searching";
                    lblStatus.ForeColor = Color.DarkOrange;
                }
            }
        }

        private void TxtKeywordFilter_KeyDown(object? sender, KeyEventArgs e)
        {
            // DO NOT intercept Space key - let it work normally
            // This fixes the caret jumping bug
        }

        // 1. CORRECTED FILE LOAD METHOD
        private void BtnLoadFile_Click(object? sender, EventArgs e)
        {
            try
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Filter = "Log/Text Files (*.log;*.txt)|*.log;*.txt|All Files (*.*)|*.*",
                    Title = "Select Log File",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = openFileDialog.FileName;
                    var fileInfo = new System.IO.FileInfo(filePath);

                    if (fileInfo.Length > 5_242_880)
                    {
                        var result = MessageBox.Show(
                            string.Format("The selected file is {0:N2} MB. Large files may take time to load and process.\n\nDo you want to continue?",
                                fileInfo.Length / 1024.0 / 1024.0),
                            "Large File Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (result != DialogResult.Yes)
                            return;
                    }

                    // BOM-aware encoding detection
                    using var reader = new System.IO.StreamReader(filePath, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    string content = reader.ReadToEnd();

                    txtLogInput.Text = content;

                    string[] lines = content.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                    lblLoadedFile.Text = string.Format("Loaded: {0} ({1:N0} lines)", 
                        System.IO.Path.GetFileName(filePath), 
                        lines.Length);

                    lblStatus.Text = string.Format("File loaded: {0} lines ready for analysis", lines.Length);

                    currentResult = null;
                    displayedRows.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading file: " + ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnClearAll_Click(object? sender, EventArgs e)
        {
            // Clear log input
            txtLogInput.ReadOnly = false;  // Make editable for new log
            txtLogInput.Clear();

            // Clear results
            displayedRows.Clear();

            // Clear analysis result
            currentResult = null;

            // Clear keyword filter
            txtKeywordFilter.Clear();

            // Reset status
            lblStatus.Text = "Ready";
            lblLoadedFile.Text = "";

            // Reset checkboxes to default
            chkCritical.Checked = true;
            chkError.Checked = true;
            chkWarning.Checked = true;
            chkSuccess.Checked = true;
            chkNRC.Checked = true;
        }

        private void BtnDecoder_Click(object? sender, EventArgs e)
        {
            try
            {
                using var decoderForm = new DecoderForm();
                decoderForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening decoder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SeverityFilter_CheckedChanged(object? sender, EventArgs e)
        {
            if (currentResult != null)
            {
                ApplyFiltersAndDisplay();
            }
        }

        private void NrcFilter_CheckedChanged(object? sender, EventArgs e)
        {
            if (currentResult != null)
            {
                ApplyFiltersAndDisplay();
            }
        }

        private void ApplyFiltersAndDisplay()
        {
            if (currentResult == null)
                return;

            System.Diagnostics.Debug.WriteLine($"==== ApplyFiltersAndDisplay START ====");

            // Build the list of rows to display
            var rowsToDisplay = BuildDisplayedRows();

            System.Diagnostics.Debug.WriteLine($"BuildDisplayedRows returned: {rowsToDisplay.Count} rows");

            // Update the binding source
            resultsBindingSource.RaiseListChangedEvents = false;
            displayedRows.Clear();

            foreach (var row in rowsToDisplay)
            {
                displayedRows.Add(row);
            }

            System.Diagnostics.Debug.WriteLine($"displayedRows.Count after add: {displayedRows.Count}");

            resultsBindingSource.RaiseListChangedEvents = true;
            resultsBindingSource.ResetBindings(false);

            // Force binding context refresh
            if (dgvResults.DataSource != null && BindingContext != null)
            {
                var cm = (CurrencyManager)BindingContext[dgvResults.DataSource];
                cm.Refresh();
            }

            // Force grid refresh
            dgvResults.Invalidate();
            dgvResults.Refresh();
            dgvResults.Update();

            // Debug logging
            System.Diagnostics.Debug.WriteLine($"==== BINDING UPDATE ====");
            System.Diagnostics.Debug.WriteLine($"BindingList count: {displayedRows.Count}");
            System.Diagnostics.Debug.WriteLine($"Grid rows count: {dgvResults.Rows.Count}");
            System.Diagnostics.Debug.WriteLine($"Grid visible: {dgvResults.Visible}, Enabled: {dgvResults.Enabled}");
            System.Diagnostics.Debug.WriteLine($"Grid Bounds: {dgvResults.Bounds}");
            System.Diagnostics.Debug.WriteLine($"Columns count: {dgvResults.Columns.Count}");
            if (dgvResults.Columns.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Column 0 width: {dgvResults.Columns[0].Width}, visible: {dgvResults.Columns[0].Visible}");
                System.Diagnostics.Debug.WriteLine($"ColumnHeadersVisible: {dgvResults.ColumnHeadersVisible}");
                System.Diagnostics.Debug.WriteLine($"ColumnHeadersHeight: {dgvResults.ColumnHeadersHeight}");
            }

            // Update status
            lblStatus.Text += string.Format(" | Displayed: {0} rows", displayedRows.Count);

            System.Diagnostics.Debug.WriteLine($"==== ApplyFiltersAndDisplay END ====");
        }

        // 2. ENHANCED SANITIZER METHOD
        private string SanitizeForGrid(string? input)
        {
            if (input == null || input.Length == 0)
                return string.Empty;

            // Remove null characters first
            var sanitized = input.Replace("\0", "");

            // Replace tabs, newlines, and carriage returns with spaces
            sanitized = sanitized.Replace('\t', ' ')
                                 .Replace('\n', ' ')
                                 .Replace('\r', ' ');

            // Remove all control characters except space (ASCII 32)
            // Keep printable ASCII (32-126) and extended Unicode (>= 128)
            var chars = sanitized.Where(c => 
                c == ' ' || 
                (c >= 32 && c <= 126) || 
                c >= 128
            ).ToArray();
            
            sanitized = new string(chars);

            // Collapse multiple spaces to single space
            while (sanitized.Contains("  "))
                sanitized = sanitized.Replace("  ", " ");

            return sanitized.Trim();
        }

        private List<ResultRow> BuildDisplayedRows()
        {
            var result = new List<ResultRow>();

            if (currentResult == null)
                return result;

            // Parse keywords
            string[] keywords = ParseKeywords(txtKeywordFilter.Text);

            System.Diagnostics.Debug.WriteLine($"==== BuildDisplayedRows ====");
            System.Diagnostics.Debug.WriteLine($"Total AllLines: {currentResult.AllLines.Count}");
            System.Diagnostics.Debug.WriteLine($"Keywords: [{string.Join(", ", keywords)}] Count: {keywords.Length}");

            // 3. UPDATED BINDING IN BuildDisplayedRows - KEYWORD MODE
            if (keywords.Length > 0)
            {
                int matchCount = 0;

                System.Diagnostics.Debug.WriteLine($"Processing {currentResult.AllLines.Count} lines with keywords: [{string.Join(", ", keywords)}]");
                System.Diagnostics.Debug.WriteLine($"Keyword count: {keywords.Length}");

                // Check if any severity filters are selected
                bool anySeveritySelected = chkCritical.Checked || chkError.Checked || 
                                          chkWarning.Checked || chkSuccess.Checked;

                System.Diagnostics.Debug.WriteLine($"Any severity selected: {anySeveritySelected}");
                System.Diagnostics.Debug.WriteLine($"  Critical: {chkCritical.Checked}, Error: {chkError.Checked}, Warning: {chkWarning.Checked}, Success: {chkSuccess.Checked}");

                // Debug: Show first few lines being searched
                for (int debugIdx = 0; debugIdx < Math.Min(5, currentResult.AllLines.Count); debugIdx++)
                {
                    var debugLine = currentResult.AllLines[debugIdx];
                    System.Diagnostics.Debug.WriteLine($"  Sample line {debugLine.LineNumber}: '{debugLine.RawText}' (Length: {(debugLine.RawText ?? "").Length})");
                }

                int totalMatches = 0;
                int linesProcessed = 0;

                foreach (var logLine in currentResult.AllLines)
                {
                    linesProcessed++;
                    string rawText = logLine.RawText ?? "";

                    bool matches = keywords.Any(kw => 
                        rawText.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);

                    // Debug: For first keyword, check each line
                    if (linesProcessed <= 10 && keywords.Length > 0)
                    {
                        string firstKeyword = keywords[0];
                        bool contains = rawText.IndexOf(firstKeyword, StringComparison.OrdinalIgnoreCase) >= 0;
                        System.Diagnostics.Debug.WriteLine($"  Line {logLine.LineNumber}: Contains '{firstKeyword}'? {contains} | Text: '{(rawText.Length > 100 ? rawText.Substring(0, 100) + "..." : rawText)}'");
                    }

                    // Debug: Log matches for "soc" keyword specifically
                    if (keywords.Any(k => k.Equals("soc", StringComparison.OrdinalIgnoreCase)) && 
                        rawText.IndexOf("soc", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"  ✓ SOC MATCH on line {logLine.LineNumber}: '{rawText}'");
                    }

                    if (matches)
                    {
                        totalMatches++;

                        FindingSeverity severity = logLine.DetectedSeverity;
                        string code = logLine.IsFinding ? "FINDING" : "KEYWORD";

                        // KEYWORD MODE: Show ALL keyword matches
                        // Don't apply severity or NRC filters - user wants to see EVERY line with their keyword

                        matchCount++;

                        Color rowColor = severity switch
                        {
                            FindingSeverity.Critical => Color.LightCoral,
                            FindingSeverity.Error => Color.LightSalmon,
                            FindingSeverity.Warning => Color.LightYellow,
                            FindingSeverity.Success => Color.LightGreen,
                            FindingSeverity.Info => Color.LightCyan,
                            _ => Color.White
                        };

                        string lineText = SanitizeForGrid(rawText);
                        string timestamp = ExtractTimestamp(rawText);

                        result.Add(new ResultRow
                        {
                            LineNumber = logLine.LineNumber,
                            Timestamp = timestamp,
                            Code = SanitizeForGrid(code),
                            Severity = SanitizeForGrid(severity.ToString()),
                            LineText = lineText,
                            RowColor = rowColor
                        });
                    }
                }

                System.Diagnostics.Debug.WriteLine($"==== KEYWORD SEARCH COMPLETE ====");
                System.Diagnostics.Debug.WriteLine($"Lines processed: {linesProcessed}");
                System.Diagnostics.Debug.WriteLine($"Total matches: {totalMatches}");
                System.Diagnostics.Debug.WriteLine($"Rows added to result list: {matchCount}");

                lblStatus.Text = string.Format("Keyword Search: {0} lines scanned | {1} matches found | Keywords: [{2}]",
                    currentResult.AllLines.Count,
                    matchCount,
                    string.Join(", ", keywords));
            }

            // 4. UPDATED BINDING IN BuildDisplayedRows - NORMAL MODE
            else
            {
                bool anySeveritySelected = chkCritical.Checked || chkError.Checked || 
                                      chkWarning.Checked || chkSuccess.Checked;

                // Show only findings (no non-findings)
                List<Finding> findingsToShow = currentResult.Findings.ToList();

                if (anySeveritySelected)
                {
                    findingsToShow = findingsToShow.Where(f =>
                        (chkCritical.Checked && f.Severity == FindingSeverity.Critical) ||
                        (chkError.Checked && f.Severity == FindingSeverity.Error) ||
                        (chkWarning.Checked && f.Severity == FindingSeverity.Warning) ||
                        (chkSuccess.Checked && f.Severity == FindingSeverity.Success)
                    ).ToList();
                }
                else if (!chkNRC.Checked)
                {
                    // Only clear if both severity filters AND NRC filter are inactive
                    findingsToShow.Clear();
                    lblStatus.Text = "No filters active. Select severity filters, NRC filter, or enter keywords.";
                }

                System.Diagnostics.Debug.WriteLine($"Findings mode: {findingsToShow.Count} findings");

                // Convert to ResultRow
                foreach (var finding in findingsToShow)
                {
                    // Apply NRC code filter
                    if (!ShouldShowBasedOnNrcFilter(finding.LineText ?? ""))
                    {
                        continue;  // Skip lines with filtered-out NRC codes
                    }

                    Color rowColor = finding.Severity switch
                    {
                        FindingSeverity.Critical => Color.LightCoral,
                        FindingSeverity.Error => Color.LightSalmon,
                        FindingSeverity.Warning => Color.LightYellow,
                        FindingSeverity.Success => Color.LightGreen,
                        FindingSeverity.Info => Color.LightCyan,
                        _ => Color.White
                    };

                    string lineText = SanitizeForGrid(finding.LineText ?? "");
                    string timestamp = ExtractTimestamp(finding.LineText ?? "");

                    result.Add(new ResultRow
                    {
                        LineNumber = finding.LineNumber,
                        Timestamp = timestamp,
                        Code = SanitizeForGrid(finding.Code ?? "UNKNOWN"),
                        Severity = SanitizeForGrid(finding.Severity.ToString()),
                        LineText = lineText,
                        RowColor = rowColor
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total rows to display: {result.Count}");
            return result;
        }

        private string[] ParseKeywords(string keywordText)
        {
            // Trim whitespace first
            keywordText = keywordText?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(keywordText))
                return Array.Empty<string>();

            // Split by comma, space, semicolon, tab, newline
            char[] separators = { ',', ' ', ';', '\t', '\r', '\n' };
            var tokens = keywordText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // Trim and filter - remove any tokens that are just whitespace or single chars that might be artifacts
            return tokens
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Checks if a line should be shown based on NRC filter
        /// When NRC is checked: Shows lines containing NRC patterns
        /// When NRC is unchecked and no severities selected: Hides all
        /// When NRC is unchecked and severities selected: Shows non-NRC lines
        /// </summary>
        private bool ShouldShowBasedOnNrcFilter(string lineText)
        {
            if (string.IsNullOrEmpty(lineText))
                return false;

            // Check if any severity filters are active
            bool anySeveritySelected = chkCritical.Checked || chkError.Checked || 
                                      chkWarning.Checked || chkSuccess.Checked;

            // Simplified NRC detection (no regex to avoid runtime issues)
            // Look for explicit "NRC" or "Negative Response" mentions
            bool containsNrc = lineText.Contains("NRC", StringComparison.OrdinalIgnoreCase) ||
                              lineText.Contains("Negative Response", StringComparison.OrdinalIgnoreCase);

            // If NRC is checked and no severity filters are active, only show NRC lines
            if (chkNRC.Checked && !anySeveritySelected)
            {
                return containsNrc;
            }

            // If NRC is unchecked, hide NRC lines
            if (!chkNRC.Checked && containsNrc)
            {
                return false;
            }

            return true;  // Show the line by default
        }

        private string ExtractTimestamp(string logLine)
        {
            if (string.IsNullOrWhiteSpace(logLine))
                return "";

            // Common timestamp patterns at the beginning of log lines
            var patterns = new[]
            {
                // ISO 8601: 2024-01-15T14:30:45.123Z or 2024-01-15 14:30:45.123
                @"^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?(?:Z|[+-]\d{2}:?\d{2})?",

                // Common log format: 01/15/2024 14:30:45 or 01-15-2024 14:30:45
                @"^\d{2}[/-]\d{2}[/-]\d{4}\s+\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?",

                // Time only: 14:30:45.123 or 14:30:45
                @"^\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?",

                // Unix timestamp: [1234567890] or [1234567890.123]
                @"^\[\d{10,13}(?:\.\d{1,6})?\]",

                // Brackets with date: [2024-01-15 14:30:45]
                @"^\[\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?\]",

                // Month/Day format: Jan 15 14:30:45 or 01/15 14:30:45
                @"^(?:[A-Z][a-z]{2}\s+\d{1,2}|\d{2}/\d{2})\s+\d{2}:\d{2}:\d{2}(?:\.\d{1,6})?",
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(logLine, pattern);
                if (match.Success)
                {
                    // Remove brackets if present
                    return match.Value.Trim('[', ']', ' ');
                }
            }

            return "";
        }

        private void MenuDecodePayload_Click(object? sender, EventArgs e)
        {
            try
            {
                if (dgvResults.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a row to decode.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var selectedRow = dgvResults.SelectedRows[0];
                var rowIndex = selectedRow.Index;

                if (rowIndex >= 0 && rowIndex < displayedRows.Count)
                {
                    var resultRow = displayedRows[rowIndex];
                    var lineText = resultRow.LineText;

                    if (string.IsNullOrWhiteSpace(lineText))
                    {
                        MessageBox.Show("Selected row has no line text to decode.", "Empty Line", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Decode the payload
                    var decoded = DecoderIntegration.TryDecodeFromLine(lineText);

                    // Show result in a dialog
                    var resultForm = new Form
                    {
                        Text = $"Decoded Payload - Line {resultRow.LineNumber}",
                        Size = new Size(800, 600),
                        StartPosition = FormStartPosition.CenterParent,
                        ShowIcon = false,
                        MinimizeBox = false,
                        MaximizeBox = true
                    };

                    var txtResult = new TextBox
                    {
                        Multiline = true,
                        Dock = DockStyle.Fill,
                        ScrollBars = ScrollBars.Both,
                        Font = new Font("Consolas", 9F),
                        ReadOnly = true,
                        BackColor = Color.White,
                        Text = decoded.ToFormattedString()
                    };

                    var btnClose = new Button
                    {
                        Text = "Close",
                        Dock = DockStyle.Bottom,
                        Height = 40,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                    };
                    btnClose.Click += (s, ev) => resultForm.Close();

                    resultForm.Controls.Add(txtResult);
                    resultForm.Controls.Add(btnClose);

                    resultForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error decoding payload: {ex.Message}", "Decode Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuRunDecoderTests_Click(object? sender, EventArgs e)
        {
            try
            {
                var testResults = DecoderIntegration.RunDecoderSelfTests();

                var resultForm = new Form
                {
                    Text = "Decoder Self-Test Results",
                    Size = new Size(700, 500),
                    StartPosition = FormStartPosition.CenterParent,
                    ShowIcon = false,
                    MinimizeBox = false,
                    MaximizeBox = true
                };

                var txtResult = new TextBox
                {
                    Multiline = true,
                    Dock = DockStyle.Fill,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 9F),
                    ReadOnly = true,
                    BackColor = Color.White,
                    Text = string.Join(Environment.NewLine + Environment.NewLine, testResults)
                };

                var btnClose = new Button
                {
                    Text = "Close",
                    Dock = DockStyle.Bottom,
                    Height = 40,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnClose.Click += (s, ev) => resultForm.Close();

                resultForm.Controls.Add(txtResult);
                resultForm.Controls.Add(btnClose);

                resultForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running tests: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Simple Search (Wireshark-style)

        /// <summary>
        /// Finds next occurrence of search text (like Wireshark Ctrl+F)
        /// </summary>
        private void FindNextInLog(string searchText)
        {
            if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(txtLogInput.Text))
                return;

            int startIndex = txtLogInput.SelectionStart + txtLogInput.SelectionLength;

            if (startIndex >= txtLogInput.Text.Length)
                startIndex = 0; // Wrap around

            int index = txtLogInput.Text.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                txtLogInput.Focus();  // Must focus before selecting to make ScrollToCaret work
                txtLogInput.SelectionStart = index;
                txtLogInput.SelectionLength = searchText.Length;
                txtLogInput.ScrollToCaret();
                lblStatus.Text = $"Found '{searchText}' at position {index}";
            }
            else
            {
                // Try from beginning (wrap)
                index = txtLogInput.Text.IndexOf(searchText, 0, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    txtLogInput.Focus();  // Must focus before selecting to make ScrollToCaret work
                    txtLogInput.SelectionStart = index;
                    txtLogInput.SelectionLength = searchText.Length;
                    txtLogInput.ScrollToCaret();
                    lblStatus.Text = $"Search wrapped - found at position {index}";
                }
                else
                {
                    lblStatus.Text = $"'{searchText}' not found";
                }
            }
        }

        /// <summary>
        /// Finds previous occurrence of search text
        /// </summary>
        private void FindPrevInLog(string searchText)
        {
            if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(txtLogInput.Text))
                return;

            int startIndex = txtLogInput.SelectionStart - 1;

            if (startIndex < 0)
                startIndex = txtLogInput.Text.Length; // Wrap to end

            int index = txtLogInput.Text.LastIndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                txtLogInput.Focus();  // Must focus before selecting to make ScrollToCaret work
                txtLogInput.SelectionStart = index;
                txtLogInput.SelectionLength = searchText.Length;
                txtLogInput.ScrollToCaret();
                lblStatus.Text = $"Found '{searchText}' at position {index}";
            }
            else
            {
                // Try from end (wrap)
                index = txtLogInput.Text.LastIndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    txtLogInput.Focus();  // Must focus before selecting to make ScrollToCaret work
                    txtLogInput.SelectionStart = index;
                    txtLogInput.SelectionLength = searchText.Length;
                    txtLogInput.ScrollToCaret();
                    lblStatus.Text = $"Search wrapped - found at position {index}";
                }
                else
                {
                    lblStatus.Text = $"'{searchText}' not found";
                }
            }
        }

        #endregion
    }
}
