using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace ipswintakplugin
{
    public partial class IPSDataWindow : Window
    {
        private readonly string _rawJson;
        private string _formattedText;
        private bool _isFormatted = false;

        public IPSDataWindow(string jsonData)
        {
            InitializeComponent();
            // Store the raw JSON string.
            _rawJson = PrettifyJson(jsonData);
            // Show raw JSON initially.
            IPSDataTextBlock.Text = _rawJson;
        }

        /// <summary>
        /// Toggles between raw JSON and a custom formatted view.
        /// </summary>
        private void ToggleFormatButton_Click(object sender, RoutedEventArgs e)
        {
            _isFormatted = !_isFormatted;
            if (_isFormatted)
            {
                // Convert raw JSON to a more friendly format.
                _formattedText = FormatJsonAsText(_rawJson);
                IPSDataTextBlock.Text = _formattedText;
                ToggleFormatButton.Content = "Switch to Raw";
            }
            else
            {
                IPSDataTextBlock.Text = _rawJson;
                ToggleFormatButton.Content = "Switch to Formatted";
            }
        }

        /// <summary>
        /// Prettifies the JSON (adds indentation) using JObject.
        /// </summary>
        private string PrettifyJson(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                return token.ToString(Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                return $"Error prettifying JSON: {ex.Message}";
            }
        }

        /// <summary>
        /// Converts JSON into a more readable text layout (removes extra quotes and braces).
        /// </summary>
        private string FormatJsonAsText(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                return FormatJToken(token, 0);
            }
            catch (Exception ex)
            {
                return $"Error formatting JSON: {ex.Message}";
            }
        }

        /// <summary>
        /// Recursively formats a JToken into a simple text representation.
        /// </summary>
        private string FormatJToken(JToken token, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            StringBuilder sb = new StringBuilder();

            if (token is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    sb.Append(indent);
                    sb.Append(property.Name);
                    sb.Append(": ");
                    if (property.Value is JValue)
                    {
                        // For primitive values, output without extra quotes.
                        sb.AppendLine(property.Value.ToString());
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.Append(FormatJToken(property.Value, indentLevel + 1));
                    }
                }
            }
            else if (token is JArray array)
            {
                foreach (var item in array)
                {
                    sb.Append(indent);
                    sb.Append("- ");
                    if (item is JValue)
                    {
                        sb.AppendLine(item.ToString());
                    }
                    else
                    {
                        sb.AppendLine();
                        sb.Append(FormatJToken(item, indentLevel + 1));
                    }
                }
            }
            else if (token is JValue value)
            {
                sb.AppendLine(value.ToString());
            }

            return sb.ToString();
        }
    }
}
