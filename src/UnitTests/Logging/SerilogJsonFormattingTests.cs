using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Logging;

[TestFixture]
public sealed class SerilogJsonFormattingTests
{
    [Test]
    public void When_LoggingWithRenderedCompactJsonFormatter_OutputsJsonWithStructuredTokens()
    {
        using var writer = new StringWriter();
        var formatter = new RenderedCompactJsonFormatter();
        using var log = new LoggerConfiguration()
            .WriteTo.Sink(new FormatterSink(writer, formatter))
            .CreateLogger();

        log.Information("Processed {OrderId}", 42);

        var line = writer.ToString().Trim();
        line.ShouldStartWith("{");
        line.ShouldContain("\"@t\"");
        line.ShouldContain("\"@m\"");
        line.ShouldContain("OrderId");
    }

    private sealed class FormatterSink(TextWriter writer, ITextFormatter formatter) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => formatter.Format(logEvent, writer);
    }
}
