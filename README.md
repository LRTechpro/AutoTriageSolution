# AutoTriageSolution

AutoTriageSolution is a C# WinForms application that performs automated log triage and severity analysis.
It is designed to simulate a real-world automotive and cybersecurity diagnostic triage tool.

## Features
- Paste or load log files
- Automatic detection of ERROR, WARN, and SUCCESS events
- Severity filtering via checkboxes
- Line-numbered results table
- Summary scoring based on findings
- Clean, code-first WinForms UI
- Modular architecture with Core DLL and GUI separation

## Architecture
- **AutoTriage.Core**  
  Contains analysis logic, severity classification, and domain models.

- **AutoTriage.Gui**  
  WinForms front-end responsible for UI rendering and user interaction.

## Screenshots

### Main UI with Loaded Logs
![Main UI](AutoTriage.Core/Screenshot/AutoTriage_1.png)

### Analysis Results with Severity Breakdown
![Analysis Results](AutoTriage.Core/Screenshot/AutoTriage_2.png)

### Cleared State / Fresh Session
![Cleared State](AutoTriage.Core/Screenshot/AutoTriage_3.png)

## Technologies Used
- C#
- .NET (WinForms)
- DataGridView
- Modular DLL architecture
- Git & GitHub

## Status
This project is actively developed as part of coursework and portfolio demonstration.
