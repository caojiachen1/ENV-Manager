using System.Windows;

namespace EnvVarViewer
{
    public partial class ConfirmDeleteWindow : Wpf.Ui.Controls.FluentWindow
    {
        public string VariableName { get; set; }
        public string VariableDescription { get; set; }
        public string WarningMessage { get; set; }

        public ConfirmDeleteWindow(string variableName)
        {
            InitializeComponent();
            DataContext = this;
            VariableName = variableName;
            
            // Validate if the variable is a Windows system critical environment variable
            string upperVarName = variableName.ToUpper();
            if (upperVarName == "PATH" || 
                upperVarName == "TEMP" || 
                upperVarName == "TMP" || 
                upperVarName == "SYSTEMROOT" || 
                upperVarName == "WINDIR" || 
                upperVarName == "APPDATA" || 
                upperVarName == "LOCALAPPDATA" || 
                upperVarName == "PROGRAMFILES" || 
                upperVarName == "PROGRAMFILES(X86)" || 
                upperVarName == "PROGRAMDATA" || 
                upperVarName == "USERPROFILE" || 
                upperVarName == "ALLUSERSPROFILE")
            {
                VariableDescription = "System critical environment variable required for Windows system operation.";
                WarningMessage = $"System critical variable {variableName} cannot be deleted! Deletion may cause system instability or crash.";
                ConfirmButton.IsEnabled = false;
                return;
            }

            // Check for important development environment variables
            // First check for CUDA-related variables (NVIDIA GPU development)
            if (variableName.ToUpper().StartsWith("CUDA_PATH"))
            {
                VariableDescription = "Installation path for CUDA Development Toolkit, used for NVIDIA GPU programming and deep learning frameworks.";
                WarningMessage = "Deleting this variable may affect applications that depend on CUDA!";
            }
            else switch (upperVarName)
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

        /// <summary>
        /// Handles the confirm button click event
        /// Sets dialog result to true and closes the window
        /// </summary>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles the cancel button click event
        /// Sets dialog result to false and closes the window
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}