using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Diagnostics;
using System.Windows;

namespace EnvVarViewer.Models
{
    /// <summary>
    /// Handles interactions with environment variables.
    /// </summary>
    public class EnvironmentVariableModel
    {
        /// <summary>
        /// Loads environment variables from the system.
        /// </summary>
        /// <param name="target">The environment variable target (User or Machine).</param>
        /// <returns>A dictionary containing environment variables for the specified target.</returns>
        public Dictionary<string, string> LoadEnvVars(EnvironmentVariableTarget target)
        {
            var envVars = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables(target))
            {
                envVars.Add(entry.Key.ToString(), entry.Value.ToString());
            }
            return envVars;
        }

        /// <summary>
        /// Sets an environment variable.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        /// <param name="target">The environment variable target (User or Machine).</param>
        public void SetEnvVar(string name, string value, EnvironmentVariableTarget target)
        {
            Environment.SetEnvironmentVariable(name, value, target);
        }

        /// <summary>
        /// Deletes an environment variable.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="target">The environment variable target (User or Machine).</param>
        public void DeleteEnvVar(string name, EnvironmentVariableTarget target)
        {
            Environment.SetEnvironmentVariable(name, null, target);
        }

        /// <summary>
        /// Checks if the application is running with administrator privileges.
        /// </summary>
        /// <returns>True if running as administrator, false otherwise.</returns>
        public bool IsAdministrator()
        {
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>
        /// Elevates the application process to run with administrator privileges.
        /// </summary>
        public void Elevate()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // User cancelled the UAC prompt
            }
            // Application.Current.Shutdown();
        }
    }
}