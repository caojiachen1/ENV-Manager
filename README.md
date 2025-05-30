# ENV-Manager
Modernized GUI environment variable manager

## Project Description
ENV-Manager is a modern Windows environment variables management tool with graphical interface, allowing users to view, add, modify and delete system/user environment variables easily.

## Features
- Visual management of system and user environment variables
- Support CRUD operations for environment variables
- Dedicated interface for editing PATH variables
- Backup and restore functionality for environment variables
- Clean and intuitive user interface

## Installation
1. Navigate to the [Releases](https://github.com/caojiachen1/ENV-Manager/releases) page of the GitHub repository.
2. Download the latest `portable.zip` file from the assets section of the release.
3. Extract the contents of the `portable.zip` file.
4. Navigate to the extracted folder and double - click the main executable file to launch the application.

## Usage
1. Main interface displays all current environment variables
2. Use toolbar buttons to add, modify or delete variables
3. Backup function saves current environment configuration
4. Restore function recovers configuration from backup file

## Building from Source
1. Prerequisites:
   - .NET 8.0 SDK or later
   - Visual Studio 2022 or later

2. Build steps:
   ```bash
   git clone https://github.com/caojiachen1/ENV-Manager.git
   cd ENV-Manager
   dotnet restore
   dotnet build
   ```

3. Running the application:
   ```bash
   dotnet run --project ENV-Manager.csproj
   ```

4. Creating a release package:
   ```bash
   dotnet publish -c Release -o ./publish
   ```
