namespace Serilog.Site24x7.TextFormatter
{
    using Serilog.Events;
    using Serilog.Formatting;
    using Serilog.Formatting.Json;
    using Serilog.Parsing;
    using System;
    using System.Text.Encodings.Web;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class Site24x7TextFormatter : ITextFormatter
    {
        private readonly JsonValueFormatter _valueFormatter;

        public Site24x7TextFormatter(JsonValueFormatter valueFormatter = null)
        {
            _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            var buffer = new StringWriter();

            try
            {
                FormatContent(logEvent, buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            output.WriteLine(buffer.ToString());
        }

        public static void FormatContent(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.Write("{\"_zl_timestamp\":\"");
            output.Write(logEvent.Timestamp.ToUnixTimeMilliseconds());

            output.Write("\",\"Level\":\"");
            output.Write(logEvent.Level);

            output.Write("\",\"MessageTemplate\":");
            string msgTemp = HtmlEncoder.Default.Encode(logEvent.MessageTemplate.Text);
            JsonValueFormatter.WriteQuotedJsonString(msgTemp, output);

            output.Write(",\"RenderedMessage\":");

            var message = logEvent.MessageTemplate.Render(logEvent.Properties);
            var msg = HtmlEncoder.Default.Encode(message.ToString());
            JsonValueFormatter.WriteQuotedJsonString(msg, output);

            if (logEvent.Exception != null)
            {
                output.Write(",\"Exception\":");
                var expMsg = HtmlEncoder.Default.Encode(logEvent.Exception.ToString());
                JsonValueFormatter.WriteQuotedJsonString(expMsg, output);
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

        public static void WriteProperties(
         IReadOnlyDictionary<string, LogEventPropertyValue> properties,
         TextWriter output)
        {
            output.Write(",");
            var precedingDelimiter = "";

            foreach (var property in properties)
            {
                output.Write(precedingDelimiter);
                precedingDelimiter = ",";

                JsonValueFormatter.WriteQuotedJsonString(property.Key, output);
                output.Write(':');  
                JsonValueFormatter.WriteQuotedJsonString(property.Value.ToString(), output);
            }
        }

        public static void WriteRenderings(
           IEnumerable<IGrouping<string, PropertyToken>> tokensWithFormat,
           IReadOnlyDictionary<string, LogEventPropertyValue> properties,
           TextWriter output)
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
                    var formMsg = HtmlEncoder.Default.Encode(format.Format);
                    JsonValueFormatter.WriteQuotedJsonString(formMsg, output);

                    output.Write(",\"Rendering\":");
                    var sw = new StringWriter();
                    format.Render(properties, sw);
                    var renderMsg = HtmlEncoder.Default.Encode(sw.ToString());
                    JsonValueFormatter.WriteQuotedJsonString(renderMsg, output);
                    output.Write('}');
                }

                output.Write(']');
            }

            output.Write('}');
        }
    }
}