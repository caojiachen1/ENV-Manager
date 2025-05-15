using System;
using System.Windows.Input;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// 实现 ICommand 接口的命令类，用于在 MVVM 模式中处理命令绑定
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 创建一个新的命令实例
        /// </summary>
        /// <param name="execute">执行命令时要调用的方法</param>
        /// <param name="canExecute">决定命令是否可以执行的方法</param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 确定此命令是否可以在其当前状态下执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <summary>
        /// 执行命令的方法
        /// </summary>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// 手动触发命令状态更新
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <summary>
    /// 带参数的 RelayCommand 实现
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 创建一个新的命令实例
        /// </summary>
        /// <param name="execute">执行命令时要调用的方法</param>
        /// <param name="canExecute">决定命令是否可以执行的方法</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 确定此命令是否可以在其当前状态下执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        /// <summary>
        /// 执行命令的方法
        /// </summary>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        /// <summary>
        /// 手动触发命令状态更新
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}