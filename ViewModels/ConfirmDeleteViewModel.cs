using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// ViewModel for Confirm Delete Environment Variable Window
    /// </summary>
    public class ConfirmDeleteViewModel : ViewModelBase
    {
        private string _variableName;
        private string _variableDescription;
        private string _warningMessage;
        private bool _canConfirm = true;

        /// <summary>
        /// Environment variable name
        /// </summary>
        public string VariableName
        {
            get => _variableName;
            set => SetProperty(ref _variableName, value);
        }

        /// <summary>
        /// Environment variable description
        /// </summary>
        public string VariableDescription
        {
            get => _variableDescription;
            set => SetProperty(ref _variableDescription, value);
        }

        /// <summary>
        /// Warning message for deletion
        /// </summary>
        public string WarningMessage
        {
            get => _warningMessage;
            set => SetProperty(ref _warningMessage, value);
        }

        /// <summary>
        /// Whether deletion can be confirmed (false for critical system variables)
        /// </summary>
        public bool CanConfirm
        {
            get => _canConfirm;
            set => SetProperty(ref _canConfirm, value);
        }

        /// <summary>
        /// Command to confirm deletion
        /// </summary>
        public ICommand ConfirmCommand { get; }

        /// <summary>
        /// Command to cancel deletion
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Constructor - initializes with variable name to delete
        /// </summary>
        /// <param name="variableName">Environment variable name</param>
        public ConfirmDeleteViewModel(string variableName)
        {
            VariableName = variableName;
            ConfirmCommand = new RelayCommand(() => OnConfirm());
            CancelCommand = new RelayCommand(() => OnCancel());
            ValidateVariable();
        }

        private static readonly HashSet<string> CriticalSystemVariables = new(StringComparer.OrdinalIgnoreCase)
        {
            "PATH", "TEMP", "TMP", "SYSTEMROOT", "WINDIR", "APPDATA", 
            "LOCALAPPDATA", "PROGRAMFILES", "PROGRAMFILES(X86)", 
            "PROGRAMDATA", "USERPROFILE", "ALLUSERSPROFILE", "COMSPEC",
            "PATHEXT", "OS", "PROCESSOR_ARCHITECTURE"
        };

        /// <summary>
        /// Validates if the environment variable can be safely deleted
        /// Checks for system critical variables and important development variables
        /// </summary>
        private void ValidateVariable()
        {
            if (CriticalSystemVariables.Contains(VariableName))
            {
                VariableDescription = "System critical environment variable required for Windows system operation.";
                WarningMessage = $"Critical system variable '{VariableName}' cannot be deleted safely!";
                CanConfirm = false;
                return;
            }

            // Check important development environment variables
            // First check CUDA related variables
            if (VariableName.StartsWith("CUDA_PATH", StringComparison.OrdinalIgnoreCase))
            {
                VariableDescription = "Installation path for CUDA Development Toolkit, used for NVIDIA GPU programming and deep learning frameworks.";
                WarningMessage = "Deleting this variable may affect applications that depend on CUDA!";
                return;
            }

            switch (VariableName.ToUpper())
            {
                case "JAVA_HOME":
                    VariableDescription = "Java Development Kit (JDK) installation path, used for Java application development and runtime environment.";
                    WarningMessage = "Deleting this variable may affect Java applications running!";
                    break;
                case "PYTHON_HOME":
                case "PYTHONPATH":
                    VariableDescription = "Python interpreter path and module search path, used for Python application runtime environment.";
                    WarningMessage = "Deleting this variable may affect Python applications running!";
                    break;
                case "MAVEN_HOME":
                case "M2_HOME":
                    VariableDescription = "Apache Maven build tool installation path, used for Java project dependency management and building.";
                    WarningMessage = "Deleting this variable may affect Maven project builds!";
                    break;
                case "NODE_PATH":
                case "NODE_HOME":
                    VariableDescription = "Node.js runtime environment path, used for JavaScript/Node.js application development.";
                    WarningMessage = "Deleting this variable may affect Node.js applications running!";
                    break;
                case "ANDROID_HOME":
                case "ANDROID_SDK_ROOT":
                    VariableDescription = "Android SDK installation path, used for Android application development.";
                    WarningMessage = "Deleting this variable may affect Android development environment!";
                    break;
                case "GOROOT":
                case "GOPATH":
                    VariableDescription = "Go language development environment path, used for Go application development.";
                    WarningMessage = "Deleting this variable may affect Go development environment!";
                    break;
                case "GRADLE_HOME":
                case "GRADLE_USER_HOME":
                    VariableDescription = "Gradle build tool path, used for Java/Android project building.";
                    WarningMessage = "Deleting this variable may affect Gradle project builds!";
                    break;
                default:
                    WarningMessage = "This operation cannot be undone.";
                    break;
            }
        }

        private void OnConfirm()
        {
            DialogResult = true;
            SetStatusMessage($"Confirmed deletion of variable '{VariableName}'");
            CloseWindow?.Invoke();
        }

        private void OnCancel()
        {
            DialogResult = false;
            SetStatusMessage("Deletion cancelled");
            CloseWindow?.Invoke();
        }

        /// <summary>
        /// Action to close the confirmation window
        /// </summary>
        public Action CloseWindow { get; set; }

        /// <summary>
        /// Dialog result (true=confirmed, false=cancelled)
        /// </summary>
        public bool? DialogResult { get; private set; }
    }
}