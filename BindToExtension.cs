using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

/*
 * Extension author:Luis Perez
 * 
 * ┌ Instructions for use ──────────────────────────────────────────────────────────┐
 * │Binding:                                                                        │
 * │{Binding}                                                                       │
 * │    => {BindTo}                                                                 │
 * │{Binding Path=PathToProperty, RelativeSource={RelativeSource Self}}             │
 * │    => {BindTo PathToProperty}                                                  │
 * │{Binding Path=PathToProperty,                                                   │
 * │    RelativeSource={RelativeSource AncestorType={x:Type typeOfAncestor}}}       │
 * │    => {BindTo Ancestor.typeOfAncestor.PathToProperty}                          │
 * │{Binding Path=PathToProperty, RelativeSource={RelativeSource TemplatedParent}}  │
 * │    => {BindTo Template.PathToProperty}                                         │
 * │{Binding Path=Text, ElementName=MyTextBox}                                      │
 * │    => {BindTo #MyTextBox.Text}                                                 │
 * │{Binding Path=MyBoolVar, RelativeSource={RelativeSource Self},                  │
 * │    Converter={StaticResource InverseBooleanConverter}}                         │
 * │    => {Binding !MyBoolVar}                                                     │
 * │                                                                                │
 * │Method Binding:                                                                 │
 * │in your class                                                                   │
 * │private void SaveObject() {                                                     │
 * │do something                                                                    │
 * │}                                                                               │
 * │in your xaml                                                                    │
 * │{BindTo SaveObject()}                                                           │
 * │                                                                                │
 * └────────────────────────────────────────────────────────────────────────────────┘
 */
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "BindTo")]
namespace BindTo
{
    public class BindToExtension : MarkupExtension
    {
        #region properties

        private Binding _binding;
        private string _path;
        private string _methodName;

        #endregion

        #region constructors

        public BindToExtension() { }

        public BindToExtension(string path)
        {
            _path = path;
        }

        #endregion

        #region methods

        public void ProcessPath(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(_path))
            {
                _binding = new Binding();
                return;
            }

            var parts = _path.Split('.').Select(o => o.Trim()).ToArray();

            RelativeSource relativeSource = null;
            string elementName = null;

            var partIndex = 0;

            // 格式：{BindTo #MyTextBox.Text}
            if (parts[0].StartsWith("#"))
            {
                // 获取对象名
                elementName = parts[0].Substring(1);
                partIndex++;
            }
            // 格式：{BindTo Ancestor.typeOfAncestor.PathToProperty}
            else if ("ancestors" == parts[0].ToLower() || "ancestor" == parts[0].ToLower())
            {
                if (2 > parts.Length)
                    throw new Exception("Invalid path, expected exactly 2 identifiers ancestors.#Type#.[Path] (e.g. Ancestors.DataGrid, Ancestors.DataGrid.SelectedItem, Ancestors.DataGrid.SelectedItem.Text)");

                var typeName = parts[1];
                var type = (Type) new TypeExtension(typeName).ProvideValue(serviceProvider);
                if (null == type)
                    throw new Exception($"Could not find type: {typeName}");

                relativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, type, 1);
                partIndex += 2;
            }
            // 格式：{BindTo Template.PathToProperty}
            else if ("template" == parts[0].ToLower() || "templateparent" == parts[0].ToLower() ||
                "templated" == parts[0].ToLower() || "templatedparent" == parts[0].ToLower())
            {
                relativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent);
                partIndex++;
            }
            // 格式：{BindTo ThisWindow}
            else if ("thiswindow" == parts[0].ToLower())
            {
                relativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Window), 1);
                partIndex++;
            }
            // 格式：{BindTo This}
            else if ("this" == parts[0].ToLower())
            {
                relativeSource = new RelativeSource(RelativeSourceMode.Self);
                partIndex++;
            }

            var parts4Path = parts.Skip(partIndex);
            IValueConverter valueConverter = null;

            if (parts4Path.Any())
            {
                var sLastPart4Path = parts4Path.Last();

                if (sLastPart4Path.EndsWith("()"))
                {
                    parts4Path = parts4Path.Take(parts4Path.Count() - 1);
                    _methodName = sLastPart4Path.Remove(sLastPart4Path.Length - 2);
                    valueConverter = new CallMethodValueConverter(_methodName);
                }
            }

            var path = string.Join(".", parts4Path.ToArray());

            if (string.IsNullOrWhiteSpace(path))
                _binding = new Binding();
            else
                _binding = new Binding(path);

            if (null != elementName)
                _binding.ElementName = elementName;

            if (null != relativeSource)
                _binding.RelativeSource = relativeSource;

            if (null != valueConverter)
                _binding.Converter = valueConverter;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // 防止设计时编辑器在ProcessPath中显示与TypeExtension的用户有关的错误
            if (!(serviceProvider is IXamlTypeResolver))
                return null;

            ProcessPath(serviceProvider);
            return _binding.ProvideValue(serviceProvider);
        }

        #endregion

        #region inline classes

        private class CallMethodValueConverter : IValueConverter
        {
            private readonly string _methodName;

            public CallMethodValueConverter(string methodName)
            {
                _methodName = methodName;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (null == value)
                    return null;
                return new CallMethodCommand(value, _methodName);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class CallMethodCommand : ICommand
        {
            private readonly object _obj;

            private readonly MethodInfo _methodInfo;
            private readonly bool _methodAcceptsParameter;

            private readonly MethodInfo _canMethodInfo;
            private readonly bool _canMethodAcceptsParameter;

            public CallMethodCommand(object obj, string methodName)
            {
                _obj = obj;

                _methodInfo = _obj.GetType().GetMethod(methodName);

                if (null == _methodInfo) return;

                var parameters = _methodInfo.GetParameters();
                if (2 < parameters.Length)
                    throw new Exception("You can only bind to a methods take take 0 or 1 parameters.");

                _canMethodInfo = _obj.GetType().GetMethod("Can" + methodName);
                if (null != _canMethodInfo)
                {
                    // 判断Can方法的返回值是否为布尔型
                    if (typeof(bool) != _canMethodInfo.ReturnType)
                        throw new Exception("'Can' method must return boolean.");

                    var canParameters = _methodInfo.GetParameters();
                    if (2 < canParameters.Length)
                        throw new Exception("You can only bind to a methods take take 0 or 1 parameters.");
                    _canMethodAcceptsParameter = parameters.Any();
                }
                _methodAcceptsParameter = parameters.Any();
            }

            public bool CanExecute(object parameter)
            {
                if (null == _canMethodInfo)
                    return true;

                var parameters = !_methodAcceptsParameter ? null : new[] { parameter };
                return (bool) _canMethodInfo.Invoke(_obj, parameters);
            }

#pragma warning disable 67  // CanExecuteChanged is not being used but is required by ICommand
            public event EventHandler CanExecuteChanged;
#pragma warning restore 67  // CanExecuteChanged is not being used but is required by ICommand

            public void Execute(object parameter)
            {
                var parameters = !_methodAcceptsParameter ? null : new[] { parameter };
                _methodInfo.Invoke(_obj, parameters);
            }
        }

        #endregion
    }
}
