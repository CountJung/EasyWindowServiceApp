using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EasyWindowServiceApp
{
    public class ViewModelAddOn : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CommandAddOn : ICommand
    {
        private readonly Action<object>? actionSet;
        private readonly Predicate<object>? predicateSet;

        public CommandAddOn(Action<object>? action) : this(action, null) { }
        public CommandAddOn(Action<object>? action, Predicate<object>? predicate)
        {
            actionSet = action;
            predicateSet = predicate;
        }
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
        // use null forgiving operator ! to avoid CS8602
        public bool CanExecute(object? parameter)
        { return predicateSet == null || predicateSet(parameter!); }

        public void Execute(object? parameter) => actionSet!(parameter!);
    }
}
