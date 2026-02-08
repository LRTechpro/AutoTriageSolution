# AutoTriageSolution

AutoTriageSolution is a Windows-based diagnostic utility built in C# (.NET WinForms) for automated log triage and severity analysis.  
It is designed to reflect how professional diagnostic, security, and operational tooling separates analysis logic from presentation to support reuse, reliability, and long-term maintainability.

The application processes raw log data, identifies notable events, classifies severity, and presents structured results suitable for technical review and troubleshooting workflows.

---

## Key Capabilities

- Load or paste raw log data
- Automated detection of ERROR, WARN, and SUCCESS events
- Severity classification and filtering
- Line-numbered, structured results view
- Summary scoring based on detected findings
- Deterministic, rule-based analysis (no UI coupling)
- Clean, code-first WinForms interface

---

## Architecture Overview

The solution is intentionally split into two projects to mirror production-grade tooling patterns.

### **AutoTriage.Core** (Class Library / DLL)
- Contains all log parsing and analysis logic
- Implements severity classification and domain models
- Produces structured analysis results independent of UI concerns
- Designed for reuse by other front ends (CLI, service, API, etc.)

### **AutoTriage.Gui** (WinForms Application)
- Handles user interaction and visualization
- Displays analysis results produced by the Core DLL
- Provides filtering, navigation, and presentation logic only
- Contains no analysis or classification rules

This separation ensures the analysis engine remains reusable, testable, and stable as interfaces evolve.

---

## Screenshots

### Main Interface with no Log Loaded
![Main UI](AutoTriage.Core/Screenshot/AutoTriage_1.png)

### Main Interface with Loaded Logs
![Analysis Results](AutoTriage.Core/Screenshot/AutoTriage_2.png)

### Analysis Results with Severity Breakdown
![Cleared State](AutoTriage.Core/Screenshot/AutoTriage_3.png)

---

## Technologies Used

- C#
- .NET (WinForms)
- Class Library (DLL) architecture
- DataGridView
- Git & GitHub

---

## Distribution

A packaged Windows executable is available via GitHub Releases.  
The release bundle contains all required runtime files and can be executed without building the solution locally.

---

## Project Status

This project is actively maintained as a professional diagnostic tooling example and portfolio artifact.  
The architecture and implementation are intentionally aligned with real-world analysis and security tool design patterns.
