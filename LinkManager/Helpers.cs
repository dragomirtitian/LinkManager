using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LinkManager
{

    public class Comamnd : ICommand
    {
        public static ICommand Single(ref ICommand field, Action<object> execute, Func<object, bool> canExecute = null)
        {
            return field ?? (field = new Comamnd
            {
                Execute = execute,
                CanExecute = canExecute
            });
        }
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public Func<object, bool> CanExecute { get; set; }
        bool ICommand.CanExecute(object parameter)
        {
            return this.CanExecute?.Invoke(parameter) ?? true;
        }

        public Action<object> Execute { get; set; }
        void ICommand.Execute(object parameter)
        {
            this.Execute(parameter);
        }
    }

    public class Base : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                this.RaisePropertyChanged(name);
                return true;
            }
            return false;
        }
        public void RaisePropertyChanged(string name)
        {

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public static class Commands
    {


        public static object GetDoubleClickParameter(DependencyObject obj)
        {
            return (object)obj.GetValue(DoubleClickParameterProperty);
        }

        public static void SetDoubleClickParameter(DependencyObject obj, object value)
        {
            obj.SetValue(DoubleClickParameterProperty, value);
        }

        // Using a DependencyProperty as the backing store for DoubleClickParameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoubleClickParameterProperty =
            DependencyProperty.RegisterAttached("DoubleClickParameter", typeof(object), typeof(Commands), new PropertyMetadata(null));



        public static ICommand GetDoubleClick(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(DoubleClickProperty);
        }

        public static void SetDoubleClick(DependencyObject obj, ICommand value)
        {
            obj.SetValue(DoubleClickProperty, value);
        }

        // Using a DependencyProperty as the backing store for DoubleClick.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoubleClickProperty =
            DependencyProperty.RegisterAttached("DoubleClick", typeof(ICommand), typeof(Commands), new PropertyMetadata(null, (d, e) =>
            {
                var target = d as Control;
                target.MouseDoubleClick += Target_MouseDoubleClick;
            }));

        private static void Target_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var c = sender as Control;
            var cmd = GetDoubleClick(c);
            var param = GetDoubleClickParameter(c);
            if (cmd.CanExecute(param)) cmd.Execute(param);
        }
    }

    public static class EnumerableExt
    {
        public static void ForEach<T>(this IEnumerable<T> @this, Action<T> action)
        {
            foreach (var item in @this)
            {
                action(item);
            }
        }
    }
}
