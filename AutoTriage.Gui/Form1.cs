using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AutoTriage.Gui
{
    /// <summary>
    /// Main WinForms user interface for the Auto Log Triage Helper.
    /// 
    /// Design intent:
    /// - This project (AutoTriage.Gui) is responsible ONLY for the user experience:
    ///   input, buttons, filtering, and presenting results.
    /// - The analysis logic should ultimately live in the DLL project (AutoTriage.Core)
    ///   so it can be reused and tested independently of the UI.
    /// </summary>
    public class Form1 : Form
    {
        // =======================================================
        // UI CONTROL DECLARATIONS (GUI COMPONENTS)
        // =======================================================
        // These fields represent the "surface area" of the UI.
        // Declaring them as readonly ensures the control references
        // cannot be re-assigned after initialization (safer state management).
        private readonly TextBox txtLogInput = new TextBox();
        private readonly Button btnAnalyze = new Button();
        private readonly Button btnLoadFile = new Button();
        private readonly Button btnClear = new Button();

        private readonly CheckBox chkShowErrors = new CheckBox();
        private readonly CheckBox chkShowWarnings = new CheckBox();
        private readonly CheckBox chkShowSuccess = new CheckBox();

        private readonly DataGridView dgvResults = new DataGridView();
        private readonly Label lblTitle = new Label();
        private readonly Label lblSummary = new Label();

        // =======================================================
        // FORM INITIALIZATION (ENTRY POINT INTO THE UI)
        // =======================================================
        // The constructor performs three responsibilities:
        // 1) Apply high-level window settings (title, size, startup location)
        // 2) Build the UI layout and configure all controls in code (no designer)
        // 3) Wire all user actions (button clicks + checkbox filters) to handlers
        public Form1()
        {
            // -----------------------
            // Window / Form Settings
            // -----------------------
            // Text: visible title on the top bar (professional tool naming).
            // StartPosition: centers the window for predictable user experience.
            // MinimumSize: prevents the UI from becoming unusable when resized too small.
            Text = "Auto Log Triage Helper";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 650);

            // -----------------------
            // Layout Construction
            // -----------------------
            // Build the UI entirely in code (code-first UI).
            // This demonstrates control configuration and layout management directly.
            BuildLayout();

            // -----------------------
            // Event Wiring (User Actions)
            // -----------------------
            // Buttons
            btnClear.Click += (_, __) => ClearAll();
            btnLoadFile.Click += (_, __) => LoadFromFile();

            // NOTE: Currently calling AnalyzeWithDll().
            // For the final "DLL assignment" architecture, this should call the DLL analyzer:
            // e.g., new LogAnalyzer().Analyze(txtLogInput.Text)
            btnAnalyze.Click += (_, __) => AnalyzeWithDll();

            // Checkboxes (filters)
            // When the filter toggles change, we reapply filtering to what is displayed.
            chkShowErrors.CheckedChanged += (_, __) => FilterChanged();
            chkShowWarnings.CheckedChanged += (_, __) => FilterChanged();
            chkShowSuccess.CheckedChanged += (_, __) => FilterChanged();

            // Initialize the summary line so the UI starts in a clean, known state.
            UpdateSummary(totalLines: 0, errors: 0, warnings: 0, success: 0, score: 0);
        }

        // =======================================================
        // UI LAYOUT (COMPOSITION OF CONTROLS)
        // =======================================================
        // This method constructs a layout that feels like a real log triage tool:
        // - Title area
        // - Log input area
        // - Action buttons + filter toggles
        // - Results table + summary
        private void BuildLayout()
        {
            // Root layout uses a TableLayoutPanel to create predictable resizing behavior.
            // Rows:
            //   Row 0: Title
            //   Row 1: Log input (fixed height)
            //   Row 2: Controls row (buttons + checkboxes)
            //   Row 3: Results + Summary (expands to fill remaining space)
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12),
            };

            // Row sizing strategy:
            // - AutoSize for title and controls row
            // - Fixed height for the log input area (so it stays usable)
            // - Percent for results so it grows/shrinks with the window
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // Title row
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));    // Log input row
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));         // Controls row
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));     // Results row

            Controls.Add(root);

            // -----------------------
            // Title Label
            // -----------------------
            // This sets a clear identity for the tool (professional UX).
            lblTitle.Text = "Auto Log Triage Helper";
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.Margin = new Padding(0, 0, 0, 8);
            root.Controls.Add(lblTitle, 0, 0);

            // -----------------------
            // Log Input TextBox
            // -----------------------
            // Multiline + scrollbars + monospaced font helps users paste and inspect logs.
            txtLogInput.Multiline = true;
            txtLogInput.ScrollBars = ScrollBars.Both;
            txtLogInput.WordWrap = false;              // Keeps log alignment predictable
            txtLogInput.Font = new Font("Consolas", 10); // Typical "log viewer" look
            txtLogInput.Dock = DockStyle.Fill;
            txtLogInput.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(txtLogInput, 0, 1);

            // -----------------------
            // Controls Row (Buttons + Filters)
            // -----------------------
            // Two-column TableLayoutPanel:
            // - Left: buttons (Analyze/Load/Clear)
            // - Right: filter toggles (Errors/Warn/Success)
            var controlsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
            };

            controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // Buttons
            controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // Filters
            root.Controls.Add(controlsRow, 0, 2);

            // Buttons panel uses FlowLayoutPanel so buttons naturally line up left-to-right.
            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            controlsRow.Controls.Add(buttonsPanel, 0, 0);

            // Button configuration:
            // - Consistent sizing improves usability and visual balance
            btnAnalyze.Text = "Analyze";
            btnAnalyze.Size = new Size(110, 34);

            btnLoadFile.Text = "Load File";
            btnLoadFile.Size = new Size(110, 34);

            btnClear.Text = "Clear";
            btnClear.Size = new Size(110, 34);

            buttonsPanel.Controls.Add(btnAnalyze);
            buttonsPanel.Controls.Add(btnLoadFile);
            buttonsPanel.Controls.Add(btnClear);

            // Filters panel uses FlowLayoutPanel so toggles align and are easy to scan.
            var filtersPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(18, 6, 0, 0),
                Padding = new Padding(0),
            };
            controlsRow.Controls.Add(filtersPanel, 1, 0);

            // Default filters to checked so users see results immediately.
            chkShowErrors.Text = "Show Errors";
            chkShowErrors.Checked = true;
            chkShowErrors.AutoSize = true;

            chkShowWarnings.Text = "Show Warnings";
            chkShowWarnings.Checked = true;
            chkShowWarnings.AutoSize = true;

            chkShowSuccess.Text = "Show Success";
            chkShowSuccess.Checked = true;
            chkShowSuccess.AutoSize = true;

            filtersPanel.Controls.Add(chkShowErrors);
            filtersPanel.Controls.Add(chkShowWarnings);
            filtersPanel.Controls.Add(chkShowSuccess);

            // -----------------------
            // Results Area (Grid + Summary)
            // -----------------------
            // This container ensures the grid expands while the summary stays visible.
            var resultsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            resultsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid fills space
            resultsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // Summary fixed
            root.Controls.Add(resultsContainer, 0, 3);

            ConfigureResultsGrid();
            resultsContainer.Controls.Add(dgvResults, 0, 0);

            lblSummary.AutoSize = true;
            lblSummary.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblSummary.Margin = new Padding(0, 8, 0, 0);
            resultsContainer.Controls.Add(lblSummary, 0, 1);
        }

        // =======================================================
        // RESULTS GRID CONFIGURATION (DATA PRESENTATION)
        // =======================================================
        // This method configures the DataGridView to behave like a read-only results viewer.
        // Key UX goals:
        // - Read-only results (prevents accidental edits)
        // - Full-row selection (feels like a log viewer)
        // - Auto-sized columns (consistent readability)
        private void ConfigureResultsGrid()
        {
            dgvResults.Dock = DockStyle.Fill;

            // Lock down the grid so it behaves like a viewer, not a spreadsheet editor.
            dgvResults.ReadOnly = true;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.AllowUserToResizeRows = false;

            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.MultiSelect = false;

            // Auto-size columns to fill space evenly with emphasis on Message column.
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Ensure a predictable schema each time the UI is built.
            dgvResults.Columns.Clear();

            // Column: Line number in the source log (helps correlation).
            var colLine = new DataGridViewTextBoxColumn
            {
                Name = "colLine",
                HeaderText = "Line",
                FillWeight = 12
            };

            // Column: Severity classification (error/warn/success).
            var colSeverity = new DataGridViewTextBoxColumn
            {
                Name = "colSeverity",
                HeaderText = "Severity",
                FillWeight = 18
            };

            // Column: Short code used to categorize the finding.
            var colCode = new DataGridViewTextBoxColumn
            {
                Name = "colCode",
                HeaderText = "Code",
                FillWeight = 18
            };

            // Column: Full line message (largest column for readability).
            var colMessage = new DataGridViewTextBoxColumn
            {
                Name = "colMessage",
                HeaderText = "Message",
                FillWeight = 52
            };

            dgvResults.Columns.AddRange(colLine, colSeverity, colCode, colMessage);
        }

        // =======================================================
        // USER ACTIONS (BUTTON HANDLERS)
        // =======================================================

        /// <summary>
        /// Clears the current UI state:
        /// - removes pasted log text
        /// - clears any results in the grid
        /// - resets the summary counters and score
        /// 
        /// This supports "never terminates prematurely" by restoring a clean state safely.
        /// </summary>
        private void ClearAll()
        {
            txtLogInput.Clear();
            dgvResults.Rows.Clear();
            UpdateSummary(0, 0, 0, 0, 0);
        }

        /// <summary>
        /// Loads log text from a file using OpenFileDialog and places it into the input textbox.
        /// Error handling is included so the program remains stable on file I/O failures.
        /// </summary>
        private void LoadFromFile()
        {
            // NOTE: "using var" requires C# 8+. If your course/lab uses C# 7.3,
            // convert this to:
            // using (var ofd = new OpenFileDialog()) { ... }
            using var ofd = new OpenFileDialog
            {
                Title = "Select a log file",
                Filter = "Log/Text files (*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*"
            };

            // User cancelled: return gracefully without side effects.
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // Read entire file into the textbox. For very large logs,
                // you could stream line-by-line, but full read is acceptable for this assignment.
                txtLogInput.Text = File.ReadAllText(ofd.FileName);
            }
            catch (Exception ex)
            {
                // Keep the application alive even when file read fails.
                MessageBox.Show(this, $"Could not read file.\n\n{ex.Message}", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =======================================================
        // ANALYSIS (DLL INTEGRATION)
        // =======================================================
        // IMPORTANT ARCHITECTURE NOTE:
        // For this assignment, analysis logic is intentionally separated into
        // the AutoTriage.Core DLL. This GUI method is responsible only for:
        // 1) Passing raw log input to the DLL
        // 2) Displaying the returned analysis results in the UI
        //
        // This design mirrors real-world applications where business logic
        // is decoupled from presentation logic.
        /// <summary>
        /// Calls the AutoTriage.Core DLL to analyze the log input
        /// and displays the results in the results grid.
        /// </summary>
        private void AnalyzeWithDll()
        {
            // Capture current text input once to avoid repeatedly accessing the textbox.
            var text = txtLogInput.Text;

            // Guard clause: prompt user instead of failing on empty input.
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(this, "Paste logs or load a file before analyzing.", "No Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Clear previous results so each analysis run starts fresh.
            dgvResults.Rows.Clear();

            // Split into lines (array of strings) for line-by-line processing.
            // This demonstrates working with array types and string processing.
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Counters used for summary statistics and scoring.
            int errors = 0, warnings = 0, success = 0;

            // Iterate through each line and apply simple classification rules.
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string upper = line.ToUpperInvariant();

                string severity;
                string code;

                // Classification rules:
                // - ERROR / FAIL => Error
                // - WARN         => Warning
                // - SUCCESS/COMPLETE => Success
                if (upper.Contains("ERROR") || upper.Contains("FAIL"))
                {
                    severity = "ERROR";
                    code = "E-LOG";
                    errors++;
                }
                else if (upper.Contains("WARN"))
                {
                    severity = "WARN";
                    code = "W-LOG";
                    warnings++;
                }
                else if (upper.Contains("SUCCESS") || upper.Contains("COMPLETE"))
                {
                    severity = "SUCCESS";
                    code = "S-LOG";
                    success++;
                }
                else
                {
                    // If it is not relevant to triage, we skip it.
                    continue;
                }

                // Apply user-selected filter toggles before displaying in the grid.
                if (ShouldDisplaySeverity(severity))
                {
                    // Add a row to the results grid.
                    // Columns: LineNumber, Severity, Code, Message
                    dgvResults.Rows.Add(i + 1, severity, code, line);
                }
            }

            // Compute score based on weighted issues.
            int score = ComputeScore(errors, warnings);

            // Update summary line at bottom of UI.
            UpdateSummary(lines.Length, errors, warnings, success, score);
        }

        // =======================================================
        // FILTERING / SCORING / SUMMARY (UI SUPPORT FUNCTIONS)
        // =======================================================

        /// <summary>
        /// Determines whether a finding should be displayed based on the filter checkboxes.
        /// </summary>
        /// <param name="severity">Severity string derived from analysis logic.</param>
        /// <returns>True if the severity is enabled in the UI filters; otherwise false.</returns>
        private bool ShouldDisplaySeverity(string severity)
        {
            // Switch expression maps severity category to a checkbox toggle.
            return severity switch
            {
                "ERROR" => chkShowErrors.Checked,
                "WARN" => chkShowWarnings.Checked,
                "SUCCESS" => chkShowSuccess.Checked,
                _ => true
            };
        }

        /// <summary>
        /// Computes a simple 0-100 health score.
        /// - Start at 100
        /// - Subtract 15 points per error
        /// - Subtract 5 points per warning
        /// - Clamp final value between 0 and 100
        /// </summary>
        private int ComputeScore(int errors, int warnings)
        {
            int score = 100 - (errors * 15) - (warnings * 5);

            // Clamp score to maintain a predictable range.
            if (score < 0) score = 0;
            if (score > 100) score = 100;

            return score;
        }

        /// <summary>
        /// Called when filter checkboxes change.
        /// Re-runs analysis so the results grid reflects the current filter choices.
        /// </summary>
        private void FilterChanged()
        {
            // If there is text present, rerun analysis to apply updated filters.
            // This ensures the UI reacts immediately without requiring another button press.
            if (!string.IsNullOrWhiteSpace(txtLogInput.Text))
                AnalyzeWithDll();
        }

        /// <summary>
        /// Updates the bottom summary label with analysis counts and computed score.
        /// </summary>
        private void UpdateSummary(int totalLines, int errors, int warnings, int success, int score)
        {
            lblSummary.Text =
                $"Summary: Lines={totalLines} | Errors={errors} | Warnings={warnings} | Success={success} | Score={score}";
        }
    }
}
