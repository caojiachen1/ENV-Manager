using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Diagnostics;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;

namespace EnvVarViewer.Models
{
    /// <summary>
    /// Handles interactions with environment variables.
    /// </summary>
    public class EnvironmentVariableModel
    {
        /// <summary>
        /// Loads environment variables from the system asynchronously.
        /// </summary>
        /// <param name="target">The environment variable target (User or Machine).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A dictionary containing environment variables for the specified target.</returns>
        public async Task<Dictionary<string, string>> LoadEnvVarsAsync(EnvironmentVariableTarget target, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var envVars = new Dictionary<string, string>();
                foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables(target))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    envVars.Add(entry.Key.ToString(), entry.Value.ToString());
                }
                return envVars;
            }, cancellationToken);
        }

        /// <summary>
        /// Sets an environment variable asynchronously.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="value">The value of the environment variable.</param>
        /// <param name="target">The environment variable target (User or Machine).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public async Task SetEnvVarAsync(string name, string value, EnvironmentVariableTarget target, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Environment.SetEnvironmentVariable(name, value, target);
            }, cancellationToken);
        }

        /// <summary>
        /// Deletes an environment variable asynchronously.
        /// </summary>
        /// <param name="name">The name of the environment variable.</param>
        /// <param name="target">The environment variable target (User or Machine).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public async Task DeleteEnvVarAsync(string name, EnvironmentVariableTarget target, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                Environment.SetEnvironmentVariable(name, null, target);
            }, cancellationToken);
        }

        /// <summary>
        /// Checks if the application is running with administrator privileges asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if running as administrator, false otherwise.</returns>
        public async Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
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
            }, cancellationToken);
        }

        /// <summary>
        /// Elevates the application process to run with administrator privileges asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if elevation was successful, false if cancelled by user.</returns>
        public async Task<bool> ElevateAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
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
                    return true;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // User cancelled the UAC prompt
                    return false;
                }
            }, cancellationToken);
        }

        // Keep synchronous versions for backward compatibility
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