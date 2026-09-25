# EPA Open Source Reference

## Brief Project Description

This repository contains files for teams to reuse when working in and with EPA Open Source projects.

Also, this repository contains the link to [EPA's System Lifecycle Management Policy and Procedure](https://www.epa.gov/irmpoli8/policy-procedures-and-guidance-system-life-cycle-management) which lays out EPA's Open Source Software Policy and [EPA's Open Source Code Guidance](https://www.epa.gov/developers/open-source-software-and-epa-code-repository-requirements).

## Usage

This project builds a QSAR Toolbox add-in package (`.tbaddin`) for the TEST model set.

### Prerequisites

Before building or using the add-in, ensure you have:

- **QSAR Toolbox installed**
- **.NET v10.0 SDK** installed
- The following local runtime files in the project root:
  - `test_bin\WebTEST.jar`
  - `test_bin\jdk-26.0.2.1\`

### Build the add-in package

From the repository root, run:

```bat
build.bat