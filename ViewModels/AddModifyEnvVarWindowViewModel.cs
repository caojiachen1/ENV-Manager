using System;
using System.Collections.Generic;
using System.Windows;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// 添加或修改环境变量窗口的ViewModel
    /// </summary>
    public class AddModifyEnvVarWindowViewModel : ViewModelBase
    {
        private Dictionary<string, string> _userEnvVars;
        private Dictionary<string, string> _systemEnvVars;
        private Dictionary<string, string> _modifiedEnvVars;
        private HashSet<string> _deletedEnvVars;
        private string _originalName;
        private string _name;
        private string _value;
        private int _selectedScopeIndex;

        public event EventHandler EnvVarAdded;
        public event EventHandler EnvVarModified;

        /// <summary>
        /// 环境变量名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 环境变量值
        /// </summary>
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        /// <summary>
        /// 选中的作用域索引
        /// </summary>
        public int SelectedScopeIndex
        {
            get => _selectedScopeIndex;
            set => SetProperty(ref _selectedScopeIndex, value);
        }

        /// <summary>
        /// 保存命令
        /// </summary>
        public RelayCommand SaveCommand { get; }

        /// <summary>
        /// 初始化新实例
        /// </summary>
        public AddModifyEnvVarWindowViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars,
            Dictionary<string, string> modifiedEnvVars,
            HashSet<string> deletedEnvVars)
        {
            _userEnvVars = userEnvVars;
            _systemEnvVars = systemEnvVars;
            _modifiedEnvVars = modifiedEnvVars;
            _deletedEnvVars = deletedEnvVars;
            SelectedScopeIndex = 0; // 默认为User

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        }

        /// <summary>
        /// 初始化用于修改现有环境变量的新实例
        /// </summary>
        public AddModifyEnvVarWindowViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars,
            Dictionary<string, string> modifiedEnvVars,
            HashSet<string> deletedEnvVars,
            string name,
            string value) : this(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars)
        {
            _originalName = name;
            Name = name;
            Value = value;

            if (userEnvVars.ContainsKey(name))
            {
                SelectedScopeIndex = 0; // User
            }
            else if (systemEnvVars.ContainsKey(name))
            {
                SelectedScopeIndex = 1; // System
            }
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Value);
        }

        private void ExecuteSave()
        {
            string scope = SelectedScopeIndex switch
            {
                0 => "User",
                1 => "Machine",
                _ => "Process"
            };

            if (_originalName == null && (_userEnvVars.ContainsKey(Name) || _systemEnvVars.ContainsKey(Name) || _modifiedEnvVars.ContainsKey(Name)))
            {
                MessageBox.Show($"Environment variable '{Name}' already exists.");
                return;
            }

            EnvironmentVariableTarget target = scope switch
            {
                "Process" => EnvironmentVariableTarget.Process,
                "User" => EnvironmentVariableTarget.User,
                "Machine" => EnvironmentVariableTarget.Machine,
                _ => EnvironmentVariableTarget.Process
            };

            try
            {
                if (_originalName != null && (_userEnvVars.ContainsKey(_originalName) || _systemEnvVars.ContainsKey(_originalName) || _modifiedEnvVars.ContainsKey(_originalName)))
                {
                    Environment.SetEnvironmentVariable(_originalName, null, target);
                    Environment.SetEnvironmentVariable(Name, Value, target);
                    _modifiedEnvVars[Name] = Value;
                    if (_originalName != Name && _modifiedEnvVars.ContainsKey(_originalName))
                    {
                        _modifiedEnvVars.Remove(_originalName);
                    }
                    if (_deletedEnvVars.Contains(_originalName))
                    {
                        _deletedEnvVars.Remove(_originalName);
                    }
                    EnvVarModified?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Environment.SetEnvironmentVariable(Name, Value, target);
                    _modifiedEnvVars[Name] = Value;
                    if (_deletedEnvVars.Contains(Name))
                    {
                        _deletedEnvVars.Remove(Name);
                    }
                    EnvVarAdded?.Invoke(this, EventArgs.Empty);
                }

                CloseWindow?.Invoke(this, EventArgs.Empty);
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("Permission denied. You do not have sufficient privileges to modify environment variables at this scope.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭窗口事件
        /// </summary>
        public event EventHandler CloseWindow;
    }
}