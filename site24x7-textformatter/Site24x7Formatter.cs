namespace Serilog.Site24x7.TextFormatter
{
    using Serilog.Debugging;
    using Serilog.Events;
    using Serilog.Formatting.Json;
    using Serilog.Parsing;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public interface ISite24x7TextFormatter
    {
        void FormatContent(LogEvent logEvent, TextWriter output);
    }

    public class Site24x7TextFormatter : ISite24x7TextFormatter
    {
        public void FormatContent(LogEvent logEvent, TextWriter output)
        {
            try
            {
                if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
                if (output == null) throw new ArgumentNullException(nameof(output));

                output.Write("{\"_zl_timestamp\":\"");
                output.Write(logEvent.Timestamp.ToUnixTimeMilliseconds());

                output.Write("\",\"Level\":\"");
                output.Write(logEvent.Level);

                output.Write("\",\"MessageTemplate\":");
                JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);

                output.Write(",\"RenderedMessage\":");

                var message = logEvent.MessageTemplate.Render(logEvent.Properties);
                JsonValueFormatter.WriteQuotedJsonString(message, output);

                if (logEvent.Exception != null)
                {
                    output.Write(",\"Exception\":");
                    JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
                }

                if (logEvent.Properties.Count != 0)
                {
                    WriteProperties(logEvent.Properties, output);
                }

                var tokensWithFormat = logEvent.MessageTemplate.Tokens
                    .OfType<PropertyToken>()
                    .Where(pt => pt.Format != null);

                if (tokensWithFormat.Any())
                {
                    WriteRenderings(tokensWithFormat.GroupBy(pt => pt.PropertyName), logEvent.Properties, output);
                }

                output.Write('}');
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(ex.Message);
            }
        }

        public static void WriteProperties(
         IReadOnlyDictionary<string, LogEventPropertyValue> properties,
         TextWriter output)
        {
            try
            {
                output.Write(",");
                var precedingDelimiter = "";

                foreach (var property in properties)
                {
                    output.Write(precedingDelimiter);
                    precedingDelimiter = ",";

                    JsonValueFormatter.WriteQuotedJsonString(property.Key, output);
                    output.Write(':');
                    new JsonValueFormatter().Format(property.Value, output);
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(ex.Message);
            }
        }

        public static void WriteRenderings(
           IEnumerable<IGrouping<string, PropertyToken>> tokensWithFormat,
           IReadOnlyDictionary<string, LogEventPropertyValue> properties,
           TextWriter output)
        {
            try
            {
                output.Write(",\"Renderings\":{");

                var rdelim = "";
                foreach (var ptoken in tokensWithFormat)
                {
                    output.Write(rdelim);
                    rdelim = ",";

                    JsonValueFormatter.WriteQuotedJsonString(ptoken.Key, output);
                    output.Write(":[");

                    var fdelim = "";
                    foreach (var format in ptoken)
                    {
                        output.Write(fdelim);
                        fdelim = ",";

                        output.Write("{\"Format\":");
                        JsonValueFormatter.WriteQuotedJsonString(format.Format, output);

                        output.Write(",\"Rendering\":");
                        var sw = new StringWriter();
                        format.Render(properties, sw);
                        JsonValueFormatter.WriteQuotedJsonString(sw.ToString(), output);
                        output.Write('}');
                    }

                    output.Write(']');
                }

                output.Write('}');
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine(ex.Message);
            }
        }
    }
}