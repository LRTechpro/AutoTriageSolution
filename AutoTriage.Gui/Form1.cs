using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using AutoTriage.Core;
using Timer = System.Windows.Forms.Timer;
using System.Globalization;

namespace AutoTriage.Gui
{
    public class Form1 : Form
    {
        private static readonly string AppDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoTriage"
        );
        private static readonly string KeywordsFilePath = Path.Combine(AppDataFolder, "keywords.json");

        private readonly TextBox txtLogInput = new();
        private readonly Button btnAnalyze = new();
        private readonly Button btnLoadFile = new();
        private readonly Button btnClear = new();

        private readonly CheckBox chkShowCritical = new();
        private readonly CheckBox chkShowErrors = new();
        private readonly CheckBox chkShowWarnings = new();
        private readonly CheckBox chkShowSuccess = new();
        private readonly CheckBox chkShowInfo = new();

        private readonly Label lblCustomKeywords = new();
        private readonly TextBox txtCustomKeywords = new();
        private readonly Button btnClearKeywords = new();
        private readonly CheckBox chkKeywordOverride = new();
        private readonly Label lblCustomKeywordsHelp = new();
        private readonly Label lblKeywordFilterStatus = new();
        private readonly Label lblActiveFilters = new();
        private readonly Label lblFilterWarning = new();

        private readonly DataGridView dgvResults = new();
        private readonly Label lblTitle = new();
        private readonly Label lblSummary = new();

        // Master findings list (single source of truth) - DO NOT re-analyze, only filter
        private List<Finding> _allFindings = new List<Finding>();
        private AnalysisResult? _lastResult;
        private string[]? _lastLines;

        private static readonly string[] LineSeparators = new[] { "\r\n", "\n" };

        // Keyword debouncing - prevents filtering on every keystroke
        private readonly Timer _keywordDebounce = new Timer { Interval = 500 };
        private List<string> _customKeywords = new List<string>();

        public Form1()
        {
            Text = "Auto Log Triage Helper";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1300, 800);

            BuildLayout();

            btnClear.Click += (_, __) => ClearAll();
            btnLoadFile.Click += (_, __) => LoadFromFile();
            btnAnalyze.Click += (_, __) => AnalyzeWithDll();
            btnClearKeywords.Click += (_, __) => ClearKeywords();

            // All severity checkboxes trigger re-filter (not re-analysis)
            chkShowCritical.CheckedChanged += (_, __) => ApplyFiltersAndBind();
            chkShowErrors.CheckedChanged += (_, __) => ApplyFiltersAndBind();
            chkShowWarnings.CheckedChanged += (_, __) => ApplyFiltersAndBind();
            chkShowSuccess.CheckedChanged += (_, __) => ApplyFiltersAndBind();
            chkShowInfo.CheckedChanged += (_, __) => ApplyFiltersAndBind();
            chkKeywordOverride.CheckedChanged += (_, __) => ApplyFiltersAndBind();

            // Keyword debouncing - DO NOT modify textbox text in handler (prevents caret jump)
            txtCustomKeywords.TextChanged += (_, __) =>
            {
                _keywordDebounce.Stop();
                _keywordDebounce.Start();
            };

            _keywordDebounce.Tick += (_, __) =>
            {
                _keywordDebounce.Stop();
                OnKeywordsChanged();
            };

            // Save on focus lost
            txtCustomKeywords.Leave += (_, __) =>
            {
                _keywordDebounce.Stop();
                OnKeywordsChanged();
            };

            UpdateSummary(totalLines: 0, criticals: 0, errors: 0, warnings: 0, success: 0, info: 0, score: 0);

            // Load persisted keywords at startup
            LoadKeywords();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12),
            };

            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(root);

            lblTitle.Text = "Auto Log Triage Helper";
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.Margin = new Padding(0, 0, 0, 8);
            root.Controls.Add(lblTitle, 0, 0);

            txtLogInput.Multiline = true;
            txtLogInput.ScrollBars = ScrollBars.Both;
            txtLogInput.WordWrap = false;
            txtLogInput.Font = new Font("Consolas", 10);
            txtLogInput.Dock = DockStyle.Fill;
            txtLogInput.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(txtLogInput, 0, 1);

            // Custom Keywords Container
            var customKeywordsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(0),
            };
            customKeywordsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            customKeywordsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            customKeywordsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            customKeywordsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            customKeywordsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(customKeywordsContainer, 0, 2);

            lblCustomKeywords.Text = "Custom Keyword Filter (OR logic - any match included):";
            lblCustomKeywords.AutoSize = true;
            lblCustomKeywords.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblCustomKeywords.Margin = new Padding(0, 0, 0, 3);
            customKeywordsContainer.Controls.Add(lblCustomKeywords, 0, 0);

            txtCustomKeywords.Multiline = true;
            txtCustomKeywords.ScrollBars = ScrollBars.Vertical;
            txtCustomKeywords.WordWrap = true;
            txtCustomKeywords.Font = new Font("Consolas", 9);
            txtCustomKeywords.Dock = DockStyle.Fill;
            txtCustomKeywords.Margin = new Padding(0, 0, 0, 5);
            txtCustomKeywords.PlaceholderText = "Enter keywords (space, comma, newline separated). Example: erase fail soc timeout\nOr use quotes: \"secure boot\" timeout";
            customKeywordsContainer.Controls.Add(txtCustomKeywords, 0, 1);

            // Keyword controls row (Clear button + Override checkbox)
            var keywordControlsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 5),
                Padding = new Padding(0),
            };
            customKeywordsContainer.Controls.Add(keywordControlsPanel, 0, 2);

            btnClearKeywords.Text = "Clear Keywords";
            btnClearKeywords.Size = new Size(120, 26);
            btnClearKeywords.Margin = new Padding(0, 0, 12, 0);
            keywordControlsPanel.Controls.Add(btnClearKeywords);

            chkKeywordOverride.Text = "Custom keywords override severity filters (show matches even if severity unchecked)";
            chkKeywordOverride.AutoSize = true;
            chkKeywordOverride.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            chkKeywordOverride.ForeColor = Color.DarkBlue;
            chkKeywordOverride.Checked = false;
            keywordControlsPanel.Controls.Add(chkKeywordOverride);

            lblKeywordFilterStatus.Text = "Custom keyword filter: OFF";
            lblKeywordFilterStatus.AutoSize = true;
            lblKeywordFilterStatus.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblKeywordFilterStatus.ForeColor = Color.DarkGray;
            lblKeywordFilterStatus.Margin = new Padding(0, 0, 0, 3);
            customKeywordsContainer.Controls.Add(lblKeywordFilterStatus, 0, 3);

            lblCustomKeywordsHelp.Text = "Supports: space, comma (,), semicolon (;), newline, tab, quotes for phrases. Case-insensitive substring matching. Auto-saved to: " + KeywordsFilePath;
            lblCustomKeywordsHelp.AutoSize = true;
            lblCustomKeywordsHelp.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblCustomKeywordsHelp.ForeColor = Color.DarkGray;
            lblCustomKeywordsHelp.Margin = new Padding(0);
            customKeywordsContainer.Controls.Add(lblCustomKeywordsHelp, 0, 4);

            var controlsRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10),
            };

            controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            controlsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.Controls.Add(controlsRow, 0, 3);

            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            controlsRow.Controls.Add(buttonsPanel, 0, 0);

            btnAnalyze.Text = "Analyze";
            btnAnalyze.Size = new Size(110, 34);

            btnLoadFile.Text = "Load File";
            btnLoadFile.Size = new Size(110, 34);

            btnClear.Text = "Clear All";
            btnClear.Size = new Size(110, 34);

            buttonsPanel.Controls.Add(btnAnalyze);
            buttonsPanel.Controls.Add(btnLoadFile);
            buttonsPanel.Controls.Add(btnClear);

            var filtersPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(18, 6, 0, 0),
                Padding = new Padding(0),
            };
            controlsRow.Controls.Add(filtersPanel, 1, 0);

            chkShowCritical.Text = "Show Critical";
            chkShowCritical.Checked = true;
            chkShowCritical.AutoSize = true;
            chkShowCritical.ForeColor = Color.DarkRed;

            chkShowErrors.Text = "Show Errors";
            chkShowErrors.Checked = true;
            chkShowErrors.AutoSize = true;

            chkShowWarnings.Text = "Show Warnings";
            chkShowWarnings.Checked = true;
            chkShowWarnings.AutoSize = true;

            chkShowSuccess.Text = "Show Success";
            chkShowSuccess.Checked = true;
            chkShowSuccess.AutoSize = true;

            chkShowInfo.Text = "Show Info";
            chkShowInfo.Checked = true;
            chkShowInfo.AutoSize = true;
            chkShowInfo.ForeColor = Color.DarkBlue;

            filtersPanel.Controls.Add(chkShowCritical);
            filtersPanel.Controls.Add(chkShowErrors);
            filtersPanel.Controls.Add(chkShowWarnings);
            filtersPanel.Controls.Add(chkShowSuccess);
            filtersPanel.Controls.Add(chkShowInfo);

            lblActiveFilters.Text = "";
            lblActiveFilters.AutoSize = true;
            lblActiveFilters.Font = new Font("Segoe UI", 8, FontStyle.Italic);
            lblActiveFilters.ForeColor = Color.DarkGray;
            lblActiveFilters.Margin = new Padding(12, 0, 0, 0);
            filtersPanel.Controls.Add(lblActiveFilters);

            lblFilterWarning.Text = "";
            lblFilterWarning.AutoSize = true;
            lblFilterWarning.Font = new Font("Segoe UI", 8, FontStyle.Bold);
            lblFilterWarning.ForeColor = Color.DarkOrange;
            lblFilterWarning.Margin = new Padding(12, 0, 0, 0);
            filtersPanel.Controls.Add(lblFilterWarning);

            var resultsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0),
            };
            resultsContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            resultsContainer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(resultsContainer, 0, 4);

            ConfigureResultsGrid();
            resultsContainer.Controls.Add(dgvResults, 0, 0);

            lblSummary.AutoSize = true;
            lblSummary.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            lblSummary.Margin = new Padding(0, 8, 0, 0);
            resultsContainer.Controls.Add(lblSummary, 0, 1);
        }

        private void ConfigureResultsGrid()
        {
            dgvResults.Dock = DockStyle.Fill;
            dgvResults.ReadOnly = true;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.AllowUserToResizeRows = false;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.MultiSelect = false;
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.Columns.Clear();

            var colLine = new DataGridViewTextBoxColumn
            {
                Name = "colLine",
                HeaderText = "Line",
                FillWeight = 12
            };

            var colSeverity = new DataGridViewTextBoxColumn
            {
                Name = "colSeverity",
                HeaderText = "Severity",
                FillWeight = 18
            };

            var colCode = new DataGridViewTextBoxColumn
            {
                Name = "colCode",
                HeaderText = "Code",
                FillWeight = 18
            };

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

        private void ClearAll()
        {
            txtLogInput.Clear();
            dgvResults.Rows.Clear();
            _allFindings.Clear();
            _lastResult = null;
            _lastLines = null;
            UpdateSummary(0, 0, 0, 0, 0, 0, 0);
            lblActiveFilters.Text = "";
            lblFilterWarning.Text = "";
            UpdateKeywordFilterStatus(0, 0, 0);
        }

        private void ClearKeywords()
        {
            txtCustomKeywords.Clear();
            _customKeywords.Clear();
            SaveKeywords();
            ApplyFiltersAndBind();
        }

        private void LoadFromFile()
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "Select a log file",
                Filter = "Log/Text files (*.log;*.txt)|*.log;*.txt|All files (*.*)|*.*"
            })
            {
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    txtLogInput.Text = File.ReadAllText(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Could not read file.\n\n{ex.Message}", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =======================================================
        // KEYWORD PERSISTENCE
        // =======================================================

        /// <summary>
        /// Loads persisted keywords from AppData and populates the textbox ONCE at startup.
        /// </summary>
        private void LoadKeywords()
        {
            try
            {
                if (!File.Exists(KeywordsFilePath))
                {
                    _customKeywords.Clear();
                    UpdateKeywordFilterStatus(0, 0, 0);
                    return;
                }

                var json = File.ReadAllText(KeywordsFilePath);
                var data = JsonSerializer.Deserialize<KeywordsData>(json);

                if (data?.Keywords != null && data.Keywords.Count > 0)
                {
                    _customKeywords = data.Keywords;
                    // Set textbox ONCE at startup - no caret issues
                    txtCustomKeywords.Text = string.Join(" ", _customKeywords);
                    UpdateKeywordFilterStatus(_customKeywords.Count, 0, 0);
                }
                else
                {
                    _customKeywords.Clear();
                    UpdateKeywordFilterStatus(0, 0, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Could not load keywords from {KeywordsFilePath}.\n\n{ex.Message}",
                    "Load Keywords Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                _customKeywords.Clear();
                UpdateKeywordFilterStatus(0, 0, 0);
            }
        }

        /// <summary>
        /// Saves keywords to AppData. Called on debounced TextChanged and Leave events.
        /// </summary>
        private void SaveKeywords()
        {
            try
            {
                if (_customKeywords.Count == 0)
                {
                    if (File.Exists(KeywordsFilePath))
                    {
                        File.Delete(KeywordsFilePath);
                    }
                    return;
                }

                Directory.CreateDirectory(AppDataFolder);

                var data = new KeywordsData { Keywords = _customKeywords };
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(KeywordsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    $"Could not save keywords to {KeywordsFilePath}.\n\n{ex.Message}",
                    "Save Keywords Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        // =======================================================
        // KEYWORD PARSING
        // =======================================================

        /// <summary>
        /// Parses custom keywords from raw text input.
        /// Supports multiple separators: newline, comma, semicolon, space, tab.
        /// Supports quoted phrases: "secure boot" is treated as one token.
        /// Returns distinct, trimmed, non-empty keywords (case-insensitive comparison).
        /// </summary>
        private List<string> ParseCustomKeywords(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            var tokens = new List<string>();
            
            // Regex to match quoted phrases OR individual tokens
            // Pattern: "quoted phrase" OR non-separator characters
            var regex = new Regex(@"""([^""]*)""|([^\s,;\r\n\t]+)", RegexOptions.Compiled);

            foreach (Match match in regex.Matches(raw))
            {
                string token = match.Groups[1].Success
                    ? match.Groups[1].Value.Trim()  // Quoted phrase
                    : match.Groups[2].Value.Trim(); // Unquoted token

                // Clean up trailing punctuation (but keep internal punctuation)
                token = token.TrimEnd(',', '.', ';', ':', '!', '?', '-', '_');

                // Ignore empty and single-character tokens to reduce noise
                if (!string.IsNullOrWhiteSpace(token) && token.Length > 1)
                {
                    tokens.Add(token);
                }
            }

            // Return distinct tokens (case-insensitive) in alphabetical order
            return tokens
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Checks if a finding matches ANY of the given keywords (case-insensitive substring).
        /// Searches in: LineText, Title, Code, Severity, and raw log line.
        /// Uses OR logic: if ANY keyword matches ANY field, returns true.
        /// </summary>
        private bool FindingMatchesAnyKeyword(Finding finding, IReadOnlyList<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return true; // No keywords = show all

            // Build searchable text from finding fields
            string lineText = finding.LineText ?? string.Empty;
            string title = finding.Title ?? string.Empty;
            string code = finding.Code ?? string.Empty;
            string severity = finding.Severity.ToString();

            // Also search the raw log line if available
            string rawLine = string.Empty;
            if (_lastLines != null && finding.LineNumber > 0 && finding.LineNumber <= _lastLines.Length)
            {
                rawLine = _lastLines[finding.LineNumber - 1];
            }

            // Check if ANY keyword matches ANY field (OR logic)
            return keywords.Any(keyword =>
                lineText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                code.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                severity.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                rawLine.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Checks if a finding's severity is allowed by current checkbox filters.
        /// Uses INCLUSIVE logic: each checkbox adds that severity to allowed set.
        /// </summary>
        private bool IsSeverityAllowed(FindingSeverity severity)
        {
            return (chkShowCritical.Checked && severity == FindingSeverity.Critical) ||
                   (chkShowErrors.Checked && severity == FindingSeverity.Error) ||
                   (chkShowWarnings.Checked && severity == FindingSeverity.Warning) ||
                   (chkShowSuccess.Checked && severity == FindingSeverity.Success) ||
                   (chkShowInfo.Checked && severity == FindingSeverity.Info);
        }

        // =======================================================
        // ANALYSIS & FILTERING LOGIC
        // =======================================================

        private void AnalyzeWithDll()
        {
            string logText = txtLogInput.Text;

            if (string.IsNullOrWhiteSpace(logText))
            {
                MessageBox.Show(this, "Please enter or load log data first.", "No Input",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _lastLines = logText.Split(LineSeparators, StringSplitOptions.None);

                var analyzer = new LogAnalyzer();
                // Analyze WITHOUT keyword filtering - that's done in display layer
                _lastResult = analyzer.Analyze(_lastLines, null);

                // Store master findings list (single source of truth)
                _allFindings = _lastResult.Findings?.ToList() ?? new List<Finding>();

                // Apply filters and bind to grid (does NOT re-run analysis)
                ApplyFiltersAndBind();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Analysis failed.\n\n{ex.Message}", "Analysis Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnKeywordsChanged()
        {
            // Parse keywords WITHOUT modifying the textbox text (prevents caret jump!)
            _customKeywords = ParseCustomKeywords(txtCustomKeywords.Text);

            // Save to disk
            SaveKeywords();

            // Re-filter and display (does NOT re-run analysis)
            ApplyFiltersAndBind();
        }

        /// <summary>
        /// Single source of truth for filtering and binding.
        /// Applies severity filters, keyword filters, and binds to DataGridView.
        /// Called by: Analyze, checkbox changes, keyword changes.
        /// Does NOT re-run analysis - only filters existing _allFindings.
        /// </summary>
        private void ApplyFiltersAndBind()
        {
            dgvResults.Rows.Clear();
            lblFilterWarning.Text = "";

            if (_allFindings == null || _allFindings.Count == 0)
            {
                UpdateDisplaySummary(0, 0, 0);
                return;
            }

            // Parse current keywords from textbox
            var keywords = _customKeywords ?? new List<string>();
            bool hasKeywords = keywords.Count > 0;

            // Check which severity toggles are ON (INCLUSIVE logic)
            bool anySeverityChecked = chkShowCritical.Checked ||
                                     chkShowErrors.Checked ||
                                     chkShowWarnings.Checked ||
                                     chkShowSuccess.Checked ||
                                     chkShowInfo.Checked;

            bool keywordOverride = chkKeywordOverride.Checked;

            // PHASE 1: Apply keyword filter to get matched count (before severity)
            List<Finding> keywordMatched;
            if (hasKeywords)
            {
                keywordMatched = _allFindings
                    .Where(f => FindingMatchesAnyKeyword(f, keywords))
                    .ToList();
            }
            else
            {
                keywordMatched = _allFindings.ToList();
            }

            // PHASE 2: Apply display filtering based on override mode
            List<Finding> filtered;

            if (hasKeywords && keywordOverride)
            {
                // Override mode: show ALL keyword matches regardless of severity toggles
                filtered = keywordMatched;
            }
            else if (hasKeywords && !keywordOverride && anySeverityChecked)
            {
                // Normal mode: keyword matches AND severity allowed (AND logic)
                filtered = keywordMatched.Where(f => IsSeverityAllowed(f.Severity)).ToList();
            }
            else if (hasKeywords && !keywordOverride && !anySeverityChecked)
            {
                // Keywords ON, override OFF, all severities OFF: show nothing
                filtered = new List<Finding>();
            }
            else if (!hasKeywords && anySeverityChecked)
            {
                // No keywords, severity filters active: apply severity only
                filtered = _allFindings.Where(f => IsSeverityAllowed(f.Severity)).ToList();
            }
            else if (!hasKeywords && !anySeverityChecked)
            {
                // No keywords, no severities: show nothing
                filtered = new List<Finding>();
            }
            else
            {
                // Fallback: show all (shouldn't reach here)
                filtered = _allFindings.ToList();
            }

            // PHASE 3: Populate grid with filtered results
            foreach (var finding in filtered)
            {
                dgvResults.Rows.Add(
                    finding.LineNumber,
                    finding.Severity.ToString().ToUpperInvariant(),
                    finding.Code ?? string.Empty,
                    finding.LineText ?? string.Empty
                );
            }

            // PHASE 4: Show warning if matches are hidden by severity filters
            if (hasKeywords && !keywordOverride && anySeverityChecked && keywordMatched.Count > 0 && filtered.Count == 0)
            {
                lblFilterWarning.Text = "⚠ All keyword matches are hidden by severity filters. Enable more severity checkboxes or turn on override.";
            }
            else if (hasKeywords && !keywordOverride && anySeverityChecked && filtered.Count < keywordMatched.Count)
            {
                int hidden = keywordMatched.Count - filtered.Count;
                lblFilterWarning.Text = $"⚠ {hidden} keyword match(es) hidden by severity filters.";
            }
            else if (hasKeywords && !keywordOverride && !anySeverityChecked && keywordMatched.Count > 0)
            {
                lblFilterWarning.Text = $"⚠ {keywordMatched.Count} keyword match(es) found but all severity filters are OFF. Enable severities or turn on override.";
            }

            // Force grid refresh
            dgvResults.Refresh();

            // Update UI status labels
            UpdateDisplaySummary(_allFindings.Count, keywordMatched.Count, filtered.Count);
            UpdateKeywordFilterStatus(keywords.Count, keywordMatched.Count, filtered.Count);
            UpdateActiveFiltersLabel(anySeverityChecked);
        }

        private void UpdateKeywordFilterStatus(int keywordCount, int matchedCount, int displayedCount)
        {
            if (keywordCount > 0)
            {
                string keywordList = string.Join(", ", _customKeywords.Take(5));
                if (_customKeywords.Count > 5)
                    keywordList += ", ...";

                lblKeywordFilterStatus.Text = $"Custom keyword filter: ON ({keywordCount} keywords: {keywordList}) — Matched: {matchedCount} — Displayed: {displayedCount}";
                lblKeywordFilterStatus.ForeColor = matchedCount > 0 ? Color.DarkGreen : Color.DarkOrange;
            }
            else
            {
                lblKeywordFilterStatus.Text = "Custom keyword filter: OFF";
                lblKeywordFilterStatus.ForeColor = Color.DarkGray;
            }
        }

        private void UpdateActiveFiltersLabel(bool anySeverityChecked)
        {
            var activeFilters = new List<string>();
            if (chkShowCritical.Checked) activeFilters.Add("Critical");
            if (chkShowErrors.Checked) activeFilters.Add("Errors");
            if (chkShowWarnings.Checked) activeFilters.Add("Warnings");
            if (chkShowSuccess.Checked) activeFilters.Add("Success");
            if (chkShowInfo.Checked) activeFilters.Add("Info");

            if (!anySeverityChecked)
            {
                lblActiveFilters.Text = "(No severities selected)";
                lblActiveFilters.ForeColor = Color.DarkOrange;
            }
            else if (activeFilters.Count == 5)
            {
                lblActiveFilters.Text = "(All severities shown)";
                lblActiveFilters.ForeColor = Color.DarkGray;
            }
            else
            {
                lblActiveFilters.Text = $"(Showing: {string.Join(", ", activeFilters)})";
                lblActiveFilters.ForeColor = Color.DarkGray;
            }
        }

        private void UpdateDisplaySummary(int totalFindings, int matchedFindings, int displayedFindings)
        {
            if (_lastResult == null)
            {
                UpdateSummary(0, 0, 0, 0, 0, 0, 0);
                return;
            }

            int totalLines = _lastLines?.Length ?? 0;
            int criticals = _allFindings.Count(f => f.Severity == FindingSeverity.Critical);
            int errors = _allFindings.Count(f => f.Severity == FindingSeverity.Error);
            int warnings = _allFindings.Count(f => f.Severity == FindingSeverity.Warning);
            int success = _allFindings.Count(f => f.Severity == FindingSeverity.Success);
            int info = _allFindings.Count(f => f.Severity == FindingSeverity.Info);

            string summaryText = $"Lines: {totalLines} | Critical: {criticals} | Errors: {errors} | Warnings: {warnings} | Success: {success} | Info: {info} | Score: {_lastResult.Score}";

            if (_customKeywords.Count > 0)
            {
                summaryText += $" | Keyword Matched: {matchedFindings}";
            }

            if (displayedFindings < totalFindings)
            {
                summaryText += $" | Displayed: {displayedFindings} of {totalFindings} findings";
            }
            else if (displayedFindings == totalFindings && totalFindings > 0)
            {
                summaryText += $" | Displaying all {displayedFindings} findings";
            }

            lblSummary.Text = summaryText;
        }

        private void UpdateSummary(int totalLines, int criticals, int errors, int warnings, int success, int info, int score)
        {
            lblSummary.Text = $"Lines: {totalLines} | Critical: {criticals} | Errors: {errors} | Warnings: {warnings} | Success: {success} | Info: {info} | Score: {score}";
        }

        public class KeywordsData
        {
            public List<string> Keywords { get; set; } = new List<string>();
        }
    }
}
