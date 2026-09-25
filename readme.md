# EPA Open Source Reference

## Brief Project Description

This repository contains files for teams to reuse when working in and with EPA Open Source projects.

Also, this repository contains the link to [EPA's System Lifecycle Management Policy and Procedure](https://www.epa.gov/irmpoli8/policy-procedures-and-guidance/system-life-cycle-management) which lays out EPA's Open Source Software Policy and [EPA's Open Source Code Guidance](https://www.epa.gov/developers/open-source-software-and-epa-code-repository-requirements).

## Usage

This project builds a QSAR Toolbox add-in package (`.tbaddin`) for the TEST model set.

### Prerequisites

Before building or using the add-in, ensure you have:

- **QSAR Toolbox installed**
- **.NET SDK** installed (v10.0 is needed for QSAR Toolbox)
- The following local runtime files in the project root:
  - `test_bin\WebTEST.jar` (TEST code)
  - `test_bin\jdk-26.0.2.1\` (Java JDK)

You can download `WebTEST.jar` here:

[WebTEST.jar](https://github.com/USEPA/test-app/releases/download/v5.1.3/WebTEST.jar)

You can download a compatible JDK here:

[JDK 26 download](https://adoptium.net/temurin/releases/?version=26)

### Required folder layout

Your repository should look like this before building:

```text
repo-root\
├─ build.bat
├─ tb-test-addin-master\
├─ tbaddin_creator\
└─ test_bin\
   ├─ WebTEST.jar
   └─ jdk-26.0.2.1\