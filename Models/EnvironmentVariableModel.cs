using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Diagnostics;
using System.Windows;
using System.Threading.Tasks;
using System.Threading;
using System.Security;

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
        public async Task<Dictionary<string, string>> LoadEnvVarsAsync(EnvironmentVariableTarget target, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var envVars = new Dictionary<string, string>();
                
                try
                {
                    foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables(target))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var key = entry.Key?.ToString();
                        var value = entry.Value?.ToString();
                        
                        if (!string.IsNullOrEmpty(key))
                        {
                            envVars[key] = value ?? string.Empty;
                        }
                    }
                }
                catch (SecurityException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied when reading {target} environment variables. Administrator privileges may be required.", ex);
                }
                
                return envVars;
            }, cancellationToken);
        }

        /// <summary>
        /// Sets an environment variable asynchronously.
        /// </summary>
        public async Task SetEnvVarAsync(string name, string value, EnvironmentVariableTarget target, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Environment variable name cannot be null or empty", nameof(name));
            
            if (name.Contains('=') || name.Contains('\0'))
                throw new ArgumentException("Environment variable name contains invalid characters", nameof(name));

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    Environment.SetEnvironmentVariable(name, value, target);
                }
                catch (SecurityException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied when setting {target} environment variable '{name}'. Administrator privileges may be required.", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Deletes an environment variable asynchronously.
        /// </summary>
        public async Task DeleteEnvVarAsync(string name, EnvironmentVariableTarget target, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Environment variable name cannot be null or empty", nameof(name));

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                try
                {
                    Environment.SetEnvironmentVariable(name, null, target);
                }
                catch (SecurityException ex)
                {
                    throw new UnauthorizedAccessException($"Access denied when deleting {target} environment variable '{name}'. Administrator privileges may be required.", ex);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Checks if the application is running with administrator privileges asynchronously.
        /// </summary>
        public async Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return IsAdministrator();
            }, cancellationToken);
        }

        /// <summary>
        /// Elevates the application process to run with administrator privileges asynchronously.
        /// </summary>
        public async Task<bool> ElevateAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var currentProcess = Process.GetCurrentProcess();
                var executablePath = currentProcess.MainModule?.FileName ?? 
                                   System.Reflection.Assembly.GetExecutingAssembly().Location;

                ProcessStartInfo startInfo = new()
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = executablePath,
                    Verb = "runas"
                };

                try
                {
                    using var process = Process.Start(startInfo);
                    return process != null;
                }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
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
        }
    }
}