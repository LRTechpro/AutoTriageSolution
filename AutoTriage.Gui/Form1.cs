using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AutoTriage.Core;

namespace AutoTriage.Gui
{
    // ResultRow class for DataGridView binding
    public class ResultRow
    {
        public int LineNumber { get; set; }
        public string Code { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Title { get; set; } = "";
        public string LineText { get; set; } = "";
        public Color RowColor { get; set; } = Color.White;
    }

    public partial class Form1 : Form
    {
        private LogAnalyzer analyzer;
        private AnalysisResult? currentResult;
        private BindingList<ResultRow> displayedRows;
        private BindingSource resultsBindingSource = new BindingSource();

        // UI Controls
        private SplitContainer mainSplitContainer = null!;
        private TextBox txtLogInput = null!;
        private TextBox txtKeywordFilter = null!;
        private CheckBox chkIncludeNonFindings = null!;
        private CheckBox chkCritical = null!;
        private CheckBox chkError = null!;
        private CheckBox chkWarning = null!;
        private CheckBox chkSuccess = null!;
        private Button btnLoadFile = null!;
        private Button btnAnalyze = null!;
        private Button btnClearAll = null!;
        private DataGridView dgvResults = null!;
        private Label lblStatus = null!;
        private Label lblLoadedFile = null!;

        public Form1()
        {
            analyzer = new LogAnalyzer();
            displayedRows = new BindingList<ResultRow>();
            resultsBindingSource.DataSource = displayedRows;
            InitializeCustomUI();
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
                Font = new Font("Segoe UI", 9F)
            };
            btnClearAll.Click += BtnClearAll_Click;
            mainSplitContainer.Panel1.Controls.Add(btnClearAll);

            // Log input textbox
            txtLogInput = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F),
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
                Height = 90,  // Reduced to 90 for better space
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

            chkIncludeNonFindings = new CheckBox
            {
                Text = "Include non-finding matches",
                Location = new Point(370, 35),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9F)
            };
            chkIncludeNonFindings.CheckedChanged += ChkIncludeNonFindings_CheckedChanged;
            filterPanel.Controls.Add(chkIncludeNonFindings);

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
                BorderStyle = BorderStyle.Fixed3D,
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
                Name = "colTitle", 
                HeaderText = "Title", 
                DataPropertyName = "Title",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells,
                MinimumWidth = 150,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.False }
            });

            dgvResults.Columns.Add(new DataGridViewTextBoxColumn 
            { 
                Name = "colLineText", 
                HeaderText = "Line Text", 
                DataPropertyName = "LineText",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 300,
                DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
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
                    dgvRow.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, row.RowColor);
                    dgvRow.DefaultCellStyle.SelectionForeColor = Color.Black;
                }
            };

            mainSplitContainer.Panel2.Controls.Add(dgvResults);
            dgvResults.BringToFront();
        }

        private void BtnAnalyze_Click(object? sender, EventArgs e)
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

                // Split into lines
                string[] lines = logText.Split(new char[] { '\n' }, StringSplitOptions.None);

                // Run analysis
                currentResult = analyzer.Analyze(lines, null);

                // Update status with detailed information
                lblStatus.Text = string.Format("Parsed: {0} lines | All Lines Tracked: {1} | Findings: {2} | Critical: {3} | Error: {4} | Warning: {5} | Success: {6}",
                    currentResult.TotalLines,
                    currentResult.AllLines.Count,
                    currentResult.Findings.Count,
                    currentResult.CriticalCount,
                    currentResult.ErrorCount,
                    currentResult.WarningCount,
                    currentResult.SuccessCount);

                // Apply filters and display
                ApplyFiltersAndDisplay();

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
            // Re-apply filters without re-running analysis
            if (currentResult != null)
            {
                ApplyFiltersAndDisplay();
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
            chkIncludeNonFindings.Checked = true;
        }

        private void ChkIncludeNonFindings_CheckedChanged(object? sender, EventArgs e)
        {
            if (currentResult != null)
            {
                ApplyFiltersAndDisplay();
            }
        }

        private void SeverityFilter_CheckedChanged(object? sender, EventArgs e)
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

                foreach (var logLine in currentResult.AllLines)
                {
                    bool matches = keywords.Any(kw => 
                        logLine.RawText.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (matches)
                    {
                        matchCount++;

                        FindingSeverity severity = logLine.DetectedSeverity;
                        string code = logLine.IsFinding ? "FINDING" : "KEYWORD";

                        Color rowColor = severity switch
                        {
                            FindingSeverity.Critical => Color.LightCoral,
                            FindingSeverity.Error => Color.LightSalmon,
                            FindingSeverity.Warning => Color.LightYellow,
                            FindingSeverity.Success => Color.LightGreen,
                            FindingSeverity.Info => Color.LightCyan,
                            _ => Color.White
                        };

                        string rawText = logLine.RawText ?? "";
                        string title = SanitizeForGrid(rawText);
                        if (title.Length > 80)
                            title = title.Substring(0, 77) + "...";

                        string lineText = SanitizeForGrid(rawText);

                        result.Add(new ResultRow
                        {
                            LineNumber = logLine.LineNumber,
                            Code = SanitizeForGrid(code),
                            Severity = SanitizeForGrid(severity.ToString()),
                            Title = title,
                            LineText = lineText,
                            RowColor = rowColor
                        });
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Keyword matches: {matchCount}");
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

                List<Finding> findingsToShow;

                if (chkIncludeNonFindings.Checked)
                {
                    findingsToShow = currentResult.Findings.ToList();

                    var nonFindings = currentResult.AllLines
                        .Where(line => !line.IsFinding)
                        .Select(line => new Finding
                        {
                            LineNumber = line.LineNumber,
                            Severity = line.DetectedSeverity,
                            LineText = (line.RawText ?? "").Trim(),
                            Title = (line.RawText ?? "").Trim().Length > 80 ? 
                                   (line.RawText ?? "").Trim().Substring(0, 77) + "..." : 
                                   (line.RawText ?? "").Trim(),
                            Code = "INFO",
                            RuleId = $"LINE_{line.LineNumber}",
                            Evidence = (line.RawText ?? "").Trim(),
                            WhyItMatters = "Non-finding log line"
                        });

                    findingsToShow.AddRange(nonFindings);
                    findingsToShow = findingsToShow.OrderBy(f => f.LineNumber).ToList();
                }
                else
                {
                    findingsToShow = currentResult.Findings.ToList();
                }

                if (anySeveritySelected)
                {
                    findingsToShow = findingsToShow.Where(f =>
                        (chkCritical.Checked && f.Severity == FindingSeverity.Critical) ||
                        (chkError.Checked && f.Severity == FindingSeverity.Error) ||
                        (chkWarning.Checked && f.Severity == FindingSeverity.Warning) ||
                        (chkSuccess.Checked && f.Severity == FindingSeverity.Success)
                    ).ToList();
                }
                else
                {
                    findingsToShow.Clear();
                    lblStatus.Text = "No filters active. Select severity filters or enter keywords.";
                }

                System.Diagnostics.Debug.WriteLine($"Findings mode: {findingsToShow.Count} findings");

                // Convert to ResultRow
                foreach (var finding in findingsToShow)
                {
                    Color rowColor = finding.Severity switch
                    {
                        FindingSeverity.Critical => Color.LightCoral,
                        FindingSeverity.Error => Color.LightSalmon,
                        FindingSeverity.Warning => Color.LightYellow,
                        FindingSeverity.Success => Color.LightGreen,
                        FindingSeverity.Info => Color.LightCyan,
                        _ => Color.White
                    };

                    string rawTitle = finding.Title ?? finding.LineText ?? "";
                    string title = SanitizeForGrid(rawTitle);
                    if (title.Length > 80)
                        title = title.Substring(0, 77) + "...";

                    string lineText = SanitizeForGrid(finding.LineText ?? "");

                    result.Add(new ResultRow
                    {
                        LineNumber = finding.LineNumber,
                        Code = SanitizeForGrid(finding.Code ?? "UNKNOWN"),
                        Severity = SanitizeForGrid(finding.Severity.ToString()),
                        Title = title,
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
            if (string.IsNullOrWhiteSpace(keywordText))
                return Array.Empty<string>();

            // Split by comma, space, semicolon, tab, newline
            char[] separators = { ',', ' ', ';', '\t', '\r', '\n' };
            var tokens = keywordText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // Trim and filter
            return tokens
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
