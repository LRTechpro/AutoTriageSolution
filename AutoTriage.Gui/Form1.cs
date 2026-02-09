using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AutoTriage.Core; // Access LogAnalyzer, Finding, FindingSeverity, AnalysisResult

namespace AutoTriage.Gui
{
    /// <summary>
    /// Main WinForms user interface for the Auto Log Triage Helper.
    /// 
    /// Design intent:
    /// - This project (AutoTriage.Gui) is responsible ONLY for the user experience:
    ///   input, buttons, filtering, and presenting results.
    /// - The analysis logic lives in the DLL project (AutoTriage.Core)
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
        private readonly TextBox txtLogInput = new();
        private readonly Button btnAnalyze = new();
        private readonly Button btnLoadFile = new();
        private readonly Button btnClear = new();

        private readonly CheckBox chkShowCritical = new();  // NEW: Critical filter
        private readonly CheckBox chkShowErrors = new();
        private readonly CheckBox chkShowWarnings = new();
        private readonly CheckBox chkShowSuccess = new();

        private readonly DataGridView dgvResults = new();
        private readonly Label lblTitle = new();
        private readonly Label lblSummary = new();

        // Place this at the class level (as a private static readonly field)
        private static readonly string[] LineSeparators = new[] { "\r\n", "\n" };

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
            btnAnalyze.Click += (_, __) => AnalyzeWithDll();

            // Checkboxes (filters)
            // When the filter toggles change, we reapply filtering to what is displayed.
            // IMPORTANT: chkShowCritical is NEW and must be wired to FilterChanged()
            // so that toggling Critical on/off immediately updates the DataGridView.
            chkShowCritical.CheckedChanged += (_, __) => FilterChanged();
            chkShowErrors.CheckedChanged += (_, __) => FilterChanged();
            chkShowWarnings.CheckedChanged += (_, __) => FilterChanged();
            chkShowSuccess.CheckedChanged += (_, __) => FilterChanged();

            // Initialize the summary line so the UI starts in a clean, known state.
            // UPDATED: Now includes criticals=0 parameter.
            UpdateSummary(totalLines: 0, criticals: 0, errors: 0, warnings: 0, success: 0, score: 0);
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
            // - Right: filter toggles (Critical/Errors/Warn/Success)
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

            // DEFAULT FILTER CONFIGURATION:
            // All severity levels are checked by default so users see complete results immediately.
            // This is especially important for Critical findings which represent stop-the-line issues
            // (security breaches, ECU crashes, flash failures) that should never be hidden by default.
            
            // NEW: Show Critical checkbox
            // WHY THIS MUST BE FIRST:
            // Visual hierarchy: Critical > Error > Warning > Success
            // Placing it first in the UI reinforces that Critical findings are highest priority.
            chkShowCritical.Text = "Show Critical";
            chkShowCritical.Checked = true;  // MUST default to checked for safety/security
            chkShowCritical.AutoSize = true;
            chkShowCritical.ForeColor = Color.DarkRed;  // Visual emphasis for stop-the-line issues

            chkShowErrors.Text = "Show Errors";
            chkShowErrors.Checked = true;
            chkShowErrors.AutoSize = true;

            chkShowWarnings.Text = "Show Warnings";
            chkShowWarnings.Checked = true;
            chkShowWarnings.AutoSize = true;

            chkShowSuccess.Text = "Show Success";
            chkShowSuccess.Checked = true;
            chkShowSuccess.AutoSize = true;

            // Add to panel in priority order: Critical first, Success last
            filtersPanel.Controls.Add(chkShowCritical);  // NEW
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

            // Column: Severity classification (CRITICAL/ERROR/WARN/SUCCESS).
            // UPDATED: Now handles all four severity levels including Critical.
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
            // UPDATED: Now includes criticals parameter
            UpdateSummary(0, 0, 0, 0, 0, 0);
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
        // CRITICAL ARCHITECTURE CHANGE:
        // This method now calls AutoTriage.Core.LogAnalyzer instead of doing inline analysis.
        // This separation is essential because:
        // 1) The Core DLL contains validated, rule-based detection logic (CriticalRuleSet)
        // 2) The Core DLL can be reused in other tools (CLI, web service, unit tests)
        // 3) The GUI is responsible ONLY for presentation, not business logic
        /// <summary>
        /// Calls the AutoTriage.Core DLL to analyze the log input
        /// and displays the results in the results grid.
        /// 
        /// UPDATED: Now properly integrates with Core DLL and handles Critical severity.
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
            var lines = text.Split(LineSeparators, StringSplitOptions.None);

            // ========================================================================
            // CORE DLL INTEGRATION: Call LogAnalyzer.Analyze()
            // ========================================================================
            // WHY THIS IS CRITICAL:
            // - The Core DLL contains the validated rule-based detection logic (CriticalRuleSet)
            // - It handles LOW_VOLTAGE, LOW_SOC, UDS failures, security breaches, etc.
            // - It properly assigns FindingSeverity.Critical for stop-the-line issues
            // - The GUI should NEVER re-implement analysis logic (violates separation of concerns)
            var analyzer = new LogAnalyzer(); // <-- LogAnalyzer must be defined in AutoTriage.Core
            var analysisResult = analyzer.Analyze(lines);

            // ========================================================================
            // COUNT FINDINGS BY SEVERITY
            // ========================================================================
            // WHY WE COUNT HERE INSTEAD OF USING AnalysisResult COUNTS:
            // The AnalysisResult may not have pre-computed counts for Critical
            // (if it was added after AnalysisResult was created).
            // Counting from Findings ensures we never miss Critical findings.
            int criticals = analysisResult.Findings.Count(f => f.Severity == FindingSeverity.Critical);
            int errors = analysisResult.Findings.Count(f => f.Severity == FindingSeverity.Error);
            int warnings = analysisResult.Findings.Count(f => f.Severity == FindingSeverity.Warning);
            int success = analysisResult.Findings.Count(f => f.Severity == FindingSeverity.Success);

            // ========================================================================
            // POPULATE DATAGRIDVIEW WITH FILTERED FINDINGS
            // ========================================================================
            // WHY FILTERING HAPPENS HERE:
            // - User may toggle checkboxes to hide/show different severities
            // - The DataGridView should only display findings that pass the filter
            // - Core DLL returns ALL findings; GUI decides what to display
            foreach (var finding in analysisResult.Findings)
            {
                // Apply user-selected filter toggles before displaying in the grid.
                // CRITICAL: Must include Critical in the filter check or Critical findings won't appear!
                if (ShouldDisplayFinding(finding))
                {
                    // Map FindingSeverity enum to display string for the DataGridView
                    string severityText = GetSeverityText(finding.Severity);

                    // Add a row to the results grid.
                    // Columns: LineNumber, Severity, Code, Message
                    // UPDATED: Now uses finding.Code and finding.Title (or Message if Title is empty)
                    string displayMessage = !string.IsNullOrWhiteSpace(finding.Title)
                        ? finding.Title
                        : finding.LineText ?? "(no message)";

                    dgvResults.Rows.Add(
                        finding.LineNumber,
                        severityText,
                        finding.Code ?? "UNKNOWN",
                        displayMessage
                    );
                }
            }

            // ========================================================================
            // COMPUTE SCORE AND UPDATE SUMMARY
            // ========================================================================
            // Compute score based on weighted issues.
            // TODO: Current score does not include Critical (needs product owner input on weighting).
            // For now, score computation remains unchanged to avoid breaking existing expectations.
            int score = ComputeScore(errors, warnings);

            // Update summary line at bottom of UI.
            // UPDATED: Now includes criticals count.
            UpdateSummary(lines.Length, criticals, errors, warnings, success, score);
        }

        // =======================================================
        // FILTERING / SCORING / SUMMARY (UI SUPPORT FUNCTIONS)
        // =======================================================

        /// <summary>
        /// Determines whether a finding should be displayed based on the filter checkboxes.
        /// 
        /// UPDATED: Now handles FindingSeverity.Critical.
        /// 
        /// WHY THIS IS CRITICAL:
        /// - If we don't check chkShowCritical, Critical findings will NEVER appear in the grid
        /// - Critical findings represent stop-the-line issues (ECU crashes, security breaches, etc.)
        /// - They must be visible by default and filterable by the user
        /// </summary>
        /// <param name="finding">Finding object from Core DLL analysis.</param>
        /// <returns>True if the finding should be displayed; otherwise false.</returns>
        private bool ShouldDisplayFinding(Finding finding)
        {
            // Switch expression maps FindingSeverity enum to the corresponding checkbox state.
            // CRITICAL: Must include FindingSeverity.Critical case or those findings are hidden!
            return finding.Severity switch
            {
                FindingSeverity.Critical => chkShowCritical.Checked,  // NEW: Critical filter
                FindingSeverity.Error => chkShowErrors.Checked,
                FindingSeverity.Warning => chkShowWarnings.Checked,
                FindingSeverity.Success => chkShowSuccess.Checked,
                _ => true  // Unknown severity: display by default (defensive programming)
            };
        }

        /// <summary>
        /// Maps FindingSeverity enum values to human-readable display strings.
        /// 
        /// UPDATED: Now includes Critical mapping.
        /// 
        /// WHY THIS IS NECESSARY:
        /// - The DataGridView expects string values for display
        /// - The Core DLL uses FindingSeverity enum for type safety
        /// - This method bridges the gap between Core (enum) and GUI (string)
        /// </summary>
        /// <param name="severity">FindingSeverity enum value from Core DLL.</param>
        /// <returns>Display string for the Severity column.</returns>
        private string GetSeverityText(FindingSeverity severity)
        {
            // Switch expression for clean, readable mapping.
            // CRITICAL: Must include Critical case or it will display as "UNKNOWN"!
            return severity switch
            {
                FindingSeverity.Critical => "CRITICAL",  // NEW: Critical text
                FindingSeverity.Error => "ERROR",
                FindingSeverity.Warning => "WARN",
                FindingSeverity.Success => "SUCCESS",
                _ => "UNKNOWN"  // Defensive fallback
            };
        }

        /// <summary>
        /// Computes a simple 0-100 health score.
        /// - Start at 100
        /// - Subtract 15 points per error
        /// - Subtract 5 points per warning
        /// - Clamp final value between 0 and 100
        /// 
        /// TODO: Incorporate Critical findings into score calculation.
        /// Current implementation does not deduct points for Critical because:
        /// 1) Product owner has not defined Critical weighting yet
        /// 2) Critical may be so severe it should immediately set score to 0
        /// 3) Or Critical may require a separate "severity score" vs "health score"
        /// 
        /// For now, this method remains unchanged to avoid breaking existing score expectations.
        /// </summary>
        private static int ComputeScore(int errors, int warnings)
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
        /// 
        /// UPDATED: Now triggers when chkShowCritical changes (wired in constructor).
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
        /// 
        /// UPDATED: Now includes criticals count.
        /// 
        /// WHY THIS IS CRITICAL:
        /// - Triage engineers need to see Critical finding counts immediately
        /// - Critical findings represent stop-the-line issues (security, crashes, flash failures)
        /// - Omitting Critical count from summary would hide essential information
        /// </summary>
        /// <param name="totalLines">Total lines processed.</param>
        /// <param name="criticals">Number of Critical-severity findings.</param>
        /// <param name="errors">Number of Error-severity findings.</param>
        /// <param name="warnings">Number of Warning-severity findings.</param>
        /// <param name="success">Number of Success-severity findings.</param>
        /// <param name="score">Computed health score (0-100).</param>
        private void UpdateSummary(int totalLines, int criticals, int errors, int warnings, int success, int score)
        {
            // Format string now includes Criticals before Errors (priority order).
            // NOTE: Score currently does not include Critical weighting (see TODO in ComputeScore).
            lblSummary.Text =
                $"Summary: Lines={totalLines} | Criticals={criticals} | Errors={errors} | Warnings={warnings} | Success={success} | Score={score} | TODO: include Critical in score";
        }
    }
}
