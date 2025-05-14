using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace EnvVarViewer
{
    public partial class AddModifyEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private Dictionary<string, string> userEnvVars;
        private Dictionary<string, string> systemEnvVars;
        private Dictionary<string, string> modifiedEnvVars;
        private HashSet<string> deletedEnvVars;
        private string originalName;

        public event EventHandler EnvVarAdded;
        public event EventHandler EnvVarModified;

        /// <summary>
        /// Initializes a new instance of the AddModifyEnvVarWindow class
        /// </summary>
        /// <param name="userEnvVars">User environment variables</param>
        /// <param name="systemEnvVars">System environment variables</param>
        /// <param name="modifiedEnvVars">Modified environment variables</param>
        /// <param name="deletedEnvVars">Deleted environment variables</param>
        public AddModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, Dictionary<string, string> modifiedEnvVars, HashSet<string> deletedEnvVars)
        {
            InitializeComponent();
            this.userEnvVars = userEnvVars;
            this.systemEnvVars = systemEnvVars;
            this.modifiedEnvVars = modifiedEnvVars;
            this.deletedEnvVars = deletedEnvVars;
            ScopeComboBox.SelectedIndex = 0; // Default to User
        }

        /// <summary>
        /// Initializes a new instance for modifying an existing environment variable
        /// </summary>
        /// <param name="name">Name of the environment variable to modify</param>
        /// <param name="value">New value for the environment variable</param>
        public AddModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, Dictionary<string, string> modifiedEnvVars, HashSet<string> deletedEnvVars, string name, string value)
            : this(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars)
        {
            this.originalName = name;
            NameTextBox.Text = name;
            ValueTextBox.Text = value;
            if (userEnvVars.ContainsKey(name))
            {
                ScopeComboBox.SelectedIndex = 0; // User
            }
            else if (systemEnvVars.ContainsKey(name))
            {
                ScopeComboBox.SelectedIndex = 1; // System
            }
        }

        /// <summary>
        /// Handles the save button click event to add or modify an environment variable
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text;
            string value = ValueTextBox.Text;
            string scope = ((ComboBoxItem)ScopeComboBox.SelectedItem).Content.ToString();
            if (scope == "System") scope = "Machine";

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            {
                MessageBox.Show("Name and Value cannot be empty.");
                return;
            }
            
            // Check if the variable name already exists
            if (originalName == null && (userEnvVars.ContainsKey(name) || systemEnvVars.ContainsKey(name) || modifiedEnvVars.ContainsKey(name)))
            {
                MessageBox.Show($"Environment variable '{name}' already exists.");
                return;
            }

            EnvironmentVariableTarget target = new EnvironmentVariableTarget();
            target = scope switch
            {
                "Process" => EnvironmentVariableTarget.Process,
                "User" => EnvironmentVariableTarget.User,
                "System" => EnvironmentVariableTarget.Machine,
                _ => target
            };

            try
            {
                if (originalName != null && (userEnvVars.ContainsKey(originalName) || systemEnvVars.ContainsKey(originalName) || modifiedEnvVars.ContainsKey(originalName)))
                {
                    Environment.SetEnvironmentVariable(originalName, null, target);
                    Environment.SetEnvironmentVariable(name, value, target);
                    modifiedEnvVars[name] = value;
                    if (originalName != name && modifiedEnvVars.ContainsKey(originalName))
                    {
                        modifiedEnvVars.Remove(originalName);
                    }
                    if (deletedEnvVars.Contains(originalName))
                    {
                        deletedEnvVars.Remove(originalName);
                    }
                    EnvVarModified?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Environment.SetEnvironmentVariable(name, value, target);
                    modifiedEnvVars[name] = value;
                    if (deletedEnvVars.Contains(name))
                    {
                        deletedEnvVars.Remove(name);
                    }
                    EnvVarAdded?.Invoke(this, EventArgs.Empty);
                }

                this.Close();
            }
            catch (System.Security.SecurityException ex)
            {
                MessageBox.Show("Permission denied. You do not have sufficient privileges to modify environment variables at this scope.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
    }
}