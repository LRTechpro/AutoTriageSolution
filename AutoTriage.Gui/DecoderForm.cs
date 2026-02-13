using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoTriage.Gui
{
    public partial class DecoderForm : Form
    {
        private TextBox txtInput = null!;
        private TextBox txtOutput = null!;
        private ComboBox cboConversionType = null!;
        private Button btnConvert = null!;
        private Button btnClear = null!;
        private Button btnSwap = null!;
        private Button btnAutoDetect = null!;
        private Label lblInputLabel = null!;
        private Label lblOutputLabel = null!;
        private Label lblDetectedType = null!;

        public DecoderForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Decoder Tools";
            this.Size = new Size(900, 700);
            this.MinimumSize = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // Conversion type label
            var lblConversionType = new Label
            {
                Text = "Conversion Type:",
                Location = new Point(15, 15),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            this.Controls.Add(lblConversionType);

            // Conversion type dropdown
            cboConversionType = new ComboBox
            {
                Location = new Point(150, 12),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cboConversionType.Items.AddRange(new object[]
            {
                "Hex → ASCII",
                "ASCII → Hex",
                "Binary → Hex",
                "Hex → Binary",
                "Base64 → Text",
                "Text → Base64",
                "UDS Code Decoder"
            });
            cboConversionType.SelectedIndex = 0;
            cboConversionType.SelectedIndexChanged += CboConversionType_SelectedIndexChanged;
            this.Controls.Add(cboConversionType);

            // Auto-detect button
            btnAutoDetect = new Button
            {
                Text = "🔍 Auto Detect",
                Location = new Point(420, 10),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.LightGreen
            };
            btnAutoDetect.Click += BtnAutoDetect_Click;
            this.Controls.Add(btnAutoDetect);

            // Convert button
            btnConvert = new Button
            {
                Text = "Convert",
                Location = new Point(550, 10),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConvert.Click += BtnConvert_Click;
            this.Controls.Add(btnConvert);

            // Swap button
            btnSwap = new Button
            {
                Text = "⇅ Swap",
                Location = new Point(660, 10),
                Size = new Size(80, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSwap.Click += BtnSwap_Click;
            this.Controls.Add(btnSwap);

            // Clear button
            btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(750, 10),
                Size = new Size(80, 30),
                Font = new Font("Segoe UI", 9F)
            };
            btnClear.Click += BtnClear_Click;
            this.Controls.Add(btnClear);

            // Detected type label
            lblDetectedType = new Label
            {
                Text = "Auto-detection: Ready",
                Location = new Point(420, 45),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DarkGreen
            };
            this.Controls.Add(lblDetectedType);

            // Input label
            lblInputLabel = new Label
            {
                Text = "Input (Hex):",
                Location = new Point(15, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblInputLabel);

            // Input textbox
            txtInput = new TextBox
            {
                Location = new Point(15, 95),
                Size = new Size(850, 250),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9F),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(txtInput);

            // Output label
            lblOutputLabel = new Label
            {
                Text = "Output (ASCII):",
                Location = new Point(15, 360),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(lblOutputLabel);

            // Output textbox
            txtOutput = new TextBox
            {
                Location = new Point(15, 385),
                Size = new Size(850, 250),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(txtOutput);

            // Update labels based on initial selection
            UpdateLabels();
        }

        private void CboConversionType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            string selection = cboConversionType.SelectedItem?.ToString() ?? "";
            
            switch (selection)
            {
                case "Hex → ASCII":
                    lblInputLabel.Text = "Input (Hex):";
                    lblOutputLabel.Text = "Output (ASCII):";
                    btnSwap.Visible = true;
                    break;
                case "ASCII → Hex":
                    lblInputLabel.Text = "Input (ASCII):";
                    lblOutputLabel.Text = "Output (Hex):";
                    btnSwap.Visible = true;
                    break;
                case "Binary → Hex":
                    lblInputLabel.Text = "Input (Binary):";
                    lblOutputLabel.Text = "Output (Hex):";
                    btnSwap.Visible = true;
                    break;
                case "Hex → Binary":
                    lblInputLabel.Text = "Input (Hex):";
                    lblOutputLabel.Text = "Output (Binary):";
                    btnSwap.Visible = true;
                    break;
                case "Base64 → Text":
                    lblInputLabel.Text = "Input (Base64):";
                    lblOutputLabel.Text = "Output (Text):";
                    btnSwap.Visible = true;
                    break;
                case "Text → Base64":
                    lblInputLabel.Text = "Input (Text):";
                    lblOutputLabel.Text = "Output (Base64):";
                    btnSwap.Visible = true;
                    break;
                case "UDS Code Decoder":
                    lblInputLabel.Text = "Input (UDS Code - e.g., 7F 10 11):";
                    lblOutputLabel.Text = "Decoded Information:";
                    btnSwap.Visible = false;
                    break;
            }
        }

        private void BtnConvert_Click(object? sender, EventArgs e)
        {
            try
            {
                string input = txtInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("Please enter input data.", "No Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string selection = cboConversionType.SelectedItem?.ToString() ?? "";
                string output = string.Empty;

                try
                {
                    switch (selection)
                    {
                        case "Hex → ASCII":
                            output = HexToAscii(input);
                            break;
                        case "ASCII → Hex":
                            output = AsciiToHex(input);
                            break;
                        case "Binary → Hex":
                            output = BinaryToHex(input);
                            break;
                        case "Hex → Binary":
                            output = HexToBinary(input);
                            break;
                        case "Base64 → Text":
                            output = Base64ToText(input);
                            break;
                        case "Text → Base64":
                            output = TextToBase64(input);
                            break;
                        case "UDS Code Decoder":
                            output = DecodeUDS(input);
                            break;
                    }

                    txtOutput.Text = output;
                }
                catch (Exception innerEx)
                {
                    txtOutput.Text = $"❌ Conversion failed\n\n" +
                                   $"Error: {innerEx.Message}\n\n" +
                                   $"Suggestion: Check that your input format matches the selected conversion type.\n" +
                                   $"Try using 'Auto Detect' to automatically determine the input type.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion error: {ex.Message}\n\nPlease check your input format.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSwap_Click(object? sender, EventArgs e)
        {
            // Swap input and output
            string temp = txtInput.Text;
            txtInput.Text = txtOutput.Text;
            txtOutput.Text = temp;

            // Swap conversion direction
            string selection = cboConversionType.SelectedItem?.ToString() ?? "";
            switch (selection)
            {
                case "Hex → ASCII":
                    cboConversionType.SelectedItem = "ASCII → Hex";
                    break;
                case "ASCII → Hex":
                    cboConversionType.SelectedItem = "Hex → ASCII";
                    break;
                case "Binary → Hex":
                    cboConversionType.SelectedItem = "Hex → Binary";
                    break;
                case "Hex → Binary":
                    cboConversionType.SelectedItem = "Binary → Hex";
                    break;
                case "Base64 → Text":
                    cboConversionType.SelectedItem = "Text → Base64";
                    break;
                case "Text → Base64":
                    cboConversionType.SelectedItem = "Base64 → Text";
                    break;
            }
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            txtInput.Clear();
            txtOutput.Clear();
            lblDetectedType.Text = "Auto-detection: Ready";
            lblDetectedType.ForeColor = Color.DarkGreen;
        }

        private void BtnAutoDetect_Click(object? sender, EventArgs e)
        {
            try
            {
                string input = txtInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("Please enter input data to detect.", "No Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string detectedType = DetectInputType(input);
                lblDetectedType.Text = $"Detected: {detectedType}";
                lblDetectedType.ForeColor = Color.DarkBlue;

                // Auto-select the appropriate conversion type
                switch (detectedType)
                {
                    case "UDS Diagnostic Code":
                        cboConversionType.SelectedItem = "UDS Code Decoder";
                        BtnConvert_Click(sender, e);
                        break;
                    case "Hexadecimal":
                        cboConversionType.SelectedItem = "Hex → ASCII";
                        BtnConvert_Click(sender, e);
                        break;
                    case "Binary":
                        cboConversionType.SelectedItem = "Binary → Hex";
                        BtnConvert_Click(sender, e);
                        break;
                    case "Base64":
                        cboConversionType.SelectedItem = "Base64 → Text";
                        BtnConvert_Click(sender, e);
                        break;
                    case "ASCII Text":
                        cboConversionType.SelectedItem = "ASCII → Hex";
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Detection error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string DetectInputType(string input)
        {
            input = input.Trim();

            if (string.IsNullOrEmpty(input))
                return "Unknown";

            // Remove common formatting characters for analysis
            string cleanInput = input.Replace(" ", "").Replace("-", "").Replace("0x", "").Replace(",", "").Replace(":", "");

            // 1. Check for UDS diagnostic codes (most specific first)
            if (cleanInput.Length >= 2 && IsHex(cleanInput))
            {
                try
                {
                    byte firstByte = Convert.ToByte(cleanInput.Substring(0, 2), 16);

                    // UDS service IDs (requests)
                    if (firstByte == 0x10 || firstByte == 0x11 || firstByte == 0x14 || firstByte == 0x19 || 
                        firstByte == 0x22 || firstByte == 0x23 || firstByte == 0x27 || firstByte == 0x28 || 
                        firstByte == 0x2E || firstByte == 0x2F || firstByte == 0x31 || firstByte == 0x34 || 
                        firstByte == 0x35 || firstByte == 0x36 || firstByte == 0x37 || firstByte == 0x3E || 
                        firstByte == 0x85)
                    {
                        return "UDS Diagnostic Code";
                    }

                    // UDS positive responses (service ID + 0x40)
                    if (firstByte >= 0x50 && firstByte <= 0xFE)
                    {
                        byte baseService = (byte)(firstByte & 0xBF);
                        if (baseService == 0x10 || baseService == 0x11 || baseService == 0x14 || 
                            baseService == 0x19 || baseService == 0x22 || baseService == 0x23 || 
                            baseService == 0x27 || baseService == 0x28 || baseService == 0x2E || 
                            baseService == 0x2F || baseService == 0x31 || baseService == 0x34 || 
                            baseService == 0x35 || baseService == 0x36 || baseService == 0x37 || 
                            baseService == 0x3E || baseService == 0x45)
                        {
                            return "UDS Diagnostic Code";
                        }
                    }

                    // UDS negative response (0x7F)
                    if (firstByte == 0x7F)
                    {
                        return "UDS Diagnostic Code";
                    }
                }
                catch
                {
                    // Continue with other checks
                }
            }

            // 2. Check for binary (only 0s and 1s, must be at least 8 bits)
            string binaryTest = input.Replace(" ", "").Replace("-", "");
            if (binaryTest.Length >= 8 && binaryTest.All(c => c == '0' || c == '1'))
            {
                return "Binary";
            }

            // 3. Check for Base64 (before hex, as base64 can contain hex-like characters)
            if (IsBase64(input))
            {
                // Additional validation: Base64 typically has good length and character distribution
                int letterCount = input.Count(c => char.IsLetter(c));
                int digitCount = input.Count(c => char.IsDigit(c));
                if (letterCount > 0 && (letterCount + digitCount) > cleanInput.Length / 2)
                {
                    return "Base64";
                }
            }

            // 4. Check for hexadecimal
            if (IsHex(cleanInput) && cleanInput.Length % 2 == 0 && cleanInput.Length >= 2)
            {
                return "Hexadecimal";
            }

            // 5. Default to ASCII text
            return "ASCII Text";
        }

        private bool IsHex(string input)
        {
            return !string.IsNullOrEmpty(input) && input.All(c => 
                (c >= '0' && c <= '9') || 
                (c >= 'A' && c <= 'F') || 
                (c >= 'a' && c <= 'f'));
        }

        private bool IsBase64(string input)
        {
            input = input.Trim();
            if (string.IsNullOrEmpty(input) || input.Length % 4 != 0)
                return false;

            try
            {
                Convert.FromBase64String(input);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Conversion Methods

        private string HexToAscii(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "").Replace("0x", "").Replace(",", "");
            
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            var sb = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexByte = hex.Substring(i, 2);
                byte b = Convert.ToByte(hexByte, 16);
                
                // Print printable ASCII, otherwise show as dot
                if (b >= 32 && b <= 126)
                    sb.Append((char)b);
                else
                    sb.Append('.');
            }
            
            return sb.ToString();
        }

        private string AsciiToHex(string ascii)
        {
            var sb = new StringBuilder();
            foreach (char c in ascii)
            {
                sb.Append(((int)c).ToString("X2"));
                sb.Append(" ");
            }
            return sb.ToString().Trim();
        }

        private string BinaryToHex(string binary)
        {
            binary = binary.Replace(" ", "").Replace("-", "");
            
            if (binary.Length % 8 != 0)
                throw new ArgumentException("Binary string length must be multiple of 8");

            var sb = new StringBuilder();
            for (int i = 0; i < binary.Length; i += 8)
            {
                string byteBinary = binary.Substring(i, 8);
                byte b = Convert.ToByte(byteBinary, 2);
                sb.Append(b.ToString("X2"));
                sb.Append(" ");
            }
            return sb.ToString().Trim();
        }

        private string HexToBinary(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "").Replace("0x", "").Replace(",", "");
            
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            var sb = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
            {
                string hexByte = hex.Substring(i, 2);
                byte b = Convert.ToByte(hexByte, 16);
                sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
                sb.Append(" ");
            }
            return sb.ToString().Trim();
        }

        private string Base64ToText(string base64)
        {
            byte[] data = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(data);
        }

        private string TextToBase64(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(data);
        }

        private string DecodeUDS(string udsCode)
        {
            // Clean up input
            udsCode = udsCode.Replace(" ", "").Replace("-", "").Replace("0x", "").Replace(",", "").Replace(":", "").ToUpper();

            // Validate input
            if (string.IsNullOrEmpty(udsCode))
                throw new ArgumentException("UDS code is empty");

            if (!IsHex(udsCode))
                throw new ArgumentException($"Invalid hex characters in UDS code: '{udsCode}'");

            if (udsCode.Length < 2)
                throw new ArgumentException($"UDS code too short (need at least 1 byte): '{udsCode}'");

            var sb = new StringBuilder();
            sb.AppendLine("╔════════════════════════════════════════════════════════════════");
            sb.AppendLine("║   UDS DIAGNOSTIC CODE DECODER");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════\n");

            // Parse bytes
            List<byte> bytes = new List<byte>();
            try
            {
                for (int i = 0; i < udsCode.Length; i += 2)
                {
                    if (i + 1 < udsCode.Length)
                    {
                        bytes.Add(Convert.ToByte(udsCode.Substring(i, 2), 16));
                    }
                    else
                    {
                        // Handle odd-length strings
                        sb.AppendLine("⚠️  WARNING: Odd-length hex string. Last character ignored.\n");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error parsing hex bytes: {ex.Message}");
            }

            if (bytes.Count == 0)
                throw new ArgumentException("No valid bytes found in UDS code");

            byte serviceId = bytes[0];

            // Check if it's a negative response (0x7F)
            if (serviceId == 0x7F)
            {
                if (bytes.Count < 3)
                {
                    sb.AppendLine("📛 RESPONSE TYPE: NEGATIVE RESPONSE (INCOMPLETE)\n");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine("❌ ERROR: Negative response (0x7F) requires at least 3 bytes:");
                    sb.AppendLine("   - Byte 0: 0x7F (negative response indicator)");
                    sb.AppendLine("   - Byte 1: Requested service ID");
                    sb.AppendLine("   - Byte 2: Negative Response Code (NRC)");
                    sb.AppendLine($"\n   Current bytes: {bytes.Count}");
                }
                else
                {
                    sb.AppendLine("📛 RESPONSE TYPE: NEGATIVE RESPONSE (REJECTION)\n");
                    sb.AppendLine("═══════════════════════════════════════════════════════════════");

                    byte requestedService = bytes[1];
                    byte nrc = bytes[2];

                    sb.AppendLine($"Requested Service ID: 0x{requestedService:X2}");
                    sb.AppendLine($"Service Name: {GetUDSServiceName(requestedService)}");
                    sb.AppendLine($"Service Purpose: {GetUDSServiceDescription(requestedService)}\n");

                    sb.AppendLine($"❌ Negative Response Code (NRC): 0x{nrc:X2}");
                    sb.AppendLine($"NRC Meaning: {GetNRCDescription(nrc)}");
                    sb.AppendLine($"Action Required: {GetNRCAction(nrc)}\n");

                    sb.AppendLine("═══════════════════════════════════════════════════════════════");
                    sb.AppendLine("🔍 INTERPRETATION:");
                    sb.AppendLine($"   The ECU rejected the '{GetUDSServiceName(requestedService)}' request.");
                    sb.AppendLine($"   Reason: {GetNRCDescription(nrc)}");
                    sb.AppendLine($"   This typically happens when {GetNRCCause(nrc)}");
                }
            }
            else
            {
                // Positive response or request
                bool isResponse = (serviceId & 0x40) != 0;
                byte baseService = (byte)(serviceId & 0xBF);

                if (isResponse)
                {
                    sb.AppendLine("✅ RESPONSE TYPE: POSITIVE RESPONSE (SUCCESS)\n");
                }
                else
                {
                    sb.AppendLine("📤 RESPONSE TYPE: REQUEST (FROM TESTER)\n");
                }

                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine($"Service ID: 0x{serviceId:X2}");

                if (isResponse)
                {
                    sb.AppendLine($"Base Service: 0x{baseService:X2} (Response = Request + 0x40)");
                }

                sb.AppendLine($"Service Name: {GetUDSServiceName(baseService)}");
                sb.AppendLine($"Service Category: {GetServiceCategory(baseService)}");
                sb.AppendLine($"Service Purpose: {GetUDSServiceDescription(baseService)}\n");

                // Decode sub-function if present
                if (bytes.Count >= 2)
                {
                    byte subFunction = bytes[1];
                    string subFuncInfo = DecodeSubFunction(baseService, subFunction);
                    if (!string.IsNullOrEmpty(subFuncInfo))
                    {
                        sb.AppendLine($"Sub-Function: 0x{subFunction:X2}");
                        sb.AppendLine($"Sub-Function Details: {subFuncInfo}\n");
                    }
                }

                // Show additional data bytes if present
                if (bytes.Count > 2)
                {
                    sb.AppendLine($"📊 Additional Data: {bytes.Count - 2} bytes");
                    sb.Append("   Bytes: ");
                    for (int i = 2; i < Math.Min(bytes.Count, 10); i++)
                    {
                        sb.Append($"0x{bytes[i]:X2} ");
                    }
                    if (bytes.Count > 10)
                    {
                        sb.Append($"... ({bytes.Count - 10} more)");
                    }
                    sb.AppendLine("\n");
                }

                // Provide contextual interpretation
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("🔍 INTERPRETATION:");
                sb.AppendLine(GetContextualInterpretation(baseService, bytes, isResponse));
            }

            // Show raw hex
            sb.AppendLine("\n═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"📋 Raw Hex: {FormatHexString(udsCode)}");
            sb.AppendLine($"📏 Length: {bytes.Count} bytes ({bytes.Count * 8} bits)");

            return sb.ToString();
        }

        private string GetServiceCategory(byte serviceId)
        {
            return serviceId switch
            {
                0x10 or 0x11 or 0x28 or 0x3E or 0x85 => "Session & Communication Control",
                0x14 or 0x19 => "Diagnostic Trouble Codes",
                0x22 or 0x23 or 0x2E => "Data Read/Write",
                0x27 => "Security",
                0x2F or 0x31 => "Input/Output Control",
                0x34 or 0x35 or 0x36 or 0x37 => "Data Transfer (Upload/Download)",
                _ => "Unknown/Proprietary"
            };
        }

        private string DecodeSubFunction(byte serviceId, byte subFunction)
        {
            bool suppressPosResponse = (subFunction & 0x80) != 0;
            byte subFuncBase = (byte)(subFunction & 0x7F);

            string description = serviceId switch
            {
                0x10 => subFuncBase switch  // DiagnosticSessionControl
                {
                    0x01 => "Default Session (normal operation mode)",
                    0x02 => "Programming Session (for ECU reprogramming/flashing)",
                    0x03 => "Extended Diagnostic Session (access to all diagnostic services)",
                    0x04 => "Safety System Diagnostic Session",
                    _ => $"Custom Session Type (0x{subFuncBase:X2})"
                },
                0x11 => subFuncBase switch  // ECUReset
                {
                    0x01 => "Hard Reset (power cycle)",
                    0x02 => "Key Off/On Reset (ignition cycle)",
                    0x03 => "Soft Reset (application restart)",
                    0x04 => "Enable Rapid Power Shutdown",
                    0x05 => "Disable Rapid Power Shutdown",
                    _ => $"Custom Reset Type (0x{subFuncBase:X2})"
                },
                0x27 => subFuncBase switch  // SecurityAccess
                {
                    0x01 or 0x03 or 0x05 or 0x07 => $"Request Seed (Level {(subFuncBase + 1) / 2})",
                    0x02 or 0x04 or 0x06 or 0x08 => $"Send Key (Level {subFuncBase / 2})",
                    _ => $"Security Level 0x{subFuncBase:X2}"
                },
                0x28 => subFuncBase switch  // CommunicationControl
                {
                    0x00 => "Enable Rx and Tx",
                    0x01 => "Enable Rx, Disable Tx",
                    0x02 => "Disable Rx, Enable Tx",
                    0x03 => "Disable Rx and Tx",
                    _ => $"Communication Mode 0x{subFuncBase:X2}"
                },
                0x31 => subFuncBase switch  // RoutineControl
                {
                    0x01 => "Start Routine",
                    0x02 => "Stop Routine",
                    0x03 => "Request Routine Results",
                    _ => $"Routine Control Type 0x{subFuncBase:X2}"
                },
                0x85 => subFuncBase switch  // ControlDTCSetting
                {
                    0x01 => "Turn DTC Recording ON",
                    0x02 => "Turn DTC Recording OFF",
                    _ => $"DTC Setting 0x{subFuncBase:X2}"
                },
                _ => ""
            };

            if (suppressPosResponse && !string.IsNullOrEmpty(description))
            {
                description += " [Suppress Positive Response]";
            }

            return description;
        }

        private string GetContextualInterpretation(byte serviceId, List<byte> bytes, bool isResponse)
        {
            string action = isResponse ? "responded to" : "is requesting";

            return serviceId switch
            {
                0x10 => $"   The tester {action} a diagnostic session change.\n   This controls which services are available.",
                0x11 => $"   The tester {action} an ECU reset.\n   The ECU will restart after this operation.",
                0x14 => $"   The tester {action} clearing diagnostic trouble codes.\n   This removes stored fault information.",
                0x19 => $"   The tester {action} reading diagnostic trouble codes.\n   This retrieves stored fault information.",
                0x22 => $"   The tester {action} reading data from the ECU.\n   This retrieves specific data values (DID: Data Identifier).",
                0x23 => $"   The tester {action} reading memory by address.\n   This retrieves raw memory contents.",
                0x27 => bytes.Count >= 2 && (bytes[1] & 0x7F) % 2 == 1 
                    ? $"   The tester {action} SEED for security access.\n   This is step 1 of unlocking the ECU."
                    : $"   The tester {action} KEY for security access.\n   This is step 2 of unlocking the ECU.",
                0x28 => $"   The tester {action} communication control.\n   This manages ECU communication behavior.",
                0x2E => $"   The tester {action} writing data to the ECU.\n   This modifies specific data values.",
                0x2F => $"   The tester {action} input/output control.\n   This directly controls ECU outputs or inputs.",
                0x31 => $"   The tester {action} routine control.\n   This starts/stops/checks an ECU routine/test.",
                0x34 => $"   The tester {action} DOWNLOAD to ECU (tester → ECU).\n   This prepares to send data/software to the ECU.",
                0x35 => $"   The tester {action} UPLOAD from ECU (ECU → tester).\n   This prepares to receive data from the ECU.",
                0x36 => $"   The tester {action} transferring a data block.\n   This sends/receives a chunk of data.",
                0x37 => $"   The tester {action} ending the transfer session.\n   This finalizes the upload/download operation.",
                0x3E => $"   The tester {action} a keep-alive message.\n   This prevents the diagnostic session from timing out.",
                0x85 => $"   The tester {action} DTC setting control.\n   This enables/disables diagnostic trouble code recording.",
                _ => $"   The tester {action} a {(serviceId >= 0x50 && serviceId < 0x80 ? "proprietary" : "unknown")} service."
            };
        }

        private string GetNRCAction(byte nrc)
        {
            return nrc switch
            {
                0x11 => "Check if the ECU supports this service in the current session.",
                0x12 => "Check if the sub-function is valid for this service.",
                0x13 => "Verify the message format and length are correct.",
                0x21 => "Wait and retry the request.",
                0x22 => "Check preconditions (e.g., engine state, session type).",
                0x24 => "Ensure proper request sequence (e.g., security access before writing).",
                0x31 => "Verify parameters are within valid ranges.",
                0x33 => "Complete security access procedure first.",
                0x35 => "Use the correct security key for this ECU.",
                0x36 => "Wait for the timeout period before retrying.",
                0x37 => "Wait for the required time delay before retrying.",
                0x78 => "Wait for the final response; the ECU is still processing.",
                0x7E or 0x7F => "Switch to a different diagnostic session (e.g., extended or programming).",
                _ => "Consult ECU documentation for specific resolution steps."
            };
        }

        private string GetUDSServiceName(byte serviceId)
        {
            return serviceId switch
            {
                0x10 => "DiagnosticSessionControl",
                0x11 => "ECUReset",
                0x14 => "ClearDiagnosticInformation",
                0x19 => "ReadDTCInformation",
                0x22 => "ReadDataByIdentifier",
                0x23 => "ReadMemoryByAddress",
                0x27 => "SecurityAccess",
                0x28 => "CommunicationControl",
                0x2E => "WriteDataByIdentifier",
                0x2F => "InputOutputControlByIdentifier",
                0x31 => "RoutineControl",
                0x34 => "RequestDownload",
                0x35 => "RequestUpload",
                0x36 => "TransferData",
                0x37 => "RequestTransferExit",
                0x3E => "TesterPresent",
                0x85 => "ControlDTCSetting",
                _ => "Unknown Service"
            };
        }

        private string GetUDSServiceDescription(byte serviceId)
        {
            return serviceId switch
            {
                0x10 => "Enable different diagnostic sessions",
                0x11 => "Request ECU reset",
                0x14 => "Clear diagnostic trouble codes",
                0x19 => "Read diagnostic trouble code information",
                0x22 => "Read data from ECU memory",
                0x23 => "Read memory by address",
                0x27 => "Unlock security access to ECU",
                0x28 => "Control communication parameters",
                0x2E => "Write data to ECU memory",
                0x2F => "Control input/output parameters",
                0x31 => "Start/stop routines",
                0x34 => "Initiate data download to ECU",
                0x35 => "Initiate data upload from ECU",
                0x36 => "Transfer data blocks",
                0x37 => "Terminate transfer session",
                0x3E => "Keep diagnostic session alive",
                0x85 => "Control DTC setting behavior",
                _ => "Unknown or proprietary service"
            };
        }

        private string GetNRCDescription(byte nrc)
        {
            return nrc switch
            {
                0x10 => "General Reject",
                0x11 => "Service Not Supported",
                0x12 => "SubFunction Not Supported",
                0x13 => "Incorrect Message Length or Invalid Format",
                0x14 => "Response Too Long",
                0x21 => "Busy Repeat Request",
                0x22 => "Conditions Not Correct",
                0x24 => "Request Sequence Error",
                0x25 => "No Response From Subnet Component",
                0x26 => "Failure Prevents Execution Of Requested Action",
                0x31 => "Request Out Of Range",
                0x33 => "Security Access Denied",
                0x35 => "Invalid Key",
                0x36 => "Exceed Number Of Attempts",
                0x37 => "Required Time Delay Not Expired",
                0x70 => "Upload Download Not Accepted",
                0x71 => "Transfer Data Suspended",
                0x72 => "General Programming Failure",
                0x73 => "Wrong Block Sequence Counter",
                0x78 => "Request Correctly Received - Response Pending",
                0x7E => "SubFunction Not Supported In Active Session",
                0x7F => "Service Not Supported In Active Session",
                _ => "Unknown NRC"
            };
        }

        private string GetNRCCause(byte nrc)
        {
            return nrc switch
            {
                0x10 => "the ECU rejected the request for an unspecified reason.",
                0x11 => "the requested service is not implemented in this ECU.",
                0x12 => "the requested sub-function is not supported.",
                0x13 => "the message has incorrect length or invalid data format.",
                0x14 => "the response data is too large to transmit.",
                0x21 => "the ECU is currently busy processing another request.",
                0x22 => "preconditions are not met (e.g., wrong gear, engine state, or voltage).",
                0x24 => "the request was sent out of sequence (e.g., writing before unlocking).",
                0x25 => "a required subnet component failed to respond.",
                0x26 => "an internal failure prevents executing the request.",
                0x31 => "parameters are outside valid ranges (e.g., invalid address or value).",
                0x33 => "security access is required but not granted.",
                0x35 => "the provided security key is incorrect.",
                0x36 => "too many failed security attempts; ECU is locked temporarily.",
                0x37 => "a mandatory delay period has not elapsed yet.",
                0x70 => "upload/download is not allowed in the current state.",
                0x71 => "data transfer was suspended due to an issue.",
                0x72 => "a general error occurred during programming.",
                0x73 => "the data block sequence number is wrong.",
                0x78 => "the ECU is processing the request; this is an interim response.",
                0x7E => "the sub-function is not available in the current diagnostic session.",
                0x7F => "the service is not available in the current diagnostic session.",
                _ => "an unknown error occurred."
            };
        }

        private string FormatHexString(string hex)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < hex.Length; i += 2)
            {
                if (i > 0) sb.Append(" ");
                sb.Append(hex.Substring(i, Math.Min(2, hex.Length - i)));
            }
            return sb.ToString().ToUpper();
        }
    }
}
